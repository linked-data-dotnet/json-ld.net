using JsonLD.Core;
using JsonLD.Impl;

namespace JsonLD.Impl
{
    internal class NQuadTripleCallback : IJSONLDTripleCallback
    {
        public virtual object Call(RDFDataset dataset)
        {
            return RDFDatasetUtils.ToNQuads(dataset);
        }
    }
}
