﻿using Grpc.Core;
using Pro.Elector.Proto;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Pro.Elector.Communication {
  /*
   * Task for forwarding data between network stream (e.g. from socket server) to gRPC tunnel.
   */ 
  class NetworkStreamToTunnelRelay {
    public NetworkStreamToTunnelRelay(NetworkStream networkStream, IAsyncStreamReader<TunnelMessage> responseStream, IAsyncStreamWriter<TunnelMessage> requestStream) {
      this.target_id_ = -1;
      this.stream_ = networkStream;
      this.in_queue_ = responseStream;
      this.out_queue_ = requestStream;
    }

    public async Task Run() {
      try {
        Task<bool> tunnel_read_task = in_queue_.MoveNext();
        await tunnel_read_task;
        Task<int> read_task = Async.NeverEndingTask<int>.Instance;
        Task tunnel_write_task = Task.CompletedTask;
        while (true) {
          await Task.WhenAny(Task.WhenAll(read_task, tunnel_write_task), tunnel_read_task);
          if (tunnel_read_task.IsCompleted) {
            var has_next = tunnel_read_task.Result;
            var bytes = has_next ? in_queue_.Current.Payload : null;
            if (bytes != null) {
              if (bytes.Length == 0) {
                log_.InfoFormat("zero message from {0}", in_queue_.Current.SourceId);
                target_id_ = in_queue_.Current.SourceId;
                // Received server availability message, also send out client connection initiation
                // message.
                out_queue_.WriteAsync(new TunnelMessage() { TargetId = target_id_ }).Wait();
                read_task = stream_.ReadAsync(incoming_bytes_, 0, incoming_bytes_.Length);
              } else {
                log_.DebugFormat("writing {0}", bytes.Length);
                bytes.WriteTo(stream_);
              }
              tunnel_read_task = in_queue_.MoveNext();
            } else {
              break;
            }
          } else {
            if (read_task.Result == 0)
              break;
            log_.DebugFormat("read {0}, sending to {1}", read_task.Result, target_id_);
            tunnel_write_task = out_queue_.WriteAsync(new TunnelMessage() {
              TargetId = target_id_, Payload = Google.Protobuf.ByteString.CopyFrom(incoming_bytes_, 0, read_task.Result)
            });
            read_task = stream_.ReadAsync(incoming_bytes_, 0, incoming_bytes_.Length);
          }
        }
      } catch (Exception e) {
        log_.Warn("exception in network to tunnel rely", e);
      }
    }

    long target_id_;
    readonly NetworkStream stream_;
    readonly IAsyncStreamReader<TunnelMessage> in_queue_;
    readonly IAsyncStreamWriter<TunnelMessage> out_queue_;
    readonly byte[] incoming_bytes_ = new byte[4096];

    static readonly log4net.ILog log_ = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
  }

  /*
   * Socket server forwarding connections and byte transfers to gRPC tunnel call.
   */
  public class SocketServerToGrpcTunnel {
    public SocketServerToGrpcTunnel(AsyncDuplexStreamingCall<TunnelMessage, TunnelMessage> call) {
      this.terminal_call_ = call;
    }

    public void StartServerRely(int port) {
      Task.Run(() => server_rely_task(port));
    }

    async Task server_rely_task(int port) {
      CancellationTokenSource cts = new CancellationTokenSource();
      TcpListener listener = new TcpListener(IPAddress.Any, port);
      try {
        listener.Start();
        //just fire and forget. We break from the "forgotten" async loops
        //in AcceptClientsAsync using a CancellationToken from `cts`
        await AcceptClientsAsync(listener, cts.Token);
      } catch(Exception e) {
        log_.Warn("exception on tcp listening", e);
      } finally {
        cts.Cancel();
        listener.Stop();
      }
    }

    async Task AcceptClientsAsync(TcpListener listener, CancellationToken ct) {
      var clientCounter = 0;
      try {
        while (!ct.IsCancellationRequested) {
          TcpClient client = await listener.AcceptTcpClientAsync()
                                              .ConfigureAwait(false);
          clientCounter++;
          //once again, just fire and forget, and use the CancellationToken
          //to signal to the "forgotten" async invocation.
          PassBytesFromSocketToTunnel(client, clientCounter, ct);
        }
      } catch (Exception e) {
        log_.Warn("Exception in accept", e);
      }
    }
    async Task PassBytesFromSocketToTunnel(TcpClient client, int clientIndex, CancellationToken ct) {
      try {
        log_.InfoFormat("New client ({0}) connected", clientIndex);
        using (client) {
          var buf = new byte[4096];
          var stream = client.GetStream();
          //var server_rely = new Rely("server", stream, reply_queue_, received_queue_);
          var server_rely = new NetworkStreamToTunnelRelay(stream, this.terminal_call_.ResponseStream, this.terminal_call_.RequestStream);
          await server_rely.Run();
        }
      } catch (Exception e) {
        log_.Warn("Exception in passing bytes from socket to tunnel", e);
      } finally {
        log_.InfoFormat("Client ({0}) disconnected", clientIndex);
      }
    }

    AsyncDuplexStreamingCall<TunnelMessage, TunnelMessage> terminal_call_;

    static readonly log4net.ILog log_ = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
  }
}
