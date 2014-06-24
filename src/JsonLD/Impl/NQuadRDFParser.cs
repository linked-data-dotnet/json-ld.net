using JsonLD.Core;
using JsonLD.Impl;
using Newtonsoft.Json.Linq;

namespace JsonLD.Impl
{
    public class NQuadRDFParser : IRDFParser
    {
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual RDFDataset Parse(JToken input)
        {
            if (input.Type == JTokenType.String)
            {
                return RDFDatasetUtils.ParseNQuads((string)input);
            }
            else
            {
                throw new JsonLdError(JsonLdError.Error.InvalidInput, "NQuad Parser expected string input."
                    );
            }
        }
    }
}
