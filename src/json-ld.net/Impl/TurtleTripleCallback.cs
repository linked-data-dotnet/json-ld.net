using System.Collections.Generic;
using JsonLD.Core;
using JsonLD.GenericJson;

namespace JsonLD.Impl
{
    internal class TurtleTripleCallback : IJSONLDTripleCallback
    {
        private const int MaxLineLength = 160;

        private const int TabSpaces = 4;

        private const string ColsKey = "..cols..";

        private sealed class _Dictionary_32 : Dictionary<string, string>
        {
            public _Dictionary_32()
            {
                {
                }
            }
        }

        internal readonly IDictionary<string, string> availableNamespaces = new _Dictionary_32
            ();

        internal ICollection<string> usedNamespaces;

        public TurtleTripleCallback()
        {
        }

        // this shouldn't be a
        // valid iri/bnode i
        // hope!
        // TODO: fill with default namespaces
        public virtual object Call(RDFDataset dataset)
        {
            foreach (KeyValuePair<string, string> e in dataset.GetNamespaces().GetEnumerableSelf
                ())
            {
                availableNamespaces[e.Value] = e.Key;
            }
            usedNamespaces = new HashSet<string>();
            GenericJsonObject refs = new GenericJsonObject();
            GenericJsonObject ttl = new GenericJsonObject();
            foreach (string graphName in dataset.Keys)
            {
                string localGraphName = graphName;
                IList<RDFDataset.Quad> triples = (IList<RDFDataset.Quad>)dataset.GetQuads(localGraphName);
                if ("@default".Equals(localGraphName))
                {
                    localGraphName = null;
                }
                // http://www.w3.org/TR/turtle/#unlabeled-bnodes
                // TODO: implement nesting for unlabled nodes
                // map of what the output should look like
                // subj (or [ if bnode) > pred > obj
                // > obj (set ref if IRI)
                // > pred > obj (set ref if bnode)
                // subj > etc etc etc
                // subjid -> [ ref, ref, ref ]
                string prevSubject = string.Empty;
                string prevPredicate = string.Empty;
                GenericJsonObject thisSubject = null;
                GenericJsonArray thisPredicate = null;
                foreach (RDFDataset.Quad triple in triples)
                {
                    string subject = triple.GetSubject().GetValue();
                    string predicate = triple.GetPredicate().GetValue();
                    if (prevSubject.Equals(subject))
                    {
                        if (prevPredicate.Equals(predicate))
                        {
                        }
                        else
                        {
                            // nothing to do
                            // new predicate
                            if (thisSubject.ContainsKey(predicate))
                            {
                                thisPredicate = (GenericJsonArray)thisSubject[predicate];
                            }
                            else
                            {
                                thisPredicate = new GenericJsonArray();
                                thisSubject[predicate] = thisPredicate;
                            }
                            prevPredicate = predicate;
                        }
                    }
                    else
                    {
                        // new subject
                        if (ttl.ContainsKey(subject))
                        {
                            thisSubject = (GenericJsonObject)ttl[subject];
                        }
                        else
                        {
                            thisSubject = new GenericJsonObject();
                            ttl[subject] = thisSubject;
                        }
                        if (thisSubject.ContainsKey(predicate))
                        {
                            thisPredicate = (GenericJsonArray)thisSubject[predicate];
                        }
                        else
                        {
                            thisPredicate = new GenericJsonArray();
                            thisSubject[predicate] = thisPredicate;
                        }
                        prevSubject = subject;
                        prevPredicate = predicate;
                    }
                    if (triple.GetObject().IsLiteral())
                    {
                        thisPredicate.Add(triple.GetObject());
                    }
                    else
                    {
                        string o = triple.GetObject().GetValue();
                        if (o.StartsWith("_:"))
                        {
                            // add ref to o
                            if (!refs.ContainsKey(o))
                            {
                                refs[o] = new GenericJsonArray();
                            }
                            ((GenericJsonArray)refs[o]).Add(thisPredicate);
                        }
                        thisPredicate.Add(o);
                    }
                }
            }
            GenericJsonObject collections = new GenericJsonObject();
            GenericJsonArray subjects = new GenericJsonArray(ttl.GetKeys());
            // find collections
            foreach (string subj in subjects)
            {
                GenericJsonObject preds = (GenericJsonObject)ttl[subj];
                if (preds != null && preds.ContainsKey(JSONLDConsts.RdfFirst))
                {
                    GenericJsonArray col = new GenericJsonArray();
                    collections[subj] = col;
                    while (true)
                    {
                        GenericJsonArray first = (GenericJsonArray)JsonLD.Collections.Remove(preds, JSONLDConsts.RdfFirst);
                        GenericJsonToken o = first[0];
                        col.Add(o);
                        // refs
                        if (refs.ContainsKey((string)o))
                        {
                            ((GenericJsonArray)refs[(string)o]).Remove(first);
                            ((GenericJsonArray)refs[(string)o]).Add(col);
                        }
                        string next = (string)JsonLD.Collections.Remove(preds, JSONLDConsts.RdfRest)[0
                            ];
                        if (JSONLDConsts.RdfNil.Equals(next))
                        {
                            // end of this list
                            break;
                        }
                        // if collections already contains a value for "next", add
                        // it to this col and break out
                        if (collections.ContainsKey(next))
                        {
                            JsonLD.Collections.AddAll(col, (GenericJsonArray)JsonLD.Collections.Remove(collections, next));
                            break;
                        }
                        preds = (GenericJsonObject)JsonLD.Collections.Remove(ttl, next);
                        JsonLD.Collections.Remove(refs, next);
                    }
                }
            }
            // process refs (nesting referenced bnodes if only one reference to them
            // in the whole graph)
            foreach (string id in refs.GetKeys())
            {
                // skip items if there is more than one reference to them in the
                // graph
                if (((GenericJsonArray)refs[id]).Count > 1)
                {
                    continue;
                }
                // otherwise embed them into the referenced location
                GenericJsonToken @object = JsonLD.Collections.Remove(ttl, id);
                if (collections.ContainsKey(id))
                {
                    @object = new GenericJsonObject();
                    GenericJsonArray tmp = new GenericJsonArray();
                    tmp.Add(JsonLD.Collections.Remove(collections, id));
                    ((GenericJsonObject)@object)[ColsKey] = tmp;
                }
                GenericJsonArray predicate = (GenericJsonArray)refs[id][0];
                // replace the one bnode ref with the object
                predicate[predicate.LastIndexOf(id)] = (GenericJsonToken)@object;
            }
            // replace the rest of the collections
            foreach (string id_1 in collections.GetKeys())
            {
                GenericJsonObject subj_1 = (GenericJsonObject)ttl[id_1];
                if (!subj_1.ContainsKey(ColsKey))
                {
                    subj_1[ColsKey] = new GenericJsonArray();
                }
                ((GenericJsonArray)subj_1[ColsKey]).Add(collections[id_1]);
            }
            // build turtle output
            string output = GenerateTurtle(ttl, 0, 0, false);
            string prefixes = string.Empty;
            foreach (string prefix in usedNamespaces)
            {
                string name = availableNamespaces[prefix];
                prefixes += "@prefix " + name + ": <" + prefix + "> .\n";
            }
            return (string.Empty.Equals(prefixes) ? string.Empty : prefixes + "\n") + output;
        }

        private string GenerateObject(object @object, string sep, bool hasNext, int indentation
            , int lineLength)
        {
            string rval = string.Empty;
            string obj;
            if (@object is string)
            {
                obj = GetURI((string)@object);
            }
            else
            {
                if (@object is RDFDataset.Literal)
                {
                    obj = ((RDFDataset.Literal)@object).GetValue();
                    string lang = ((RDFDataset.Literal)@object).GetLanguage();
                    string dt = ((RDFDataset.Literal)@object).GetDatatype();
                    if (lang != null)
                    {
                        obj = "\"" + obj + "\"";
                        obj += "@" + lang;
                    }
                    else
                    {
                        if (dt != null)
                        {
                            // TODO: this probably isn't an exclusive list of all the
                            // datatype literals that can be represented as native types
                            if (!(JSONLDConsts.XsdDouble.Equals(dt) || JSONLDConsts.XsdInteger.Equals(dt) || 
                                JSONLDConsts.XsdFloat.Equals(dt) || JSONLDConsts.XsdBoolean.Equals(dt)))
                            {
                                obj = "\"" + obj + "\"";
                                if (!JSONLDConsts.XsdString.Equals(dt))
                                {
                                    obj += "^^" + GetURI(dt);
                                }
                            }
                        }
                        else
                        {
                            obj = "\"" + obj + "\"";
                        }
                    }
                }
                else
                {
                    // must be an object
                    GenericJsonObject tmp = new GenericJsonObject();
                    tmp["_:x"] = (GenericJsonObject)@object;
                    obj = GenerateTurtle(tmp, indentation + 1, lineLength, true);
                }
            }
            int idxofcr = obj.IndexOf("\n");
            // check if output will fix in the max line length (factor in comma if
            // not the last item, current line length and length to the next CR)
            if ((hasNext ? 1 : 0) + lineLength + (idxofcr != -1 ? idxofcr : obj.Length) > MaxLineLength)
            {
                rval += "\n" + Tabs(indentation + 1);
                lineLength = (indentation + 1) * TabSpaces;
            }
            rval += obj;
            if (idxofcr != -1)
            {
                lineLength += (obj.Length - obj.LastIndexOf("\n"));
            }
            else
            {
                lineLength += obj.Length;
            }
            if (hasNext)
            {
                rval += sep;
                lineLength += sep.Length;
                if (lineLength < MaxLineLength)
                {
                    rval += " ";
                    lineLength++;
                }
                else
                {
                    rval += "\n";
                }
            }
            return rval;
        }

        private string GenerateTurtle(GenericJsonObject ttl, int indentation, int lineLength, bool isObject)
        {
            string rval = string.Empty;
            IEnumerator<string> subjIter = ttl.GetKeys().GetEnumerator();
            while (subjIter.MoveNext())
            {
                string subject = subjIter.Current;
                GenericJsonObject subjval = (GenericJsonObject)ttl[subject];
                // boolean isBlankNode = subject.startsWith("_:");
                bool hasOpenBnodeBracket = false;
                if (subject.StartsWith("_:"))
                {
                    // only open blank node bracket the node doesn't contain any
                    // collections
                    if (!subjval.ContainsKey(ColsKey))
                    {
                        rval += "[ ";
                        lineLength += 2;
                        hasOpenBnodeBracket = true;
                    }
                    // TODO: according to http://www.rdfabout.com/demo/validator/
                    // 1) collections as objects cannot contain any predicates other
                    // than rdf:first and rdf:rest
                    // 2) collections cannot be surrounded with [ ]
                    // check for collection
                    if (subjval.ContainsKey(ColsKey))
                    {
                        GenericJsonArray collections = (GenericJsonArray)JsonLD.Collections.Remove(subjval, ColsKey);
                        foreach (GenericJsonToken collection in collections)
                        {
                            rval += "( ";
                            lineLength += 2;

                            // TODO(sblom): What does JSON.NET "Children" do?
                            IEnumerator<GenericJsonToken> objIter = ((GenericJsonArray)collection).GetEnumerator();
                            while (objIter.MoveNext())
                            {
                                GenericJsonToken @object = objIter.Current;
                                rval += GenerateObject(@object, string.Empty, objIter.MoveNext(), indentation, lineLength
                                    );
                                lineLength = rval.Length - rval.LastIndexOf("\n");
                            }
                            rval += " ) ";
                            lineLength += 3;
                        }
                    }
                }
                else
                {
                    // check for blank node
                    rval += GetURI(subject) + " ";
                    lineLength += subject.Length + 1;
                }
                IEnumerator<string> predIter = ttl[subject].GetKeys().GetEnumerator();
                while (predIter.MoveNext())
                {
                    string predicate = predIter.Current;
                    rval += GetURI(predicate) + " ";
                    lineLength += predicate.Length + 1;
                    IEnumerator<GenericJsonToken> objIter = ((GenericJsonArray)ttl[subject][predicate]).GetEnumerator();
                    while (objIter.MoveNext())
                    {
                        GenericJsonToken @object = objIter.Current;
                        rval += GenerateObject(@object, ",", objIter.MoveNext(), indentation, lineLength);
                        lineLength = rval.Length - rval.LastIndexOf("\n");
                    }
                    if (predIter.MoveNext())
                    {
                        rval += " ;\n" + Tabs(indentation + 1);
                        lineLength = (indentation + 1) * TabSpaces;
                    }
                }
                if (hasOpenBnodeBracket)
                {
                    rval += " ]";
                }
                if (!isObject)
                {
                    rval += " .\n";
                    if (subjIter.MoveNext())
                    {
                        // add blank space if we have another
                        // object below this
                        rval += "\n";
                    }
                }
            }
            return rval;
        }

        // TODO: Assert (TAB_SPACES == 4) otherwise this needs to be edited, and
        // should fail to compile
        private string Tabs(int tabs)
        {
            string rval = string.Empty;
            for (int i = 0; i < tabs; i++)
            {
                rval += "    ";
            }
            // using spaces for tabs
            return rval;
        }

        /// <summary>
        /// checks the URI for a prefix, and if one is found, set used prefixes to
        /// true
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        private string GetURI(string uri)
        {
            // check for bnode
            if (uri.StartsWith("_:"))
            {
                // return the bnode id
                return uri;
            }
            foreach (string prefix in availableNamespaces.Keys)
            {
                if (uri.StartsWith(prefix))
                {
                    usedNamespaces.Add(prefix);
                    // return the prefixed URI
                    return availableNamespaces[prefix] + ":" + JsonLD.JavaCompat.Substring(uri, prefix.
                        Length);
                }
            }
            // return the full URI
            return "<" + uri + ">";
        }
    }
}
