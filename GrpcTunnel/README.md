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

Example:
Run following commands (in separate terminals) to test simple use case of running tunnel server and connecting to it with example server and client:
```
dotnet.exe run -p src/csharp/example/ tunnel_server
```
```
dotnet.exe run -p src/csharp/example/ example_server
```
```
dotnet.exe run -p src/csharp/example/ example_client
```
