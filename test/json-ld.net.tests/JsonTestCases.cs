using System.Collections.Generic;
using System.Linq;

namespace JsonLD.Test
{
    internal class JsonTestCases
    {        
        public JsonTestCases(string name, List<object> sequence, string dataPath)
        {
            Name = name;
            Sequence = sequence.Cast<Dictionary<string, object>>().Select(dictionary => new JsonTestCase(dictionary, dataPath, AreRemoteDocumentTests));
        }

        public bool AreRemoteDocumentTests { get => Name == "Remote document"; } // horrible convention matches existing tests

        internal IEnumerable<JsonTestCase> Sequence { get; private set; }

        internal string Name { get; private set; }
    }
}