using System.Collections.Generic;
using JsonLD.Core;
using JsonLD.Impl;
using Newtonsoft.Json.Linq;

namespace JsonLD.Impl
{
    /// <summary>
    /// A (probably terribly slow) Parser for turtle -&gt; the internal RDFDataset used
    /// by JSOND-Java
    /// TODO: this probably needs to be changed to use a proper parser/lexer
    /// </summary>
    /// <author>Tristan</author>
    public class TurtleRDFParser : IRDFParser
    {
        internal class Regex
        {
            public static readonly Pattern PrefixId = Pattern.Compile("@prefix" + JsonLD.Core.Regex
                .Ws1N + JsonLD.Core.Regex.PnameNs + JsonLD.Core.Regex.Ws1N + JsonLD.Core.Regex
                .Iriref + JsonLD.Core.Regex.Ws0N + "\\." + JsonLD.Core.Regex.Ws0N);

            public static readonly Pattern Base = Pattern.Compile("@base" + JsonLD.Core.Regex
                .Ws1N + JsonLD.Core.Regex.Iriref + JsonLD.Core.Regex.Ws0N + "\\." + JsonLD.Core.Regex
                .Ws0N);

            public static readonly Pattern SparqlPrefix = Pattern.Compile("[Pp][Rr][Ee][Ff][Ii][Xx]"
                 + JsonLD.Core.Regex.Ws + JsonLD.Core.Regex.PnameNs + JsonLD.Core.Regex
                .Ws + JsonLD.Core.Regex.Iriref + JsonLD.Core.Regex.Ws0N);

            public static readonly Pattern SparqlBase = Pattern.Compile("[Bb][Aa][Ss][Ee]" + 
                JsonLD.Core.Regex.Ws + JsonLD.Core.Regex.Iriref + JsonLD.Core.Regex.Ws0N
                );

            public static readonly Pattern PrefixedName = Pattern.Compile("(?:" + JsonLD.Core.Regex
                .PnameLn + "|" + JsonLD.Core.Regex.PnameNs + ")");

            public static readonly Pattern Iri = Pattern.Compile("(?:" + JsonLD.Core.Regex
                .Iriref + "|" + PrefixedName + ")");

            public static readonly Pattern Anon = Pattern.Compile("(?:\\[" + JsonLD.Core.Regex
                .Ws + "*\\])");

            public static readonly Pattern BlankNode = Pattern.Compile(JsonLD.Core.Regex.BlankNodeLabel
                 + "|" + Anon);

            public static readonly Pattern String = Pattern.Compile("(" + JsonLD.Core.Regex
                .StringLiteralLongSingleQuote + "|" + JsonLD.Core.Regex.StringLiteralLongQuote
                 + "|" + JsonLD.Core.Regex.StringLiteralQuote + "|" + JsonLD.Core.Regex.StringLiteralSingleQuote
                 + ")");

            public static readonly Pattern BooleanLiteral = Pattern.Compile("(true|false)");

            public static readonly Pattern RdfLiteral = Pattern.Compile(String + "(?:" + JsonLD.Core.Regex
                .Langtag + "|\\^\\^" + Iri + ")?");

            public static readonly Pattern NumericLiteral = Pattern.Compile("(" + JsonLD.Core.Regex
                .Double + ")|(" + JsonLD.Core.Regex.Decimal + ")|(" + JsonLD.Core.Regex.Integer
                 + ")");

            public static readonly Pattern Literal = Pattern.Compile(RdfLiteral + "|" + NumericLiteral
                 + "|" + BooleanLiteral);

            public static readonly Pattern Directive = Pattern.Compile("^(?:" + PrefixId + "|"
                 + Base + "|" + SparqlPrefix + "|" + SparqlBase + ")");

            public static readonly Pattern Subject = Pattern.Compile("^" + Iri + "|" + BlankNode
                );

            public static readonly Pattern Predicate = Pattern.Compile("^" + Iri + "|a" + JsonLD.Core.Regex
                .Ws1N);

            public static readonly Pattern Object = Pattern.Compile("^" + Iri + "|" + BlankNode
                 + "|" + Literal);

            public static readonly Pattern Eoln = Pattern.Compile("(?:\r\n)|(?:\n)|(?:\r)");

            public static readonly Pattern NextEoln = Pattern.Compile("^.*(?:" + Eoln + ")" +
                 JsonLD.Core.Regex.Ws0N);

            public static readonly Pattern CommentOrWs = Pattern.Compile("^(?:(?:[#].*(?:" + 
                Eoln + ")" + JsonLD.Core.Regex.Ws0N + ")|(?:" + JsonLD.Core.Regex.Ws1N + "))"
                );
            // others
            // final public static Pattern WS_AT_LINE_START = Pattern.compile("^" +
            // WS_1_N);
            // final public static Pattern EMPTY_LINE = Pattern.compile("^" + WS +
            // "*$");
        }

        private class State
        {
            internal string baseIri = string.Empty;

            internal IDictionary<string, string> namespaces = new Dictionary<string, string>(
                );

            internal string curSubject = null;

            internal string curPredicate = null;

            internal string line = null;

            internal int lineNumber = 0;

            internal int linePosition = 0;

            internal UniqueNamer namer = new UniqueNamer("_:b");

            private readonly Stack<IDictionary<string, string>> stack = new Stack<IDictionary
                <string, string>>();

            public bool expectingBnodeClose = false;

            /// <exception cref="JsonLD.Core.JsonLdError"></exception>
            public State(TurtleRDFParser _enclosing, string input)
            {
                this._enclosing = _enclosing;
                // int bnodes = 0;
                // {{ getName(); }}; // call
                // getName() after
                // construction to make
                // first active bnode _:b1
                this.line = input;
                this.lineNumber = 1;
                this.AdvanceLinePosition(0);
            }

            public virtual void Push()
            {
                this.stack.Push(new _Dictionary_126(this));
                this.expectingBnodeClose = true;
                this.curSubject = null;
                this.curPredicate = null;
            }

            private sealed class _Dictionary_126 : Dictionary<string, string>
            {
                State _enclosing;
                public _Dictionary_126(State trp)
                {
                    _enclosing = trp;
                    {
                        this[this._enclosing.curSubject] = this._enclosing.curPredicate;
                    }
                }
            }

            public virtual void Pop()
            {
                if (this.stack.Count > 0)
                {
                    foreach (KeyValuePair<string, string> x in this.stack.Pop().GetEnumerableSelf())
                    {
                        this.curSubject = x.Key;
                        this.curPredicate = x.Value;
                    }
                }
                if (this.stack.Count == 0)
                {
                    this.expectingBnodeClose = false;
                }
            }

            /// <exception cref="JsonLD.Core.JsonLdError"></exception>
            private void AdvanceLineNumber()
            {
                Matcher match = TurtleRDFParser.Regex.NextEoln.Matcher(this.line);
                if (match.Find())
                {
                    string[] split = match.Group(0).Split(string.Empty + TurtleRDFParser.Regex.Eoln);
                    this.lineNumber += (split.Length - 1);
                    this.linePosition += split[split.Length - 1].Length;
                    this.line = JsonLD.JavaCompat.Substring(this.line, match.Group(0).Length);
                }
            }

            /// <exception cref="JsonLD.Core.JsonLdError"></exception>
            public virtual void AdvanceLinePosition(int len)
            {
                if (len > 0)
                {
                    this.linePosition += len;
                    this.line = JsonLD.JavaCompat.Substring(this.line, len);
                }
                while (!string.Empty.Equals(this.line))
                {
                    // clear any whitespace
                    Matcher match = TurtleRDFParser.Regex.CommentOrWs.Matcher(this.line);
                    if (match.Find() && match.Group(0).Length > 0)
                    {
                        Matcher eoln = TurtleRDFParser.Regex.Eoln.Matcher(match.Group(0));
                        int end = 0;
                        while (eoln.Find())
                        {
                            this.lineNumber += 1;
                            end = eoln.End();
                        }
                        this.linePosition = match.Group(0).Length - end;
                        this.line = JsonLD.JavaCompat.Substring(this.line, match.Group(0).Length);
                    }
                    else
                    {
                        break;
                    }
                }
                if (string.Empty.Equals(this.line) && !this.EndIsOK())
                {
                    throw new JsonLdError(JsonLdError.Error.ParseError, "Error while parsing Turtle; unexpected end of input. {line: "
                         + this.lineNumber + ", position:" + this.linePosition + "}");
                }
            }

            private bool EndIsOK()
            {
                return this.curSubject == null && this.stack.Count == 0;
            }

            /// <exception cref="JsonLD.Core.JsonLdError"></exception>
            public virtual string ExpandIRI(string ns, string name)
            {
                if (this.namespaces.ContainsKey(ns))
                {
                    return this.namespaces[ns] + name;
                }
                else
                {
                    throw new JsonLdError(JsonLdError.Error.ParseError, "No prefix found for: " + ns 
                        + " {line: " + this.lineNumber + ", position:" + this.linePosition + "}");
                }
            }

            private readonly TurtleRDFParser _enclosing;
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual RDFDataset Parse(JToken input)
        {
            if (!(input.Type == JTokenType.String))
            {
                throw new JsonLdError(JsonLdError.Error.InvalidInput, "Invalid input; Triple RDF Parser requires a string input"
                    );
            }
            RDFDataset result = new RDFDataset();
            TurtleRDFParser.State state = new TurtleRDFParser.State(this, (string)input);
            while (!string.Empty.Equals(state.line))
            {
                // check if line is a directive
                Matcher match = TurtleRDFParser.Regex.Directive.Matcher(state.line);
                if (match.Find())
                {
                    if (match.Group(1) != null || match.Group(4) != null)
                    {
                        string ns = match.Group(1) != null ? match.Group(1) : match.Group(4);
                        string iri = match.Group(1) != null ? match.Group(2) : match.Group(5);
                        if (!iri.Contains(":"))
                        {
                            iri = state.baseIri + iri;
                        }
                        iri = RDFDatasetUtils.Unescape(iri);
                        ValidateIRI(state, iri);
                        state.namespaces[ns] = iri;
                        result.SetNamespace(ns, iri);
                    }
                    else
                    {
                        string @base = match.Group(3) != null ? match.Group(3) : match.Group(6);
                        @base = RDFDatasetUtils.Unescape(@base);
                        ValidateIRI(state, @base);
                        if (!@base.Contains(":"))
                        {
                            state.baseIri = state.baseIri + @base;
                        }
                        else
                        {
                            state.baseIri = @base;
                        }
                    }
                    state.AdvanceLinePosition(match.Group(0).Length);
                    continue;
                }
                if (state.curSubject == null)
                {
                    // we need to match a subject
                    match = TurtleRDFParser.Regex.Subject.Matcher(state.line);
                    if (match.Find())
                    {
                        string iri;
                        if (match.Group(1) != null)
                        {
                            // matched IRI
                            iri = RDFDatasetUtils.Unescape(match.Group(1));
                            if (!iri.Contains(":"))
                            {
                                iri = state.baseIri + iri;
                            }
                        }
                        else
                        {
                            if (match.Group(2) != null)
                            {
                                // matched NS:NAME
                                string ns = match.Group(2);
                                string name = UnescapeReserved(match.Group(3));
                                iri = state.ExpandIRI(ns, name);
                            }
                            else
                            {
                                if (match.Group(4) != null)
                                {
                                    // match ns: only
                                    iri = state.ExpandIRI(match.Group(4), string.Empty);
                                }
                                else
                                {
                                    if (match.Group(5) != null)
                                    {
                                        // matched BNODE
                                        iri = state.namer.GetName(match.Group(0).Trim());
                                    }
                                    else
                                    {
                                        // matched anon node
                                        iri = state.namer.GetName();
                                    }
                                }
                            }
                        }
                        // make sure IRI still matches an IRI after escaping
                        ValidateIRI(state, iri);
                        state.curSubject = iri;
                        state.AdvanceLinePosition(match.Group(0).Length);
                    }
                    else
                    {
                        // handle blank nodes
                        if (state.line.StartsWith("["))
                        {
                            string bnode = state.namer.GetName();
                            state.AdvanceLinePosition(1);
                            state.Push();
                            state.curSubject = bnode;
                        }
                        else
                        {
                            // handle collections
                            if (state.line.StartsWith("("))
                            {
                                string bnode = state.namer.GetName();
                                // so we know we want a predicate if the collection close
                                // isn't followed by a subject end
                                state.curSubject = bnode;
                                state.AdvanceLinePosition(1);
                                state.Push();
                                state.curSubject = bnode;
                                state.curPredicate = JSONLDConsts.RdfFirst;
                            }
                            else
                            {
                                // make sure we have a subject already
                                throw new JsonLdError(JsonLdError.Error.ParseError, "Error while parsing Turtle; missing expected subject. {line: "
                                     + state.lineNumber + "position: " + state.linePosition + "}");
                            }
                        }
                    }
                }
                if (state.curPredicate == null)
                {
                    // match predicate
                    match = TurtleRDFParser.Regex.Predicate.Matcher(state.line);
                    if (match.Find())
                    {
                        string iri = string.Empty;
                        if (match.Group(1) != null)
                        {
                            // matched IRI
                            iri = RDFDatasetUtils.Unescape(match.Group(1));
                            if (!iri.Contains(":"))
                            {
                                iri = state.baseIri + iri;
                            }
                        }
                        else
                        {
                            if (match.Group(2) != null)
                            {
                                // matched NS:NAME
                                string ns = match.Group(2);
                                string name = UnescapeReserved(match.Group(3));
                                iri = state.ExpandIRI(ns, name);
                            }
                            else
                            {
                                if (match.Group(4) != null)
                                {
                                    // matched ns:
                                    iri = state.ExpandIRI(match.Group(4), string.Empty);
                                }
                                else
                                {
                                    // matched "a"
                                    iri = JSONLDConsts.RdfType;
                                }
                            }
                        }
                        ValidateIRI(state, iri);
                        state.curPredicate = iri;
                        state.AdvanceLinePosition(match.Group(0).Length);
                    }
                    else
                    {
                        throw new JsonLdError(JsonLdError.Error.ParseError, "Error while parsing Turtle; missing expected predicate. {line: "
                             + state.lineNumber + "position: " + state.linePosition + "}");
                    }
                }
                // expecting bnode or object
                // match BNODE values
                if (state.line.StartsWith("["))
                {
                    string bnode = state.namer.GetName();
                    result.AddTriple(state.curSubject, state.curPredicate, bnode);
                    state.AdvanceLinePosition(1);
                    // check for anonymous objects
                    if (state.line.StartsWith("]"))
                    {
                        state.AdvanceLinePosition(1);
                    }
                    else
                    {
                        // next we expect a statement or object separator
                        // otherwise we're inside the blank node
                        state.Push();
                        state.curSubject = bnode;
                        // next we expect a predicate
                        continue;
                    }
                }
                else
                {
                    // match collections
                    if (state.line.StartsWith("("))
                    {
                        state.AdvanceLinePosition(1);
                        // check for empty collection
                        if (state.line.StartsWith(")"))
                        {
                            state.AdvanceLinePosition(1);
                            result.AddTriple(state.curSubject, state.curPredicate, JSONLDConsts.RdfNil);
                        }
                        else
                        {
                            // next we expect a statement or object separator
                            // otherwise we're inside the collection
                            string bnode = state.namer.GetName();
                            result.AddTriple(state.curSubject, state.curPredicate, bnode);
                            state.Push();
                            state.curSubject = bnode;
                            state.curPredicate = JSONLDConsts.RdfFirst;
                            continue;
                        }
                    }
                    else
                    {
                        // match object
                        match = TurtleRDFParser.Regex.Object.Matcher(state.line);
                        if (match.Find())
                        {
                            string iri = null;
                            if (match.Group(1) != null)
                            {
                                // matched IRI
                                iri = RDFDatasetUtils.Unescape(match.Group(1));
                                if (!iri.Contains(":"))
                                {
                                    iri = state.baseIri + iri;
                                }
                            }
                            else
                            {
                                if (match.Group(2) != null)
                                {
                                    // matched NS:NAME
                                    string ns = match.Group(2);
                                    string name = UnescapeReserved(match.Group(3));
                                    iri = state.ExpandIRI(ns, name);
                                }
                                else
                                {
                                    if (match.Group(4) != null)
                                    {
                                        // matched ns:
                                        iri = state.ExpandIRI(match.Group(4), string.Empty);
                                    }
                                    else
                                    {
                                        if (match.Group(5) != null)
                                        {
                                            // matched BNODE
                                            iri = state.namer.GetName(match.Group(0).Trim());
                                        }
                                    }
                                }
                            }
                            if (iri != null)
                            {
                                ValidateIRI(state, iri);
                                // we have a object
                                result.AddTriple(state.curSubject, state.curPredicate, iri);
                            }
                            else
                            {
                                // we have a literal
                                string value = match.Group(6);
                                string lang = null;
                                string datatype = null;
                                if (value != null)
                                {
                                    // we have a string literal
                                    value = UnquoteString(value);
                                    value = RDFDatasetUtils.Unescape(value);
                                    lang = match.Group(7);
                                    if (lang == null)
                                    {
                                        if (match.Group(8) != null)
                                        {
                                            datatype = RDFDatasetUtils.Unescape(match.Group(8));
                                            if (!datatype.Contains(":"))
                                            {
                                                datatype = state.baseIri + datatype;
                                            }
                                            ValidateIRI(state, datatype);
                                        }
                                        else
                                        {
                                            if (match.Group(9) != null)
                                            {
                                                datatype = state.ExpandIRI(match.Group(9), UnescapeReserved(match.Group(10)));
                                            }
                                            else
                                            {
                                                if (match.Group(11) != null)
                                                {
                                                    datatype = state.ExpandIRI(match.Group(11), string.Empty);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        datatype = JSONLDConsts.RdfLangstring;
                                    }
                                }
                                else
                                {
                                    if (match.Group(12) != null)
                                    {
                                        // integer literal
                                        value = match.Group(12);
                                        datatype = JSONLDConsts.XsdDouble;
                                    }
                                    else
                                    {
                                        if (match.Group(13) != null)
                                        {
                                            // decimal literal
                                            value = match.Group(13);
                                            datatype = JSONLDConsts.XsdDecimal;
                                        }
                                        else
                                        {
                                            if (match.Group(14) != null)
                                            {
                                                // double literal
                                                value = match.Group(14);
                                                datatype = JSONLDConsts.XsdInteger;
                                            }
                                            else
                                            {
                                                if (match.Group(15) != null)
                                                {
                                                    // boolean literal
                                                    value = match.Group(15);
                                                    datatype = JSONLDConsts.XsdBoolean;
                                                }
                                            }
                                        }
                                    }
                                }
                                result.AddTriple(state.curSubject, state.curPredicate, value, datatype, lang);
                            }
                            state.AdvanceLinePosition(match.Group(0).Length);
                        }
                        else
                        {
                            throw new JsonLdError(JsonLdError.Error.ParseError, "Error while parsing Turtle; missing expected object or blank node. {line: "
                                 + state.lineNumber + "position: " + state.linePosition + "}");
                        }
                    }
                }
                // close collection
                bool collectionClosed = false;
                while (state.line.StartsWith(")"))
                {
                    if (!JSONLDConsts.RdfFirst.Equals(state.curPredicate))
                    {
                        throw new JsonLdError(JsonLdError.Error.ParseError, "Error while parsing Turtle; unexpected ). {line: "
                             + state.lineNumber + "position: " + state.linePosition + "}");
                    }
                    result.AddTriple(state.curSubject, JSONLDConsts.RdfRest, JSONLDConsts.RdfNil);
                    state.Pop();
                    state.AdvanceLinePosition(1);
                    collectionClosed = true;
                }
                bool expectDotOrPred = false;
                // match end of bnode
                if (state.line.StartsWith("]"))
                {
                    string bnode = state.curSubject;
                    state.Pop();
                    state.AdvanceLinePosition(1);
                    if (state.curSubject == null)
                    {
                        // this is a bnode as a subject and we
                        // expect either a . or a predicate
                        state.curSubject = bnode;
                        expectDotOrPred = true;
                    }
                }
                // match list separator
                if (!expectDotOrPred && state.line.StartsWith(","))
                {
                    state.AdvanceLinePosition(1);
                    // now we expect another object/bnode
                    continue;
                }
                // match predicate end
                if (!expectDotOrPred)
                {
                    while (state.line.StartsWith(";"))
                    {
                        state.curPredicate = null;
                        state.AdvanceLinePosition(1);
                        // now we expect another predicate, or a dot
                        expectDotOrPred = true;
                    }
                }
                if (state.line.StartsWith("."))
                {
                    if (state.expectingBnodeClose)
                    {
                        throw new JsonLdError(JsonLdError.Error.ParseError, "Error while parsing Turtle; missing expected )\"]\". {line: "
                             + state.lineNumber + "position: " + state.linePosition + "}");
                    }
                    state.curSubject = null;
                    state.curPredicate = null;
                    state.AdvanceLinePosition(1);
                    // this can now be the end of the document.
                    continue;
                }
                else
                {
                    if (expectDotOrPred)
                    {
                        // we're expecting another predicate since we didn't find a dot
                        continue;
                    }
                }
                // if we're in a collection
                if (JSONLDConsts.RdfFirst.Equals(state.curPredicate))
                {
                    string bnode = state.namer.GetName();
                    result.AddTriple(state.curSubject, JSONLDConsts.RdfRest, bnode);
                    state.curSubject = bnode;
                    continue;
                }
                if (collectionClosed)
                {
                    // we expect another object
                    // TODO: it's not clear yet if this is valid
                    continue;
                }
                // if we get here, we're missing a close statement
                throw new JsonLdError(JsonLdError.Error.ParseError, "Error while parsing Turtle; missing expected \"]\" \",\" \";\" or \".\". {line: "
                     + state.lineNumber + "position: " + state.linePosition + "}");
            }
            return result;
        }

        internal static readonly Pattern IrirefMinusContainer = Pattern.Compile("(?:(?:[^\\x00-\\x20<>\"{}|\\^`\\\\]|"
             + JsonLD.Core.Regex.Uchar + ")*)|" + TurtleRDFParser.Regex.PrefixedName);

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private void ValidateIRI(TurtleRDFParser.State state, string iri)
        {
            if (!IrirefMinusContainer.Matcher(iri).Matches())
            {
                throw new JsonLdError(JsonLdError.Error.ParseError, "Error while parsing Turtle; invalid IRI after escaping. {line: "
                     + state.lineNumber + "position: " + state.linePosition + "}");
            }
        }

        private static readonly Pattern PnLocalEscMatched = Pattern.Compile("[\\\\]([_~\\.\\-!$&'\\(\\)*+,;=/?#@%])"
            );

        internal static string UnescapeReserved(string str)
        {
            if (str != null)
            {
                Matcher m = PnLocalEscMatched.Matcher(str);
                if (m.Find())
                {
                    return m.ReplaceAll("$1");
                }
            }
            return str;
        }

        private string UnquoteString(string value)
        {
            if (value.StartsWith("\"\"\"") || value.StartsWith("'''"))
            {
                return JsonLD.JavaCompat.Substring(value, 3, value.Length - 3);
            }
            else
            {
                if (value.StartsWith("\"") || value.StartsWith("'"))
                {
                    return JsonLD.JavaCompat.Substring(value, 1, value.Length - 1);
                }
            }
            return value;
        }
    }
}
