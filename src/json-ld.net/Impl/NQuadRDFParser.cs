using JsonLD.Core;
using JsonLD.OmniJson;
using JsonLD.Impl;

namespace JsonLD.Impl
{
    internal class NQuadRDFParser : IRDFParser
    {
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual RDFDataset Parse(OmniJsonToken input)
        {
            if (input.Type == OmniJsonTokenType.String)
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
