using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JsonLD.Core;
using JsonLD.OmniJson;
using JsonLD.Util;

namespace JsonLD.Core
{
    internal class JsonLdUtils
    {
        private const int MaxContextUrls = 10;

        private static readonly IList<string> keywords = new[] { 
            "@base",
            "@context",
            "@container",
            "@default",
            "@embed",
            "@explicit",
            "@graph",
            "@id",
            "@index",
            "@language",
            "@list",
            "@omitDefault",
            "@reverse",
            "@preserve",
            "@set",
            "@type",
            "@value",
            "@vocab"
        };

        /// <summary>Returns whether or not the given value is a keyword (or a keyword alias).
        /// 	</summary>
        /// <remarks>Returns whether or not the given value is a keyword (or a keyword alias).
        /// 	</remarks>
        /// <param name="v">the value to check.</param>
        /// <?></?>
        /// <returns>true if the value is a keyword, false if not.</returns>
        internal static bool IsKeyword(OmniJsonToken key)
        {
            if (!IsString(key))
            {
                return false;
            }
            var keyString = (string)key;
            return keywords.Contains(keyString);
        }

        public static bool DeepCompare(OmniJsonToken v1, OmniJsonToken v2, bool listOrderMatters)
        {
            if (v1 == null)
            {
                return v2 == null;
            }
            else
            {
                if (v2 == null)
                {
                    return v1 == null;
                }
                else
                {
                    if (v1 is OmniJsonObject && v2 is OmniJsonObject)
                    {
                        OmniJsonObject m1 = (OmniJsonObject)v1;
                        OmniJsonObject m2 = (OmniJsonObject)v2;
                        if (m1.Count != m2.Count)
                        {
                            return false;
                        }
                        foreach (string key in m1.GetKeys())
                        {
                            if (!((IDictionary<string,OmniJsonToken>)m2).ContainsKey(key) ||
                                !DeepCompare(m1[key], m2[key], listOrderMatters))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    else
                    {
                        if (v1 is OmniJsonArray && v2 is OmniJsonArray)
                        {
                            OmniJsonArray l1 = (OmniJsonArray)v1;
                            OmniJsonArray l2 = (OmniJsonArray)v2;
                            var l1Count = l1.Count;
                            var l2Count = l2.Count;
                            if (l1Count != l2Count)
                            {
                                return false;
                            }
                            // used to mark members of l2 that we have already matched to avoid
                            // matching the same item twice for lists that have duplicates
                            bool[] alreadyMatched = new bool[l2Count];
                            for (int i = 0; i < l1Count; i++)
                            {
                                OmniJsonToken o1 = l1[i];
                                bool gotmatch = false;
                                if (listOrderMatters)
                                {
                                    gotmatch = DeepCompare(o1, l2[i], listOrderMatters);
                                }
                                else
                                {
                                    for (int j = 0; j < l2Count; j++)
                                    {
                                        if (!alreadyMatched[j] && DeepCompare(o1, l2[j], listOrderMatters))
                                        {
                                            alreadyMatched[j] = true;
                                            gotmatch = true;
                                            break;
                                        }
                                    }
                                }
                                if (!gotmatch)
                                {
                                    return false;
                                }
                            }
                            return true;
                        }
                        else
                        {
                            var v1String = v1.ToString().Replace("\r\n", "").Replace("\n", "").Replace("http:", "https:");
                            var v2String = v2.ToString().Replace("\r\n", "").Replace("\n", "").Replace("http:", "https:");
                            return v1String.Equals(v2String);
                        }
                    }
                }
            }
        }

        public static bool DeepCompare(OmniJsonToken v1, OmniJsonToken v2)
        {
            return DeepCompare(v1, v2, false);
        }

        public static bool DeepContains(OmniJsonArray values, OmniJsonToken value)
        {
            foreach (OmniJsonToken item in values)
            {
                if (DeepCompare(item, value, false))
                {
                    return true;
                }
            }
            return false;
        }

        internal static void MergeValue(OmniJsonObject obj, string key, OmniJsonToken value)
        {
            MergeValue(obj, key, value, skipSetContainsCheck: false);
        }

        internal static void MergeValue(OmniJsonObject obj, string key, OmniJsonToken value, bool skipSetContainsCheck)
        {
            if (obj == null)
            {
                return;
            }
            OmniJsonArray values = (OmniJsonArray)obj[key];
            if (values == null)
            {
                values = new OmniJsonArray();
                obj[key] = values;
            }
            if (skipSetContainsCheck ||
                "@list".Equals(key) ||
                (value is OmniJsonObject && ((IDictionary<string, OmniJsonToken>)value).ContainsKey("@list")) ||
                !DeepContains(values, (OmniJsonToken)value))
            {
                values.Add(value);
            }
        }

        internal static void MergeCompactedValue(OmniJsonObject obj, string 
            key, OmniJsonToken value)
        {
            if (obj == null)
            {
                return;
            }
            OmniJsonToken prop = obj[key];
            if (prop.IsNull())
            {
                obj[key] = value;
                return;
            }
            if (!(prop is OmniJsonArray))
            {
                OmniJsonArray tmp = new OmniJsonArray();
                tmp.Add(prop);
            }
            if (value is OmniJsonArray)
            {
                JsonLD.Collections.AddAll(((OmniJsonArray)prop), (OmniJsonArray)value);
            }
            else
            {
                ((OmniJsonArray)prop).Add(value);
            }
        }

        public static bool IsAbsoluteIri(string value)
        {
            // TODO: this is a bit simplistic!
            return value != null && value.Contains(":");
        }

        /// <summary>Returns true if the given value is a subject with properties.</summary>
        /// <remarks>Returns true if the given value is a subject with properties.</remarks>
        /// <param name="v">the value to check.</param>
        /// <returns>true if the value is a subject with properties, false if not.</returns>
        internal static bool IsNode(OmniJsonToken v)
        {
            // Note: A value is a subject if all of these hold true:
            // 1. It is an Object.
            // 2. It is not a @value, @set, or @list.
            // 3. It has more than 1 key OR any existing key is not @id.
            if (v is OmniJsonObject && !(((IDictionary<string, OmniJsonToken>)v).ContainsKey("@value") || ((IDictionary<string, OmniJsonToken>
                )v).ContainsKey("@set") || ((IDictionary<string, OmniJsonToken>)v).ContainsKey("@list")))
            {
                return ((IDictionary<string, OmniJsonToken>)v).Count > 1 || !((IDictionary<string, OmniJsonToken>)v).ContainsKey
                    ("@id");
            }
            return false;
        }

        /// <summary>Returns true if the given value is a subject reference.</summary>
        /// <remarks>Returns true if the given value is a subject reference.</remarks>
        /// <param name="v">the value to check.</param>
        /// <returns>true if the value is a subject reference, false if not.</returns>
        internal static bool IsNodeReference(OmniJsonToken v)
        {
            // Note: A value is a subject reference if all of these hold true:
            // 1. It is an Object.
            // 2. It has a single key: @id.
            return (v is OmniJsonObject && ((IDictionary<string, OmniJsonToken>)v).Count == 1 && ((IDictionary
                <string, OmniJsonToken>)v).ContainsKey("@id"));
        }

        // TODO: fix this test
        public static bool IsRelativeIri(string value)
        {
            if (!(IsKeyword(value) || IsAbsoluteIri(value)))
            {
                return true;
            }
            return false;
        }

        // //////////////////////////////////////////////////// OLD CODE BELOW
        /// <summary>Adds a value to a subject.</summary>
        /// <remarks>
        /// Adds a value to a subject. If the value is an array, all values in the
        /// array will be added.
        /// Note: If the value is a subject that already exists as a property of the
        /// given subject, this method makes no attempt to deeply merge properties.
        /// Instead, the value will not be added.
        /// </remarks>
        /// <param name="subject">the subject to add the value to.</param>
        /// <param name="property">the property that relates the value to the subject.</param>
        /// <param name="value">the value to add.</param>
        /// <?></?>
        /// <?></?>
        internal static void AddValue(OmniJsonObject subject, string property
            , OmniJsonToken value, bool propertyIsArray, bool allowDuplicate)
        {
            if (IsArray(value))
            {
                if (((OmniJsonArray)value).Count == 0 && propertyIsArray && !subject.ContainsKey(property
                    ))
                {
                    subject[property] = new OmniJsonArray();
                }
                foreach (OmniJsonToken val in (OmniJsonArray)value)
                {
                    AddValue(subject, property, val, propertyIsArray, allowDuplicate);
                }
            }
            else
            {
                if (subject.ContainsKey(property))
                {
                    // check if subject already has the value if duplicates not allowed
                    bool hasValue = !allowDuplicate && HasValue(subject, property, value);
                    // make property an array if value not present or always an array
                    if (!IsArray(subject[property]) && (!hasValue || propertyIsArray))
                    {
                        OmniJsonArray tmp = new OmniJsonArray();
                        tmp.Add(subject[property]);
                        subject[property] = tmp;
                    }
                    // add new value
                    if (!hasValue)
                    {
                        ((OmniJsonArray)subject[property]).Add(value);
                    }
                }
                else
                {
                    // add new value as a set or single value
                    OmniJsonToken tmp;
                    if (propertyIsArray)
                    {
                        tmp = new OmniJsonArray();
                        ((OmniJsonArray)tmp).Add(value);
                    }
                    else
                    {
                        tmp = value;
                    }
                    subject[property] = tmp;
                }
            }
        }

        internal static void AddValue(OmniJsonObject subject, string property
            , OmniJsonToken value, bool propertyIsArray)
        {
            AddValue(subject, property, value, propertyIsArray, true);
        }

        internal static void AddValue(OmniJsonObject subject, string property
            , OmniJsonToken value)
        {
            AddValue(subject, property, value, false, true);
        }

        /// <summary>Prepends a base IRI to the given relative IRI.</summary>
        /// <remarks>Prepends a base IRI to the given relative IRI.</remarks>
        /// <param name="base">the base IRI.</param>
        /// <param name="iri">the relative IRI.</param>
        /// <returns>
        /// the absolute IRI.
        /// TODO: the URL class isn't as forgiving as the Node.js url parser,
        /// we may need to re-implement the parser here to support the
        /// flexibility required
        /// </returns>
        private static string PrependBase(OmniJsonToken baseobj, string iri)
        {
            // already an absolute IRI
            if (iri.IndexOf(":") != -1)
            {
                return iri;
            }
            // parse base if it is a string
            URL @base;
            if (IsString(baseobj))
            {
                @base = URL.Parse((string)baseobj);
            }
            else
            {
                // assume base is already a URL
                @base = baseobj.Value<URL>();
            }
            URL rel = URL.Parse(iri);
            // start hierarchical part
            string hierPart = @base.protocol;
            if (!string.Empty.Equals(rel.authority))
            {
                hierPart += "//" + rel.authority;
            }
            else
            {
                if (!string.Empty.Equals(@base.href))
                {
                    hierPart += "//" + @base.authority;
                }
            }
            // per RFC3986 normalize
            string path;
            // IRI represents an absolute path
            if (rel.pathname.IndexOf("/") == 0)
            {
                path = rel.pathname;
            }
            else
            {
                path = @base.pathname;
                // append relative path to the end of the last directory from base
                if (!string.Empty.Equals(rel.pathname))
                {
                    path = JsonLD.JavaCompat.Substring(path, 0, path.LastIndexOf("/") + 1);
                    if (path.Length > 0 && !path.EndsWith("/"))
                    {
                        path += "/";
                    }
                    path += rel.pathname;
                }
            }
            // remove slashes anddots in path
            path = URL.RemoveDotSegments(path, !string.Empty.Equals(hierPart));
            // add query and hash
            if (!string.Empty.Equals(rel.query))
            {
                path += "?" + rel.query;
            }
            if (!string.Empty.Equals(rel.hash))
            {
                path += rel.hash;
            }
            string rval = hierPart + path;
            if (string.Empty.Equals(rval))
            {
                return "./";
            }
            return rval;
        }

        /// <summary>Expands a language map.</summary>
        /// <remarks>Expands a language map.</remarks>
        /// <param name="languageMap">the language map to expand.</param>
        /// <returns>the expanded language map.</returns>
        /// <exception cref="JsonLdError">JsonLdError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal static OmniJsonArray ExpandLanguageMap(OmniJsonObject languageMap
            )
        {
            OmniJsonArray rval = new OmniJsonArray();
            IList<string> keys = new List<string>(languageMap.GetKeys());
            keys.SortInPlace();
            // lexicographically sort languages
            foreach (string key in keys)
            {
                OmniJsonToken val;
                if (!IsArray(languageMap[key]))
                {
                    val = new OmniJsonArray();
                    ((OmniJsonArray)val).Add(languageMap[key]);
                }
                else
                {
                    val = (OmniJsonArray)languageMap[key];
                }
                foreach (OmniJsonToken item in val)
                {
                    if (!IsString(item))
                    {
                        throw new JsonLdError(JsonLdError.Error.SyntaxError);
                    }
                    OmniJsonObject tmp = new OmniJsonObject();
                    tmp["@value"] = item;
                    tmp["@language"] = key.ToLower();
                    rval.Add(tmp);
                }
            }
            return rval;
        }

        /// <summary>Throws an exception if the given value is not a valid @type value.</summary>
        /// <remarks>Throws an exception if the given value is not a valid @type value.</remarks>
        /// <param name="v">the value to check.</param>
        /// <exception cref="JsonLdError">JsonLdError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal static bool ValidateTypeValue(OmniJsonToken v)
        {
            if (v.IsNull())
            {
                throw new ArgumentNullException("\"@type\" value cannot be null");
            }
            // must be a string, subject reference, or empty object
            if (v.Type == OmniJsonTokenType.String || (v is OmniJsonObject && (JavaCompat.ContainsKey
                (((OmniJsonObject)v), "@id") || ((OmniJsonArray)v).Count == 0)))
            {
                return true;
            }
            // must be an array
            bool isValid = false;
            if (v is OmniJsonArray)
            {
                isValid = true;
                foreach (OmniJsonToken i in (OmniJsonArray)v)
                {
                    if (!(i.Type == OmniJsonTokenType.String || i is OmniJsonObject && JavaCompat.ContainsKey
                        (((OmniJsonObject)i), "@id")))
                    {
                        isValid = false;
                        break;
                    }
                }
            }
            if (!isValid)
            {
                throw new JsonLdError(JsonLdError.Error.SyntaxError);
            }
            return true;
        }

        /// <summary>Removes a base IRI from the given absolute IRI.</summary>
        /// <remarks>Removes a base IRI from the given absolute IRI.</remarks>
        /// <param name="base">the base IRI.</param>
        /// <param name="iri">the absolute IRI.</param>
        /// <returns>the relative IRI if relative to base, otherwise the absolute IRI.</returns>
        private static string RemoveBase(OmniJsonToken baseobj, string iri)
        {
            URL @base;
            if (IsString(baseobj))
            {
                @base = URL.Parse((string)baseobj);
            }
            else
            {
                @base = baseobj.Value<URL>();
            }
            // establish base root
            string root = string.Empty;
            if (!string.Empty.Equals(@base.href))
            {
                root += (@base.protocol) + "//" + @base.authority;
            }
            else
            {
                // support network-path reference with empty base
                if (iri.IndexOf("//") != 0)
                {
                    root += "//";
                }
            }
            // IRI not relative to base
            if (iri.IndexOf(root) != 0)
            {
                return iri;
            }
            // remove root from IRI and parse remainder
            URL rel = URL.Parse(JsonLD.JavaCompat.Substring(iri, root.Length));
            // remove path segments that match
            IList<string> baseSegments = _split(@base.normalizedPath, "/");
            IList<string> iriSegments = _split(rel.normalizedPath, "/");
            while (baseSegments.Count > 0 && iriSegments.Count > 0)
            {
                if (!baseSegments[0].Equals(iriSegments[0]))
                {
                    break;
                }
                if (baseSegments.Count > 0)
                {
                    baseSegments.RemoveAt(0);
                }
                if (iriSegments.Count > 0)
                {
                    iriSegments.RemoveAt(0);
                }
            }
            // use '../' for each non-matching base segment
            string rval = string.Empty;
            if (baseSegments.Count > 0)
            {
                // don't count the last segment if it isn't a path (doesn't end in
                // '/')
                // don't count empty first segment, it means base began with '/'
                if (!@base.normalizedPath.EndsWith("/") || string.Empty.Equals(baseSegments[0]))
                {
                    baseSegments.RemoveAt(baseSegments.Count - 1);
                }
                for (int i = 0; i < baseSegments.Count; ++i)
                {
                    rval += "../";
                }
            }
            // prepend remaining segments
            rval += _join(iriSegments, "/");
            // add query and hash
            if (!string.Empty.Equals(rel.query))
            {
                rval += "?" + rel.query;
            }
            if (!string.Empty.Equals(rel.hash))
            {
                rval += rel.hash;
            }
            if (string.Empty.Equals(rval))
            {
                rval = "./";
            }
            return rval;
        }

        /// <summary>Removes the @preserve keywords as the last step of the framing algorithm.
        /// 	</summary>
        /// <remarks>Removes the @preserve keywords as the last step of the framing algorithm.
        /// 	</remarks>
        /// <param name="ctx">the active context used to compact the input.</param>
        /// <param name="input">the framed, compacted output.</param>
        /// <param name="options">the compaction options used.</param>
        /// <returns>the resulting output.</returns>
        /// <exception cref="JsonLdError">JsonLdError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal static OmniJsonToken RemovePreserve(Context ctx, OmniJsonToken input, JsonLdOptions opts
            )
        {
            // recurse through arrays
            if (IsArray(input))
            {
                OmniJsonArray output = new OmniJsonArray();
                foreach (OmniJsonToken i in (OmniJsonArray)input)
                {
                    OmniJsonToken result = RemovePreserve(ctx, i, opts);
                    // drop nulls from arrays
                    if (!result.IsNull())
                    {
                        output.Add(result);
                    }
                }
                input = output;
            }
            else
            {
                if (IsObject(input))
                {
                    // remove @preserve
                    if (JavaCompat.ContainsKey(((OmniJsonObject)input), "@preserve"))
                    {
                        if (((OmniJsonObject)input)["@preserve"].SafeCompare("@null"))
                        {
                            return null;
                        }
                        return ((OmniJsonObject)input)["@preserve"];
                    }
                    // skip @values
                    if (IsValue(input))
                    {
                        return input;
                    }
                    // recurse through @lists
                    if (IsList(input))
                    {
                        ((OmniJsonObject)input)["@list"] = RemovePreserve(ctx, ((OmniJsonObject)input)["@list"], opts);
                        return input;
                    }
                    // recurse through properties
                    foreach (string prop in input.GetKeys())
                    {
                        OmniJsonToken result = RemovePreserve(ctx, ((OmniJsonObject)input)[prop], opts
                            );
                        string container = ctx.GetContainer(prop);
                        if (opts.GetCompactArrays() && IsArray(result) && ((OmniJsonArray)result).Count ==
                             1 && container == null)
                        {
                            result = ((OmniJsonArray)result)[0];
                        }
                        ((OmniJsonObject)input)[prop] = result;
                    }
                }
            }
            return input;
        }

        /// <summary>replicate javascript .join because i'm too lazy to keep doing it manually
        /// 	</summary>
        /// <param name="iriSegments"></param>
        /// <param name="string"></param>
        /// <returns></returns>
        private static string _join(IList<string> list, string joiner)
        {
            string rval = string.Empty;
            if (list.Count > 0)
            {
                rval += list[0];
            }
            for (int i = 1; i < list.Count; i++)
            {
                rval += joiner + list[i];
            }
            return rval;
        }

        /// <summary>
        /// replicates the functionality of javascript .split, which has different
        /// results to java's String.split if there is a trailing /
        /// </summary>
        /// <param name="string"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
        private static IList<string> _split(string @string, string delim)
        {
            IList<string> rval = new List<string>(System.Linq.Enumerable.ToList(@string.Split
                (delim)));
            if (@string.EndsWith("/"))
            {
                // javascript .split includes a blank entry if the string ends with
                // the delimiter, java .split does not so we need to add it manually
                rval.Add(string.Empty);
            }
            return rval;
        }

        /// <summary>Compares two strings first based on length and then lexicographically.</summary>
        /// <remarks>Compares two strings first based on length and then lexicographically.</remarks>
        /// <param name="a">the first string.</param>
        /// <param name="b">the second string.</param>
        /// <returns>-1 if a &lt; b, 1 if a &gt; b, 0 if a == b.</returns>
        internal static int CompareShortestLeast(string a, string b)
        {
            if (a.Length < b.Length)
            {
                return -1;
            }
            else
            {
                if (b.Length < a.Length)
                {
                    return 1;
                }
            }
            return System.Math.Sign(string.CompareOrdinal(a, b));
        }

        /// <summary>Determines if the given value is a property of the given subject.</summary>
        /// <remarks>Determines if the given value is a property of the given subject.</remarks>
        /// <param name="subject">the subject to check.</param>
        /// <param name="property">the property to check.</param>
        /// <param name="value">the value to check.</param>
        /// <returns>true if the value exists, false if not.</returns>
        internal static bool HasValue(OmniJsonObject subject, string property
            , OmniJsonToken value)
        {
            bool rval = false;
            if (HasProperty(subject, property))
            {
                OmniJsonToken val = subject[property];
                bool isList = IsList(val);
                if (isList || val is OmniJsonArray)
                {
                    if (isList)
                    {
                        val = (OmniJsonObject)val["@list"];
                    }
                    foreach (OmniJsonToken i in (OmniJsonArray)val)
                    {
                        if (CompareValues(value, i))
                        {
                            rval = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (!(value is OmniJsonArray))
                    {
                        rval = CompareValues(value, val);
                    }
                }
            }
            return rval;
        }

        private static bool HasProperty(OmniJsonObject subject, string property
            )
        {
            bool rval = false;
            if (subject.ContainsKey(property))
            {
                OmniJsonToken value = subject[property];
                rval = (!(value is OmniJsonArray) || ((OmniJsonArray)value).Count > 0);
            }
            return rval;
        }

        /// <summary>Compares two JSON-LD values for equality.</summary>
        /// <remarks>
        /// Compares two JSON-LD values for equality. Two JSON-LD values will be
        /// considered equal if:
        /// 1. They are both primitives of the same type and value. 2. They are both @values
        /// with the same @value, @type, and @language, OR 3. They both have @ids
        /// they are the same.
        /// </remarks>
        /// <param name="v1">the first value.</param>
        /// <param name="v2">the second value.</param>
        /// <returns>true if v1 and v2 are considered equal, false if not.</returns>
        internal static bool CompareValues(OmniJsonToken v1, OmniJsonToken v2)
        {
            if (v1.Equals(v2))
            {
                return true;
            }
            if (IsValue(v1) && IsValue(v2) && Obj.Equals(((OmniJsonObject)v1)["@value"
                ], ((OmniJsonObject)v2)["@value"]) && Obj.Equals(((OmniJsonObject)v1)["@type"], ((OmniJsonObject)v2)["@type"]) && Obj.Equals
                (((OmniJsonObject)v1)["@language"], ((OmniJsonObject)v2
                )["@language"]) && Obj.Equals(((OmniJsonObject)v1)["@index"], ((OmniJsonObject)v2)["@index"]))
            {
                return true;
            }
            if ((v1 is OmniJsonObject && JavaCompat.ContainsKey(((OmniJsonObject)v1), "@id")) &&
                 (v2 is OmniJsonObject && JavaCompat.ContainsKey(((OmniJsonObject)v2), "@id")) &&
                ((OmniJsonObject)v1)["@id"].Equals(((OmniJsonObject)v2
                )["@id"]))
            {
                return true;
            }
            return false;
        }

        /// <summary>Removes a value from a subject.</summary>
        /// <remarks>Removes a value from a subject.</remarks>
        /// <param name="subject">the subject.</param>
        /// <param name="property">the property that relates the value to the subject.</param>
        /// <param name="value">the value to remove.</param>
        /// <?></?>
        internal static void RemoveValue(OmniJsonObject subject, string property
            , OmniJsonObject value)
        {
            RemoveValue(subject, property, value, false);
        }

        internal static void RemoveValue(OmniJsonObject subject, string property
            , OmniJsonObject value, bool propertyIsArray)
        {
            // filter out value
            OmniJsonArray values = new OmniJsonArray();
            if (subject[property] is OmniJsonArray)
            {
                foreach (OmniJsonToken e in ((OmniJsonArray)subject[property]))
                {
                    if (!e.SafeCompare(value))
                    {
                        values.Add(value);
                    }
                }
            }
            else
            {
                if (!subject[property].SafeCompare(value))
                {
                    values.Add(subject[property]);
                }
            }
            if (values.Count == 0)
            {
                JsonLD.Collections.Remove(subject, property);
            }
            else
            {
                if (values.Count == 1 && !propertyIsArray)
                {
                    subject[property] = values[0];
                }
                else
                {
                    subject[property] = values;
                }
            }
        }

        /// <summary>Returns true if the given value is a blank node.</summary>
        /// <remarks>Returns true if the given value is a blank node.</remarks>
        /// <param name="v">the value to check.</param>
        /// <returns>true if the value is a blank node, false if not.</returns>
        internal static bool IsBlankNode(OmniJsonToken v)
        {
            // Note: A value is a blank node if all of these hold true:
            // 1. It is an Object.
            // 2. If it has an @id key its value begins with '_:'.
            // 3. It has no keys OR is not a @value, @set, or @list.
            if (v is OmniJsonObject)
            {
                if (JavaCompat.ContainsKey(((OmniJsonObject)v), "@id"))
                {
                    return ((string)((OmniJsonObject)v)["@id"]).StartsWith("_:");
                }
                else
                {
                    return ((OmniJsonObject)v).Count == 0 || !(JavaCompat.ContainsKey(((OmniJsonObject)v), "@value") ||
                         JavaCompat.ContainsKey(((OmniJsonObject)v), "@set") || JavaCompat.ContainsKey(((OmniJsonObject)v), "@list"));
                }
            }
            return false;
        }

        /// <summary>Resolves external @context URLs using the given URL resolver.</summary>
        /// <remarks>
        /// Resolves external @context URLs using the given URL resolver. Each
        /// instance of @context in the input that refers to a URL will be replaced
        /// with the JSON @context found at that URL.
        /// </remarks>
        /// <param name="input">the JSON-LD input with possible contexts.</param>
        /// <param name="resolver">(url, callback(err, jsonCtx)) the URL resolver to use.</param>
        /// <param name="callback">(err, input) called once the operation completes.</param>
        /// <exception cref="JsonLdError">JsonLdError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal static void ResolveContextUrls(OmniJsonToken input)
        {
            Resolve(input, new OmniJsonObject());
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private static void Resolve(OmniJsonToken input, OmniJsonObject cycles)
        {
            Pattern regex = Pattern.Compile("(http|https)://(\\w+:{0,1}\\w*@)?(\\S+)(:[0-9]+)?(/|/([\\w#!:.?+=&%@!\\-/]))?"
                );
            if (cycles.Count > MaxContextUrls)
            {
                throw new JsonLdError(JsonLdError.Error.UnknownError);
            }
            // for tracking the URLs to resolve
            OmniJsonObject urls = new OmniJsonObject();
            // find all URLs in the given input
            if (!FindContextUrls(input, urls, false))
            {
                // finished
                FindContextUrls(input, urls, true);
            }
            // queue all unresolved URLs
            OmniJsonArray queue = new OmniJsonArray();
            foreach (string url in urls.GetKeys())
            {
                if (urls[url].SafeCompare(false))
                {
                    // validate URL
                    if (!regex.Matcher(url).Matches())
                    {
                        throw new JsonLdError(JsonLdError.Error.UnknownError);
                    }
                    queue.Add(url);
                }
            }
            // resolve URLs in queue
            int count = queue.Count;
            foreach (string url_1 in queue)
            {
                // check for context URL cycle
                if (cycles.ContainsKey(url_1))
                {
                    throw new JsonLdError(JsonLdError.Error.UnknownError);
                }
                OmniJsonObject _cycles = (OmniJsonObject)Clone(cycles);
                _cycles[url_1] = true;
                try
                {
                    OmniJsonObject ctx = (OmniJsonObject)new DocumentLoader().LoadDocument(url_1).Document;
                    if (!ctx.ContainsKey("@context"))
                    {
                        ctx = new OmniJsonObject();
                        ctx["@context"] = new OmniJsonObject();
                    }
                    Resolve(ctx, _cycles);
                    urls[url_1] = ctx["@context"];
                    count -= 1;
                    if (count == 0)
                    {
                        FindContextUrls(input, urls, true);
                    }
                }
                //catch (JsonParseException)
                //{
                //    throw new JsonLdError(JsonLdError.Error.UnknownError);
                //}
                //catch (MalformedURLException)
                //{
                //    throw new JsonLdError(JsonLdError.Error.UnknownError);
                //}
                catch (IOException)
                {
                    throw new JsonLdError(JsonLdError.Error.UnknownError);
                }
            }
        }

        /// <summary>Finds all @context URLs in the given JSON-LD input.</summary>
        /// <remarks>Finds all @context URLs in the given JSON-LD input.</remarks>
        /// <param name="input">the JSON-LD input.</param>
        /// <param name="urls">a map of URLs (url =&gt; false/@contexts).</param>
        /// <param name="replace">true to replace the URLs in the given input with the</param>
        /// <contexts>from the urls map, false not to.</contexts>
        /// <returns>true if new URLs to resolve were found, false if not.</returns>
        private static bool FindContextUrls(OmniJsonToken input, OmniJsonObject urls
            , bool replace)
        {
            int count = urls.Count;
            if (input is OmniJsonArray)
            {
                foreach (OmniJsonToken i in (OmniJsonArray)input)
                {
                    FindContextUrls(i, urls, replace);
                }
                return count < urls.Count;
            }
            else
            {
                if (input is OmniJsonObject)
                {
                    foreach (string key in input.GetKeys())
                    {
                        if (!"@context".Equals(key))
                        {
                            FindContextUrls(((OmniJsonObject)input)[key], urls, replace);
                            continue;
                        }
                        // get @context
                        OmniJsonToken ctx = ((OmniJsonObject)input)[key];
                        // array @context
                        if (ctx is OmniJsonArray)
                        {
                            int length = ((OmniJsonArray)ctx).Count;
                            for (int i = 0; i < length; i++)
                            {
                                OmniJsonToken _ctx = ((OmniJsonArray)ctx)[i];
                                if (_ctx.Type == OmniJsonTokenType.String)
                                {
                                    // replace w/@context if requested
                                    if (replace)
                                    {
                                        _ctx = urls[(string)_ctx];
                                        if (_ctx is OmniJsonArray)
                                        {
                                            // add flattened context
                                            ((OmniJsonArray)ctx).RemoveAt(i);
                                            JsonLD.Collections.AddAllObj(((OmniJsonArray)ctx), (ICollection)_ctx);
                                            i += ((OmniJsonArray)_ctx).Count;
                                            length += ((OmniJsonArray)_ctx).Count;
                                        }
                                        else
                                        {
                                            ((OmniJsonArray)ctx)[i] = _ctx;
                                        }
                                    }
                                    else
                                    {
                                        // @context URL found
                                        if (!urls.ContainsKey((string)_ctx))
                                        {
                                            urls[(string)_ctx] = false;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // string @context
                            if (ctx.Type == OmniJsonTokenType.String)
                            {
                                // replace w/@context if requested
                                if (replace)
                                {
                                    ((OmniJsonObject)input)[key] = urls[(string)ctx];
                                }
                                else
                                {
                                    // @context URL found
                                    if (!urls.ContainsKey((string)ctx))
                                    {
                                        urls[(string)ctx] = false;
                                    }
                                }
                            }
                        }
                    }
                    return (count < urls.Count);
                }
            }
            return false;
        }

        internal static OmniJsonToken Clone(OmniJsonToken value)
        {
            return value.DeepClone();
        }

        /// <summary>Returns true if the given value is a JSON-LD Array</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsArray(OmniJsonToken v)
        {
            return (v is OmniJsonArray);
        }

        /// <summary>Returns true if the given value is a JSON-LD List</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsList(OmniJsonToken v)
        {
            return (v is OmniJsonObject && ((IDictionary<string, OmniJsonToken>)v).ContainsKey("@list")
                );
        }

        /// <summary>Returns true if the given value is a JSON-LD Object</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsObject(OmniJsonToken v)
        {
            return (v is OmniJsonObject);
        }

        /// <summary>Returns true if the given value is a JSON-LD value</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsValue(OmniJsonToken v)
        {
            return (v is OmniJsonObject && ((IDictionary<string, OmniJsonToken>)v).ContainsKey("@value"
                ));
        }

        /// <summary>Returns true if the given value is a JSON-LD string</summary>
        /// <param name="v">the value to check.</param>
        /// <returns></returns>
        internal static bool IsString(OmniJsonToken v)
        {
            // TODO: should this return true for arrays of strings as well?
            return (v.Type == OmniJsonTokenType.String);
        }
    }
}
