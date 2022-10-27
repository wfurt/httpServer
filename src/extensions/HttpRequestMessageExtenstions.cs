
using System.Net.Http;

namespace System.Net.Http
{
    public static class HttpRequestMessageExtenstions
    {
        public static bool HasHeaders(this HttpRequestMessage request)
        {
            return request.Headers != null && request.Headers.Count() > 0;
        }
        /*
        public static void SetVersionWithoutValidation(this HttpRequestMessage request, Version version)
        {
            request.Version = version;
        }
        */
    }
}
