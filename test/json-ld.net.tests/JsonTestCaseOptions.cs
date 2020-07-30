namespace JsonLD.Test
{
    internal class JsonTestCaseOptions
    {    
        internal bool? CompactArrays { get; private set; }
     
        internal string Base { get; private set; }

        internal string ExpandContext { get; private set; }

        internal bool? ProduceGeneralizedRdf { get; private set; }

        internal bool? UseNativeTypes { get; private set; }

        internal bool? UseRdfType { get; private set; }
    }
}