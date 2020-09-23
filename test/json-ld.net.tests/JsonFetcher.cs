using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace JsonLD.Test
{
    internal class JsonFetcher
    {
        internal JToken GetJson(JToken j, string rootDirectory)
        {
            try
            {
                if (j == null || j.Type == JTokenType.Null) return null;
                using (Stream manifestStream = File.OpenRead(Path.Combine(rootDirectory, (string)j)))
                using (TextReader reader = new StreamReader(manifestStream))
                using (JsonReader jreader = new JsonTextReader(reader)
                {
                    DateParseHandling = DateParseHandling.None
                })
                {
                    return JToken.ReadFrom(jreader);
                }
            }
            catch (Exception e)
            { // TODO: this should not be here, figure out why this is needed or catch specific exception.
                return null;
            }
        }

        internal static string GetJsonAsString(string folderPath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentOutOfRangeException(nameof(fileName), "Empty or whitespace");
            var filePath = Path.Combine(folderPath, fileName);
            using (var manifestStream = File.OpenRead(filePath))
            using (var reader = new StreamReader(manifestStream))
            {
                return reader.ReadToEnd();
            }
        }        

        internal static string GetRemoteJsonAsString(string input) => throw new NotImplementedException("Not even sure if this should be implemented. Need to double check whether remote test cases are supposed to use the core library to resolve the remote document or whether it's valid for the test case itself to retrieve it");
    }
}