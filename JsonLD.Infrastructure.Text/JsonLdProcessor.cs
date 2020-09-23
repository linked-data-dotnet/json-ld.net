using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonLD.Infrastructure.Text
{
    public class JsonLdProcessor
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };

        public static string Compact(string input, string context, JsonLdOptions options)
        {
            var parsedInput = AsJToken(input);
            var parsedContext = AsJToken(context);
            var wrappedOptions = options.AsCore();
            var processed = Core.JsonLdProcessor.Compact(parsedInput, parsedContext, wrappedOptions);
            return processed.ToString();
        }

        public static string Expand(string input, JsonLdOptions options)
        {
            var parsedInput = AsJToken(input);
            var wrappedOptions = options.AsCore();
            var processed = Core.JsonLdProcessor.Expand(parsedInput, wrappedOptions);
            return processed.ToString();
        }

        public static string Flatten(string input, string context, JsonLdOptions options)
        {
            var parsedInput = AsJToken(input);
            var parsedContext = AsJToken(context);
            var wrappedOptions = options.AsCore();
            var processed = Core.JsonLdProcessor.Flatten(parsedInput, parsedContext, wrappedOptions);
            return processed.ToString();
        }

        public static string Frame(string input, string frame, JsonLdOptions options)
        {
            var parsedInput = AsJToken(input);
            var parsedFrame = AsJToken(frame);
            var wrappedOptions = options.AsCore();
            var processed = Core.JsonLdProcessor.Frame(parsedInput, parsedFrame, wrappedOptions);
            return processed.ToString();
        }

        public static string FromRDF(string input, JsonLdOptions options)
        {
            var parsedInput = (JToken)input;
            var wrappedOptions = options.AsCore();
            var processed = Core.JsonLdProcessor.FromRDF(parsedInput, wrappedOptions);
            return processed.ToString();
        }

        public static object Normalize(string input, JsonLdOptions options)
        {
            var parsedInput = AsJToken(input);
            var wrappedOptions = options.AsCore();
            var processed = Core.JsonLdProcessor.Normalize(parsedInput, wrappedOptions);
            return processed;
        }

        public static string ToRDF(string input, JsonLdOptions options)
        {
            var parsedInput = AsJToken(input);
            var wrappedOptions = options.AsCore();
            var processed = Core.JsonLdProcessor.ToRDF(parsedInput, wrappedOptions);
            return processed.ToString();
        }

        private static JToken AsJToken(string json) => json == null ? null : JsonConvert.DeserializeObject<JToken>(json, _settings);
    }
}