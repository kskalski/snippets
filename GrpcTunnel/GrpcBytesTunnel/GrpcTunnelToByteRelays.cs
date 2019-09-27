using Google.Protobuf;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Pro.Elector.Communication {
  /**
   * Transport interface providing asynchrounous byte chunks reading and writing.
   */
  public interface IByteRelay : IAsyncStreamReader<ByteString>, IAsyncStreamWriter<ByteString>, IDisposable {
  }

  public interface IByteRelayFactory {
    IByteRelay Create();
  }

  /**
   * Implementation of byte chunks transport by means of TCP socket:
   * - series of bytes read from socket are written to async stream and 
   * - chunks of bytes read from async stream and written to the socket.
   */
  class ByteRelayToTcpClient : IByteRelay {
    public ByteRelayToTcpClient(IPEndPoint ip_endpoint) {
      cli_.Connect(ip_endpoint);
      stream_ = cli_.GetStream();
    }

    public ByteString Current { private set; get; }

    public WriteOptions WriteOptions { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public async Task<bool> MoveNext(CancellationToken cancellationToken) {
      int num_read = await stream_.ReadAsync(incoming_bytes_, 0, incoming_bytes_.Length, cancellationToken);
      if (num_read == 0)
        return false;

      Current = ByteString.CopyFrom(incoming_bytes_, 0, num_read);
      return true;
    }

    public Task WriteAsync(ByteString message) {
      var bytes = message.ToByteArray();
      return stream_.WriteAsync(bytes, 0, bytes.Length);
    }

    public void Dispose() {
      stream_.Dispose();
      cli_.Close();
    }

    readonly TcpClient cli_ = new TcpClient();
    readonly NetworkStream stream_;
    readonly byte[] incoming_bytes_ = new byte[4096];
  }

  public class TcpRelayFactory : IByteRelayFactory {
    public TcpRelayFactory(IPEndPoint socket_address_target) {
      socket_address_target_ = socket_address_target;
    }
    public override string ToString() {
      return "tcp relays at " + socket_address_target_;
    }
    public IByteRelay Create() { return new ByteRelayToTcpClient(socket_address_target_); }

    IPEndPoint socket_address_target_;
  }

  /**
   * Establishes (and maintains / reconnects on errors) tunnel connection to ElectorTunnel service.
   * Manages creation of byte relays for each unique SourceId peer occurring in received TunnelMessage
   * initiating bytes chunks passing from such messages to the byte relay corresponding to that peer.
   *
   * Messages with empty payload are markers for requested creation and disposal of byte relays for given SourceId.
   */ 
  public class GrpcTunnelToByteRelays {
    public GrpcTunnelToByteRelays(Channel tunnel_channel, IByteRelayFactory relay_factory) {
      tunnel_channel_ = tunnel_channel;
      relay_factory_ = relay_factory;
    }

    public void Start() {
      execution_task_ = Task.Run(execution_loop);
    }

    public void Stop() {
      canceller_.Cancel();
      execution_task_.Wait();
    }

    StatusCode rpc_code_for_task(Task task) {
      if (task.IsCanceled)
        return StatusCode.Cancelled;
      if (!task.IsFaulted)
        return StatusCode.OK;
      var ex = task.Exception?.InnerException as RpcException;
      if (ex != null)
        return ex.StatusCode;
      return StatusCode.Unknown;
    }

    async Task execution_loop() {
      log_.InfoFormat("Enabling tunneled connection from {0} to {1}", tunnel_channel_.Target, relay_factory_);
      int wait_before_call_ms = 0;
      while (!canceller_.IsCancellationRequested) {
        try {
          if (wait_before_call_ms > 0)
            await Task.Delay(wait_before_call_ms);
          start_call();
          wait_before_call_ms = Math.Min(10000, Math.Max(10, wait_before_call_ms * 2));

          Task<bool> tunnel_read_task = in_stream.MoveNext(canceller_.Token);
          Task tunnel_write_task = Async.NeverEndingTask<object>.Instance;
          Task<Task<bool>> relays_task = Async.NeverEndingTask<Task<bool>>.Instance;

          while (!canceller_.IsCancellationRequested) {
            Task completed = await Task.WhenAny(tunnel_read_task, tunnel_write_task, relays_task);
            if (completed.Status != TaskStatus.RanToCompletion) {
              var status = rpc_code_for_task(completed);
              log_.DebugFormat("tunnel sub-task finished {0}", status);
              if (status != StatusCode.Cancelled && status != StatusCode.Unavailable)
                log_.Warn("tunnel sub-task fault reason", completed.Exception);
              break;
            } else if (completed == tunnel_read_task) {
              if (tunnel_read_task.Result) {
                var bytes = in_stream.Current.Payload;
                var peer_id = in_stream.Current.SourceId;
                IByteRelay relay;
                var relay_exists = relays_.TryGetValue(peer_id, out relay);
                if (bytes.Length == 0) {
                  // Marks connection and disconnection of peer with given id
                  if (relay_exists)
                    close_and_remove_peer(peer_id);
                  else
                    connect_and_add_new_peer(peer_id);
                  relays_task = wait_for_relays_task(tunnel_write_task);
                } else if (relay_exists) {
                  await relay.WriteAsync(bytes);
                } else {
                  log_.WarnFormat("non empty message to nonexisting relay {0}", peer_id);
                }
                tunnel_read_task = in_stream.MoveNext(canceller_.Token);
                wait_before_call_ms = 0;
              } else {
                log_.Info("Tunnel stream ended");
                break;
              }
            } else if (completed == tunnel_write_task) {
              tunnel_write_task = Async.NeverEndingTask<object>.Instance;
              relays_task = wait_for_relays_task(tunnel_write_task);
            } else if (completed == relays_task) {
              int t = 0;
              foreach (var id_relay in relays_) {
                if (relays_task.Result == relay_reading_tasks_[t]) {
                  ByteString payload = ByteString.Empty;
                  bool has_next = relay_reading_tasks_[t].Result;
                  if (has_next) {
                    payload = id_relay.Value.Current;
                    relay_reading_tasks_[t] = id_relay.Value.MoveNext(canceller_.Token);
                  } else {
                    close_and_remove_peer(id_relay.Key, t);
                  }
                  log_.DebugFormat("read from socket for peer {0} bytes {1}", id_relay.Key, payload?.Length);
                  var tunnel_message = new Proto.TunnelMessage() {
                    TargetId = id_relay.Key, Payload = payload
                  };
                  tunnel_write_task = tunnel_call_.RequestStream.WriteAsync(tunnel_message);
                  relays_task = Async.NeverEndingTask<Task<bool>>.Instance;
                  break;
                }
                ++t;
              }

            } else {
              log_.ErrorFormat("unknown task finished {0}", completed);
              break;
            }
          }
        } catch (TaskCanceledException) {
          break;
        } catch (Exception e) {
          log_.Warn("problem processing elector tunnel call", e);
        } finally {
          maybe_close_call();
        }
      }
      log_.Info("Shutting down tunnel relay channel");
      await tunnel_channel_.ShutdownAsync();
      log_.Info("Finished tunnel relay dispatch");
    }

    Task<Task<bool>> wait_for_relays_task(Task tunnel_write_task) {
      if (relay_reading_tasks_.Length > 0 && tunnel_write_task == Async.NeverEndingTask<object>.Instance)
        return Task.WhenAny(relay_reading_tasks_);
      return Async.NeverEndingTask<Task<bool>>.Instance;
    }

    IAsyncStreamReader<Proto.TunnelMessage> in_stream { get => tunnel_call_.ResponseStream; }

    void start_call() {
      log_.DebugFormat("Establishing tunnel stream to {0}", tunnel_channel_.Target);
      var tunnel_client = new Proto.ElectorTunnel.ElectorTunnelClient(tunnel_channel_);
      tunnel_call_ = tunnel_client.OpenChannelAsServer();
    }

    void maybe_close_call() {
      if (tunnel_call_ != null) {
        tunnel_call_.Dispose();
        tunnel_call_ = null;
      }
    }

    void connect_and_add_new_peer(long peer_id) {
      log_.InfoFormat("adding tunneling peer {0}", peer_id);
      relays_[peer_id] = relay_factory_.Create();
      Array.Resize(ref relay_reading_tasks_, relays_.Count);
      int t = 0;
      foreach (var id_relay in relays_) {
        if (id_relay.Key == peer_id) {
          Array.Copy(relay_reading_tasks_, t, relay_reading_tasks_, t + 1, relay_reading_tasks_.Length - t - 1);
          relay_reading_tasks_[t] = id_relay.Value.MoveNext(canceller_.Token);
          break;
        }
        ++t;
      }
    }

    void close_and_remove_peer(long peer_id, int t = -1) {
      log_.InfoFormat("removing tunneling peer {0}", peer_id);
      if (t < 0) {
        foreach (var id_relay in relays_) {
          ++t;
          if (id_relay.Key == peer_id) {
            id_relay.Value.Dispose();
            break;
          }
        }
      }

      relays_.Remove(peer_id);
      Array.Copy(relay_reading_tasks_, t + 1, relay_reading_tasks_, t, relay_reading_tasks_.Length - t - 1);
      Array.Resize(ref relay_reading_tasks_, relay_reading_tasks_.Length - 1);
    }

    // Initialized on construction
    readonly Channel tunnel_channel_;
    readonly IByteRelayFactory relay_factory_;

    // Initialized on start-up and re-connections
    AsyncDuplexStreamingCall<Proto.TunnelMessage, Proto.TunnelMessage> tunnel_call_;
    Task execution_task_;

    // State
    readonly SortedDictionary<long, IByteRelay> relays_ = new SortedDictionary<long, IByteRelay>();
    Task<bool>[] relay_reading_tasks_ = new Task<bool>[] { };
    readonly CancellationTokenSource canceller_ = new CancellationTokenSource();

    static readonly log4net.ILog log_ = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
  }
}
