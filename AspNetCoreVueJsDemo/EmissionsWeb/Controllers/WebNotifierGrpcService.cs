﻿using Emissions.Proto.Notifications;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Emissions.Controllers {

    public class NotificationQueue {
        public class ActiveClient {
            public async Task<ListenResponse> NextNotification(CancellationToken token) {
                await has_data_.Task.WaitAsync(token);
                ListenResponse result = null;
                lock (this) {
                    result = notification_;
                    notification_ = null;
                    has_data_ = new();
                }
                return result;
            }
            public void AddNotification(ListenResponse notification) {
                lock (this) {
                    if (notification_ == null) {
                        notification_ = notification;
                    } else {
                        notification_.EntriesChanged |= notification.EntriesChanged;
                        notification_.ReportsChanged |= notification.ReportsChanged;
                    }
                    has_data_.TrySetResult();
                }
            }

            TaskCompletionSource has_data_ = new();
            ListenResponse notification_;
        }

        public ActiveClient AddEndpoint(string id) {
            var client = new ActiveClient();
            lock (active_clients_) {
                if (!active_clients_.TryAdd(id, client))
                    throw new RpcException(new Status(StatusCode.AlreadyExists, "duplicate call"));
            }
            return client;
        }

        public void AddNotification(string endpoint_id, ListenResponse notification) {
            ActiveClient client;
            lock (active_clients_) {
                client = active_clients_.GetValueOrDefault(endpoint_id);
            }
            if (client != null)
                client.AddNotification(notification);
        }

        internal void RemoveEndpoint(string user_id) {
            lock (active_clients_) {
                active_clients_.Remove(user_id);
            }
        }

        readonly Dictionary<string, ActiveClient> active_clients_ = new();
    }

    [Authorize]
    public class WebNotifierGrpcService: Proto.Services.WebNotifier.WebNotifierBase {
        public WebNotifierGrpcService(NotificationQueue notifications, ILogger<WebNotifierGrpcService> logger) {
            notifications_ = notifications;
            log_ = logger;
        }

        public async override Task Listen(ListenRequest request, IServerStreamWriter<ListenResponse> responseStream, ServerCallContext context) {
            await responseStream.WriteAsync(new ListenResponse() { EntriesChanged = true, ReportsChanged = true });
            var user_id = currentUserId(context.GetHttpContext().User);
            try {
                var client = notifications_.AddEndpoint(user_id);
                if (client == null)
                    throw new RpcException(new Status(StatusCode.AlreadyExists, "duplicate call"));
                while (!context.CancellationToken.IsCancellationRequested) {
                    var notification = await client.NextNotification(context.CancellationToken);
                    await responseStream.WriteAsync(notification);
                }
            } catch (TaskCanceledException e) {
                log_.LogDebug("Finishing Listen handling {0}", e.Message);
            } finally {
                notifications_.RemoveEndpoint(user_id);
            }
        }

        protected string currentUserId(ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        readonly NotificationQueue notifications_ = new();
        readonly ILogger log_;
    }
}
