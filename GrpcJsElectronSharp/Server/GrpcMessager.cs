using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IMessageSequencesAccessor<T, R> {
  int GetClientNr(T t);
  void SetClientNr(T t, int nr);
  int GetServerNr(T t);
  void SetServerNr(T t, int nr);
  int GetClientNr(R t);
  void SetClientNr(R t, int nr);
  int GetServerNr(R t);
  void SetServerNr(R t, int nr);
}

public class GrpcMessager<T, R, A> : IDisposable
  where T : class where R : class where A : IMessageSequencesAccessor<T, R>, new() {
  class PendingExchange {
    public readonly TaskCompletionSource<Tuple<T, int>> Request = new TaskCompletionSource<Tuple<T, int>>();
    public readonly TaskCompletionSource<R> Response = new TaskCompletionSource<R>();
    public readonly int OwnSequenceNr;

    public PendingExchange(int seq_nr) {
      OwnSequenceNr = seq_nr;
    }
  }

  public GrpcMessager(IAsyncStreamWriter<T> sink, IAsyncEnumerator<R> source, Func<R, T> req_handler) {
    requests_sink_ = sink;
    responses_source_ = source;
    seq_accessor_ = new A();
    peer_requests_handler_ = req_handler;
  }
  // For testing
  public GrpcMessager() {
    seq_accessor_ = new A();
  }

  public void Dispose() {
    lifetime_.TrySetCanceled();
  }

  public bool IsCancelled {
    get { return lifetime_.Task.IsCanceled; }
  }

  public async Task Run(CancellationToken token) {
    token.Register(Dispose);
    try {
      PendingExchange current_exchange = pending_;
      Queue<PendingExchange> response_exchanges = new Queue<PendingExchange>();
      Task sending_task = current_exchange.Request.Task;
      Task<bool> receving_task = null;
      while (!lifetime_.Task.IsCanceled) {
        if (receving_task == null)
          receving_task = responses_source_.MoveNext(token);

        if (await Task.WhenAny(receving_task, sending_task, lifetime_.Task) == lifetime_.Task) {
          current_exchange.Request.TrySetCanceled();
          current_exchange.Response.TrySetCanceled();
          break;
        }

        if (receving_task.IsCompleted) {
          Console.WriteLine("At {0} received something {1}", DateTime.Now.ToLongTimeString(), receving_task);
          if (receving_task.IsFaulted || receving_task.IsCanceled || !receving_task.Result)
            break;

          var msg_from_peer = responses_source_.Current;
          int own_resp_seq_nr = seq_accessor_.GetServerNr(msg_from_peer);
          int peer_resp_seq_nr = seq_accessor_.GetClientNr(msg_from_peer);
          if (peer_resp_seq_nr > last_peer_seq_) {
            T res = peer_requests_handler_?.Invoke(msg_from_peer);
            if (res != null) {
              var exchange = new PendingExchange(current_exchange.OwnSequenceNr);
              exchange.Request.SetResult(Tuple.Create(res, 0));
              response_exchanges.Enqueue(exchange);
            }
          } else if ((own_resp_seq_nr == 0 || own_resp_seq_nr == current_exchange.OwnSequenceNr) &&
                     current_exchange.Request.Task.IsCompleted &&
                     current_exchange.Request.Task.Result.Item2 > 0) {
            current_exchange.Response.TrySetResult(msg_from_peer);
          }
          last_peer_seq_ = peer_resp_seq_nr;
          receving_task = null;
        }
        if (sending_task.IsCompleted) {
          if (sending_task == current_exchange.Request.Task) {
            sending_task = requests_sink_.WriteAsync(current_exchange.Request.Task.Result.Item1);
          } else if (sending_task == current_exchange.Response.Task) {
            if (response_exchanges.Count > 0) {
              current_exchange = response_exchanges.Dequeue();
            } else {
              current_exchange = pending_;
            }
            sending_task = current_exchange.Request.Task;
          } else {
            sending_task = current_exchange.Response.Task;
          }
        }
      }
    } catch (Exception ex) {
      System.Console.WriteLine("interrupted execution {0}", ex);
    }
  }

  public virtual R Send(T request, int timeout_ms) {
    var p = Interlocked.Exchange(ref pending_, new PendingExchange(Interlocked.Increment(ref current_own_seq_)));
    seq_accessor_.SetClientNr(request, last_peer_seq_);
    seq_accessor_.SetServerNr(request, p.OwnSequenceNr);
    p.Request.SetResult(Tuple.Create(request, timeout_ms));
    return sync_wait_reception(p);
  }

  private R sync_wait_reception(PendingExchange exchange) {
    var response_task = exchange.Response.Task;
    int timeout_ms = exchange.Request.Task.Result.Item2;
    try {
      if (timeout_ms == 0 || !response_task.Wait(timeout_ms)) {
        return null;
      }
    } catch (AggregateException ex) {
      System.Console.WriteLine("handled exception while waiting {0}", ex);
      lifetime_.TrySetCanceled();
      return null;
    } finally {
      exchange.Response.TrySetCanceled();
    }
    return response_task.Status == TaskStatus.RanToCompletion ? response_task.Result : null;
  }

  readonly TaskCompletionSource<bool> lifetime_ = new TaskCompletionSource<bool>();

  int last_peer_seq_;
  int current_own_seq_ = 1;
  PendingExchange pending_ = new PendingExchange(1);
  readonly Func<R, T> peer_requests_handler_;
  readonly IAsyncStreamWriter<T> requests_sink_;
  readonly IAsyncEnumerator<R> responses_source_;
  readonly IMessageSequencesAccessor<T, R> seq_accessor_;
}

public class WindowMsgSequenceAccessor : IMessageSequencesAccessor<WindowFormsToExternal, WindowExternalToForms> {
  public int GetClientNr(WindowExternalToForms t) { return t.ExternalSequenceNr; }
  public int GetClientNr(WindowFormsToExternal t) { return t.ExternalSequenceNr; }
  public int GetServerNr(WindowExternalToForms t) { return t.FormsSequenceNr; }
  public int GetServerNr(WindowFormsToExternal t) { return t.FormsSequenceNr; }
  public void SetClientNr(WindowExternalToForms t, int nr) { t.ExternalSequenceNr = nr; }
  public void SetClientNr(WindowFormsToExternal t, int nr) { t.ExternalSequenceNr = nr; }
  public void SetServerNr(WindowExternalToForms t, int nr) { t.FormsSequenceNr = nr; }
  public void SetServerNr(WindowFormsToExternal t, int nr) { t.FormsSequenceNr = nr; }
}
