
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net.Http.Headers
{
        /// <summary>
        /// Key/value pairs of headers. The value is either a raw <see cref="string"/> or a <see cref="HttpHeaders.HeaderStoreItemInfo"/>.
        /// We're using a custom type instead of <see cref="KeyValuePair{TKey, TValue}"/> because we need ref access to fields.
        /// </summary>
        internal struct HeaderEntry
        {
            public HeaderDescriptor Key;
            public object Value;

            public HeaderEntry(HeaderDescriptor key, object value)
            {
                Key = key;
                Value = value;
            }
        }

        
    public static class HttpHeadersExtenstions
    {
        internal static bool TryAddWithoutValidation(this HttpHeaders headers, HeaderDescriptor descriptor, string? value)
        {
            throw new PlatformNotSupportedException("TryAddWithoutValidation");
            //return false;
        }

        internal static object? GetSingleParsedValue(this HttpHeaders headers, HeaderDescriptor descriptor)
        {
            if (!headers.TryGetAndParseHeaderInfo(descriptor, out HeaderStoreItemInfo? info))
            {
                return null;
            }

            return info.GetSingleParsedValue();
        }

        private static bool TryGetAndParseHeaderInfo(this HttpHeaders headers, HeaderDescriptor key, [NotNullWhen(true)] out HeaderStoreItemInfo? info)
        {
            throw new PlatformNotSupportedException("TryGetAndParseHeaderInfo");
            /*
            ref object storeValueRef = ref GetValueRefOrNullRef(headers, key);
            if (!Unsafe.IsNullRef(ref storeValueRef))
            {
                object value = storeValueRef;
                if (value is HeaderStoreItemInfo hsi)
                {
                    info = hsi;
                }
                else
                {
                    Debug.Assert(value is string);
                    storeValueRef = info = new HeaderStoreItemInfo() { RawValue = value };
                }

                ParseRawHeaderValues(key, info);
                return true;
            }

            info = null;
            return false;
            */
        }

        internal static bool Contains(this HttpHeaders headers, HeaderDescriptor key)
        {
            return !Unsafe.IsNullRef(ref headers.GetValueRefOrNullRef(key));
        }

        internal static ref object GetValueRefOrNullRef(this HttpHeaders headers, HeaderDescriptor key)
        {
            throw new PlatformNotSupportedException("GetValueRefOrNullRef");
            /*
            ref object valueRef = ref Unsafe.NullRef<object>();

            object? store = _headerStore;
            if (store is HeaderEntry[] entries)
            {
                for (int i = 0; i < _count && i < entries.Length; i++)
                {
                    if (key.Equals(entries[i].Key))
                    {
                        valueRef = ref entries[i].Value;
                        break;
                    }
                }
            }
            else if (store is not null)
            {
                valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(Unsafe.As<Dictionary<HeaderDescriptor, object>>(store), key);
            }

            return ref valueRef;
            */
        }

        private static void ParseRawHeaderValues(HeaderDescriptor descriptor, HeaderStoreItemInfo info)
        {
            // Unlike TryGetHeaderInfo() this method tries to parse all non-validated header values (if any)
            // before returning to the caller.
            lock (info)
            {
                Debug.Assert(!info.IsEmpty);
                if (info.RawValue != null)
                {
                    if (info.RawValue is List<string> rawValues)
                    {
                        foreach (string rawValue in rawValues)
                        {
                            ParseSingleRawHeaderValue(info, descriptor, rawValue);
                        }
                    }
                    else
                    {
                        string? rawValue = info.RawValue as string;
                        Debug.Assert(rawValue is not null);
                        ParseSingleRawHeaderValue(info, descriptor, rawValue);
                    }

                    // At this point all values are either in info.ParsedValue, info.InvalidValue. Reset RawValue.
                    Debug.Assert(info.ParsedAndInvalidValues is not null);
                    info.RawValue = null;
                }
            }
        }

        internal static HeaderEntry[]? GetEntriesArray(this HttpHeaders headers)
        {
            throw new PlatformNotSupportedException("GetEntriesArray");
        }

        private static void ParseSingleRawHeaderValue(HeaderStoreItemInfo info, HeaderDescriptor descriptor, string rawValue)
        {
            throw new PlatformNotSupportedException("ParseSingleRawHeaderValue");
            /*
            Debug.Assert(Monitor.IsEntered(info));
            if (descriptor.Parser == null)
            {
                if (HttpRuleParser.ContainsNewLine(rawValue))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, SR.Format(SR.net_http_log_headers_no_newlines, descriptor.Name, rawValue));
                    AddInvalidValue(info, rawValue);
                }
                else
                {
                    AddParsedValue(info, rawValue);
                }
            }
            else
            {
                if (!TryParseAndAddRawHeaderValue(descriptor, info, rawValue, true))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.HeadersInvalidValue(descriptor.Name, rawValue);
                }
            }
            */
        }

    }

    internal sealed class HeaderStoreItemInfo
    {
        internal HeaderStoreItemInfo() { }

        internal object? RawValue;
        internal object? ParsedAndInvalidValues;

        public bool CanAddParsedValue(HttpHeaderParser parser)
        {
            Debug.Assert(parser != null, "There should be no reason to call CanAddValue if there is no parser for the current header.");

            // If the header only supports one value, and we have already a value set, then we can't add
            // another value. E.g. the 'Date' header only supports one value. We can't add multiple timestamps
            // to 'Date'.
            // So if this is a known header, ask the parser if it supports multiple values and check whether
            // we already have a (valid or invalid) value.
            // Note that we ignore the rawValue by purpose: E.g. we are parsing 2 raw values for a header only
            // supporting 1 value. When the first value gets parsed, CanAddValue returns true and we add the
            // parsed value to ParsedValue. When the second value is parsed, CanAddValue returns false, because
            // we have already a parsed value.
            return parser.SupportsMultipleValues || ParsedAndInvalidValues is null;
        }

        [Conditional("DEBUG")]
        public void AssertContainsNoInvalidValues()
        {
            if (ParsedAndInvalidValues is not null)
            {
                if (ParsedAndInvalidValues is List<object> list)
                {
                    foreach (object item in list)
                    {
                        Debug.Assert(item is not InvalidValue);
                    }
                }
                else
                {
                    Debug.Assert(ParsedAndInvalidValues is not InvalidValue);
                }
            }
        }

        public object? GetSingleParsedValue()
        {
            if (ParsedAndInvalidValues is not null)
            {
                if (ParsedAndInvalidValues is List<object> list)
                {
                    AssertContainsSingleParsedValue(list);
                    foreach (object item in list)
                    {
                        if (item is not InvalidValue)
                        {
                            return item;
                        }
                    }
                }
                else
                {
                    if (ParsedAndInvalidValues is not InvalidValue)
                    {
                        return ParsedAndInvalidValues;
                    }
                }
            }

            return null;
        }

        [Conditional("DEBUG")]
        private static void AssertContainsSingleParsedValue(List<object> list)
        {
            int count = 0;
            foreach (object item in list)
            {
                if (item is not InvalidValue)
                {
                    count++;
                }
            }

            Debug.Assert(count == 1, "Only a single parsed value should be stored for this parser");
        }

        public bool IsEmpty => RawValue == null && ParsedAndInvalidValues == null;
    }

    internal sealed class InvalidValue
    {
        private readonly string _value;

        public InvalidValue(string value)
        {
            Debug.Assert(value is not null);
            _value = value;
        }

        public override string ToString() => _value;
    }
}
