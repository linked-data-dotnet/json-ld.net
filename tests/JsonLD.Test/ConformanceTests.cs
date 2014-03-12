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
            if (id.Contains("error"))
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
        string[] manifests = new[] { "compact-manifest.jsonld", "error-manifest.jsonld", "expand-manifest.jsonld", "flatten-manifest.jsonld", "frame-manifest.jsonld", "normalize-manifest.jsonld" };

        public ConformanceCases()
        {

        }

        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (string manifest in manifests)
            {
                JToken manifestJson;

                manifestJson = GetJson(manifest);

                foreach (var testcase in manifestJson["sequence"])
                {
                    Func<JToken> run;
                    ConformanceCase newCase = new ConformanceCase();

                    newCase.input = GetJson(testcase["input"]);
                    newCase.output = GetJson(testcase["expect"]);
                    newCase.context = GetJson(testcase["context"]);
                    newCase.frame = GetJson(testcase["frame"]);
                    newCase.error = testcase["expect"];

                    var options = new JsonLdOptions("http://json-ld.org/test-suite/tests/" + (string)testcase["input"]);

                    if (manifest.StartsWith("compact"))
                    {
                        if (((string)testcase["@id"]).Contains("0070"))
                        {
                            options.SetCompactArrays(false);
                        }
                        run = () => JsonLdProcessor.Compact(newCase.input, newCase.context, options);
                    }
                    else if (manifest.StartsWith("expand"))
                    {
                        if (((string)testcase["@id"]).Contains("0076"))
                        {
                            options.SetBase("http://example/base/");
                        }
                        if (((string)testcase["@id"]).Contains("0077"))
                        {
                            newCase.context = GetJson(testcase["option"]["expandContext"]);
                            options.SetExpandContext((JObject)newCase.context);
                        }
                        run = () => JsonLdProcessor.Expand(newCase.input, options);
                    }
                    else if (manifest.StartsWith("error"))
                    {
                        newCase.output = new JObject();
                        newCase.output["error"] = newCase.error;
                        run = () => {
                            try {
                                JsonLdProcessor.Flatten(newCase.input, newCase.context, options);
                            }
                            catch (JsonLdError err)
                            {
                                JObject result = new JObject();
                                result["error"] = err.Message;
                                return result;
                            }
                            return new JValue((object)null);
                        };
                    }
                    else if (manifest.StartsWith("flatten"))
                    {
                        if (((string)testcase["@id"]).Contains("0044"))
                        {
                            options.SetCompactArrays(false);
                        }
                        run = () => JsonLdProcessor.Flatten(newCase.input, newCase.context, options);
                    }
                    else if (manifest.StartsWith("frame"))
                    {
                        run = () => JsonLdProcessor.Frame(newCase.input, newCase.frame, options);
                    }
                    else if (manifest.StartsWith("remote-doc"))
                    {
                        run = () =>
                        {
                            var doc = new DocumentLoader().LoadDocument("http://json-ld.org/test-suite/tests/" + testcase["input"]).Document;
                            return JsonLdProcessor.Expand(doc, options);
                        };
                    }
                    else 
                    {
                        continue;
                        run = () => { throw new Exception(); };
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
