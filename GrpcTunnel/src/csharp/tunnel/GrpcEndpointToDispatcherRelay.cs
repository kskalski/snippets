using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pro.Elector.Communication {
  /**
   * Grpc streaming endpoint handler (Task), which allows processing involved messages by synchronous components:
   *   - dispatcher called on every received message
   *   - synchronous send method providing queueing and serial (one at a time) write out of messages to send
   * */
  public class GrpcEndpointToDispatcherRelay<T> {
    public GrpcEndpointToDispatcherRelay(long id, IAsyncStreamReader<T> req, IServerStreamWriter<T> res,
                                         CancellationToken cancel_token, IDispatcher<T> dispatcher) {
      this.Id = id;
      request_stream_ = req;
      response_stream_ = res;
      cancel_token_ = cancel_token;
      Dispatcher = dispatcher;
    }

    /**
     * Asynchronously reads and passes messages of type T from (grpc) stream reader to
     * synchrononous `dispatcher`.
     * Also exposes synchronous `EnqueueSend` method to queue messages for sending through
     * (grpc) stream writer.
     * */
    public async Task Run() {
      Task<bool> read_task = request_stream_.MoveNext(cancel_token_);
      Task<T> queue_read_task = response_queue_.DequeueAsync();
      Task write_task = Async.NeverEndingTask<object>.Instance;
      try {
        while (!cancel_token_.IsCancellationRequested) {
          Task completed = await Task.WhenAny(read_task, queue_read_task, write_task);
          if (completed == read_task) {
            if (!read_task.Result)
              break;
            Dispatcher.Send(Id, request_stream_.Current);
            read_task = request_stream_.MoveNext(cancel_token_);
          } else if (completed == queue_read_task) {
            var queue_message = queue_read_task.Result;
            if (queue_message == null)
              break;

            write_task = response_stream_.WriteAsync(queue_message);
            queue_read_task = Async.NeverEndingTask<T>.Instance;
          } else if (completed == write_task) {
            queue_read_task = response_queue_.DequeueAsync();
            write_task = Async.NeverEndingTask<object>.Instance;
          } else {
            log_.ErrorFormat("Unknown completed task {0}", completed);
            break;
          }
        }
      } catch (TaskCanceledException) {
      } catch (Exception e) {
        log_.WarnFormat("error processing agent {0} due to {1}", Id, e);
      } finally {
        Dispatcher.RemoveEndpoint(Id);
      }
    }

    public void EnqueueSend(T message) {
      response_queue_.Enqueue(message);
    }

    public long Id { get; private set; }
    public IDispatcher<T> Dispatcher { private get; set; }

    readonly IAsyncStreamReader<T> request_stream_;
    readonly IServerStreamWriter<T> response_stream_;
    readonly Async.AsyncConsumerQueue<T> response_queue_ = new Async.AsyncConsumerQueue<T>();
    readonly CancellationToken cancel_token_;

    static readonly log4net.ILog log_ = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
  }

  public interface IDispatcher<T> {
    /** Directs message to appropriate endpoint registered in this dispatcher. */
    void Send(long source_id, T message);

    /** Registers endpoint to be managed by this dispatcher and receive messages sent through it.
     * target_server_id == null denotes that endpoint is a server, otherwise it's a client side
     */
    bool AddEndpoint(GrpcEndpointToDispatcherRelay<T> endpoint, long? target_server_id);

    /** Removes endpoint from being managed by this dispatcher and receive messages sent through it. */
    void RemoveEndpoint(long id);
  }
}
