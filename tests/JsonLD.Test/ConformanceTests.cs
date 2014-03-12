using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;
using System.IO;
using Newtonsoft.Json;
using JsonLD.Core;
using JsonLD.Util;

namespace JsonLD.Test
{
    public class ConformanceTests
    {
        [Theory, ClassData(typeof(ConformanceCases))]
        public void ConformanceTestPasses(string id, string testname, ConformanceCase conformanceCase)
        {
            JToken result = conformanceCase.run();
            if (conformanceCase.error != null)
            {
                Assert.True(((string)result["error"]).StartsWith((string)conformanceCase.error), "Resulting error doesn't match expectations.");
            }
            else
            {
                Assert.True(JsonLdUtils.DeepCompare(result, conformanceCase.output), "Returned JSON doesn't match expectations.");
            }
        }
    }

    public class ConformanceCase
    {
        public JToken input { get; set; }
        public JToken context { get; set; }
        public JToken frame { get; set; }
        public JToken output { get; set; }
        public JToken error { get; set; }
        public Func<JToken> run { get; set; }
    }

    public class ConformanceCases: IEnumerable<object[]>
    {
        string[] manifests = new[] { "compact-manifest.jsonld", "error-manifest.jsonld", "expand-manifest.jsonld", "flatten-manifest.jsonld", "frame-manifest.jsonld", "remote-doc-manifest.jsonld" };

        public ConformanceCases()
        {

        }

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (string manifest in manifests)
            {
                JToken manifestJson;

                manifestJson = GetJson(manifest);

                foreach (JObject testcase in manifestJson["sequence"])
                {
                    Func<JToken> run;
                    ConformanceCase newCase = new ConformanceCase();

                    newCase.input = GetJson(testcase["input"]);
                    newCase.context = GetJson(testcase["context"]);
                    newCase.frame = GetJson(testcase["frame"]);

                    var options = new JsonLdOptions("http://json-ld.org/test-suite/tests/" + (string)testcase["input"]);

                    var testType = (JArray)testcase["@type"];

                    if (testType.Any((s) => (string)s == "jld:NegativeEvaluationTest"))
                    {
                        newCase.error = testcase["expect"];
                    }
                    else if (testType.Any((s) => (string)s == "jld:PositiveEvaluationTest"))
                    {
                        newCase.output = GetJson(testcase["expect"]);
                    }
                    else
                    {
                        throw new Exception("Expecting either positive or negative evaluation test.");
                    }

                    JToken optionToken;
                    JToken value;

                    if (testcase.TryGetValue("option", out optionToken))
                    {
                        JObject optionDescription = (JObject)optionToken;

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
                            newCase.context = GetJson(testcase["option"]["expandContext"]);
                            options.SetExpandContext((JObject)newCase.context);
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
                    else
                    {
                        run = () => { throw new Exception("Couldn't find a test type, apparently."); };
                    }

                    if ((string)manifestJson["name"] == "Remote document")
                    {
                        Func<JToken> innerRun = run;
                        run = () =>
                        {
                            var remoteDoc = options.documentLoader.LoadDocument("http://json-ld.org/test-suite/tests/" + (string)testcase["input"]);
                            newCase.input = remoteDoc.Document;
                            options.SetBase(remoteDoc.DocumentUrl);
                            options.SetExpandContext((JObject)remoteDoc.Context);
                            return innerRun();
                        };
                    }

                    if (testType.Any((s) => (string)s == "jld:NegativeEvaluationTest"))
                    {
                        Func<JToken> innerRun = run;
                        run = () =>
                        {
                            try
                            {
                                return innerRun();
                            }
                            catch (JsonLdError err)
                            {
                                JObject result = new JObject();
                                result["error"] = err.Message;
                                return result;
                            }
                        };
                    }

                    newCase.run = run;

                    yield return new object[] { manifest + (string)testcase["@id"], (string)testcase["name"], newCase };
                }
            }
        }

        private JToken GetJson(JToken j)
        {
            try {
                if (j.Type == JTokenType.Null) return null;
                using ( Stream manifestStream = File.OpenRead("W3C\\" + (string)j))
                using (TextReader reader = new StreamReader(manifestStream))
                using (JsonReader jreader = new Newtonsoft.Json.JsonTextReader(reader))
                {
                    return JToken.ReadFrom(jreader);
                }
            }
            catch
            {
                return null;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new Exception("auggh");
        }
    }
}
