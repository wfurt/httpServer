// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using HttpServer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Http.QPack;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides a pool of connections to the same endpoint.</summary>
    internal sealed class HttpConnectionPool : IDisposable
    {
        private readonly HttpConnectionKind _kind;
        public byte[]? HostHeaderValueBytes;    // to keep HttpConnection happy
        public HttpConnectionKind Kind => _kind;

        public HttpServerOptions Settings = new HttpServerOptions();

        public HttpConnectionPool(object poolManager, HttpConnectionKind kind, string? host, int port, string? sslHostName, Uri? proxyUri)
        {
            _kind = kind;
        }

        public void InvalidateHttp11Connection(HttpConnection connection, bool disposing)
        {

        }

        public void RecycleHttp11Connection(HttpConnection connection)
        {

        }

        public void Dispose() => Dispose(true);

        internal void Dispose(bool disposing)
        {

        }
    }
}
