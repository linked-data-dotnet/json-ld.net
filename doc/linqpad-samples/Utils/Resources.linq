<Query Kind="Program">
  <Reference Relative="..\context.json">context.json</Reference>
  <Reference Relative="..\doc.json">doc.json</Reference>
  <Reference Relative="..\frame.json">frame.json</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

void Main()
{

}

public static class Resources
{
	public static readonly JObject Doc = JObject.Parse(File.ReadAllText(Util.GetFullPath("doc.json")));
	public static readonly JObject Context = JObject.Parse(File.ReadAllText(Util.GetFullPath("context.json")));
	public static readonly JObject Frame = JObject.Parse(File.ReadAllText(Util.GetFullPath("frame.json")));
}
