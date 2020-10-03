using System;
using System.Collections;
using System.IO;
using System.Linq;
using JsonLD.Util;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace JsonLD.Util
{
    /// <summary>A bunch of functions to make loading JSON easy</summary>
    /// <author>tristan</author>
    internal class JSONUtils
    {
        static JSONUtils()
        {
        }

        /// <exception cref="Com.Fasterxml.Jackson.Core.JsonParseException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        public static JToken FromString(string jsonString)
        {
            return FromReader(new StringReader(jsonString));
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static JToken FromReader(TextReader r)
        {
            var serializer = new JsonSerializer();
            
            using (var reader = new JsonTextReader(r))
            {
                var result = (JToken)serializer.Deserialize(reader);
                return result;
            }
        }

        /// <exception cref="Com.Fasterxml.Jackson.Core.JsonGenerationException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        public static void Write(TextWriter w, JToken jsonObject)
        {
            var serializer = new JsonSerializer();
            using (var writer = new JsonTextWriter(w))
            {
                serializer.Serialize(writer, jsonObject);
            }
        }

        /// <exception cref="Com.Fasterxml.Jackson.Core.JsonGenerationException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        public static void WritePrettyPrint(TextWriter w, JToken jsonObject)
        {
            var serializer = new JsonSerializer();
            using (var writer = new JsonTextWriter(w))
            {
                writer.Formatting = Formatting.Indented;
                serializer.Serialize(writer, jsonObject);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        public static JToken FromInputStream(Stream content)
        {
            return FromInputStream(content, "UTF-8");
        }

        // no readers from
        // inputstreams w.o.
        // encoding!!
        /// <exception cref="System.IO.IOException"></exception>
        public static JToken FromInputStream(Stream content, string enc)
        {
            return FromReader(new StreamReader(content, System.Text.Encoding.GetEncoding(enc)));
        }

        public static string ToPrettyString(JToken obj)
        {
            StringWriter sw = new StringWriter();
            try
            {
                WritePrettyPrint(sw, obj);
            }
            catch
            {
                // TODO Is this really possible with stringwriter?
                // I think it's only there because of the interface
                // however, if so... well, we have to do something!
                // it seems weird for toString to throw an IOException
                throw;
            }
            return sw.ToString();
        }

        public static string ToString(JToken obj)
        {
            // throws
            // JsonGenerationException,
            // JsonMappingException {
            StringWriter sw = new StringWriter();
            try
            {
                Write(sw, obj);
            }
            catch
            {
                // TODO Is this really possible with stringwriter?
                // I think it's only there because of the interface
                // however, if so... well, we have to do something!
                // it seems weird for toString to throw an IOException
                throw;
            }
            return sw.ToString();
        }

        public static JToken FromURL(Uri url)
        {
            return FromURLAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns a Map, List, or String containing the contents of the JSON
        /// resource resolved from the URL.
        /// </summary>
        /// <remarks>
        /// Returns a Map, List, or String containing the contents of the JSON
        /// resource resolved from the URL.
        /// </remarks>
        /// <param name="url">The URL to resolve</param>
        /// <returns>
        /// The Map, List, or String that represent the JSON resource
        /// resolved from the URL
        /// </returns>
        /// <exception cref="Com.Fasterxml.Jackson.Core.JsonParseException">If the JSON was not valid.
        /// 	</exception>
        /// <exception cref="System.IO.IOException">If there was an error resolving the resource.
        /// 	</exception>
        public static async Task<JToken> FromURLAsync(Uri url)
        {
            using (var response = await LDHttpClient.FetchAsync(url.ToString()).ConfigureAwait(false)) {
                return FromInputStream(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
            }
        }
    }
}
