using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;
using System.IO;
using JsonLD.Core;
using JsonLD.Util;

namespace JsonLD.Test.Raw
{
    public class ConformanceTests
    {
        [Theory, ClassData(typeof(ConformanceCases))]
        public void ConformanceTestPasses(string id, ConformanceCase conformanceCase)
        {
            try
            {
                string result = conformanceCase.run();
                if (!JsonLdUtils.DeepCompare(result, conformanceCase.output))
                {
                    #if DEBUG
                    Console.WriteLine(id);
                    Console.WriteLine("Actual:");
                    Console.Write(JSONUtils.ToPrettyString(result));
                    Console.WriteLine("--------------------------");
                    Console.WriteLine("Expected:");
                    Console.Write(JSONUtils.ToPrettyString(conformanceCase.output));
                    Console.WriteLine("--------------------------");
                    #endif

                    Assert.True(false, "Returned JSON doesn't match expectations.");
                }

            }
            catch (Exception ex)
            {
                if (conformanceCase.error == default) throw; // unexpected error
                Assert.True(ex.Message.StartsWith(conformanceCase.error), "Resulting error doesn't match expectations.");
            }
        }
    }

    public class ConformanceCase
    {
        public string input { get; set; }
        public string context { get; set; }
        public string frame { get; set; }
        public string output { get; set; }
        public string error { get; set; }
        public Func<string> run { get; set; }
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
// Test tests are not supported on CORE CLR
#if !PORTABLE && !IS_CORECLR
            "error-manifest.jsonld",
            "remote-doc-manifest.jsonld",
#endif
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
                var manifestJson = jsonFetcher.GetTestCases(manifest, rootDirectory);

                foreach (var testCase in manifestJson.Sequence)
                {
                    Func<string> run;
                    ConformanceCase newCase = new ConformanceCase();

                    newCase.input = testCase.GetInputJson();
                    newCase.context = testCase.GetContextJson();
                    newCase.frame = testCase.GetFrameJson();

                    var options = new JsonLD.Core.Raw.JsonLdOptions("http://json-ld.org/test-suite/tests/" + testCase.Input);

                    var testType = testCase.Type;

                    if (testType.Any((s) => s == "jld:NegativeEvaluationTest"))
                    {
                        newCase.error = testCase.Expect;
                    }
                    else if (testType.Any((s) => s == "jld:PositiveEvaluationTest"))
                    {
                        if (testType.Any((s) => new List<string> {"jld:ToRDFTest", "jld:NormalizeTest"}.Contains(s)))
                        {
                            newCase.output = File.ReadAllText(Path.Combine("W3C", testCase.Expect));
                        }
                        else if (testType.Any((s) => s == "jld:FromRDFTest"))
                        {
                            newCase.input = File.ReadAllText(Path.Combine("W3C", testCase.Input));
                            newCase.output = testCase.GetExpectJson();
                        }
                        else
                        {
                            newCase.output = testCase.GetExpectJson();
                        }
                    }
                    else
                    {
                        throw new Exception("Expecting either positive or negative evaluation test.");
                    }                    

                    if (testCase.Options != null)
                    {                        
                        if (testCase.Options.CompactArrays.HasValue)
                        {
                            options.SetCompactArrays(testCase.Options.CompactArrays.Value);
                        }
                        if (testCase.Options.Base != default)
                        {
                            options.SetBase(testCase.Options.Base);
                        }
                        if (testCase.Options.ExpandContext != default)
                        {
                            newCase.context = testCase.GetExpandContextJson();
                            options.SetExpandContext(newCase.context);
                        }
                        if (testCase.Options.ProduceGeneralizedRdf.HasValue)
                        {
                            options.SetProduceGeneralizedRdf(testCase.Options.ProduceGeneralizedRdf.Value);
                        }
                        if (testCase.Options.UseNativeTypes.HasValue)
                        {
                            options.SetUseNativeTypes(testCase.Options.UseNativeTypes.Value);
                        }
                        if (testCase.Options.UseRdfType.HasValue)
                        {
                            options.SetUseRdfType(testCase.Options.UseRdfType.Value);
                        }
                    }

                    if (testType.Any((s) => s == "jld:CompactTest"))
                    {
                        run = () => Core.Raw.JsonLdProcessor.Compact(newCase.input, newCase.context, options);
                    }
                    else if (testType.Any((s) => s == "jld:ExpandTest"))
                    {
                        run = () => Core.Raw.JsonLdProcessor.Expand(newCase.input, options);
                    }
                    else if (testType.Any((s) => s == "jld:FlattenTest"))
                    {
                        run = () => Core.Raw.JsonLdProcessor.Flatten(newCase.input, newCase.context, options);
                    }
                    else if (testType.Any((s) => s == "jld:FrameTest"))
                    {
                        run = () => Core.Raw.JsonLdProcessor.Frame(newCase.input, newCase.frame, options);
                    }
                    else if (testType.Any((s) => s == "jld:NormalizeTest"))
                    {
                        run = () => RDFDatasetUtils.ToNQuads((RDFDataset)Core.Raw.JsonLdProcessor.Normalize(newCase.input, options)).Replace("\n", "\r\n");
                    }
                    else if (testType.Any((s) => s == "jld:ToRDFTest"))
                    {
                        options.format = "application/nquads";
                        run = () => Core.Raw.JsonLdProcessor.ToRDF(newCase.input, options).Replace("\n", "\r\n");
                        
                    }
                    else if (testType.Any((s) => s == "jld:FromRDFTest"))
                    {
                        options.format = "application/nquads";
                        run = () => Core.Raw.JsonLdProcessor.FromRDF(newCase.input,options);
                    }
                    else
                    {
                        run = () => { throw new Exception("Couldn't find a test type, apparently."); };
                    }

                    if (manifestJson.Name == "Remote document")
                    {
                        Func<string> innerRun = run;
                        run = () =>
                        {
                            var remoteDoc = options.documentLoader.LoadDocument("https://json-ld.org/test-suite/tests/" + testCase.Input);
                            newCase.input = remoteDoc.Document;
                            options.SetBase(remoteDoc.DocumentUrl);
                            options.SetExpandContext(remoteDoc.Context);
                            return innerRun();
                        };
                    }

                    newCase.run = run;

                    yield return new object[] { manifest + testCase.Id, newCase };
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new Exception("auggh");
        }
    }
}
