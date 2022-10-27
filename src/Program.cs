// See https://aka.ms/new-console-template for more information
using System;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;


namespace TestServer 
{
    public class Program
    {
        private static bool _debug = true;
//        private static async ValueTask<System.Net.Http.HttpListenerResponse> RequestHandler(System.Net.Http.HttpListenerRequest request, System.Net.Http.HttpListenerResponse response)
        private static ValueTask<System.Net.Http.HttpListenerResponse> RequestHandler(System.Net.Http.HttpListenerRequest request, System.Net.Http.HttpListenerResponse response)

        {
            response.Headers.Set("Content-Type", "text/plain");
            response.ContentLength64 = 5;
            byte[] buffer = Encoding.UTF8.GetBytes("Hello there!\n");
            response.SendChunked = new Random().Next(1, 3) == 2;
            if (!response.SendChunked)
            {
                response.ContentLength64 = buffer.Length;
            }

            using Stream stream = response.OutputStream;

            //await stream.WriteAsync(buffer, default);
            //return response;
            // sync version
            stream.Write(buffer, 0, buffer.Length);
            return ValueTask.FromResult<System.Net.Http.HttpListenerResponse>(response);
        }

        private static void ErrorHandler(ConnetionStream connection, Exception ex)
        {
            Console.WriteLine("Connection {0} git errror {1}", connection, ex.Message);
            if (_debug)
            {
                Console.WriteLine(ex);
            }
        }
        private static System.Net.Http.HttpListener Legacy()
        {
            var listener = new System.Net.Http.HttpListener();
            Console.WriteLine(listener.Prefixes);

            return listener;
        }

        public static async Task Main(string[] args)
        {
            X509Certificate2 serverCert = new X509Certificate2("server.pfx", "password");
            SslServerAuthenticationOptions sslOptions = new SslServerAuthenticationOptions()
            { 
                EnabledSslProtocols = SslProtocols.Tls12,
                ServerCertificateContext = SslStreamCertificateContext.Create(serverCert, null, offline: true)
            };

            // Plain HTTP server
            HttpServerOptions options = new HttpServerOptions() {  ContentCallback = RequestHandler, ErrorCallback = ErrorHandler };
            
            HttpServer httpServer = new HttpServer(new IPEndPoint(IPAddress.Any, 11000), options);

            // Add HTTPS
            options.ServerOptionsSelectionCallback = (stream, clientHelloInfo, state, cancellationToken) =>
            {
                return ValueTask.FromResult<SslServerAuthenticationOptions>(sslOptions);
            };
            HttpServer httpsServer = new HttpServer(new IPEndPoint(IPAddress.Any, 12000), options);

            var lister = Legacy();
                 

            Task[] acceptTasks = new Task[] { httpServer.Start(), httpsServer.Start() };
            Console.WriteLine("Presss ENTER to exit....");
            Console.ReadLine();
            // This will wake up the acceopting tasks.
            httpServer.Dispose();
            httpsServer.Dispose();
            await Task.WhenAll(acceptTasks);
        }
    }
}


