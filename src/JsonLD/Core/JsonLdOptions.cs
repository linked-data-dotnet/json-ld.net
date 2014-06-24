using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    /// <summary>http://json-ld.org/spec/latest/json-ld-api/#the-jsonldoptions-type</summary>
    /// <author>tristan</author>
    public class JsonLdOptions
    {
        public JsonLdOptions()
        {
            this.SetBase(string.Empty);
        }

        public JsonLdOptions(string @base)
        {
            this.SetBase(@base);
        }

        public virtual JsonLD.Core.JsonLdOptions Clone()
        {
            JsonLD.Core.JsonLdOptions rval = new JsonLD.Core.JsonLdOptions(GetBase());
            return rval;
        }

        private string @base = null;

        private bool compactArrays = true;

        private JObject expandContext = null;

        private string processingMode = "json-ld-1.0";

        private bool? embed = null;

        private bool? @explicit = null;

        private bool? omitDefault = null;

        internal bool useRdfType = false;

        internal bool useNativeTypes = false;

        private bool produceGeneralizedRdf = false;

        // base options
        // frame options
        // rdf conversion options
        public virtual bool? GetEmbed()
        {
            return embed;
        }

        public virtual void SetEmbed(bool? embed)
        {
            this.embed = embed;
        }

        public virtual bool? GetExplicit()
        {
            return @explicit;
        }

        public virtual void SetExplicit(bool? @explicit)
        {
            this.@explicit = @explicit;
        }

        public virtual bool? GetOmitDefault()
        {
            return omitDefault;
        }

        public virtual void SetOmitDefault(bool? omitDefault)
        {
            this.omitDefault = omitDefault;
        }

        public virtual bool GetCompactArrays()
        {
            return compactArrays;
        }

        public virtual void SetCompactArrays(bool compactArrays)
        {
            this.compactArrays = compactArrays;
        }

        public virtual JObject GetExpandContext()
        {
            return expandContext;
        }

        public virtual void SetExpandContext(JObject expandContext)
        {
            this.expandContext = expandContext;
        }

        public virtual string GetProcessingMode()
        {
            return processingMode;
        }

        public virtual void SetProcessingMode(string processingMode)
        {
            this.processingMode = processingMode;
        }

        public virtual string GetBase()
        {
            return @base;
        }

        public virtual void SetBase(string @base)
        {
            this.@base = @base;
        }

        public virtual bool GetUseRdfType()
        {
            return useRdfType;
        }

        public virtual void SetUseRdfType(bool useRdfType)
        {
            this.useRdfType = useRdfType;
        }

        public virtual bool GetUseNativeTypes()
        {
            return useNativeTypes;
        }

        public virtual void SetUseNativeTypes(bool useNativeTypes)
        {
            this.useNativeTypes = useNativeTypes;
        }

        public virtual bool GetProduceGeneralizedRdf()
        {
            // TODO Auto-generated method stub
            return this.produceGeneralizedRdf;
        }

        public virtual void SetProduceGeneralizedRdf(bool produceGeneralizedRdf)
        {
            this.produceGeneralizedRdf = produceGeneralizedRdf;
        }

        public string format = null;

        public bool useNamespaces = false;

        public string outputForm = null;

        public DocumentLoader documentLoader = new DocumentLoader();
        // TODO: THE FOLLOWING ONLY EXIST SO I DON'T HAVE TO DELETE A LOT OF CODE,
        // REMOVE IT WHEN DONE
    }
}
