using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JsonLD.Core;
using JsonLD.GenericJson;
using JsonLD.Impl;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Extensions;

namespace JsonLD.Test
{
    public class NQuadsParserTests
    {
        private const string BasePath = @"NQuads";
        private static readonly string ManifestPath = Path.Combine(BasePath, "manifest.ttl");
        private static readonly GenericJsonObject ManifestFrame = (GenericJsonObject)GenericJsonToken.Parse(@"
{
    '@context': {
        'mf': 'http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#',
        'rdfs': 'http://www.w3.org/2000/01/rdf-schema#',
        'rdft': 'http://www.w3.org/ns/rdftest#',
        'mf:entries': { '@container': '@list'},
        'mf:action': { '@type': '@id'}
    },
    '@type': 'mf:Manifest'
}");

        private readonly NQuadRDFParser _parser;

        public NQuadsParserTests()
        {
            _parser = new NQuadRDFParser();
        }

        [Theory]
        [MemberData(nameof(PositiveTestCases))]
        public void PositiveParseTest(string path)
        {
            // given
            string quads = File.ReadAllText(Path.Combine(BasePath, path));

            // when
            _parser.Parse(quads);
        }

        [Theory]
        [MemberData(nameof(NegativeTestCases))]
        public void NegativeParseTest(string path)
        {
            // given
            string quads = File.ReadAllText(Path.Combine(BasePath, path));

            // when
            Assert.Throws<JsonLdError>(() => _parser.Parse(quads));
        }

        [Fact]
        public void ParseBlankNodesTest()
        {
            // given
            const string path = "rdf11blanknodes.nq";
            string quads = File.ReadAllText(Path.Combine(BasePath, path));

            // when
            _parser.Parse(quads);
        }

        public static IEnumerable<object[]> PositiveTestCases
        {
            get
            {
                var manifest = JsonLdProcessor.FromRDF(File.ReadAllText(ManifestPath), new TurtleRDFParser());
                var framed = JsonLdProcessor.Frame(manifest, ManifestFrame, new JsonLdOptions());

                return from GenericJsonToken testCase in framed["@graph"][0]["mf:entries"]
                       where (string)testCase["@type"] != "rdft:TestNQuadsNegativeSyntax"
                       select new object[] { (string)testCase["mf:action"] };
            }
        }

        public static IEnumerable<object[]> NegativeTestCases
        {
            get
            {
                var manifest = JsonLdProcessor.FromRDF(File.ReadAllText(ManifestPath), new TurtleRDFParser());
                var framed = JsonLdProcessor.Frame(manifest, ManifestFrame, new JsonLdOptions());

                return from GenericJsonToken testCase in framed["@graph"][0]["mf:entries"]
                       where (string)testCase["@type"] == "rdft:TestNQuadsNegativeSyntax"
                       select new object[] { (string)testCase["mf:action"] };
            }
        }
    }
}