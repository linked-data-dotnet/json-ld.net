using JsonLD.GenericJson;
using System;
using System.IO;
using System.Text.Json;

namespace JsonLD.Test
{
    public class JsonFetcher
    {
        public GenericJsonToken GetJson(GenericJsonToken j, string rootDirectory)
        {
            try
            {
                if (j == null || j.Type == GenericJsonTokenType.Null) return null;
                //using (Stream manifestStream = File.OpenRead(Path.Combine(rootDirectory, (string)j)))
                //using (TextReader reader = new StreamReader(manifestStream))
                //using (JsonReader jreader = new JsonTextReader(reader)
                //{
                //    DateParseHandling = DateParseHandling.None
                //})
                //{
                //    return GenericJsonToken.ReadFrom(jreader);
                //}
                var str = File.ReadAllText(Path.Combine(rootDirectory, (string)j));

                var deserializeOptions = new JsonSerializerOptions();
                deserializeOptions.Converters.Add(new GenericConverter());

                return GenericJsonToken.CreateGenericJsonToken(JsonSerializer.Deserialize<object>(str, deserializeOptions));
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}