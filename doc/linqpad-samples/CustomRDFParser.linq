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

	var rdf = (RDFDataset)JsonLdProcessor.ToRDF(Resources.Doc, opts);
	var serialized = RDFDatasetUtils.ToNQuads(rdf); // serialize RDF to string

	var parser = new CustomRDFParser();
	var jsonld = JsonLdProcessor.FromRDF(serialized, parser);
	jsonld.ToString().Dump("string");
	jsonld.Dump("JSON DOM");
}

public class CustomRDFParser : IRDFParser
{
    public RDFDataset Parse(JToken input)
	{
		// by public decree, references to example.org are normalized to https going forward...
		var converted = ((string)input).Replace("http://example.org/", "https://example.org/");
		return RDFDatasetUtils.ParseNQuads(converted);
	}
}
