package pro.elector.kaczatko.connection;

import android.util.Log;

import com.google.protobuf.ByteString;
import com.squareup.okhttp.ConnectionSpec;

import java.io.Closeable;
import java.io.IOException;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.concurrent.atomic.AtomicReference;

import javax.net.ssl.SSLContext;

import io.grpc.ManagedChannel;
import io.grpc.Metadata;
import io.grpc.okhttp.OkHttpChannelBuilder;
import io.grpc.stub.MetadataUtils;
import io.grpc.stub.StreamObserver;
import pro.elector.proto.ElectorTunnelGrpc;
import pro.elector.proto.Tunnel;

public class SocketServerToGrpcTunnel implements Closeable {
    public SocketServerToGrpcTunnel(long target_id, SSLContext ssl_ctx, String tunnel_host, int tunnel_port, int serving_port) {
        target_id_ = target_id;
        serving_port_ = serving_port;
        channel_ = OkHttpChannelBuilder.forAddress(tunnel_host, tunnel_port)
                .useTransportSecurity()
                .connectionSpec(ConnectionSpec.MODERN_TLS)
                .sslSocketFactory(ssl_ctx.getSocketFactory())
                .keepAliveWithoutCalls(true)
                .build();
        Metadata metadata = new Metadata();
        metadata.put(Metadata.Key.of("target_id", Metadata.ASCII_STRING_MARSHALLER), Long.toString(target_id));
        async_stub_ = MetadataUtils.attachHeaders(ElectorTunnelGrpc.newStub(this.channel_), metadata);
    }

    public void start() {
        Log.i("tunnel", "Starting tunnel maintenance thread");
        System.out.println();
        execution_thread_.start();
    }
    public void close() {
        tunnel_closed(true);
        channel_.shutdown();
        try {
            execution_thread_.join();
        } catch (InterruptedException e) {
        }
    }

    private class TunnelMessageStreamObserver implements StreamObserver<Tunnel.TunnelMessage> {
        @Override public void onNext(Tunnel.TunnelMessage message) {
            Log.i("tunnel", "Received message " + message);
            if (message.getPayload() == null || message.getPayload().isEmpty())
                change_elector_availability();
            else
                send_through_socket(message);
        }

        @Override public void onError(Throwable t) {
            Log.w("tunnel", "error on tunnel", t);
            tunnel_closed(false);
        }

        @Override public void onCompleted() {
            tunnel_closed(false);
        }
    }

    private void execution_loop() {
        while (true) {
            // Always keep connection alive
            if (!start_call()) break;

            byte[] in_bytes = new byte[4096];
            while (wait_for_available_elector()) {
                try (ServerSocket server = new ServerSocket(serving_port_);
                     Socket socket = server.accept()) {
                    Log.i("tunnel", "accepted socket connection");
                    socket_.set(socket);
                    send_through_tunnel(ByteString.EMPTY);

                    int num_read;
                    while ((num_read = read_from_socket(in_bytes)) > 0)
                        send_through_tunnel(ByteString.copyFrom(in_bytes, 0, num_read));
                } catch (IOException e) {
                    Log.w("tunnel", "Error while handling socket", e);
                } finally {
                    Log.i("tunnel", "closing socket connection");
                    mark_socket_close();
                }
            }
        }
    }

    private synchronized boolean start_call() {
        if (stop_all_)
            return false;
        Log.i("tunnel", "Attempting to create terminal channel");
        tunnel_sender_ = async_stub_.openChannelAsClient(new TunnelMessageStreamObserver());
        return true;
    }

    private synchronized void change_elector_availability() {
        elector_available_ = !elector_available_;
        this.notify();
    }

    private synchronized boolean wait_for_available_elector() {
        while (!elector_available_ && !stop_all_ && tunnel_sender_ != null) {
            try {
                this.wait();
            } catch (InterruptedException e) {
                break;
            }
        }
        return elector_available_;
    }

    private synchronized void send_through_tunnel(ByteString bytes) {
        if (tunnel_sender_ == null)
                return;
        Log.i("tunnel", "sending message with " + bytes);
        tunnel_sender_.onNext(Tunnel.TunnelMessage.newBuilder()
                .setTargetId(target_id_)
                .setPayload(bytes)
                .build());
    }

    private synchronized void tunnel_closed(boolean stop_requested) {
        if (stop_requested) {
            stop_all_ = true;
            if (tunnel_sender_ != null)
                tunnel_sender_.onCompleted();
        }
        tunnel_sender_ = null;
        socket_.set(null);
        elector_available_ = false;
        this.notify();
    }

    private int read_from_socket(byte[] in_bytes) throws IOException {
        Socket socket = socket_.get();
        if (socket != null)
            return socket.getInputStream().read(in_bytes);
        return 0;
    }

    private void send_through_socket(Tunnel.TunnelMessage message) {
        Socket socket = socket_.get();
        if (socket == null)
            return;
        try {
            message.getPayload().writeTo(socket.getOutputStream());
        } catch (IOException e) {
            Log.w("tunnel", "Error sending to socket", e);
            mark_socket_close();
        }
    }

    private void mark_socket_close() {
        if (socket_.getAndSet(null) != null)
            send_through_tunnel(ByteString.EMPTY);
    }

    private final long target_id_;
    private final int serving_port_;
    private final ManagedChannel channel_;
    private final ElectorTunnelGrpc.ElectorTunnelStub async_stub_;
    private final Thread execution_thread_ = new Thread(this::execution_loop);

    private volatile StreamObserver<Tunnel.TunnelMessage> tunnel_sender_;
    private final AtomicReference<Socket> socket_ = new AtomicReference<>();

    private boolean elector_available_ = false;
    private boolean stop_all_ = false;
}
