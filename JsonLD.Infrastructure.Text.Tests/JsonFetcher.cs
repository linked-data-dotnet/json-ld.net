using System.Collections.Generic;

namespace JsonLD.Infrastructure.Text.Tests
{
    internal static class JsonFetcher
    {
        public static JsonTestCases GetTestCases(string manifest, string rootDirectory)
        {
            var json = Test.JsonFetcher.GetJsonAsString(rootDirectory, manifest);
            var parsed = TinyJson.JSONParser.FromJson<Dictionary<string, object>>(json);
            var sequence = parsed.Required<List<object>>("sequence");
            var name = parsed.Required<string>("name");
            return new JsonTestCases(name, sequence, rootDirectory);
        }
    }
}