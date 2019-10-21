Sketch prototype of library providing byte-level gRPC tunnel service. 
Currently it allows forwarding data between actual TCP sockets (intended
to be gRPC client / server endpoints).

The architecture follows roughly following flow:

```
original gRPC client
    | tcp
   \/
NetworkStreamToTunnelRelay, NetworkStreamToTunnelRelay, ...
   |||
SocketServerToGrpcTunnel 
    | grpc tunnel call
   \/
ElectorTunnelService 
   /\
    | grpc tunnel call
GrpcTunnelToByteRelays
   |||
ByteRelayToTcpClient, ByteRelayToTcpClient, ...
     | tcp
    \/
original gRPC server
``` 

ElectorTunnelService:
   - GrpcEndpointToDispatcherRelay \
   - GrpcEndpointToDispatcherRelay <-----> IDispatcher (routes messages between endpoints)
   - GrpcEndpointToDispatcherRelay /
