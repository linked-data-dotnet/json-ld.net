using System;
using System.Collections.Generic;

namespace JsonLD.Test
{
    internal class JsonTestCase
    {
        internal string Id { get; private set; }
        internal IEnumerable<string> Type { get; private set; }
        internal string Input { get; private set; }
        internal string Expect { get; private set; }
        internal JsonTestCaseOptions Options { get; private set; }        
        internal string GetInputJson() => throw new NotImplementedException();
        internal string GetContextJson() => throw new NotImplementedException();
        internal string GetFrameJson() => throw new NotImplementedException();
        internal string GetExpectJson() => throw new NotImplementedException();
        internal string GetExpandContextJson() => throw new NotImplementedException();
    }
}