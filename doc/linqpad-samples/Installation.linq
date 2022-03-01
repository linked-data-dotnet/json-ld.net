<Query Kind="Statements">
  <Reference Relative="..\..\src\json-ld.net\bin\Debug\netstandard1.1\json-ld.net.dll">json-ld.net.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>JsonLD.Core</Namespace>
</Query>

var json = "{'@context':{'test':'http://www.example.org/'},'test:hello':'world'}";
var document = JObject.Parse(json);
var expanded = JsonLdProcessor.Expand(document);

expanded.ToString().Dump("string");
expanded.Dump("JSON DOM");