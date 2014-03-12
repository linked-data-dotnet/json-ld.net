using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JsonLD.Core;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
	public class JsonLdUtils
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
		internal static bool IsKeyword(JToken key)
		{
			if (!IsString(key))
			{
				return false;
			}
            var keyString = (string)key;
            return keywords.Contains(keyString);
		}

		public static bool DeepCompare(JToken v1, JToken v2, bool listOrderMatters)
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
					if (v1 is JObject && v2 is JObject)
					{
                        JObject m1 = (JObject)v1;
                        JObject m2 = (JObject)v2;
						if (m1.Count != m2.Count)
						{
							return false;
						}
						foreach (string key in m1.GetKeys())
						{
							if (!((IDictionary<string,JToken>)m2).ContainsKey(key) ||
                                !DeepCompare(m1[key], m2[key], listOrderMatters))
							{
								return false;
							}
						}
						return true;
					}
					else
					{
                        if (v1 is JArray && v2 is JArray)
						{
                            JArray l1 = (JArray)v1;
                            JArray l2 = (JArray)v2;
							if (l1.Count != l2.Count)
							{
								return false;
							}
							// used to mark members of l2 that we have already matched to avoid
							// matching the same item twice for lists that have duplicates
							bool[] alreadyMatched = new bool[l2.Count];
							for (int i = 0; i < l1.Count; i++)
							{
								JToken o1 = l1[i];
								bool gotmatch = false;
								if (listOrderMatters)
								{
									gotmatch = DeepCompare(o1, l2[i], listOrderMatters);
								}
								else
								{
									for (int j = 0; j < l2.Count; j++)
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
							return v1.Equals(v2);
						}
					}
				}
			}
		}

        public static bool DeepCompare(JToken v1, JToken v2)
		{
			return DeepCompare(v1, v2, false);
		}

		public static bool DeepContains(JArray values, JToken value)
		{
			foreach (JToken item in values)
			{
				if (DeepCompare(item, value, false))
				{
					return true;
				}
			}
			return false;
		}

		internal static void MergeValue(JObject obj, string key, JToken
			 value)
		{
			if (obj == null)
			{
				return;
			}
			JArray values = (JArray)obj[key];
			if (values == null)
			{
				values = new JArray();
				obj[key] = values;
			}
			if ("@list".Equals(key) || (value is JObject && ((IDictionary<string, JToken>
				)value).ContainsKey("@list")) || !DeepContains(values, (JToken)value))
			{
				values.Add(value);
			}
		}

		internal static void MergeCompactedValue(JObject obj, string 
			key, JToken value)
		{
			if (obj == null)
			{
				return;
			}
			JToken prop = obj[key];
			if (prop.IsNull())
			{
				obj[key] = value;
				return;
			}
			if (!(prop is JArray))
			{
                JArray tmp = new JArray();
				tmp.Add(prop);
			}
            if (value is JArray)
			{
                JsonLD.Collections.AddAll(((JArray)prop), (JArray)value);
			}
			else
			{
                ((JArray)prop).Add(value);
			}
		}

		public static bool IsAbsoluteIri(string value)
		{
			// TODO: this is a bit simplistic!
			return value.Contains(":");
		}

		/// <summary>Returns true if the given value is a subject with properties.</summary>
		/// <remarks>Returns true if the given value is a subject with properties.</remarks>
		/// <param name="v">the value to check.</param>
		/// <returns>true if the value is a subject with properties, false if not.</returns>
		internal static bool IsNode(JToken v)
		{
			// Note: A value is a subject if all of these hold true:
			// 1. It is an Object.
			// 2. It is not a @value, @set, or @list.
			// 3. It has more than 1 key OR any existing key is not @id.
            if (v is JObject && !(((IDictionary<string, JToken>)v).ContainsKey("@value") || ((IDictionary<string, JToken>
                )v).ContainsKey("@set") || ((IDictionary<string, JToken>)v).ContainsKey("@list")))
			{
                return ((IDictionary<string, JToken>)v).Count > 1 || !((IDictionary<string, JToken>)v).ContainsKey
					("@id");
			}
			return false;
		}

		/// <summary>Returns true if the given value is a subject reference.</summary>
		/// <remarks>Returns true if the given value is a subject reference.</remarks>
		/// <param name="v">the value to check.</param>
		/// <returns>true if the value is a subject reference, false if not.</returns>
		internal static bool IsNodeReference(JToken v)
		{
			// Note: A value is a subject reference if all of these hold true:
			// 1. It is an Object.
			// 2. It has a single key: @id.
            return (v is JObject && ((IDictionary<string, JToken>)v).Count == 1 && ((IDictionary
                <string, JToken>)v).ContainsKey("@id"));
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
		internal static void AddValue(JObject subject, string property
			, JToken value, bool propertyIsArray, bool allowDuplicate)
		{
			if (IsArray(value))
			{
				if (((JArray)value).Count == 0 && propertyIsArray && !subject.ContainsKey(property
					))
				{
					subject[property] = new JArray();
				}
                foreach (JToken val in (JArray)value)
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
                        JArray tmp = new JArray();
						tmp.Add(subject[property]);
						subject[property] = tmp;
					}
					// add new value
					if (!hasValue)
					{
                        ((JArray)subject[property]).Add(value);
					}
				}
				else
				{
					// add new value as a set or single value
					JToken tmp;
					if (propertyIsArray)
					{
                        tmp = new JArray();
                        ((JArray)tmp).Add(value);
					}
					else
					{
						tmp = value;
					}
					subject[property] = tmp;
				}
			}
		}

		internal static void AddValue(JObject subject, string property
			, JToken value, bool propertyIsArray)
		{
			AddValue(subject, property, value, propertyIsArray, true);
		}

		internal static void AddValue(JObject subject, string property
			, JToken value)
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
		private static string PrependBase(JToken baseobj, string iri)
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
		internal static JArray ExpandLanguageMap(JObject languageMap
			)
		{
            JArray rval = new JArray();
            IList<string> keys = new List<string>(languageMap.GetKeys());
			keys.SortInPlace();
			// lexicographically sort languages
			foreach (string key in keys)
			{
				JToken val;
				if (!IsArray(languageMap[key]))
				{
					val = new JArray();
					((JArray)val).Add(languageMap[key]);
				}
				else
				{
					val = (JArray)languageMap[key];
				}
				foreach (JToken item in val)
				{
					if (!IsString(item))
					{
						throw new JsonLdError(JsonLdError.Error.SyntaxError);
					}
					JObject tmp = new JObject();
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
		internal static bool ValidateTypeValue(JToken v)
		{
			if (v.IsNull())
			{
				throw new ArgumentNullException("\"@type\" value cannot be null");
			}
			// must be a string, subject reference, or empty object
			if (v.Type == JTokenType.String || (v is JObject && (((JObject)v).ContainsKey
                ("@id") || ((JArray)v).Count == 0)))
			{
				return true;
			}
			// must be an array
			bool isValid = false;
			if (v is JArray)
			{
				isValid = true;
                foreach (JToken i in (JArray)v)
				{
                    if (!(i.Type == JTokenType.String || i is JObject && ((JObject)i).ContainsKey
						("@id")))
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
		private static string RemoveBase(JToken baseobj, string iri)
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
		internal static JToken RemovePreserve(Context ctx, JToken input, JsonLdOptions opts
			)
		{
			// recurse through arrays
			if (IsArray(input))
			{
                JArray output = new JArray();
				foreach (JToken i in (JArray)input)
				{
					JToken result = RemovePreserve(ctx, i, opts);
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
					if (((JObject)input).ContainsKey("@preserve"))
					{
                        if (((JObject)input)["@preserve"].SafeCompare("@null"))
						{
							return null;
						}
                        return ((JObject)input)["@preserve"];
					}
					// skip @values
					if (IsValue(input))
					{
						return input;
					}
					// recurse through @lists
					if (IsList(input))
					{
                        ((JObject)input)["@list"] = RemovePreserve(ctx, ((JObject)input)["@list"], opts);
						return input;
					}
					// recurse through properties
                    foreach (string prop in input.GetKeys())
					{
                        JToken result = RemovePreserve(ctx, ((JObject)input)[prop], opts
							);
						string container = ctx.GetContainer(prop);
                        if (opts.GetCompactArrays() && IsArray(result) && ((JArray)result).Count ==
							 1 && container == null)
						{
                            result = ((JArray)result)[0];
						}
						((JObject)input)[prop] = result;
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
		internal static bool HasValue(JObject subject, string property
			, JToken value)
		{
			bool rval = false;
			if (HasProperty(subject, property))
			{
				JToken val = subject[property];
				bool isList = IsList(val);
				if (isList || val is JArray)
				{
					if (isList)
					{
						val = (JObject)val["@list"];
					}
					foreach (JToken i in (JArray)val)
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
					if (!(value is JArray))
					{
						rval = CompareValues(value, val);
					}
				}
			}
			return rval;
		}

		private static bool HasProperty(JObject subject, string property
			)
		{
			bool rval = false;
			if (subject.ContainsKey(property))
			{
				JToken value = subject[property];
				rval = (!(value is JArray) || ((JArray)value).Count > 0);
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
		internal static bool CompareValues(JToken v1, JToken v2)
		{
			if (v1.Equals(v2))
			{
				return true;
			}
			if (IsValue(v1) && IsValue(v2) && Obj.Equals(((JObject)v1)["@value"
                ], ((JObject)v2)["@value"]) && Obj.Equals(((JObject)v1)["@type"], ((JObject)v2)["@type"]) && Obj.Equals
                (((JObject)v1)["@language"], ((JObject)v2
                )["@language"]) && Obj.Equals(((JObject)v1)["@index"], ((JObject)v2)["@index"]))
			{
				return true;
			}
            if ((v1 is JObject && ((JObject)v1).ContainsKey("@id")) &&
                 (v2 is JObject && ((JObject)v2).ContainsKey("@id")) &&
                ((JObject)v1)["@id"].Equals(((JObject)v2
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
        internal static void RemoveValue(JObject subject, string property
            , JObject value)
		{
			RemoveValue(subject, property, value, false);
		}

        internal static void RemoveValue(JObject subject, string property
            , JObject value, bool propertyIsArray)
		{
			// filter out value
			JArray values = new JArray();
            if (subject[property] is JArray)
			{
                foreach (JToken e in ((JArray)subject[property]))
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
		internal static bool IsBlankNode(JToken v)
		{
			// Note: A value is a blank node if all of these hold true:
			// 1. It is an Object.
			// 2. If it has an @id key its value begins with '_:'.
			// 3. It has no keys OR is not a @value, @set, or @list.
            if (v is JObject)
			{
				if (((JObject)v).ContainsKey("@id"))
				{
                    return ((string)((JObject)v)["@id"]).StartsWith("_:");
				}
				else
				{
                    return ((JObject)v).Count == 0 || !(((JObject)v).ContainsKey("@value") ||
                         ((JObject)v).ContainsKey("@set") || ((JObject)v).ContainsKey("@list"));
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
		internal static void ResolveContextUrls(JToken input)
		{
            Resolve(input, new JObject());
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private static void Resolve(JToken input, JObject cycles)
		{
			Pattern regex = Pattern.Compile("(http|https)://(\\w+:{0,1}\\w*@)?(\\S+)(:[0-9]+)?(/|/([\\w#!:.?+=&%@!\\-/]))?"
				);
			if (cycles.Count > MaxContextUrls)
			{
				throw new JsonLdError(JsonLdError.Error.UnknownError);
			}
			// for tracking the URLs to resolve
			JObject urls = new JObject();
			// find all URLs in the given input
			if (!FindContextUrls(input, urls, false))
			{
				// finished
				FindContextUrls(input, urls, true);
			}
			// queue all unresolved URLs
            JArray queue = new JArray();
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
                JObject _cycles = (JObject)Clone(cycles);
				_cycles[url_1] = true;
				try
				{
                    JObject ctx = (JObject)new DocumentLoader().LoadDocument(url_1).Document;
					if (!ctx.ContainsKey("@context"))
					{
						ctx = new JObject();
                        ctx["@context"] = new JObject();
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
        private static bool FindContextUrls(JToken input, JObject urls
			, bool replace)
		{
			int count = urls.Count;
			if (input is JArray)
			{
                foreach (JToken i in (JArray)input)
				{
					FindContextUrls(i, urls, replace);
				}
				return count < urls.Count;
			}
			else
			{
				if (input is JObject)
				{
					foreach (string key in input.GetKeys())
					{
						if (!"@context".Equals(key))
						{
                            FindContextUrls(((JObject)input)[key], urls, replace);
							continue;
						}
						// get @context
                        JToken ctx = ((JObject)input)[key];
						// array @context
						if (ctx is JArray)
						{
                            int length = ((JArray)ctx).Count;
							for (int i = 0; i < length; i++)
							{
                                JToken _ctx = ((JArray)ctx)[i];
								if (_ctx.Type == JTokenType.String)
								{
									// replace w/@context if requested
									if (replace)
									{
										_ctx = urls[(string)_ctx];
										if (_ctx is JArray)
										{
											// add flattened context
                                            ((JArray)ctx).RemoveAt(i);
                                            JsonLD.Collections.AddAllObj(((JArray)ctx), (ICollection)_ctx);
                                            i += ((JArray)_ctx).Count;
                                            length += ((JArray)_ctx).Count;
										}
										else
										{
                                            ((JArray)ctx)[i] = _ctx;
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
							if (ctx.Type == JTokenType.String)
							{
								// replace w/@context if requested
								if (replace)
								{
									((JObject)input)[key] = urls[(string)ctx];
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

        internal static JToken Clone(JToken value)
		{
            return value.DeepClone();
		}

		/// <summary>Returns true if the given value is a JSON-LD Array</summary>
		/// <param name="v">the value to check.</param>
		/// <returns></returns>
        internal static bool IsArray(JToken v)
		{
			return (v is JArray);
		}

		/// <summary>Returns true if the given value is a JSON-LD List</summary>
		/// <param name="v">the value to check.</param>
		/// <returns></returns>
        internal static bool IsList(JToken v)
		{
			return (v is JObject && ((IDictionary<string, JToken>)v).ContainsKey("@list")
				);
		}

		/// <summary>Returns true if the given value is a JSON-LD Object</summary>
		/// <param name="v">the value to check.</param>
		/// <returns></returns>
        internal static bool IsObject(JToken v)
		{
			return (v is JObject);
		}

		/// <summary>Returns true if the given value is a JSON-LD value</summary>
		/// <param name="v">the value to check.</param>
		/// <returns></returns>
        internal static bool IsValue(JToken v)
		{
			return (v is JObject && ((IDictionary<string, JToken>)v).ContainsKey("@value"
				));
		}

		/// <summary>Returns true if the given value is a JSON-LD string</summary>
		/// <param name="v">the value to check.</param>
		/// <returns></returns>
		internal static bool IsString(JToken v)
		{
			// TODO: should this return true for arrays of strings as well?
			return (v.Type == JTokenType.String);
		}
	}
}
