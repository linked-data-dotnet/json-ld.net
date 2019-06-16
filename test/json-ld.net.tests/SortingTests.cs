using JsonLD.Core;
using JsonLD.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace JsonLD.Test
{
    public class SortingTests
    {
        [Theory, MemberData(nameof(SortingTestCases))]
        public void RunJsonLdProcessor(string id, SortingTestCase testCase)
        {
            JToken result = testCase.run();
            if (testCase.error != null)
            {
                Assert.True(((string)result["error"]).StartsWith((string)testCase.error), "Resulting error doesn't match expectations.");
            }
            else
            {
                if (!JsonLdUtils.DeepCompare(result, testCase.output))
                {
#if DEBUG
                    Console.WriteLine(id);
                    Console.WriteLine("Actual:");
                    Console.Write(JSONUtils.ToPrettyString(result));
                    Console.WriteLine("--------------------------");
                    Console.WriteLine("Expected:");
                    Console.Write(JSONUtils.ToPrettyString(testCase.output));
                    Console.WriteLine("--------------------------");
#endif

                    Assert.True(false, "Returned JSON doesn't match expectations.");
                }
            }
        }

        public class SortingTestCase
        {
            public JToken  input { get; set; }
            public JToken context { get; set; }
            public JToken output { get; set; }
            public JToken frame { get; set; }
            public JToken error { get; set; }
            public Func<JToken> run { get; set; }
        }

        private static string[] GetManifests()
        {
            return new[] {
            //"compact-manifest.jsonld"
            //"expand-manifest.jsonld",
            //"flatten-manifest.jsonld",
            //"frame-manifest.jsonld",
            //"toRdf-manifest.jsonld",
            "fromRdf-manifest.jsonld",
            //"normalize-manifest.jsonld"
            };
        }

        public static IEnumerable<object[]> SortingTestCases()
        {
            var manifests = GetManifests();
            var jsonFetcher = new JsonFetcher();
            var rootDirectory = Path.Combine("Sorting", "W3C");

            foreach (string manifest in manifests)
            {
                JToken manifestJson = jsonFetcher.GetJson(manifest, rootDirectory);

                foreach (JObject testcase in manifestJson["sequence"])
                {
                    Func<JToken> run;
                    SortingTestCase newCase = new SortingTestCase();

                    newCase.input = jsonFetcher.GetJson(manifestJson["input"], rootDirectory);
                    newCase.output = jsonFetcher.GetJson(testcase["expect"], rootDirectory);
                    newCase.context = jsonFetcher.GetJson(manifestJson["test-context"], rootDirectory);

                    var options = new JsonLdOptions();
                    
                    var sortType = (string)testcase["sort-type"];

                    if (sortType == "jld:GraphsAndNodes")
                    {
                        options.SetSortGraphs(true);
                        options.SetSortGraphNodes(true);
                    }
                    else if (sortType == "jld:Graphs")
                    {
                        options.SetSortGraphs(true);
                        options.SetSortGraphNodes(false);
                    }
                    else if (sortType == "jld:Nodes")
                    {
                        options.SetSortGraphs(false);
                        options.SetSortGraphNodes(true);
                    }
                    else if (sortType == "jld:None")
                    {
                        options.SetSortGraphs(false);
                        options.SetSortGraphNodes(false);
                    }

                    JsonLdApi jsonLdApi = new JsonLdApi(options);
                    Context activeCtx = new Context(newCase.context, options);

                    var testType = (string)testcase["test-type"];

                    //if (testType == "jld:Compact")
                    {
                      //  run = () => jsonLdApi.FromRDF(rdf);
                    }
                    //else if (testType == "jld:Expand")
                    //{
                    //    //run = () => JsonLdProcessor.Expand(newCase.input, options);
                    //}
                    //else if (testType == "jld:Flatten")
                    //{
                    //    //run = () => JsonLdProcessor.Flatten(newCase.input, newCase.context, options);
                    //}
                    //else if (testType == "jld:Frame")
                    //{
                    //    //run = () => JsonLdProcessor.Frame(newCase.input, newCase.frame, options);
                    //}
                    //else if (testType == "jld:Normalize")
                    //{
                    //    run = () => new JValue(
                    //            RDFDatasetUtils.ToNQuads((RDFDataset)JsonLdProcessor.Normalize(newCase.input, options)).Replace("\n", "\r\n")
                    //        );
                    //}
                    //else if (testType == "jld:ToRDF")
                    //{
                    //    options.format = "application/nquads";
                    //    //run = () => new JValue(
                    //    //    ((string)JsonLdProcessor.ToRDF(newCase.input, options)).Replace("\n", "\r\n")
                    //    //);
                    //}
                    //else if (testType == "jld:FromRDF")
                    {
                        JToken quads = newCase.input["quads"];
                        RDFDataset rdf = new RDFDataset();

                        foreach (JToken quad in quads)
                        {
                            string subject = (string)quad["subject"];
                            string predicate = (string)quad["predicate"];
                            string value = (string)quad["value"];
                            string graph = (string)quad["graph"];

                            rdf.AddQuad(subject, predicate, value, graph);
                        }

                        options.format = "application/nquads";

                        run = () => jsonLdApi.FromRDF(rdf);
                    }
                    //else
                    //{
                    //    run = () => { throw new Exception("Couldn't find a test type, apparently."); };
                    //}

                    newCase.run = run;

                    yield return new object[] { manifest + (string)testcase["@id"], newCase };
                }
            }
        }
    }
}
