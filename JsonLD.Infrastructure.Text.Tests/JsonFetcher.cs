using System;
using System.Collections.Generic;
using System.IO;

namespace JsonLD.Infrastructure.Text.Tests
{
    internal static class JsonFetcher
    {
        public static JsonTestCases GetTestCases(string manifest, string rootDirectory)
        {
            var json = GetJsonAsString(rootDirectory, manifest);
            var parsed = TinyJson.JSONParser.FromJson<Dictionary<string, object>>(json);
            var sequence = parsed.Required<List<object>>("sequence");
            var name = parsed.Required<string>("name");
            return new JsonTestCases(name, sequence, rootDirectory);
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