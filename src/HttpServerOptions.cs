using System.Net.Security;

namespace System.Net.Http
{
    public delegate ValueTask<HttpListenerResponse> ContentCallback(HttpListenerRequest request, HttpListenerResponse response);
    public delegate void ErrorCallback(ConnetionStream stream, Exception exception);
    public class HttpServerOptions
    {
        internal TimeSpan _maxResponseDrainTime = TimeSpan.FromSeconds(2);
        internal int _maxResponseDrainSize = 1024 * 1024;
        internal HeaderEncodingSelector<HttpRequestMessage>? _requestHeaderEncodingSelector;
        internal  HeaderEncodingSelector<HttpRequestMessage>? _responseHeaderEncodingSelector;
        internal int _maxResponseHeadersLength = 4000;

        public int ListenBacklog = 100;

        public ServerOptionsSelectionCallback? ServerOptionsSelectionCallback;
        public ContentCallback? ContentCallback;
        public ErrorCallback? ErrorCallback;

        public int MaxResponseHeadersByteLength => (int)Math.Min(int.MaxValue, _maxResponseHeadersLength * 1024L);
    }
}
