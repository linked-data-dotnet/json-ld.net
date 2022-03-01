<Query Kind="Statements">
  <Reference Relative="..\..\src\json-ld.net\bin\Debug\netstandard1.1\json-ld.net.dll">json-ld.net.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>JsonLD.Core</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

#load "Utils/Resources"

var opts = new JsonLdOptions();
var compacted = JsonLdProcessor.Compact(Resources.Doc, Resources.Context, opts);

var expanded = JsonLdProcessor.Expand(compacted);

expanded.ToString().Dump("string");
expanded.Dump("JSON DOM");