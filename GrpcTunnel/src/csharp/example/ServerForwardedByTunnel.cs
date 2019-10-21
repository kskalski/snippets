using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Example;
using Grpc.Core;

namespace example {
  class ServerForwardedByTunnel : Example.Example.ExampleBase {
    public override Task<ExampleResponse> Reverse(ExampleRequest request, ServerCallContext context) {
      char[] array = request.Text.ToCharArray();
      Array.Reverse(array);
      return Task.FromResult(new ExampleResponse() { ReversedText = new string(array) });
    }

    public static void Run(Tuple<string, int> tunnel_server_hostport, int example_server_port) {
      Server example_server = new Server {
        Services = { Example.Example.BindService(new ServerForwardedByTunnel()) },
        Ports = { new ServerPort("localhost", example_server_port, ServerCredentials.Insecure) }
      };
      Task.Run(example_server.Start);

      var tunnel_channel = new Channel(tunnel_server_hostport.Item1, tunnel_server_hostport.Item2, ChannelCredentials.Insecure);
      var tunnel_metadata = new Metadata();
      tunnel_metadata.Add("source_id", "987");
      var relay = new Pro.Elector.Communication.GrpcTunnelToByteRelays(
        tunnel_channel,
        tunnel_metadata,
        new Pro.Elector.Communication.TcpRelayFactory(
          new IPEndPoint(IPAddress.Loopback, example_server_port)));
      relay.Start();

      Console.WriteLine("Press any key to stop the tunneling of server...");
      Console.ReadKey();

      relay.Stop();
      example_server.ShutdownAsync().Wait();
    }
  }
}
