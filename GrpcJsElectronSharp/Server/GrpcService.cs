using Grpc.Core;
using System;
using System.Threading.Tasks;

public class GrpcService : IDisposable {
    public GrpcService() {
      elector_gateway_ = new ElectorGatewayImpl(this);
      server_credentials_ = ServerCredentials.Insecure;
    }

    public void RunServer() {
      server_ = new Server {
        Services = { ElectorGateway.BindService(elector_gateway_) },
        Ports = { new ServerPort("0.0.0.0", 15745, server_credentials_) }
      };
      server_.Start();
      Console.WriteLine("Server started");
    }

    public void Dispose() {
      var shutdown_task = server_?.ShutdownAsync();
      lock (this) {
        if (window_messager_ != null)
          window_messager_.Dispose();
        window_messager_ = null;
      }
      if (shutdown_task != null) {
        shutdown_task.Wait();
      }
    }


    public bool WindowControl(GrpcMessager<WindowFormsToExternal, WindowExternalToForms, WindowMsgSequenceAccessor> messager) {
      lock (this) {
        if (window_messager_ != null) {
          window_messager_.Dispose();
        }
        window_messager_ = messager;
      }
      return true;
    }

    public event Func<WindowExternalToForms, WindowFormsToExternal> onReceivedWindowRequest;
    internal WindowFormsToExternal HandleWindowRequest(WindowExternalToForms msg) {
      var handler = onReceivedWindowRequest;
      if (handler == null)
        return null;
      return handler(msg);
    }

    public WindowExternalToForms SendMessage(WindowFormsToExternal out_msg) {
      lock (this) {
        if (window_messager_ != null)
          return window_messager_.Send(out_msg, 200);
      }
      return null;
    }

    readonly ElectorGatewayImpl elector_gateway_;
    readonly ServerCredentials server_credentials_;
    Server server_;

    GrpcMessager<WindowFormsToExternal, WindowExternalToForms, WindowMsgSequenceAccessor> window_messager_;
  }

class ElectorGatewayImpl : ElectorGateway.ElectorGatewayBase {

  public override Task WindowControl(IAsyncStreamReader<WindowExternalToForms> inStream,
                                     IServerStreamWriter<WindowFormsToExternal> outStream,
                                     ServerCallContext context) {
    var messager = new GrpcMessager<WindowFormsToExternal, WindowExternalToForms, WindowMsgSequenceAccessor>(outStream, inStream, service_.HandleWindowRequest);
    if (!service_.WindowControl(messager))
      throw new RpcException(new Status(StatusCode.Unauthenticated, "could not authenticate"));
    return messager.Run(context.CancellationToken);
  }

  readonly GrpcService service_;

  public ElectorGatewayImpl(GrpcService grpcService) {
    service_ = grpcService;
  }
}
