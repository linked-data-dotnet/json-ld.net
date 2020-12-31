using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;
using Xunit;
using System.IO;
using JsonLD.Core;
using JsonLD.Util;
using JsonLD.OmniJson;
using System.Diagnostics;

namespace JsonLD.Test
{
    public class ConformanceTests
    {
        [Theory, ClassData(typeof(ConformanceCases))]
        public void ConformanceTestPasses(string id, ConformanceCase conformanceCase)
        {
            OmniJsonToken result = conformanceCase.run();
            if (conformanceCase.error != null)
            {
                Assert.True(((string)result["error"]).StartsWith((string)conformanceCase.error), "Resulting error doesn't match expectations.");
            }
            else
            {
                if (!JsonLdUtils.DeepCompare(result, conformanceCase.output))
                {
                    #if DEBUG
                    Console.WriteLine(id);
                    Console.WriteLine("Actual:");
                    Console.Write(TinyJson.JSONWriter.ToJson(result));
                    Console.WriteLine("--------------------------");
                    Console.WriteLine("Expected:");
                    Console.Write(TinyJson.JSONWriter.ToJson(conformanceCase.output));
                    Console.WriteLine("--------------------------");
                    #endif

                    Assert.True(false, "Returned JSON doesn't match expectations.");
                }
            }
        }
    }

    public class ConformanceCase
    {
        public OmniJsonToken input { get; set; }
        public OmniJsonToken context { get; set; }
        public OmniJsonToken frame { get; set; }
        public OmniJsonToken output { get; set; }
        public OmniJsonToken error { get; set; }
        public Func<OmniJsonToken> run { get; set; }
    }

    public class ConformanceCases: IEnumerable<object[]>
    {
        string[] manifests = new[] {
            "compact-manifest.jsonld",
            "expand-manifest.jsonld",
            "flatten-manifest.jsonld",
            "frame-manifest.jsonld",
            "toRdf-manifest.jsonld",
            "fromRdf-manifest.jsonld",
            "normalize-manifest.jsonld",
            "error-manifest.jsonld",
            "remote-doc-manifest.jsonld",
        };

        public ConformanceCases()
        {

        }

        public IEnumerator<object[]> GetEnumerator()
        {
            var jsonFetcher = new JsonFetcher();
            var rootDirectory = "W3C";

            foreach (string manifest in manifests)
            {
                OmniJsonToken manifestJson = jsonFetcher.GetJson(manifest, rootDirectory);
                var isRemoteTest = (string)manifestJson["name"] == "Remote document";

                foreach (OmniJsonObject testcase in manifestJson["sequence"])
                {
                    Func<OmniJsonToken> run;
                    ConformanceCase newCase = new ConformanceCase();

                    // Load input file if not remote test. Remote tests load from the web at test execution time.
                    if (!isRemoteTest)
                    {
                        newCase.input = jsonFetcher.GetJson(testcase["input"], rootDirectory);
                    }
                    newCase.context = jsonFetcher.GetJson(testcase["context"], rootDirectory);
                    newCase.frame = jsonFetcher.GetJson(testcase["frame"], rootDirectory);

                    var options = new JsonLdOptions("http://json-ld.org/test-suite/tests/" + (string)testcase["input"]);

                    var testType = (OmniJsonArray)testcase["@type"];

                    if (testType.Any((s) => (string)s == "jld:NegativeEvaluationTest"))
                    {
                        newCase.error = testcase["expect"];
                    }
                    else if (testType.Any((s) => (string)s == "jld:PositiveEvaluationTest"))
                    {
                        if (testType.Any((s) => new List<string> {"jld:ToRDFTest", "jld:NormalizeTest"}.Contains((string)s)))
                        {
                            newCase.output = File.ReadAllText(Path.Combine("W3C", (string)testcase["expect"]));
                        }
                        else if (testType.Any((s) => (string)s == "jld:FromRDFTest"))
                        {
                            newCase.input = File.ReadAllText(Path.Combine("W3C", (string)testcase["input"]));
                            newCase.output = jsonFetcher.GetJson(testcase["expect"], rootDirectory);
                        }
                        else
                        {
                            newCase.output = jsonFetcher.GetJson(testcase["expect"], rootDirectory);
                        }
                    }
                    else
                    {
                        throw new Exception("Expecting either positive or negative evaluation test.");
                    }

                    OmniJsonToken optionToken;
                    OmniJsonToken value;

                    if (testcase.TryGetValue("option", out optionToken))
                    {
                        OmniJsonObject optionDescription = (OmniJsonObject)optionToken;

                        if (optionDescription.TryGetValue("compactArrays", out value))
                        {
                            options.SetCompactArrays((bool)value);
                        }
                        if (optionDescription.TryGetValue("base", out value))
                        {
                            options.SetBase((string)value);
                        }
                        if (optionDescription.TryGetValue("expandContext", out value))
                        {
                            newCase.context = jsonFetcher.GetJson(testcase["option"]["expandContext"], rootDirectory);
                            options.SetExpandContext((OmniJsonObject)newCase.context);
                        }
                        if (optionDescription.TryGetValue("produceGeneralizedRdf", out value))
                        {
                            options.SetProduceGeneralizedRdf((bool)value);
                        }
                        if (optionDescription.TryGetValue("useNativeTypes", out value))
                        {
                            options.SetUseNativeTypes((bool)value);
                        }
                        if (optionDescription.TryGetValue("useRdfType", out value))
                        {
                            options.SetUseRdfType((bool)value);
                        }
                    }

                    if (testType.Any((s) => (string)s == "jld:CompactTest"))
                    {
                        run = () => JsonLdProcessor.Compact(newCase.input, newCase.context, options);
                    }
                    else if (testType.Any((s) => (string)s == "jld:ExpandTest"))
                    {
                        run = () => JsonLdProcessor.Expand(newCase.input, options);
                    }
                    else if (testType.Any((s) => (string)s == "jld:FlattenTest"))
                    {
                        run = () => JsonLdProcessor.Flatten(newCase.input, newCase.context, options);
                    }
                    else if (testType.Any((s) => (string)s == "jld:FrameTest"))
                    {
                        run = () => JsonLdProcessor.Frame(newCase.input, newCase.frame, options);
                    }
                    else if (testType.Any((s) => (string)s == "jld:NormalizeTest"))
                    {
                        run = () => new OmniJsonValue(
                                RDFDatasetUtils.ToNQuads((RDFDataset)JsonLdProcessor.Normalize(newCase.input, options)).Replace("\n", "\r\n")
                            );
                    }
                    else if (testType.Any((s) => (string)s == "jld:ToRDFTest"))
                    {
                        options.format = "application/nquads";
                        run = () => new OmniJsonValue(
                            ((string)JsonLdProcessor.ToRDF(newCase.input, options)).Replace("\n", "\r\n")
                        );
                    }
                    else if (testType.Any((s) => (string)s == "jld:FromRDFTest"))
                    {
                        options.format = "application/nquads";
                        run = () => JsonLdProcessor.FromRDF(newCase.input,options);
                    }
                    else
                    {
                        run = () => { throw new Exception("Couldn't find a test type, apparently."); };
                    }

                    if (isRemoteTest)
                    {
                        Func<OmniJsonToken> innerRun = run;
                        run = () =>
                        {
                            var remoteDoc = options.documentLoader.LoadDocument("https://json-ld.org/test-suite/tests/" + (string)testcase["input"]);
                            newCase.input = remoteDoc.Document;
                            options.SetBase(remoteDoc.DocumentUrl);
                            options.SetExpandContext((OmniJsonObject)remoteDoc.Context);
                            return innerRun();
                        };
                    }

                    if (testType.Any((s) => (string)s == "jld:NegativeEvaluationTest"))
                    {
                        Func<OmniJsonToken> innerRun = run;
                        run = () =>
                        {
                            try
                            {
                                return innerRun();
                            }
                            catch (JsonLdError err)
                            {
                                OmniJsonObject result = new OmniJsonObject();
                                result["error"] = err.Message;
                                return result;
                            }
                        };
                    }

                    newCase.run = run;

#if false
                    var cases = @"flatten-manifest.jsonld#t0002".Split("\r\n");

                    var id = manifest + (string)testcase["@id"];

                    if (cases.Contains(id))
#endif
                        yield return new object[] { manifest + (string)testcase["@id"], newCase };
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new Exception("auggh");
        }
    }
}
