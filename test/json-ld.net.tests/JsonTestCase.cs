using System.Collections.Generic;

namespace JsonLD.Test
{
    internal class JsonTestCase
    {
        private readonly string _dataPath;
        private readonly bool _isRemoteDocumentTest;

        public JsonTestCase(Dictionary<string, object> dictionary, string dataPath, bool isRemoteDocumentTest)
        {
            Id = dictionary.Required<string>("@id");
            Type = dictionary.Optional<List<string>>("@type");
            Input = dictionary.Required<string>("input");
            Expect = dictionary.Required<string>("expect");
            Context = dictionary.Optional<string>("context");
            Frame = dictionary.Optional<string>("frame");
            _dataPath = dataPath;
            _isRemoteDocumentTest = isRemoteDocumentTest;
        }

        internal string Context { get; }
        
        internal string Expect { get; }
        
        internal string Frame { get; }
        
        internal string Id { get; }
        
        internal string Input { get; }
        
        internal JsonTestCaseOptions Options { get; }

        internal IEnumerable<string> Type { get; }

        internal string GetContextJson() => Context == null ? null : JsonFetcher.GetJsonAsString(_dataPath, Context);

        internal string GetExpandContextJson() => Options?.ExpandContext == null ? null : JsonFetcher.GetJsonAsString(_dataPath, Options.ExpandContext);

        internal string GetExpectJson() => Expect == null ? null : JsonFetcher.GetJsonAsString(_dataPath, Expect);

        internal string GetFrameJson() => Frame == null ? null : JsonFetcher.GetJsonAsString(_dataPath, Frame);

        internal string GetInputJson() => _isRemoteDocumentTest
            ? JsonFetcher.GetRemoteJsonAsString(Input)
            : JsonFetcher.GetJsonAsString(_dataPath, Input);
    }
}