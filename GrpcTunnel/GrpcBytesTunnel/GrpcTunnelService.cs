using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Pro.Elector.Proto;

namespace Pro.Elector.Communication {
  /** Provides message dispatch for server and clients endpooints assigned together.
   *  During disconnects the endpoints are notified or moved up to tunnel server to wait for peer reconnect.
   */
  class TunneledServerScope : IDispatcher<TunnelMessage> {
    public TunneledServerScope(IDispatcher<TunnelMessage> tunnel_service, GrpcEndpointToDispatcherRelay<TunnelMessage> server_endpoint) {
      server_endpoint.Dispatcher = this;
      server_endpoint_ = server_endpoint;
      tunnel_service_ = tunnel_service;
    }

    public void Send(long source_id, TunnelMessage message) {
      message.SourceId = source_id;
      lock (this) {
        if (message.TargetId == server_endpoint_.Id)
          server_endpoint_.EnqueueSend(message);
        else if (source_id == server_endpoint_.Id) {
          GrpcEndpointToDispatcherRelay<TunnelMessage> target = null;
          if (message.Payload.IsEmpty) {
            server_is_active_ = !server_is_active_;
            foreach (var endpoint in client_endpoints_by_id_.Values)
              endpoint.EnqueueSend(message);
          } else if (client_endpoints_by_id_.TryGetValue(message.TargetId, out target)) {
            target.EnqueueSend(message);
          }
        }
      }
    }

    public void RemoveEndpoint(long id) {
      log_.InfoFormat("Scope removing endpoint {0}", id);
      var goodbye_message = new TunnelMessage() { SourceId = id };
      if (id == server_endpoint_.Id) {
        tunnel_service_.RemoveEndpoint(id);
        GrpcEndpointToDispatcherRelay<TunnelMessage>[] clients;
        lock (this) {
          clients = client_endpoints_by_id_.Values.ToArray();
          client_endpoints_by_id_.Clear();
          if (!server_is_active_)
            goodbye_message = null;
        }
        foreach (var existing_peer in clients) {
          if (goodbye_message != null)
            existing_peer.EnqueueSend(goodbye_message);
          // move client endpoint to top-level waiting set
          tunnel_service_.AddEndpoint(existing_peer, id);
        }
      } else lock (this) {
          if (!client_endpoints_by_id_.Remove(id))
            return;
          if (server_is_active_)
            server_endpoint_.EnqueueSend(goodbye_message);
        }
    }

    public bool AddEndpoint(GrpcEndpointToDispatcherRelay<TunnelMessage> client_endpoint, long? target_server_id) {
      if (target_server_id != server_endpoint_.Id)
        throw new ArgumentException("target server id is not matching with this scope " + target_server_id);
      log_.InfoFormat("Adding client {0} to server scope {1}", client_endpoint.Id, server_endpoint_.Id);
      lock (this) {
        if (client_endpoints_by_id_.TryAdd(client_endpoint.Id, client_endpoint)) {
          client_endpoint.Dispatcher = this;
          if (server_is_active_)
            client_endpoint.EnqueueSend(new TunnelMessage() { SourceId = server_endpoint_.Id });
          return true;
        }
        return false;
      }
    }

    // Parent dispatcher use to notify about server disconnect and moving clients to waiting set.
    readonly IDispatcher<TunnelMessage> tunnel_service_;

    // Server endpoint is assumed to be active during lifetime of this object.
    readonly GrpcEndpointToDispatcherRelay<TunnelMessage> server_endpoint_;
    bool server_is_active_;

    // Client endpoints relating to open channels - the actual client might be active or not, changes
    // of that state is notified to server by activation/deactivation message (empty payload).
    readonly IDictionary<long, GrpcEndpointToDispatcherRelay<TunnelMessage>> client_endpoints_by_id_ =
      new Dictionary<long, GrpcEndpointToDispatcherRelay<TunnelMessage>>();

    static readonly log4net.ILog log_ = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
  }

  public class GrpcTunnelService : ElectorTunnel.ElectorTunnelBase, IDispatcher<TunnelMessage> {
    // server connects -> create entry with its endpoint and clients already connected with matching key
    // client connects -> matches any existing server -> add it to its server scope (clients set)
    //                                                -> otherwise add it to waiting set remembering desired key
    //
    // when adding client to server scope -> send notification to client about server availability
    // when disconnecting server -> send server unavailable message to all clients in its scope and move them to waiting set
    // when disconnecting client -> send client disconnect message to server of its server scope
    // 
    // server sends message -> check if target id is among clients in its server scope, forward
    // client sends message -> forward it to server endpoint owning scope with that client

    public override Task OpenChannelAsServer(IAsyncStreamReader<TunnelMessage> req, IServerStreamWriter<TunnelMessage> res, ServerCallContext context) {
      log_.DebugFormat("open channel as server from {0}", context.Peer);
      try {
        var endpoint = create_endpoint_handler(req, res, context);
        if (endpoint == null) {
          log_.InfoFormat("Rejecting open as server channel");
          context.Status = new Status(StatusCode.InvalidArgument, "cannot determine identity");
          return Task.CompletedTask;
        }

        if (!AddEndpoint(endpoint, null)) {
          log_.InfoFormat("Unable to add server scope {0}", endpoint.Id);
          context.Status = new Status(StatusCode.AlreadyExists, "cannot add peer");
          return Task.CompletedTask;
        }
        return endpoint.Run();
      } catch (Exception e) {
        log_.Error("problem handling request", e);
        context.Status = new Status(StatusCode.Internal, "unknown error");
        return Task.CompletedTask;
      }
    }

    public override Task OpenChannelAsClient(IAsyncStreamReader<TunnelMessage> req, IServerStreamWriter<TunnelMessage> res, ServerCallContext context) {
      log_.DebugFormat("open channel as client from {0}", context.Peer);
      try {
        var endpoint = create_endpoint_handler(req, res, context);
        long? target_id = GetTargetServerIdForClientCall(context);
        if (endpoint == null || target_id == null) {
          log_.InfoFormat("Rejecting open as client channel {0}, {1}", endpoint, target_id);
          context.Status = new Status(StatusCode.InvalidArgument, "cannot determine ids");
          return Task.CompletedTask;
        }

        if (!AddEndpoint(endpoint, target_id)) {
          log_.InfoFormat("Unable to add waiting terminal {0}", target_id);
          context.Status = new Status(StatusCode.AlreadyExists, "cannot add peer");
          return Task.CompletedTask;
        }
        return endpoint.Run();
      } catch (Exception e) {
        log_.Error("problem handling request", e);
        context.Status = new Status(StatusCode.Internal, "unknown error");
        return Task.CompletedTask;
      }
    }

    public void RemoveEndpoint(long id) {
      log_.InfoFormat("Top removing endpoint {0}", id);
      lock (this) {
        // TODO: add security.. 8-o
        if (!(waiting_clients_by_id_.Remove(id) || server_scopes_by_id_.Remove(id)))
          log_.WarnFormat("Failed to remove endpoint {0}", id);
      }
    }

    public bool AddEndpoint(GrpcEndpointToDispatcherRelay<TunnelMessage> endpoint, long? target_id) {
      log_.InfoFormat("Top adding endpoint {0} with target {1}", endpoint.Id, target_id);
      lock (this) {
        if (target_id != null) {
          if (!waiting_clients_by_id_.TryAdd(endpoint.Id, Tuple.Create(endpoint, target_id.Value)))
            return false;
          endpoint.Dispatcher = this;
        } else {
          if (!server_scopes_by_id_.TryAdd(endpoint.Id, new TunneledServerScope(this, endpoint))) {
            return false;
          }
        }
        try_assigning_waiting_clients(target_id ?? endpoint.Id);
      }
      return true;
    }

    void IDispatcher<TunnelMessage>.Send(long source_id, TunnelMessage message) {
      log_.ErrorFormat("spurious send on top level {0} from {1}, ignoring", message, source_id);
    }

    protected virtual long? IdentifyPeer(ServerCallContext context) {
      return null;
    }

    protected virtual long? GetTargetServerIdForClientCall(ServerCallContext context) {
      return null;
    }

    void try_assigning_waiting_clients(long server_id) {
      lock (this) {
        TunneledServerScope server_scope;
        if (!server_scopes_by_id_.TryGetValue(server_id, out server_scope))
          return;

        foreach (var entry in waiting_clients_by_id_.Values.Where(e => e.Item2 == server_id).ToArray()) {
          waiting_clients_by_id_.Remove(entry.Item1.Id);
          if (!server_scope.AddEndpoint(entry.Item1, entry.Item2))
            entry.Item1.EnqueueSend(null);
        }
      }
    }

    GrpcEndpointToDispatcherRelay<TunnelMessage> create_endpoint_handler(IAsyncStreamReader<TunnelMessage> req, IServerStreamWriter<TunnelMessage> res, ServerCallContext context) {
      var id = IdentifyPeer(context);
      log_.InfoFormat("agent from peer {0} = {1} ({2})", context.Peer, id, Utils.Strings.ToAlphanumericString(id));
      if (id != null)
        return new GrpcEndpointToDispatcherRelay<TunnelMessage>(id.Value, req, res, context.CancellationToken, this);
      return null;
    }

    // Connected server channels with clients assigned to them - presence of given server also denotes its availability.
    Dictionary<long, TunneledServerScope> server_scopes_by_id_ = new Dictionary<long, TunneledServerScope>();

    // Connected client channels that wait for their target server to be available.
    IDictionary<long, Tuple<GrpcEndpointToDispatcherRelay<TunnelMessage>, long>> waiting_clients_by_id_ =
      new SortedDictionary<long, Tuple<GrpcEndpointToDispatcherRelay<TunnelMessage>, long>>();

    static readonly log4net.ILog log_ = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
  }
}
