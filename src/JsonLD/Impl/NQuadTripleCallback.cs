using JsonLD.Core;
using JsonLD.Impl;
using Newtonsoft.Json.Linq;

namespace JsonLD.Impl
{
	public class NQuadTripleCallback : IJSONLDTripleCallback
	{
		public virtual object Call(RDFDataset dataset)
		{
			return RDFDatasetUtils.ToNQuads(dataset);
		}
	}
}
