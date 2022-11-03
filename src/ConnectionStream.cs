// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
// Experiment with https://github.com/dotnet/runtime/issues/63162

// TcpConnection was renamed to ConnectionSTream to preserve "connection"
// and address feedback that streams should end with "Stream" and to do more tna just TCP
// Real name may change

using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net
{
    internal class SslStreamWrap : SslStream
    {
        public SslStreamWrap(NetworkStream stream) : base(stream) { }

        // avoid protected InnerStream
        public new NetworkStream InnerStream => (NetworkStream)base.InnerStream;


        // https://github.com/dotnet/runtime/issues/63663
        // If we own the socket we could do Kernel SSL on Linux
        public static Task<SslStreamWrap> ConnectAsync(IPEndPoint destination, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException("NotYeti");
        }
    }

    public class ConnetionStream : NetworkStream
    {
        private SslStreamWrap? _ssl;
        public ConnetionStream(Socket socket) : base(socket, ownsSocket: true)
        {
        }

        private ConnetionStream(SslStreamWrap sslStream) : base(sslStream.InnerStream.Socket)
        {
            _ssl = sslStream;
        }

       
        // Static methods 

        public static async Task<ConnetionStream> ConnectAsync(IPEndPoint destination, CancellationToken cancellationToken = default,  SslClientAuthenticationOptions? sslOptions = null)
        {
            Socket s = new Socket(destination.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await s.ConnectAsync(destination, cancellationToken);

            if (sslOptions == null)
            {
                return new ConnetionStream(s);
            }

            try
            {
                // TODO figure out how to avoid allocation of NetworkStream
                var ssl = new SslStreamWrap(new NetworkStream(s, ownsSocket: true));
                await ssl.AuthenticateAsClientAsync(sslOptions, cancellationToken);

                return new ConnetionStream(ssl);
            }
            catch (Exception)
            {
                s.Dispose();
                throw;
            }
        }

        internal static async Task<ConnetionStream> AcceptAsync(Socket socket, ServerOptionsSelectionCallback? sslOptionCallback, CancellationToken cancellationToken = default)
        {
            if (sslOptionCallback == null)
            {
                return new ConnetionStream(socket);
            }

            var ssl = new SslStreamWrap(new NetworkStream(socket, ownsSocket: true));
            try
            {
                await ssl.AuthenticateAsServerAsync(sslOptionCallback, null, cancellationToken);
                return new ConnetionStream(ssl);
            }
            catch (Exception)
            {
                socket.Dispose();
                throw;
            }
        }

        public async Task NegotiateTls(ServerOptionsSelectionCallback sslOptionCallback, CancellationToken cancellationToken = default)
        {
            if (_ssl != null)
            {
                throw new InvalidOperationException("already done");
            }

            _ssl = new SslStreamWrap(new NetworkStream(Socket, ownsSocket: true));
            await _ssl.AuthenticateAsServerAsync(sslOptionCallback, this, cancellationToken);
        }

        public sealed override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override int Read(Span<byte> buffer) => throw new NotSupportedException();
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return _ssl != null ? _ssl.ReadAsync(buffer, cancellationToken) : Socket.ReceiveAsync(buffer, cancellationToken);
        }

        public sealed override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            Write(buffer.AsSpan(offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_ssl != null)
            {
                _ssl.Write(buffer);
            }
            else
            {
                // Does not work because of magic in NetwrokStream 
                // that makes array and calls the overload above.
                //base.Write(buffer);
                Socket.Send(buffer);
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return _ssl != null ?
                    _ssl.WriteAsync(buffer, cancellationToken) :
                    base.WriteAsync(buffer, cancellationToken);
        }

        public sealed override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }
        public X509Certificate? RemoteCertificate => _ssl != null ? _ssl.RemoteCertificate : null;

        public IPEndPoint? RemoteEndPoint => (_ssl != null ? _ssl.InnerStream.Socket.RemoteEndPoint : Socket.RemoteEndPoint) as IPEndPoint;
        public IPEndPoint? LocalEndPoint => (_ssl != null ? _ssl.InnerStream.Socket.LocalEndPoint : Socket.LocalEndPoint) as IPEndPoint;
    }

    // Mimics QuicListenerOptions
    public class ConnectionListenerOptions
    {
        public int ListenBacklog;
        public IPEndPoint ListenEndPoint;
        public ServerOptionsSelectionCallback? ServerOptionsSelectionCallback;

        public ConnectionListenerOptions(IPEndPoint endPoint)
        {
            ListenEndPoint = endPoint;
            ListenBacklog = 100;        // TODO find platform default...???
        }
    }

    public class ConnectionListener
    {
        internal Socket _socket;
        private ServerOptionsSelectionCallback? _sslOptionCallback;

        internal ConnectionListener(Socket socket, ServerOptionsSelectionCallback? sslOptionCallback = null)
        {
            _socket = socket;
            _sslOptionCallback = sslOptionCallback;
        }

        public static ConnectionListener Listen(IPAddress address, int port) => Listen(new ConnectionListenerOptions(new IPEndPoint(address, port)));
        public static ConnectionListener Listen(ConnectionListenerOptions options)
        { 
            var socket = new Socket(options.ListenEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Bind(options.ListenEndPoint);
                socket.Listen(options.ListenBacklog);

                return new ConnectionListener(socket, options.ServerOptionsSelectionCallback);
            } catch (Exception)
            {
                socket.Dispose();
                throw;
            }
        }

        public IPEndPoint LocalEndPoint => _socket.LocalEndPoint! as IPEndPoint;

        //public TcpConnection AcceptConnection();
        public async ValueTask<ConnetionStream> AcceptConnectionAsync(CancellationToken cancellationToken = default)
        {
            Socket socket = await _socket.AcceptAsync(cancellationToken);

            // TODO we could negotiate TLS here but it would blcok another accept...????

            return new ConnetionStream(socket);
        }
    }
}
