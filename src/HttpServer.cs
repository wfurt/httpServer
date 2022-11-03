// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public class HttpServer : IDisposable
    {
        ConnectionListener _listener;
        
        ContentCallback _contentCallback;
        ErrorCallback? _errorCallback;
        ServerOptionsSelectionCallback? _serverOptionsSelectionCallback;
        HttpConnectionPool _pool;
        CancellationTokenSource? _cts;
        Dictionary<Task, Socket> _pendingWork = new Dictionary<Task, Socket>();
        private int _connectionCount;


        //public delegate System.Threading.Tasks.ValueTask<System.Net.Security.SslServerAuthenticationOptions> ServerOptionsSelectionCallback(System.Net.Security.SslStream stream, System.Net.Security.SslClientHelloInfo clientHelloInfo, object? state, System.Threading.CancellationToken cancellationToken);
        //public delegate ValueTask<HttpListenerResponse> ContentCallback(HttpListenerRequest request, HttpListenerResponse response);
        public IPEndPoint LocalEndPoint => _listener.LocalEndPoint;

        public HttpServer(IPEndPoint endpoint, HttpServerOptions httpOptions)
        {
            if (httpOptions.ContentCallback == null)
            {
                throw new ArgumentException("Content handler must not be empty");
            }

            var listenerOptions = new ConnectionListenerOptions(endpoint);
            listenerOptions.ListenBacklog = httpOptions.ListenBacklog;
            listenerOptions.ServerOptionsSelectionCallback = httpOptions.ServerOptionsSelectionCallback;

            _listener = ConnectionListener.Listen(listenerOptions);
            _contentCallback = httpOptions.ContentCallback;
            _errorCallback = httpOptions.ErrorCallback;
            _serverOptionsSelectionCallback = httpOptions.ServerOptionsSelectionCallback;

            _pool = new HttpConnectionPool(this, HttpConnectionKind.Http, host: null, port: LocalEndPoint.Port, sslHostName: null, null);
        }

        private async Task HandleNewConnection(ConnetionStream stream, CancellationToken cancellationToken)
        {
            Console.WriteLine("Got Socket {0} - {1}", stream.LocalEndPoint, stream.RemoteEndPoint);

            // TODO should we push the bellow to separate task? 
            if (_serverOptionsSelectionCallback != null)
            {
                await stream.NegotiateTls(_serverOptionsSelectionCallback, cancellationToken);
            }
            var connection = new HttpConnection(_pool, stream);

            try
            {
                var request = await connection.GetRequestAsync().ConfigureAwait(false);
                var response = new HttpListenerResponse(connection);
                await _contentCallback(request, response).ConfigureAwait(false);
                await connection.WriteResponseAsync(response).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task Start()
        {
            _cts = new CancellationTokenSource();
            while (!_cts.Token.IsCancellationRequested)
            {
                ConnetionStream newStream = await _listener.AcceptConnectionAsync(_cts.Token);
                Interlocked.Increment(ref _connectionCount);

                Task t = HandleNewConnection(newStream, _cts.Token);
                await t.ContinueWith(completed => {
                    Interlocked.Decrement(ref _connectionCount);
                    switch (completed.Status)
                    {
                        //case TaskStatus.RanToCompletion: newStream.Dispose(); break;
                        case TaskStatus.Faulted:
                            {
                                if (_errorCallback != null)
                                {
                                    _errorCallback(newStream, completed.Exception?.InnerException);
                                }
                            }

                            break;
                    };
                    newStream.Dispose();
                }, TaskScheduler.Default);
            }
        }

        public void Dispose() => Dispose(true);
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Cancel();
            }
        }
    }

}
