
using System.Net.Http;

namespace System.Net.Http
{
    public static class HttpResponseMessageExtenstions
    {
        public static bool HasHeaders(this HttpResponseMessage request)
        {
            return request.Headers != null && request.Headers.Count() > 0;
        }

        public static void SetVersionWithoutValidation(this HttpResponseMessage response, Version version)
        {
            response.Version = version;
        }

        public static void SetStatusCodeWithoutValidation(this HttpResponseMessage response, HttpStatusCode statusCode)
        {
            response.StatusCode = statusCode;
        }

        public static void SetReasonPhraseWithoutValidation(this HttpResponseMessage response, string? reason)
        {
            response.ReasonPhrase = reason ?? String.Empty;
        }
    }
}
