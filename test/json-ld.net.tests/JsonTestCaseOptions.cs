using System;
using System.Collections.Generic;

namespace JsonLD.Test
{
    internal class JsonTestCaseOptions
    {
        private Dictionary<string, object> _options;

        public JsonTestCaseOptions(Dictionary<string, object> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            CompactArrays = options.Optional<bool?>("compactArrays");
            Base = options.Optional<string>("base");
            ExpandContext = options.Optional<string>("expandContext"); // this is temporary and incorrect
            ProduceGeneralizedRdf = options.Optional<bool?>("produceGeneralizedRdf");
            UseNativeTypes = options.Optional<bool?>("useNativeTypes");
            UseRdfType = options.Optional<bool?>("useRdfType");
        }

        internal bool? CompactArrays { get; private set; }
     
        internal string Base { get; private set; }

        internal string ExpandContext { get; private set; }

        internal bool? ProduceGeneralizedRdf { get; private set; }

        internal bool? UseNativeTypes { get; private set; }

        internal bool? UseRdfType { get; private set; }
    }
}