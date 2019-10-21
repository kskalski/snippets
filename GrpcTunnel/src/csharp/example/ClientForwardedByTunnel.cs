using Grpc.Core;
using Pro.Elector.Communication;
using System;
using System.Collections.Generic;
using System.Text;

namespace example {
  class ClientForwardedByTunnel {
    public static void Run(Tuple<string, int> tunnel_server_hostport, int forward_client_port = 12345) {
      Channel tunnel_channel = new Channel(tunnel_server_hostport.Item1, tunnel_server_hostport.Item2, ChannelCredentials.Insecure);
      var tunnel_client = new Pro.Elector.Proto.ElectorTunnel.ElectorTunnelClient(tunnel_channel);
      var tunnel_metadata = new Metadata();
      tunnel_metadata.Add("source_id", "123");  // forwarded client id
      tunnel_metadata.Add("target_id", "987");  // forwarder server id that clients wants to connect to
      var tunnel_call = tunnel_client.OpenChannelAsClient(new CallOptions(headers: tunnel_metadata));
      Console.WriteLine("Starting relying client connections through port " + forward_client_port + " to tunnel at " + tunnel_server_hostport);
      var s = new SocketServerToGrpcTunnel(tunnel_call);
      s.StartServerRely(forward_client_port);

      MakeTestCall(forward_client_port);

      tunnel_call.Dispose();
    }

    public static void MakeTestCall(int forward_client_port) {
      Console.WriteLine("Creating grpc client connecting to port " + forward_client_port);
      Channel client_channel = new Channel("localhost", forward_client_port, ChannelCredentials.Insecure);
      var client = new Example.Example.ExampleClient(client_channel);
      var reply = client.Reverse(new Example.ExampleRequest() { Text = "abc" });
      Console.WriteLine("Response through tunneled connection {0}", reply);
      client_channel.ShutdownAsync().Wait();
    }
  }
}
