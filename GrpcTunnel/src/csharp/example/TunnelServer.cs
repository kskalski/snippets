using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;

namespace example {
  class TunnelServer : Pro.Elector.Communication.GrpcTunnelService {
    protected override long? IdentifyPeer(ServerCallContext context) {
      return parse_id("source_id", context);
    }

    protected override long? GetTargetServerIdForClientCall(ServerCallContext context) {
      return parse_id("target_id", context);
    }

    public static void RunServerAt(int port) {
      Server server = new Server {
        Services = { Pro.Elector.Proto.ElectorTunnel.BindService(new TunnelServer()) },
        Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
      };
      server.Start();

      Console.WriteLine("Tunnel server listening on port " + port);
      Console.WriteLine("Press any key to stop the server...");
      Console.ReadKey();

      server.ShutdownAsync().Wait();
    }

    static long? parse_id(string name, ServerCallContext context) {
      long id;
      foreach (var el in context.RequestHeaders) {
        if (el.Key == name) {
          if (long.TryParse(el.Value, out id))
            return id;
          return null;
        }
      }
      return null;
    }
  }
}
