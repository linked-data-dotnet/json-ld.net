using JsonLD.Core;
using JsonLD.GenericJson;
using JsonLD.Impl;
using Newtonsoft.Json.Linq;

namespace JsonLD.Impl
{
    internal class NQuadRDFParser : IRDFParser
    {
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual RDFDataset Parse(GenericJsonToken input)
        {
            if (input.Type == GenericJsonTokenType.String)
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
