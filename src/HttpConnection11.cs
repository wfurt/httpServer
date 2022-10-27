// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal partial class HttpConnection
    {
        private readonly static HttpRequestMessage s_request = new HttpRequestMessage();
        //private readonly Socket _socket;
        //private readonly NetworkStream _networkStream;
    
//        internal NetworkStream Stream => _stream is SslStream ? ((AuthenticatedStream)_stream).InnerStream : (NetworkStream)_stream;

//        public IPEndPoint LocalEndPoint => (IPEndPoint)Stream.Socket.LocalEndPoint!;
        //public IPEndPoint RemoteEndPoint => (IPEndPoint)Stream.Socket.RemoteEndPoint!;

        public HttpConnection(HttpConnectionPool pool, Stream stream) : this(pool, stream, null)
        {
            _currentRequest = s_request; // To keep HttpClient code happy
        }

        public async Task<HttpListenerRequest> GetRequestAsync()
        {
            _allowedReadLineBytes = 4000;

            // We use the response code for requests as well since the format and rules are the same.
            var line = await ReadNextResponseHeaderLineAsync(true).ConfigureAwait(false);

            var request = new HttpListenerRequest(line.Span);
            while (true)
            {
                line = await ReadNextResponseHeaderLineAsync(true).ConfigureAwait(false);

                if (IsLineEmpty(line))
                {
                    break;
                }
                ParseHeaderNameValue(line.Span, request);
            }

            // TBD request bodies

            return request;
        }

        private static void ParseHeaderNameValue(ReadOnlySpan<byte> line, HttpListenerRequest request, bool isFromTrailer = false)
        {
            Debug.Assert(line.Length > 0);

            int pos = 0;
            while (line[pos] != (byte)':' && line[pos] != (byte)' ')
            {
                pos++;
                if (pos == line.Length)
                {
                    // Invalid header line that doesn't contain ':'.
                    //throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
                    throw new HttpRequestException("Invalid headr line!");
                }
            }

            if (pos == 0)
            {
                // Invalid empty header name.
                //throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, ""));
                throw new HttpRequestException("net_http_invalid_response_header_name");
            }

            string name = Encoding.ASCII.GetString(line.Slice(0, pos));

            /*
            if (!HeaderDescriptor.TryGet(line.Slice(0, pos), out HeaderDescriptor descriptor))
            {
                // Invalid header name.
                throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_name, Encoding.ASCII.GetString(line.Slice(0, pos))));
            }

            if (isFromTrailer && (descriptor.HeaderType & HttpHeaderType.NonTrailing) == HttpHeaderType.NonTrailing)
            {
                // Disallowed trailer fields.
                // A recipient MUST ignore fields that are forbidden to be sent in a trailer.
                //if (NetEventSource.Log.IsEnabled()) connection.Trace($"Stripping forbidden {descriptor.Name} from trailer headers.");
                return;
            }
            */

            // Eat any trailing whitespace
            while (line[pos] == (byte)' ')
            {
                pos++;
                if (pos == line.Length)
                {
                    // Invalid header line that doesn't contain ':'.
                    //throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
                    throw new HttpRequestException("net_http_invalid_response_header_line");
                }
            }

            if (line[pos++] != ':')
            {
                // Invalid header line that doesn't contain ':'.
                //throw new HttpRequestException(SR.Format(SR.net_http_invalid_response_header_line, Encoding.ASCII.GetString(line)));
                throw new HttpRequestException("net_http_invalid_response_header_line");
            }

            // Skip whitespace after colon
            while (pos < line.Length && (line[pos] == (byte)' ' || line[pos] == (byte)'\t'))
            {
                pos++;
            }

            //Debug.Assert(response.RequestMessage != null);
            // 
            //WTF      //Encoding? valueEncoding = connection._pool.Settings._responseHeaderEncodingSelector?.Invoke(descriptor.Name, response.RequestMessage);

            // Note we ignore the return value from TryAddWithoutValidation. If the header can't be added, we silently drop it.
            ReadOnlySpan<byte> value = line.Slice(pos);
            request.Headers.Set(name, Encoding.ASCII.GetString(value));
        }
   
        internal HttpWriteStream CreateResponseContentStream(HttpListenerResponse response)
        {
            // TBD. we should werify this is ok. Documantion claims Content-length needs to be
            // set before using the stream. It is not clear if this is general requirement.
            WriteResponseHeaders(response);

            HttpContentWriteStream requestContentStream = response.SendChunked ? (HttpContentWriteStream)
                new ChunkedEncodingWriteStream(this) :
                (ContentLengthWriteStream)new ContentLengthWriteStream(this, response.ContentLength64);

            return new HttpWriteStream(requestContentStream);
        }

        private static bool CanSendResponseBody(int responseCode) =>
            // We MUST NOT send message-body when we send responses with these Status codes
            responseCode is not (100 or 101 or 204 or 205 or 304);

        internal void WriteResponseHeaders(HttpListenerResponse response)
        {
            if (!response._headersSent)
            {
                response._headersSent = true;
                response.FixUpHeaders(false);

                WriteAsciiStringAsync($"HTTP/{response.ProtocolVersion.Major}.{response.ProtocolVersion.Minor} {response.StatusCode} {response.StatusDescription}\r\n", false).GetAwaiter().GetResult();

                // TBD We may do better e.g. itterate through the headers
                // The write will either finish synchronously if we have space in output buffer
                // it would block in undrelying synchronous IO
                WriteAsync(response.Headers.ToByteArray(), async: false).GetAwaiter().GetResult();
            }
        }

        internal async Task WriteResponseHeadersAsync(HttpListenerResponse response)
        {
            if (!response._headersSent)
            {
                response._headersSent = true;
                response.FixUpHeaders(false);

                await WriteAsciiStringAsync($"HTTP/{response.ProtocolVersion.Major}.{response.ProtocolVersion.Minor} {response.StatusCode} {response.StatusDescription}\r\n", true);

                // TBD We may do better
                await WriteAsync(response.Headers.ToByteArray(), async: true);
            }
        }
 
        public async Task WriteResponseAsync(HttpListenerResponse response)
        {
            await WriteResponseHeadersAsync(response);
            response._outputStream?.Dispose();
            await FlushAsync(async: true);
        }

        public class HttpWriteStream : Stream
        {
            HttpContentWriteStream _innerStream;
            bool disposed;

            public override bool CanRead => _innerStream.CanRead;
            public override bool CanWrite => _innerStream.CanWrite;
            public override bool CanSeek => _innerStream.CanSeek;
            public override long Position
            {
                get => _innerStream.Position;
                set => _innerStream.Position = value;
            }
            public override long Length => _innerStream.Length;
            public override void SetLength(long value) => _innerStream.SetLength(value);
            public override void Flush() => _innerStream.Flush();
            public override Task FlushAsync(CancellationToken ignored) => _innerStream.FlushAsync();

            public override long Seek(long position, SeekOrigin origin) => _innerStream.Seek(position, origin);

            public sealed override int Read(byte[] buffer, int offset, int count)
            {
                ValidateBufferArguments(buffer, offset, count);
                return Read(buffer.AsSpan(offset, count));
            }

            public sealed override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArguments(buffer, offset, count);
                return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override int Read(Span<byte> buffer) => throw new NotSupportedException();
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();


            public sealed override void Write(byte[] buffer, int offset, int count)
            {
                ValidateBufferArguments(buffer, offset, count);
                Write(buffer.AsSpan(offset, count));
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                _innerStream.Write(buffer);
                _innerStream.Flush();
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                await _innerStream.WriteAsync(buffer, cancellationToken);
                await _innerStream.FlushAsync();
            }
            public sealed override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArguments(buffer, offset, count);
                return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            internal HttpWriteStream(Stream stream)
            {
                _innerStream = (HttpContentWriteStream)stream;
            }

            protected override void Dispose(bool disposing)
            {
                Console.WriteLine("Dispose on PublicHttpContentStream was called {0}", disposing);
                if (disposing && !disposed)
                {
                    disposed = true;
                    try
                    {
                        _innerStream.FinishAsync(async: false).GetAwaiter().GetResult();
                    } 
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                base.Dispose(disposing);
            }
        }
    }
}
