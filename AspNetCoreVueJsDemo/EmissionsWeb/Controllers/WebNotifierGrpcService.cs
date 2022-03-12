using Emissions.Proto.Notifications;
using Grpc.Core;
using System.Threading.Tasks;

namespace Emissions.Controllers {

    public class WebNotifierGrpcService: Proto.Services.WebNotifier.WebNotifierBase {
        public async override Task Listen(ListenRequest request, IServerStreamWriter<ListenResponse> responseStream, ServerCallContext context) {
            await responseStream.WriteAsync(new ListenResponse() { EntriesChanged = true, ReportsChanged = true });
        }
    }
}
