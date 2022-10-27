// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using System.Collections.Generic;
//using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public partial class HttpListenerRequest
    {
        string _method;
        Version _version;
        readonly string _rawRequest;
        public WebHeaderCollection Headers;

        //public Uri

        public HttpListenerRequest(ReadOnlySpan<byte> requestLine)
        {
            int pos = requestLine.IndexOf((byte)32);
            if (pos < 0)
            {
                throw new HttpRequestException("NO SPACE1");
            }
            _method = Encoding.UTF8.GetString(requestLine.Slice(0, pos));
            requestLine = requestLine.Slice(pos + 1);

            pos = requestLine.IndexOf((byte)32);
            if (pos < 0)
            {
                throw new HttpRequestException("NO SPACE2");
            }
            _rawRequest = Encoding.UTF8.GetString(requestLine.Slice(0, pos));

            _version = new Version(Encoding.UTF8.GetString(requestLine.Slice(pos + 6)));

            Headers = new WebHeaderCollection();
        }
    }
}


