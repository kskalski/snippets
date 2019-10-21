using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pro.Elector.Async {
  public class AsyncConsumerQueue<T> {
    public void Enqueue(T el) {
      lock (queue_) {
        queue_.Enqueue(el);
        if (!not_empty_notify_.Task.IsCompleted)
          not_empty_notify_.SetResult(true);
      }
    }
    public async Task<T> DequeueAsync()
    {
      while (true) {
        lock (queue_) {
          if (queue_.Count > 0) {
            T result = queue_.Dequeue();
            if (queue_.Count == 0)
              not_empty_notify_ = new TaskCompletionSource<bool>();
            return result;
          }
        }
        await not_empty_notify_.Task;
      }
    }

    readonly Queue<T> queue_ = new Queue<T>();
    TaskCompletionSource<bool> not_empty_notify_ = new TaskCompletionSource<bool>();
  }

  public class NeverEndingTask<T> {
    public static Task<T> Instance {
      get { return source_.Task; }
    }
    static TaskCompletionSource<T> source_ = new TaskCompletionSource<T>();
  }
}
