using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace JsonLD.Test
{
    public class JsonFetcher
    {
        public JToken GetJson(JToken j, string rootDirectory)
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
    }
}