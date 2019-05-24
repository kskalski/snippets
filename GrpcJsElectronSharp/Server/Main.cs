using System;

class Program {
   static WindowFormsToExternal handler(WindowExternalToForms req) {
      var now = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
      long val = Convert.ToInt64(now);
      long.TryParse(req.Action, out val);
      Console.WriteLine("At {0} got req {1} = diff {2}", now, req, now - val);
      return null;
  }

   public static void Main() {
     var service = new GrpcService();
    service.RunServer();
    service.onReceivedWindowRequest += handler;
    Console.ReadKey();
    service.onReceivedWindowRequest -= handler;
    service.Dispose();
   }
}

