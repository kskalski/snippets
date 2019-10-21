using System;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]

namespace example {
  class Program {
    static void Main(string[] args) {
      log_.Info("Starting example app");
      if (args.Length >= 1) {
        if (args[0] == "tunnel_server") {
          TunnelServer.RunServerAt(12349);
          return;
        } else if (args[0] == "example_server") {
          ServerForwardedByTunnel.Run(Tuple.Create("localhost", 12349), 12347);
          return;
        } else if (args[0] == "example_client") {
          ClientForwardedByTunnel.Run(Tuple.Create("localhost", 12349), 12346);
          return;
        }
      }
      Console.WriteLine("Usage: example.exe mode\nmode = tunnel_server | example_server | example_client");
    }

    private static readonly log4net.ILog log_ = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
  }
}
