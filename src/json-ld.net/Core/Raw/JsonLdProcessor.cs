using System;
using System.Collections.Generic;
using System.Text;

namespace JsonLD.Core.Raw
{
    public class JsonLdProcessor
    {
        public static string Compact(string input, string context, JsonLdOptions options)
        {
            throw new NotImplementedException();    
        }

        public static string Expand(string input, JsonLdOptions options)
        {
            throw new NotImplementedException();
        }

        public static string Flatten(string input, string context, JsonLdOptions options)
        {
            throw new NotImplementedException();
        }

        public static string Frame(string input, string frame, JsonLdOptions options)
        {
            throw new NotImplementedException();
        }

        public static object Normalize(string input, JsonLdOptions options)
        {
            throw new NotImplementedException();
        }

        public static string ToRDF(string input, JsonLdOptions options) => throw new NotImplementedException();

        public static string FromRDF(string input, JsonLdOptions options) => throw new NotImplementedException();
    }
}
