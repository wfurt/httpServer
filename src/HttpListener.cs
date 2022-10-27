// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication.ExtendedProtection;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Net.Http
{
    public class HttpListener : IDisposable
    {
        private static readonly ConstructorInfo _HttpListenerPrefixCollectionConstructor = typeof(HttpListenerPrefixCollection).GetConstructor(BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance, new Type[] { typeof(System.Net.HttpListener) })!;

        private bool _disposed;

        private AuthenticationSchemes _authenticationScheme = AuthenticationSchemes.Anonymous;
        private PolicyEnforcement _extendedPolicyType = PolicyEnforcement.Never;
        private ExtendedProtectionPolicy? _extendedProtectionPolicy;
        //private System.Net.HttpListener _listener = new Net.HttpListener();  // we only do this to get access to prefixes
        private HttpListenerPrefixCollection _prefixes;
        private string? _realm;

        public HttpListener()
        {
            //_prefixes = _HttpListenerPrefixCollectionConstructor.Invoke(this);
            _prefixes = new HttpListenerPrefixCollection(this);
        }

        public AuthenticationSchemes AuthenticationSchemes
        {
            get => _authenticationScheme;
            set
            {
                CheckDisposed();
                _authenticationScheme = value;
                throw new PlatformNotSupportedException("NotYeti");
            }
        }

        public ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get => _extendedProtectionPolicy ?? new ExtendedProtectionPolicy(_extendedPolicyType);
            set
            {
                CheckDisposed();
                ArgumentNullException.ThrowIfNull(value);
                if (value.CustomChannelBinding != null)
                {
                    throw new ArgumentException("SR.net_listener_cannot_set_custom_cbt", nameof(value));
                }

                _extendedProtectionPolicy = value;
                throw new PlatformNotSupportedException("NotYeti");
            }
        }

        public ServiceNameCollection DefaultServiceNames => throw new PlatformNotSupportedException("NotYeti");

        public HttpListenerPrefixCollection Prefixes
        {
            get
            {
                CheckDisposed();
                return _prefixes;
            }
        }

        internal void AddPrefix(string prefix)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            CheckDisposed();

            // TODO wierd prefixes? 
            Uri uri = new Uri(prefix);
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new PlatformNotSupportedException(nameof(uri.Scheme));
            }


        }

        public string? Realm
        {
            get => _realm;
            set
            {
                CheckDisposed();
                _realm = value;
            }
        }

        public void Dispose() => Dispose(true);

        public void Dispose(bool disposing)
        {
            _disposed = true;
        }

        internal void CheckDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public class HttpListenerPrefixCollection : List<string>
        {
            HttpListener _listener;

            public HttpListenerPrefixCollection(HttpListener listener)
            {
                _listener = listener;
            }

            public new void Add(string prefix)
            {
                _listener.AddPrefix(prefix);
                base.Add(prefix);
            }
        }
    }

}
