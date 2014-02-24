using System;
using System.Collections;
using System.IO;
using JsonLD.Util;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace JsonLD.Util
{
	/// <summary>A bunch of functions to make loading JSON easy</summary>
	/// <author>tristan</author>
	public class JSONUtils
	{
		/// <summary>An HTTP Accept header that prefers JSONLD.</summary>
		/// <remarks>An HTTP Accept header that prefers JSONLD.</remarks>
		protected internal const string AcceptHeader = "application/ld+json, application/json;q=0.9, application/javascript;q=0.5, text/javascript;q=0.5, text/plain;q=0.2, */*;q=0.1";

		//private static readonly ObjectMapper JsonMapper = new ObjectMapper();

		//private static readonly JsonFactory JsonFactory = new JsonFactory(JsonMapper);

		static JSONUtils()
		{
			// Disable default Jackson behaviour to close
			// InputStreams/Readers/OutputStreams/Writers
			//JsonFactory.Disable(JsonGenerator.Feature.AutoCloseTarget);
			// Disable string retention features that may work for most JSON where
			// the field names are in limited supply, but does not work for JSON-LD
			// where a wide range of URIs are used for subjects and predicates
			//JsonFactory.Disable(JsonFactory.Feature.InternFieldNames);
			//JsonFactory.Disable(JsonFactory.Feature.CanonicalizeFieldNames);
		}

		// private static volatile IHttpClient httpClient;

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
                var result = (JObject)serializer.Deserialize(reader);
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
                serializer.Formatting = Formatting.Indented;
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
        public static JToken FromURL(URL url)
		{
            var req = HttpWebRequest.Create(new Uri(url.ToString()));
            req.Headers.Add("Accept", AcceptHeader);
            WebResponse resp = req.GetResponse();
            Stream stream = resp.GetResponseStream();
            return FromInputStream(stream);
		}
	}
}
