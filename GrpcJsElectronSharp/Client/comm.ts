import * as grpc from '@grpc/grpc-js';
import * as proto_loader from '@grpc/proto-loader';
import * as path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const PROTO_PATH = __dirname + '/win-ia32-unpacked/resources/protos/gateway.proto';
var packageDefinition = proto_loader.loadSync(
    PROTO_PATH,
    {keepCase: true,
        longs: String,
        defaults: true,
        oneofs: true
  });

export class CommunicationClient {
  public constructor(env: NodeJS.ProcessEnv) {
    let creds = grpc.credentials.createInsecure();
   
    var elector_proto: any = grpc.loadPackageDefinition(packageDefinition);
    this.grpc_client = new elector_proto.ElectorGateway('127.0.0.1:15745', creds);
  }

  public StartWindowControl(handler: (req) => any): Communicator {
    var call = this.grpc_client.WindowControl(new grpc.Metadata({ waitForReady: true }));
    return new Communicator(call, handler);
  }

  private grpc_client: any;
}

export class Communicator {
  public constructor(call, requests_handler: (req) => any) {
    this.grpc_call = call;
    this.requests_handler = requests_handler;
    call.on('data', this.incoming_message_handler.bind(this));
    call.on('error', console.log);
    call.on('end', () => this.grpc_call = null);
  }

  public SendMessage(msg: any, callback: (msg) => void = null): void {
    if (this.grpc_call == null)
      return;
    msg.FormsSequenceNr = this.peer_seq_nr;
    msg.ExternalSequenceNr = ++this.own_seq_nr;
    this.grpc_call.write(msg);
    if (callback != null)
      this.pending_exchange = [this.own_seq_nr, callback];
  }

  private incoming_message_handler(data): void {
    if (data.FormsSequenceNr > this.peer_seq_nr) {
      var response = this.requests_handler(data);
      if (response != null) {
        response.FormsSequenceNr = data.FormsSequenceNr;
        response.ExternalSequenceNr = this.own_seq_nr;
      }
    } else if (this.pending_exchange != null && this.pending_exchange[0] == data.ExternalSequenceNr) {
      this.pending_exchange[1](data);
      this.pending_exchange = null;
    }
    this.peer_seq_nr = data.FormsSequenceNr;
  }

  grpc_call: any;
  requests_handler: (req) => any;
  pending_exchange: [number, (msg) => void];
  own_seq_nr: number = 0;
  peer_seq_nr: number = 0;
}
