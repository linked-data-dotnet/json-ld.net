using System;
using System.Collections.Generic;
using System.Globalization;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    /// <summary>
    /// Starting to migrate away from using plain java Maps as the internal RDF
    /// dataset store.
    /// </summary>
    /// <remarks>
    /// Starting to migrate away from using plain java Maps as the internal RDF
    /// dataset store. Currently each item just wraps a Map based on the old format
    /// so everything doesn't break. Will phase this out once everything is using the
    /// new format.
    /// </remarks>
    /// <author>Tristan</author>
    //[System.Serializable]
    public class RDFDataset : Dictionary<string,object>
    {
        //[System.Serializable]
        public class Quad : Dictionary<string,object>, IComparable<RDFDataset.Quad>
        {
            public Quad(string subject, string predicate, string @object, string graph) : this
                (subject, predicate, @object.StartsWith("_:") ? (Node)new RDFDataset.BlankNode(@object
                ) : (Node)new RDFDataset.IRI(@object), graph)
            {
            }

            public Quad(string subject, string predicate, string value, string datatype, string
                 language, string graph) : this(subject, predicate, new RDFDataset.Literal(value
                , datatype, language), graph)
            {
            }

            private Quad(string subject, string predicate, RDFDataset.Node @object, string graph
                ) : this(subject.StartsWith("_:") ? (Node)new RDFDataset.BlankNode(subject) : (Node)new RDFDataset.IRI
                (subject), new RDFDataset.IRI(predicate), @object, graph)
            {
            }

            public Quad(RDFDataset.Node subject, RDFDataset.Node predicate, RDFDataset.Node @object
                , string graph) : base()
            {
                this["subject"] = subject;
                this["predicate"] = predicate;
                this["object"] = @object;
                if (graph != null && !"@default".Equals(graph))
                {
                    // TODO: i'm not yet sure if this should be added or if the
                    // graph should only be represented by the keys in the dataset
                    this["name"] = graph.StartsWith("_:") ? (Node)new RDFDataset.BlankNode(graph) : (Node)new RDFDataset.IRI
                        (graph);
                }
            }

            public virtual RDFDataset.Node GetSubject()
            {
                return (RDFDataset.Node)this["subject"];
            }

            public virtual RDFDataset.Node GetPredicate()
            {
                return (RDFDataset.Node)this["predicate"];
            }

            public virtual RDFDataset.Node GetObject()
            {
                return (RDFDataset.Node)this["object"];
            }

            public virtual RDFDataset.Node GetGraph()
            {
                return (RDFDataset.Node)this["name"];
            }

            public virtual int CompareTo(RDFDataset.Quad o)
            {
                if (o == null)
                {
                    return 1;
                }
                int rval = GetGraph().CompareTo(o.GetGraph());
                if (rval != 0)
                {
                    return rval;
                }
                rval = GetSubject().CompareTo(o.GetSubject());
                if (rval != 0)
                {
                    return rval;
                }
                rval = GetPredicate().CompareTo(o.GetPredicate());
                if (rval != 0)
                {
                    return rval;
                }
                return GetObject().CompareTo(o.GetObject());
            }
        }

        //[System.Serializable]
        public abstract class Node : Dictionary<string,object>, IComparable<RDFDataset.Node
            >
        {
            public abstract bool IsLiteral();

            public abstract bool IsIRI();

            public abstract bool IsBlankNode();

            public virtual string GetValue()
            {
                object value;
                return this.TryGetValue("value", out value) ? (string)value : null;
            }

            public virtual string GetDatatype()
            {
                object value;
                return this.TryGetValue("datatype", out value) ? (string)value : null;
            }

            public virtual string GetLanguage()
            {
                object value;
                return this.TryGetValue("language", out value) ? (string)value : null;
            }

            public virtual int CompareTo(RDFDataset.Node o)
            {
                if (this.IsIRI())
                {
                    if (!o.IsIRI())
                    {
                        // IRIs > everything
                        return 1;
                    }
                }
                else
                {
                    if (this.IsBlankNode())
                    {
                        if (o.IsIRI())
                        {
                            // IRI > blank node
                            return -1;
                        }
                        else
                        {
                            if (o.IsLiteral())
                            {
                                // blank node > literal
                                return 1;
                            }
                        }
                    }
                }
                return string.CompareOrdinal(this.GetValue(), o.GetValue());
            }

            /// <summary>Converts an RDF triple object to a JSON-LD object.</summary>
            /// <remarks>Converts an RDF triple object to a JSON-LD object.</remarks>
            /// <param name="o">the RDF triple object to convert.</param>
            /// <param name="useNativeTypes">true to output native types, false not to.</param>
            /// <returns>the JSON-LD object.</returns>
            /// <exception cref="JsonLdError">JsonLdError</exception>
            /// <exception cref="JsonLD.Core.JsonLdError"></exception>
            internal virtual JObject ToObject(bool useNativeTypes)
            {
                // If value is an an IRI or a blank node identifier, return a new
                // JSON object consisting
                // of a single member @id whose value is set to value.
                if (IsIRI() || IsBlankNode())
                {
                    JObject obj = new JObject();
                    obj["@id"] = GetValue();
                    return obj;
                }
                // convert literal object to JSON-LD
                JObject rval = new JObject();
                rval["@value"] = GetValue();
                // add language
                if (GetLanguage() != null)
                {
                    rval["@language"] = GetLanguage();
                }
                else
                {
                    // add datatype
                    string type = GetDatatype();
                    string value = GetValue();
                    if (useNativeTypes)
                    {
                        // use native datatypes for certain xsd types
                        if (JSONLDConsts.XsdString.Equals(type))
                        {
                        }
                        else
                        {
                            // don't add xsd:string
                            if (JSONLDConsts.XsdBoolean.Equals(type))
                            {
                                if ("true".Equals(value))
                                {
                                    rval["@value"] = true;
                                }
                                else
                                {
                                    if ("false".Equals(value))
                                    {
                                        rval["@value"] = false;
                                    }
                                }
                            }
                            else
                            {
                                if (Pattern.Matches(value, "^[+-]?[0-9]+((?:\\.?[0-9]+((?:E?[+-]?[0-9]+)|)|))$"))
                                {
                                    try
                                    {
                                        double d = double.Parse(value, CultureInfo.InvariantCulture);
                                        if (!double.IsNaN(d) && !double.IsInfinity(d))
                                        {
                                            if (JSONLDConsts.XsdInteger.Equals(type))
                                            {
                                                int i = (int)d;
                                                if (i.ToString().Equals(value))
                                                {
                                                    rval["@value"] = i;
                                                }
                                            }
                                            else
                                            {
                                                if (JSONLDConsts.XsdDouble.Equals(type))
                                                {
                                                    rval["@value"] = d;
                                                }
                                                else
                                                {
                                                    // we don't know the type, so we should add
                                                    // it to the JSON-LD
                                                    rval["@type"] = type;
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // TODO: This should never happen since we match the
                                        // value with regex!
                                        throw;
                                    }
                                }
                                else
                                {
                                    // do not add xsd:string type
                                    rval["@type"] = type;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!JSONLDConsts.XsdString.Equals(type))
                        {
                            rval["@type"] = type;
                        }
                    }
                }
                return rval;
            }
        }

        //[System.Serializable]
        public class Literal : RDFDataset.Node
        {
            public Literal(string value, string datatype, string language) : base()
            {
                this["type"] = "literal";
                this["value"] = value;
                this["datatype"] = datatype != null ? datatype : JSONLDConsts.XsdString;
                if (language != null)
                {
                    this["language"] = language;
                }
            }

            public override bool IsLiteral()
            {
                return true;
            }

            public override bool IsIRI()
            {
                return false;
            }

            public override bool IsBlankNode()
            {
                return false;
            }

            public override int CompareTo(RDFDataset.Node o)
            {
                if (o == null)
                {
                    // valid nodes are > null nodes
                    return 1;
                }
                if (o.IsIRI())
                {
                    // literals < iri
                    return -1;
                }
                if (o.IsBlankNode())
                {
                    // blank node < iri
                    return -1;
                }
                if (this.GetLanguage() == null && ((RDFDataset.Literal)o).GetLanguage() != null)
                {
                    return -1;
                }
                else
                {
                    if (this.GetLanguage() != null && ((RDFDataset.Literal)o).GetLanguage() == null)
                    {
                        return 1;
                    }
                }
                if (this.GetDatatype() != null)
                {
                    return string.CompareOrdinal(this.GetDatatype(), ((RDFDataset.Literal)o).GetDatatype
                        ());
                }
                else
                {
                    if (((RDFDataset.Literal)o).GetDatatype() != null)
                    {
                        return -1;
                    }
                }
                return 0;
            }
        }

        //[System.Serializable]
        public class IRI : RDFDataset.Node
        {
            public IRI(string iri) : base()
            {
                this["type"] = "IRI";
                this["value"] = iri;
            }

            public override bool IsLiteral()
            {
                return false;
            }

            public override bool IsIRI()
            {
                return true;
            }

            public override bool IsBlankNode()
            {
                return false;
            }
        }

        //[System.Serializable]
        public class BlankNode : RDFDataset.Node
        {
            public BlankNode(string attribute) : base()
            {
                this["type"] = "blank node";
                this["value"] = attribute;
            }

            public override bool IsLiteral()
            {
                return false;
            }

            public override bool IsIRI()
            {
                return false;
            }

            public override bool IsBlankNode()
            {
                return true;
            }
        }

        private static readonly RDFDataset.Node first = new RDFDataset.IRI(JSONLDConsts.RdfFirst
            );

        private static readonly RDFDataset.Node rest = new RDFDataset.IRI(JSONLDConsts.RdfRest
            );

        private static readonly RDFDataset.Node nil = new RDFDataset.IRI(JSONLDConsts.RdfNil
            );

        private readonly IDictionary<string, string> context;

        private JsonLdApi api;

        public RDFDataset() : base()
        {
            // private UniqueNamer namer;
            this["@default"] = new List<Quad>();
            context = new Dictionary<string, string>();
        }

        public RDFDataset(JsonLdApi jsonLdApi) : this()
        {
            // put("@context", context);
            this.api = jsonLdApi;
        }

        public virtual void SetNamespace(string ns, string prefix)
        {
            context[ns] = prefix;
        }

        public virtual string GetNamespace(string ns)
        {
            return context[ns];
        }

        /// <summary>clears all the namespaces in this dataset</summary>
        public virtual void ClearNamespaces()
        {
            context.Clear();
        }

        public virtual IDictionary<string, string> GetNamespaces()
        {
            return context;
        }

        /// <summary>Returns a valid @context containing any namespaces set</summary>
        /// <returns></returns>
        public virtual JObject GetContext()
        {
            JObject rval = new JObject();
            rval.PutAll(context);
            // replace "" with "@vocab"
            if (rval.ContainsKey(string.Empty))
            {
                rval["@vocab"] = JsonLD.Collections.Remove(rval, string.Empty);
            }
            return rval;
        }

        /// <summary>parses a @context object and sets any namespaces found within it</summary>
        /// <param name="context"></param>
        public virtual void ParseContext(JObject context)
        {
            foreach (string key in context.GetKeys())
            {
                JToken val = context[key];
                if ("@vocab".Equals(key))
                {
                    if (val.IsNull() || JsonLdUtils.IsString(val))
                    {
                        SetNamespace(string.Empty, (string)val);
                    }
                }
                else
                {
                    // TODO: the context is actually invalid, should we throw an
                    // exception?
                    if ("@context".Equals(key))
                    {
                        // go deeper!
                        ParseContext((JObject)context["@context"]);
                    }
                    else
                    {
                        if (!JsonLdUtils.IsKeyword(key))
                        {
                            // TODO: should we make sure val is a valid URI prefix (i.e. it
                            // ends with /# or ?)
                            // or is it ok that full URIs for terms are used?
                            if (val.Type == JTokenType.String)
                            {
                                SetNamespace(key, (string)context[key]);
                            }
                            else
                            {
                                if (JsonLdUtils.IsObject(val) && ((JObject)val).ContainsKey("@id"
                                    ))
                                {
                                    SetNamespace(key, (string)((JObject)val)["@id"]);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Adds a triple to the @default graph of this dataset</summary>
        /// <param name="s">the subject for the triple</param>
        /// <param name="p">the predicate for the triple</param>
        /// <param name="value">the value of the literal object for the triple</param>
        /// <param name="datatype">
        /// the datatype of the literal object for the triple (null values
        /// will default to xsd:string)
        /// </param>
        /// <param name="language">the language of the literal object for the triple (or null)
        /// 	</param>
        public virtual void AddTriple(string s, string p, string value, string datatype, 
            string language)
        {
            AddQuad(s, p, value, datatype, language, "@default");
        }

        /// <summary>Adds a triple to the specified graph of this dataset</summary>
        /// <param name="s">the subject for the triple</param>
        /// <param name="p">the predicate for the triple</param>
        /// <param name="value">the value of the literal object for the triple</param>
        /// <param name="datatype">
        /// the datatype of the literal object for the triple (null values
        /// will default to xsd:string)
        /// </param>
        /// <param name="graph">the graph to add this triple to</param>
        /// <param name="language">the language of the literal object for the triple (or null)
        /// 	</param>
        public virtual void AddQuad(string s, string p, string value, string datatype, string
             language, string graph)
        {
            if (graph == null)
            {
                graph = "@default";
            }
            if (!this.ContainsKey(graph))
            {
                this[graph] = new List<Quad>();
            }
            ((IList<Quad>)this[graph]).Add(new RDFDataset.Quad(s, p, value, datatype
                , language, graph));
        }

        /// <summary>Adds a triple to the @default graph of this dataset</summary>
        /// <param name="s">the subject for the triple</param>
        /// <param name="p">the predicate for the triple</param>
        /// <param name="o">the object for the triple</param>
        /// <param name="datatype">
        /// the datatype of the literal object for the triple (null values
        /// will default to xsd:string)
        /// </param>
        /// <param name="language">the language of the literal object for the triple (or null)
        /// 	</param>
        public virtual void AddTriple(string s, string p, string o)
        {
            AddQuad(s, p, o, "@default");
        }

        /// <summary>Adds a triple to thespecified graph of this dataset</summary>
        /// <param name="s">the subject for the triple</param>
        /// <param name="p">the predicate for the triple</param>
        /// <param name="o">the object for the triple</param>
        /// <param name="datatype">
        /// the datatype of the literal object for the triple (null values
        /// will default to xsd:string)
        /// </param>
        /// <param name="graph">the graph to add this triple to</param>
        /// <param name="language">the language of the literal object for the triple (or null)
        /// 	</param>
        public virtual void AddQuad(string s, string p, string o, string graph)
        {
            if (graph == null)
            {
                graph = "@default";
            }
            if (!this.ContainsKey(graph))
            {
                this[graph] = new List<Quad>();
            }
            ((IList<Quad>)this[graph]).Add(new RDFDataset.Quad(s, p, o, graph));
        }

        /// <summary>Creates an array of RDF triples for the given graph.</summary>
        /// <remarks>Creates an array of RDF triples for the given graph.</remarks>
        /// <param name="graph">the graph to create RDF triples for.</param>
        internal virtual void GraphToRDF(string graphName, JObject graph
            )
        {
            // 4.2)
            IList<RDFDataset.Quad> triples = new List<RDFDataset.Quad>();
            // 4.3)
            IEnumerable<string> subjects = graph.GetKeys();
            // Collections.sort(subjects);
            foreach (string id in subjects)
            {
                if (JsonLdUtils.IsRelativeIri(id))
                {
                    continue;
                }
                JObject node = (JObject)graph[id];
                JArray properties = new JArray(node.GetKeys());
                properties.SortInPlace();
                foreach (string property in properties)
                {
                    var localProperty = property;
                    JArray values;
                    // 4.3.2.1)
                    if ("@type".Equals(localProperty))
                    {
                        values = (JArray)node["@type"];
                        localProperty = JSONLDConsts.RdfType;
                    }
                    else
                    {
                        // 4.3.2.2)
                        if (JsonLdUtils.IsKeyword(localProperty))
                        {
                            continue;
                        }
                        else
                        {
                            // 4.3.2.3)
                            if (localProperty.StartsWith("_:") && !api.opts.GetProduceGeneralizedRdf())
                            {
                                continue;
                            }
                            else
                            {
                                // 4.3.2.4)
                                if (JsonLdUtils.IsRelativeIri(localProperty))
                                {
                                    continue;
                                }
                                else
                                {
                                    values = (JArray)node[localProperty];
                                }
                            }
                        }
                    }
                    RDFDataset.Node subject;
                    if (id.IndexOf("_:") == 0)
                    {
                        // NOTE: don't rename, just set it as a blank node
                        subject = new RDFDataset.BlankNode(id);
                    }
                    else
                    {
                        subject = new RDFDataset.IRI(id);
                    }
                    // RDF predicates
                    RDFDataset.Node predicate;
                    if (localProperty.StartsWith("_:"))
                    {
                        predicate = new RDFDataset.BlankNode(localProperty);
                    }
                    else
                    {
                        predicate = new RDFDataset.IRI(localProperty);
                    }
                    foreach (JToken item in values)
                    {
                        // convert @list to triples
                        if (JsonLdUtils.IsList(item))
                        {
                            JArray list = (JArray)((JObject)item)["@list"];
                            RDFDataset.Node last = null;
                            RDFDataset.Node firstBNode = nil;
                            if (!list.IsEmpty())
                            {
                                last = ObjectToRDF(list[list.Count - 1]);
                                firstBNode = new RDFDataset.BlankNode(api.GenerateBlankNodeIdentifier());
                            }
                            triples.Add(new RDFDataset.Quad(subject, predicate, firstBNode, graphName));
                            for (int i = 0; i < list.Count - 1; i++)
                            {
                                RDFDataset.Node @object = ObjectToRDF(list[i]);
                                triples.Add(new RDFDataset.Quad(firstBNode, first, @object, graphName));
                                RDFDataset.Node restBNode = new RDFDataset.BlankNode(api.GenerateBlankNodeIdentifier
                                    ());
                                triples.Add(new RDFDataset.Quad(firstBNode, rest, restBNode, graphName));
                                firstBNode = restBNode;
                            }
                            if (last != null)
                            {
                                triples.Add(new RDFDataset.Quad(firstBNode, first, last, graphName));
                                triples.Add(new RDFDataset.Quad(firstBNode, rest, nil, graphName));
                            }
                        }
                        else
                        {
                            // convert value or node object to triple
                            RDFDataset.Node @object = ObjectToRDF(item);
                            if (@object != null)
                            {
                                triples.Add(new RDFDataset.Quad(subject, predicate, @object, graphName));
                            }
                        }
                    }
                }
            }
            this[graphName] = triples;
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
        private RDFDataset.Node ObjectToRDF(JToken item)
        {
            // convert value object to RDF
            if (JsonLdUtils.IsValue(item))
            {
                JToken value = ((JObject)item)["@value"];
                JToken datatype = ((JObject)item)["@type"];
                // convert to XSD datatypes as appropriate
                if (value.Type == JTokenType.Boolean || value.Type == JTokenType.Float || value.Type == JTokenType.Integer)
                {
                    // convert to XSD datatype
                    if (value.Type == JTokenType.Boolean)
                    {
                        return new RDFDataset.Literal(value.ToString(), datatype.IsNull() ? JSONLDConsts.XsdBoolean
                             : (string)datatype, null);
                    }
                    else
                    {
                        if (value.Type == JTokenType.Float || datatype.SafeCompare(JSONLDConsts.XsdDouble))
                        {
                            // Workaround for Newtonsoft.Json's refusal to cast a JTokenType.Integer to a double.
                            if (value.Type == JTokenType.Integer)
                            {
                                int number = (int)value;
                                value = new JValue((double)number);
                            }
                            // canonical double representation
                            return new RDFDataset.Literal(string.Format("{0:0.0###############E0}", (double)value), datatype.IsNull() ? JSONLDConsts.XsdDouble
                                 : (string)datatype, null);
                        }
                        else
                        {
                            return new RDFDataset.Literal(string.Format("{0:0}",value), datatype.IsNull() ? JSONLDConsts.XsdInteger
                                 : (string)datatype, null);
                        }
                    }
                }
                else
                {
                    if (((JObject)item).ContainsKey("@language"))
                    {
                        return new RDFDataset.Literal((string)value, datatype.IsNull() ? JSONLDConsts.RdfLangstring
                             : (string)datatype, (string)((JObject)item)["@language"]);
                    }
                    else
                    {
                        return new RDFDataset.Literal((string)value, datatype.IsNull() ? JSONLDConsts.XsdString
                             : (string)datatype, null);
                    }
                }
            }
            else
            {
                // convert string/node object to RDF
                string id;
                if (JsonLdUtils.IsObject(item))
                {
                    id = (string)((JObject)item)["@id"];
                    if (JsonLdUtils.IsRelativeIri(id))
                    {
                        return null;
                    }
                }
                else
                {
                    id = (string)item;
                }
                if (id.IndexOf("_:") == 0)
                {
                    // NOTE: once again no need to rename existing blank nodes
                    return new RDFDataset.BlankNode(id);
                }
                else
                {
                    return new RDFDataset.IRI(id);
                }
            }
        }

        public virtual IList<string> GraphNames()
        {
            return new List<string>(Keys);
        }

        public virtual IList<Quad> GetQuads(string graphName)
        {
            return (IList<Quad>)this[graphName];
        }
    }
}
