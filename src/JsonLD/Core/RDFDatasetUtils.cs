using System;
using System.Collections.Generic;
using System.Text;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    public class RDFDatasetUtils
    {
        /// <summary>Creates an array of RDF triples for the given graph.</summary>
        /// <remarks>Creates an array of RDF triples for the given graph.</remarks>
        /// <param name="graph">the graph to create RDF triples for.</param>
        /// <param name="namer">a UniqueNamer for assigning blank node names.</param>
        /// <returns>the array of RDF triples for the given graph.</returns>
        [Obsolete]
        internal static JArray GraphToRDF(JObject graph, UniqueNamer
             namer)
        {
            // use RDFDataset.graphToRDF
            JArray rval = new JArray();
            foreach (string id in graph.GetKeys())
            {
                JObject node = (JObject)graph[id];
                JArray properties = new JArray(node.GetKeys());
                properties.SortInPlace();
                foreach (string property in properties)
                {
                    var eachProperty = property;
                    JToken items = node[eachProperty];
                    if ("@type".Equals(eachProperty))
                    {
                        eachProperty = JSONLDConsts.RdfType;
                    }
                    else
                    {
                        if (JsonLdUtils.IsKeyword(eachProperty))
                        {
                            continue;
                        }
                    }
                    foreach (JToken item in (JArray)items)
                    {
                        // RDF subjects
                        JObject subject = new JObject();
                        if (id.IndexOf("_:") == 0)
                        {
                            subject["type"] = "blank node";
                            subject["value"] = namer.GetName(id);
                        }
                        else
                        {
                            subject["type"] = "IRI";
                            subject["value"] = id;
                        }
                        // RDF predicates
                        JObject predicate = new JObject();
                        predicate["type"] = "IRI";
                        predicate["value"] = eachProperty;
                        // convert @list to triples
                        if (JsonLdUtils.IsList(item))
                        {
                            ListToRDF((JArray)((JObject)item)["@list"], namer, subject
                                , predicate, rval);
                        }
                        else
                        {
                            // convert value or node object to triple
                            object @object = ObjectToRDF(item, namer);
                            IDictionary<string, object> tmp = new Dictionary<string, object>();
                            tmp["subject"] = subject;
                            tmp["predicate"] = predicate;
                            tmp["object"] = @object;
                            rval.Add(tmp);
                        }
                    }
                }
            }
            return rval;
        }

        /// <summary>
        /// Converts a @list value into linked list of blank node RDF triples (an RDF
        /// collection).
        /// </summary>
        /// <remarks>
        /// Converts a @list value into linked list of blank node RDF triples (an RDF
        /// collection).
        /// </remarks>
        /// <param name="list">the @list value.</param>
        /// <param name="namer">a UniqueNamer for assigning blank node names.</param>
        /// <param name="subject">the subject for the head of the list.</param>
        /// <param name="predicate">the predicate for the head of the list.</param>
        /// <param name="triples">the array of triples to append to.</param>
        private static void ListToRDF(JArray list, UniqueNamer namer, JObject subject, JObject predicate, JArray triples
            )
        {
            JObject first = new JObject();
            first["type"] = "IRI";
            first["value"] = JSONLDConsts.RdfFirst;
            JObject rest = new JObject();
            rest["type"] = "IRI";
            rest["value"] = JSONLDConsts.RdfRest;
            JObject nil = new JObject();
            nil["type"] = "IRI";
            nil["value"] = JSONLDConsts.RdfNil;
            foreach (JToken item in list)
            {
                JObject blankNode = new JObject();
                blankNode["type"] = "blank node";
                blankNode["value"] = namer.GetName();
                {
                    JObject tmp = new JObject();
                    tmp["subject"] = subject;
                    tmp["predicate"] = predicate;
                    tmp["object"] = blankNode;
                    triples.Add(tmp);
                }
                subject = blankNode;
                predicate = first;
                JToken @object = ObjectToRDF(item, namer);
                {
                    JObject tmp = new JObject();
                    tmp["subject"] = subject;
                    tmp["predicate"] = predicate;
                    tmp["object"] = @object;
                    triples.Add(tmp);
                }
                predicate = rest;
            }
            JObject tmp_1 = new JObject();
            tmp_1["subject"] = subject;
            tmp_1["predicate"] = predicate;
            tmp_1["object"] = nil;
            triples.Add(tmp_1);
        }

        /// <summary>
        /// Converts a JSON-LD value object to an RDF literal or a JSON-LD string or
        /// node object to an RDF resource.
        /// </summary>
        /// <remarks>
        /// Converts a JSON-LD value object to an RDF literal or a JSON-LD string or
        /// node object to an RDF resource.
        /// </remarks>
        /// <param name="item">the JSON-LD value or node object.</param>
        /// <param name="namer">the UniqueNamer to use to assign blank node names.</param>
        /// <returns>the RDF literal or RDF resource.</returns>
        private static JObject ObjectToRDF(JToken item, UniqueNamer namer)
        {
            JObject @object = new JObject();
            // convert value object to RDF
            if (JsonLdUtils.IsValue(item))
            {
                @object["type"] = "literal";
                JToken value = ((JObject)item)["@value"];
                JToken datatype = ((JObject)item)["@type"];
                // convert to XSD datatypes as appropriate
                if (value.Type == JTokenType.Boolean || value.Type == JTokenType.Float || value.Type == JTokenType.Integer )
                {
                    // convert to XSD datatype
                    if (value.Type == JTokenType.Boolean)
                    {
                        @object["value"] = value.ToString();
                        @object["datatype"] = datatype.IsNull() ? JSONLDConsts.XsdBoolean : datatype;
                    }
                    else
                    {
                        if (value.Type == JTokenType.Float)
                        {
                            // canonical double representation
                            @object["value"] = string.Format("{0:0.0###############E0}", (double)value);
                            @object["datatype"] = datatype.IsNull() ? JSONLDConsts.XsdDouble : datatype;
                        }
                        else
                        {
                            DecimalFormat df = new DecimalFormat("0");
                            @object["value"] = df.Format((int)value);
                            @object["datatype"] = datatype.IsNull() ? JSONLDConsts.XsdInteger : datatype;
                        }
                    }
                }
                else
                {
                    if (((IDictionary<string, JToken>)item).ContainsKey("@language"))
                    {
                        @object["value"] = value;
                        @object["datatype"] = datatype.IsNull() ? JSONLDConsts.RdfLangstring : datatype;
                        @object["language"] = ((IDictionary<string, JToken>)item)["@language"];
                    }
                    else
                    {
                        @object["value"] = value;
                        @object["datatype"] = datatype.IsNull() ? JSONLDConsts.XsdString : datatype;
                    }
                }
            }
            else
            {
                // convert string/node object to RDF
                string id = JsonLdUtils.IsObject(item) ? (string)((JObject)item
                    )["@id"] : (string)item;
                if (id.IndexOf("_:") == 0)
                {
                    @object["type"] = "blank node";
                    @object["value"] = namer.GetName(id);
                }
                else
                {
                    @object["type"] = "IRI";
                    @object["value"] = id;
                }
            }
            return @object;
        }

        public static string ToNQuads(RDFDataset dataset)
        {
            IList<string> quads = new List<string>();
            foreach (string graphName in dataset.GraphNames())
            {
                var eachGraphName = graphName;
                IList<RDFDataset.Quad> triples = dataset.GetQuads(eachGraphName);
                if ("@default".Equals(eachGraphName))
                {
                    eachGraphName = null;
                }
                foreach (RDFDataset.Quad triple in triples)
                {
                    quads.Add(ToNQuad(triple, eachGraphName));
                }
            }

            ((List<string>)quads).Sort(StringComparer.Ordinal);

            string rval = string.Empty;
            foreach (string quad in quads)
            {
                rval += quad;
            }
            return rval;
        }

        internal static string ToNQuad(RDFDataset.Quad triple, string graphName, string bnode
            )
        {
            RDFDataset.Node s = triple.GetSubject();
            RDFDataset.Node p = triple.GetPredicate();
            RDFDataset.Node o = triple.GetObject();
            string quad = string.Empty;
            // subject is an IRI or bnode
            if (s.IsIRI())
            {
                quad += "<" + Escape(s.GetValue()) + ">";
            }
            else
            {
                // normalization mode
                if (bnode != null)
                {
                    quad += bnode.Equals(s.GetValue()) ? "_:a" : "_:z";
                }
                else
                {
                    // normal mode
                    quad += s.GetValue();
                }
            }
            if (p.IsIRI())
            {
                quad += " <" + Escape(p.GetValue()) + "> ";
            }
            else
            {
                // otherwise it must be a bnode (TODO: can we only allow this if the
                // flag is set in options?)
                quad += " " + Escape(p.GetValue()) + " ";
            }
            // object is IRI, bnode or literal
            if (o.IsIRI())
            {
                quad += "<" + Escape(o.GetValue()) + ">";
            }
            else
            {
                if (o.IsBlankNode())
                {
                    // normalization mode
                    if (bnode != null)
                    {
                        quad += bnode.Equals(o.GetValue()) ? "_:a" : "_:z";
                    }
                    else
                    {
                        // normal mode
                        quad += o.GetValue();
                    }
                }
                else
                {
                    string escaped = Escape(o.GetValue());
                    quad += "\"" + escaped + "\"";
                    if (JSONLDConsts.RdfLangstring.Equals(o.GetDatatype()))
                    {
                        quad += "@" + o.GetLanguage();
                    }
                    else
                    {
                        if (!JSONLDConsts.XsdString.Equals(o.GetDatatype()))
                        {
                            quad += "^^<" + Escape(o.GetDatatype()) + ">";
                        }
                    }
                }
            }
            // graph
            if (graphName != null)
            {
                if (graphName.IndexOf("_:") != 0)
                {
                    quad += " <" + Escape(graphName) + ">";
                }
                else
                {
                    if (bnode != null)
                    {
                        quad += " _:g";
                    }
                    else
                    {
                        quad += " " + graphName;
                    }
                }
            }
            quad += " .\n";
            return quad;
        }

        internal static string ToNQuad(RDFDataset.Quad triple, string graphName)
        {
            return ToNQuad(triple, graphName, null);
        }

        private static readonly Pattern UcharMatched = Pattern.Compile("\\u005C(?:([tbnrf\\\"'])|(?:u("
             + JsonLD.Core.Regex.Hex + "{4}))|(?:U(" + JsonLD.Core.Regex.Hex + "{8})))"
            );

        public static string Unescape(string str)
        {
            string rval = str;
            if (str != null)
            {
                Matcher m = UcharMatched.Matcher(str);
                while (m.Find())
                {
                    string uni = m.Group(0);
                    if (m.Group(1) == null)
                    {
                        string hex = m.Group(2) != null ? m.Group(2) : m.Group(3);
                        int v = System.Convert.ToInt32(hex, 16);
                        // hex =
                        // hex.replaceAll("^(?:00)+",
                        // "");
                        if (v > unchecked((int)(0xFFFF)))
                        {
                            // deal with UTF-32
                            // Integer v = Integer.parseInt(hex, 16);
                            int vt = v - unchecked((int)(0x10000));
                            int vh = vt >> 10;
                            int v1 = vt & unchecked((int)(0x3FF));
                            int w1 = unchecked((int)(0xD800)) + vh;
                            int w2 = unchecked((int)(0xDC00)) + v1;
                            StringBuilder b = new StringBuilder();
                            b.AppendCodePoint(w1);
                            b.AppendCodePoint(w2);
                            uni = b.ToString();
                        }
                        else
                        {
                            uni = char.ToString((char)v);
                        }
                    }
                    else
                    {
                        char c = m.Group(1)[0];
                        switch (c)
                        {
                            case 'b':
                            {
                                uni = "\b";
                                break;
                            }

                            case 'n':
                            {
                                uni = "\n";
                                break;
                            }

                            case 't':
                            {
                                uni = "\t";
                                break;
                            }

                            case 'f':
                            {
                                uni = "\f";
                                break;
                            }

                            case 'r':
                            {
                                uni = "\r";
                                break;
                            }

                            case '\'':
                            {
                                uni = "'";
                                break;
                            }

                            case '\"':
                            {
                                uni = "\"";
                                break;
                            }

                            case '\\':
                            {
                                uni = "\\";
                                break;
                            }

                            default:
                            {
                                // do nothing
                                continue;
                            }
                        }
                    }
                    string pat = Pattern.Quote(m.Group(0));
                    string x = JsonLD.JavaCompat.ToHexString(uni[0]);
                    rval = rval.Replace(pat, uni);
                }
            }
            return rval;
        }

        public static string Escape(string str)
        {
            string rval = string.Empty;
            for (int i = 0; i < str.Length; i++)
            {
                char hi = str[i];
                if (hi <= unchecked((int)(0x8)) || hi == unchecked((int)(0xB)) || hi == unchecked(
                    (int)(0xC)) || (hi >= unchecked((int)(0xE)) && hi <= unchecked((int)(0x1F))) || 
                    (hi >= unchecked((int)(0x7F)) && hi <= unchecked((int)(0xA0))) || ((hi >= unchecked(
                    (int)(0x24F)) && !char.IsHighSurrogate(hi))))
                {
                    // 0xA0 is end of
                    // non-printable latin-1
                    // supplement
                    // characters
                    // 0x24F is the end of latin extensions
                    // TODO: there's probably a lot of other characters that
                    // shouldn't be escaped that
                    // fall outside these ranges, this is one example from the
                    // json-ld tests
                    rval += string.Format("\\u%04x", (int)hi);
                }
                else
                {
                    if (char.IsHighSurrogate(hi))
                    {
                        char lo = str[++i];
                        int c = (hi << 10) + lo + (unchecked((int)(0x10000)) - (unchecked((int)(0xD800)) 
                            << 10) - unchecked((int)(0xDC00)));
                        rval += string.Format("\\U%08x", c);
                    }
                    else
                    {
                        switch (hi)
                        {
                            case '\b':
                            {
                                rval += "\\b";
                                break;
                            }

                            case '\n':
                            {
                                rval += "\\n";
                                break;
                            }

                            case '\t':
                            {
                                rval += "\\t";
                                break;
                            }

                            case '\f':
                            {
                                rval += "\\f";
                                break;
                            }

                            case '\r':
                            {
                                rval += "\\r";
                                break;
                            }

                            case '\"':
                            {
                                // case '\'':
                                // rval += "\\'";
                                // break;
                                rval += "\\\"";
                                // rval += "\\u0022";
                                break;
                            }

                            case '\\':
                            {
                                rval += "\\\\";
                                break;
                            }

                            default:
                            {
                                // just put the char as is
                                rval += hi;
                                break;
                            }
                        }
                    }
                }
            }
            return rval;
        }

        private class Regex
        {
            public static readonly Pattern Iri = Pattern.Compile("(?:<([^>]*)>)");

            public static readonly Pattern Bnode = Pattern.Compile("(_:(?:[A-Za-z][A-Za-z0-9]*))"
                );

            public static readonly Pattern Plain = Pattern.Compile("\"([^\"\\\\]*(?:\\\\.[^\"\\\\]*)*)\""
                );

            public static readonly Pattern Datatype = Pattern.Compile("(?:\\^\\^" + Iri + ")"
                );

            public static readonly Pattern Language = Pattern.Compile("(?:@([a-z]+(?:-[a-zA-Z0-9]+)*))"
                );

            public static readonly Pattern Literal = Pattern.Compile("(?:" + Plain + "(?:" + 
                Datatype + "|" + Language + ")?)");

            public static readonly Pattern Wso = Pattern.Compile("[ \\t]*");

            public static readonly Pattern Eoln = Pattern.Compile("(?:\r\n)|(?:\n)|(?:\r)");

            public static readonly Pattern EmptyOrComment = Pattern.Compile("^" + Wso + "(#.*)?$");

            public static readonly Pattern Subject = Pattern.Compile("(?:" + Iri + "|" + Bnode
                 + ")" + Wso);

            public static readonly Pattern Property = Pattern.Compile(Iri.GetPattern() + Wso);

            public static readonly Pattern Object = Pattern.Compile("(?:" + Iri + "|" + Bnode
                 + "|" + Literal + ")" + Wso);

            public static readonly Pattern Graph = Pattern.Compile("(?:\\.|(?:(?:" + Iri + "|"
                 + Bnode + ")" + Wso + "\\.))");

            public static readonly Pattern Quad = Pattern.Compile("^" + Wso + Subject + Property
                 + Object + Graph + Wso + "(#.*)?$");
            // define partial regexes
            // final public static Pattern IRI =
            // Pattern.compile("(?:<([^:]+:[^>]*)>)");
            // define quad part regexes
            // full quad regex
        }

        /// <summary>Parses RDF in the form of N-Quads.</summary>
        /// <remarks>Parses RDF in the form of N-Quads.</remarks>
        /// <param name="input">the N-Quads input to parse.</param>
        /// <returns>an RDF dataset.</returns>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public static RDFDataset ParseNQuads(string input)
        {
            // build RDF dataset
            RDFDataset dataset = new RDFDataset();
            // split N-Quad input into lines
            string[] lines = RDFDatasetUtils.Regex.Eoln.Split(input);
            int lineNumber = 0;
            foreach (string line in lines)
            {
                lineNumber++;
                // skip empty lines
                if (RDFDatasetUtils.Regex.EmptyOrComment.Matcher(line).Matches())
                {
                    continue;
                }
                // parse quad
                Matcher match = RDFDatasetUtils.Regex.Quad.Matcher(line);
                if (!match.Matches())
                {
                    throw new JsonLdError(JsonLdError.Error.SyntaxError, "Error while parsing N-Quads; invalid quad. line:"
                         + lineNumber);
                }
                // get subject
                RDFDataset.Node subject;
                if (match.Group(1) != null)
                {
                    subject = new RDFDataset.IRI(Unescape(match.Group(1)));
                }
                else
                {
                    subject = new RDFDataset.BlankNode(Unescape(match.Group(2)));
                }
                // get predicate
                RDFDataset.Node predicate = new RDFDataset.IRI(Unescape(match.Group(3)));
                // get object
                RDFDataset.Node @object;
                if (match.Group(4) != null)
                {
                    @object = new RDFDataset.IRI(Unescape(match.Group(4)));
                }
                else
                {
                    if (match.Group(5) != null)
                    {
                        @object = new RDFDataset.BlankNode(Unescape(match.Group(5)));
                    }
                    else
                    {
                        string language = Unescape(match.Group(8));
                        string datatype = match.Group(7) != null ? Unescape(match.Group(7)) : match.Group
                            (8) != null ? JSONLDConsts.RdfLangstring : JSONLDConsts.XsdString;
                        string unescaped = Unescape(match.Group(6));
                        @object = new RDFDataset.Literal(unescaped, datatype, language);
                    }
                }
                // get graph name ('@default' is used for the default graph)
                string name = "@default";
                if (match.Group(9) != null)
                {
                    name = Unescape(match.Group(9));
                }
                else
                {
                    if (match.Group(10) != null)
                    {
                        name = Unescape(match.Group(10));
                    }
                }
                RDFDataset.Quad triple = new RDFDataset.Quad(subject, predicate, @object, name);
                // initialise graph in dataset
                if (!dataset.ContainsKey(name))
                {
                    IList<RDFDataset.Quad> tmp = new List<RDFDataset.Quad>();
                    tmp.Add(triple);
                    dataset[name] = tmp;
                }
                else
                {
                    // add triple if unique to its graph
                    IList<RDFDataset.Quad> triples = (IList<RDFDataset.Quad>)dataset[name];
                    if (!triples.Contains(triple))
                    {
                        triples.Add(triple);
                    }
                }
            }
            return dataset;
        }
    }
}
