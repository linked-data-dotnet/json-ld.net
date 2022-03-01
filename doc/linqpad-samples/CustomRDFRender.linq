<Query Kind="Program">
  <Reference Relative="..\..\src\json-ld.net\bin\Debug\netstandard1.1\json-ld.net.dll">json-ld.net.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>JsonLD.Core</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

#load "Utils/Resources"

void Main()
{
	var opts = new JsonLdOptions();

	var callback = new JSONLDTripleCallback();
	var serialized = JsonLdProcessor.ToRDF(Resources.Doc, callback);
	serialized.Dump("RDF");
}

public class JSONLDTripleCallback : IJSONLDTripleCallback
{
	public object Call(RDFDataset dataset) =>
		RDFDatasetUtils.ToNQuads(dataset); // serialize the RDF dataset as NQuads
}
