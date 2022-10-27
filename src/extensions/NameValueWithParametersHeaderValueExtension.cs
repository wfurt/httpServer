using System.Collections.Generic;
#if DEBUG
using System.Collections;
using System.Diagnostics;
using System.Reflection;
#endif

namespace System.Net.Http.Headers
{
    internal static class NameValueWithParametersHeaderValueExtensions

    {
        public static int GetNameValueWithParametersLength(string? input, int startIndex, out object? parsedValue)
        {
            Debug.Assert(input != null);
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            int nameValueLength = NameValueHeaderValueExtensions.GetNameValueLength(input, startIndex,
                 out NameValueHeaderValue? nameValue);

            if (nameValueLength == 0)
            {
                return 0;
            }

            int current = startIndex + nameValueLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);
            NameValueWithParametersHeaderValue? nameValueWithParameters =
                nameValue as NameValueWithParametersHeaderValue;
            Debug.Assert(nameValueWithParameters != null);

            // So far we have a valid name/value pair. Check if we have also parameters for the name/value pair. If
            // yes, parse parameters. E.g. something like "name=value; param1=value1; param2=value2".
            if ((current < input.Length) && (input[current] == ';'))
            {
                current++; // skip delimiter.
                int parameterLength = NameValueHeaderValueExtensions.GetNameValueListLength(input, current, ';',
                    (UnvalidatedObjectCollection<NameValueHeaderValue>)nameValueWithParameters.Parameters);

                if (parameterLength == 0)
                {
                    return 0;
                }

                parsedValue = nameValueWithParameters;
                return current + parameterLength - startIndex;
            }

            // We have a name/value pair without parameters.
            parsedValue = nameValueWithParameters;
            return current - startIndex;
        }
    }
}                        
