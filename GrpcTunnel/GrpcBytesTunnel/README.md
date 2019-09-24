Sketch prototype of library providing byte-level gRPC tunnel service. 
Currently it allows forwarding data between actual TCP sockets (intended
to be gRPC client / server endpoints).

The architecture follows roughly following flow:

original gRPC client --tcp--> NetworkStreamToTunnelRelay --- SocketServerToGrpcTunnel --grpc tunnel call--> ElectorTunnelService
                     -------> NetworkStreamToTunnelRelay ---/

original gRPC server <--tcp--- ByteRelayToTcpClient <---- GrpcTunnelToByteRelays ---grpc tunnel call--> ElectorTunnelService
                       \------ ByteRelayToTcpClient <---/

ElectorTunnelService:
   - GrpcEndpointToDispatcherRelay \
   - GrpcEndpointToDispatcherRelay <-----> IDispatcher (routes messages between endpoints)
   - GrpcEndpointToDispatcherRelay /
