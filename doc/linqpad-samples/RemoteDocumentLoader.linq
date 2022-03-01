<Query Kind="Program">
  <Reference Relative="..\..\src\json-ld.net\bin\Debug\netstandard1.1\json-ld.net.dll">json-ld.net.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>JsonLD.Core</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

#load "Utils/Resources"

void Main()
{
	var doc = Resources.Doc;
	var remoteContext = JObject.Parse("{'@context':'http://example.org/context.jsonld'}");
	var opts = new JsonLdOptions { documentLoader = new CustomDocumentLoader() };
	var compacted = JsonLdProcessor.Compact(doc, remoteContext, opts);

	compacted.ToString().Dump("string");
	compacted.Dump("JSON DOM");
}

public class CustomDocumentLoader : DocumentLoader
{
	private static readonly string _cachedExampleOrgContext = Resources.Context.ToString();

	public override RemoteDocument LoadDocument(string url)
	{
		if (url == "http://example.org/context.jsonld") // we have this cached locally
		{
			var doc = new JObject(new JProperty("@context", JObject.Parse(_cachedExampleOrgContext)));
			return new RemoteDocument(url, doc);
		}
		else
		{
			return base.LoadDocument(url);
		}
	}
}
