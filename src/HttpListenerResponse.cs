// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using static System.Net.Http.HttpConnection;

namespace System.Net.Http
{
    public partial class HttpListenerResponse : IDisposable
    {
        private bool _disposed; // TBF
        private bool _sendChunked = false;
        private WebHeaderCollection _headers = new WebHeaderCollection();
        private long _contentLength;
        private CookieCollection? _cookies;
        private Version _version = HttpVersion.Version11;   // TODO FIX 1.1
        private int _statusCode = (int)HttpStatusCode.OK;
        private string? _statusDescription;
        internal HttpWriteStream? _outputStream;
        private HttpConnection _connection;
        internal bool _headersSent;

        //       string _method;
        //       Version _version;
        //       readonly string _rawRequest;
        //       public WebHeaderCollection Headers;

        //public Uri

        public Version ProtocolVersion
        {
            get => _version;
            set
            {
                CheckDisposed();
                ArgumentNullException.ThrowIfNull(value);
                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                {
                    //throw new ArgumentException(SR.net_wrongversion, nameof(value));
                    throw new ArgumentException("net_wrongversion");
                }

                _version = new Version(value.Major, value.Minor); // match Windows behavior, trimming to just Major.Minor
            }
        }
        public WebHeaderCollection Headers
        {
            get => _headers;
            set
            {
                _headers = new WebHeaderCollection();
                foreach (string headerName in value.AllKeys)
                {
                    _headers.Add(headerName, value[headerName]);
                }
            }
        }

        public CookieCollection Cookies
        {
            get => _cookies ??= new CookieCollection();
            set => _cookies = value;
        }

        public long ContentLength64
        {
            get => _contentLength;
            set
            {
                CheckDisposed();
                if (value >= 0)
                {
                    _contentLength = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "SR.net_clsmall");
                }
            }
        }
        public string StatusDescription
        {
            get
            {
                // if the user hasn't set this, generate on the fly, if possible.
                // We know this one is safe, no need to verify it as in the setter.
                return _statusDescription ??= HttpStatusDescription.Get(StatusCode) ?? string.Empty;
            }
            set
            {
                //CheckDisposed();
                ArgumentNullException.ThrowIfNull(value);

                // Need to verify the status description doesn't contain any control characters except HT.  We mask off the high
                // byte since that's how it's encoded.
                for (int i = 0; i < value.Length; i++)
                {
                    char c = (char)(0x000000ff & (uint)value[i]);
                    if ((c <= 31 && c != (byte)'\t') || c == 127)
                    {
                        //throw new ArgumentException(SR.net_WebHeaderInvalidControlChars, nameof(value));
                        throw new ArgumentException("net_WebHeaderInvalidControlChars");
                    }
                }

                _statusDescription = value;
            }
        }

        public int StatusCode
        {
            get => _statusCode;
            set
            {
                CheckDisposed();

                if (value < 100 || value > 999)
                {
                    throw new ProtocolViolationException("net_invalidstatus");
                }
                //   throw new ProtocolViolationException(SR.net_invalidstatus);

                _statusCode = value;
            }
        }

        public bool SendChunked
        {
            get => _sendChunked;
            set => _sendChunked = value;
        }
      
        public Stream OutputStream
        {
            get
            {
                if (_outputStream == null)
                {
                    _outputStream = _connection.CreateResponseContentStream(this);
                }

                return _outputStream;
            }
        }

        internal HttpListenerResponse(HttpConnection connection)
        {
            _connection = connection;
        }

        public void FixUpHeaders(bool closing)
        { 
            if (_headers[HttpKnownHeaderNames.Server] == null)
            {
                _headers.Set(HttpKnownHeaderNames.Server, "HttpHeaderStrings.NetCoreServerName");
            }

            if (_headers[HttpKnownHeaderNames.Date] == null)
            {
                _headers.Set(HttpKnownHeaderNames.Date, DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture));
            }

            /*
            //if (_boundaryType == BoundaryType.None)
            {
                if (_version <= HttpVersion.Version10)
                {
                    _keepAlive = false;
                }
                else
                {
                    //_boundaryType = BoundaryType.Chunked;
                    _sendChunked = true;
                }

                if (CanSendResponseBody(_statusCode))
                {
                    _contentLength = -1;
                }
                else
                {
                    //_boundaryType = BoundaryType.ContentLength;
                    _contentLength = 0;
                }
            }
            */
            //if (_boundaryType != BoundaryType.Chunked)
            if (SendChunked)
            {
                _headers.Set(HttpKnownHeaderNames.TransferEncoding, "chunked");
            }
            else
                {
                    //if (_boundaryType != BoundaryType.ContentLength && closing)
                    //{
                    //    _contentLength = CanSendResponseBody(_httpContext!.Response.StatusCode) ? -1 : 0;
                    //}

                    //if (_boundaryType == BoundaryType.ContentLength)
                    {
                        _headers.Set(HttpKnownHeaderNames.ContentLength, _contentLength.ToString("D", CultureInfo.InvariantCulture));
                    }
                }
            
        }   

        /*
        private static bool CanSendResponseBody(int responseCode) =>
            // We MUST NOT send message-body when we send responses with these Status codes
            responseCode is not (100 or 101 or 204 or 205 or 304);
        */

        public void Dispose() => Dispose(true);
        public void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
                _disposed = true;
                if (_outputStream != null)
                {
                    _outputStream.Dispose();
                    _outputStream = null;
                }
        }

        private void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

    }
}


