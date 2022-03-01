<Query Kind="Statements">
  <Reference Relative="..\..\src\json-ld.net\bin\Debug\netstandard1.1\json-ld.net.dll">json-ld.net.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>JsonLD.Core</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

#load "Utils/Resources"
#load "Utils/ObjectDumper"

var opts = new JsonLdOptions();
var normalized = (RDFDataset)JsonLdProcessor.Normalize(Resources.Doc, opts);

normalized.JsonLDDump().Dump("string");
normalized.Dump("JSON DOM");