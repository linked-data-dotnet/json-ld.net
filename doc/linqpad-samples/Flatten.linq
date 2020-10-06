<Query Kind="Statements">
  <Reference Relative="..\..\src\json-ld.net\bin\Debug\netstandard1.1\json-ld.net.dll">json-ld.net.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>JsonLD.Core</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

#load "Utils/Resources"

var opts = new JsonLdOptions();
var flattened = JsonLdProcessor.Flatten(Resources.Doc, Resources.Context, opts);

flattened.ToString().Dump("string");
flattened.Dump("JSON DOM");