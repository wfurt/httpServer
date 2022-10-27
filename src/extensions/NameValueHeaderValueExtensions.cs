using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

namespace System.Net.Http.Headers
{
    internal static class NameValueHeaderValueExtensions
    {
        private static Type runtimeNVHtype = typeof(System.Net.Http.Headers.NameValueHeaderValue);

        //Type
        private static readonly FieldInfo _name = runtimeNVHtype.GetField("_name", BindingFlags.NonPublic | BindingFlags.Instance)!;
        //private static readonly ConstructorInfo ctor = runtimeNVHtype.GetConstructor();


        //Type runtimeNVHype = typeof(System.Net.Http.Headers.NameValueHeaderValue);

        //private static 

        private static readonly Func<NameValueHeaderValue> s_defaultNameValueCreator = CreateNameValue;

        internal static int GetNameValueLength(string input, int startIndex, out NameValueHeaderValue? parsedValue)
        {
            return GetNameValueLength(input, startIndex, s_defaultNameValueCreator, out parsedValue);
        }

        internal static void SetName(this NameValueHeaderValue header, string name)
        {
            //this._name = name;
            _name.SetValue(header, name);
        }

        internal static int GetNameValueLength(string input, int startIndex,
            Func<NameValueHeaderValue> nameValueCreator, out NameValueHeaderValue? parsedValue)
        {
            Debug.Assert(input != null);
            Debug.Assert(startIndex >= 0);
            Debug.Assert(nameValueCreator != null);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the name, i.e. <name> in name/value string "<name>=<value>". Caller must remove
            // leading whitespace.
            int nameLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (nameLength == 0)
            {
                return 0;
            }

            string name = input.Substring(startIndex, nameLength);
            int current = startIndex + nameLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the separator between name and value
            if ((current == input.Length) || (input[current] != '='))
            {
                // We only have a name and that's OK. Return.
                parsedValue = nameValueCreator();
                parsedValue.SetName(name);
                current += HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespace
                return current - startIndex;
            }

            current++; // skip delimiter.
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the value, i.e. <value> in name/value string "<name>=<value>"
            int valueLength = GetValueLength(input, current);

            if (valueLength == 0)
            {
                return 0; // We have an invalid value.
            }

            // Use parameterless ctor to avoid double-parsing of name and value, i.e. skip public ctor validation.
            parsedValue = nameValueCreator();
            parsedValue.SetName(name);
            parsedValue.Value = input.Substring(current, valueLength);
            current += valueLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespace
            return current - startIndex;
        }

        internal static int GetValueLength(string input, int startIndex)
        {
            Debug.Assert(input != null);

            if (startIndex >= input.Length)
            {
                return 0;
            }

            int valueLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (valueLength == 0)
            {
                // A value can either be a token or a quoted string. Check if it is a quoted string.
                if (HttpRuleParser.GetQuotedStringLength(input, startIndex, out valueLength) != HttpParseResult.Parsed)
                {
                    // We have an invalid value. Reset the name and return.
                    return 0;
                }
            }
            return valueLength;
        }


        // Returns the length of a name/value list, separated by 'delimiter'. E.g. "a=b, c=d, e=f" adds 3
        // name/value pairs to 'nameValueCollection' if 'delimiter' equals ','.
        internal static int GetNameValueListLength(string? input, int startIndex, char delimiter,
            UnvalidatedObjectCollection<NameValueHeaderValue> nameValueCollection)
        {
            Debug.Assert(nameValueCollection != null);
            Debug.Assert(startIndex >= 0);

            if ((string.IsNullOrEmpty(input)) || (startIndex >= input.Length))
            {
                return 0;
            }

            int current = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
            while (true)
            {
                NameValueHeaderValue? parameter;
                int nameValueLength = NameValueHeaderValueExtensions.GetNameValueLength(input, current,
                    s_defaultNameValueCreator, out parameter);

                if (nameValueLength == 0)
                {
                    return 0;
                }

                nameValueCollection.Add(parameter!);
                current += nameValueLength;
                current += HttpRuleParser.GetWhitespaceLength(input, current);

                if ((current == input.Length) || (input[current] != delimiter))
                {
                    // We're done and we have at least one valid name/value pair.
                    return current - startIndex;
                }

                // input[current] is 'delimiter'. Skip the delimiter and whitespace and try to parse again.
                current++; // skip delimiter.
                current += HttpRuleParser.GetWhitespaceLength(input, current);
            }
        }

        private static NameValueHeaderValue CreateNameValue()
        {
            return (NameValueHeaderValue)Activator.CreateInstance(runtimeNVHtype)!;
        }

        internal static NameValueHeaderValue? Find(UnvalidatedObjectCollection<NameValueHeaderValue>? values, string name)
        {
            Debug.Assert((name != null) && (name.Length > 0));

            if ((values == null) || (values.Count == 0))
            {
                return null;
            }

            foreach (var value in values)
            {
                if (string.Equals(value.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }
            return null;
        }

    }
}

