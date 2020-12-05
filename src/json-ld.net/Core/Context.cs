using System;
using System.Collections;
using System.Collections.Generic;
using JsonLD.Core;
using JsonLD.GenericJson;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
	/// <summary>
	/// A helper class which still stores all the values in a map but gives member
	/// variables easily access certain keys
	/// </summary>
	/// <author>tristan</author>
	//[System.Serializable]
	public class Context : GenericJsonObject
    {
        private JsonLdOptions options;

		GenericJsonObject termDefinitions;

		internal GenericJsonObject inverse = null;

		public Context() : this(new JsonLdOptions())
		{
		}

		public Context(JsonLdOptions options) : base()
		{
			Init(options);
		}

		public Context(GenericJsonObject map, JsonLdOptions options) : base((IDictionary<string,object>)map.Unwrap()
			)
		{
			Init(options);
		}

		public Context(GenericJsonObject map) : base((IDictionary<string, object>)map.Unwrap())
		{
			Init(new JsonLdOptions());
		}

		public Context(GenericJsonToken context, JsonLdOptions opts) : base(context is GenericJsonObject ?
			(IDictionary<string, object>)(context as GenericJsonObject).Unwrap() : null)
		{
            Init(opts);
		}

		// TODO: load remote context
		private void Init(JsonLdOptions options)
		{
			this.options = options;
			if (options.GetBase() != null)
			{
				this["@base"] = options.GetBase();
			}
            this.termDefinitions = new GenericJsonObject();
		}

		/// <summary>
		/// Value Compaction Algorithm
		/// http://json-ld.org/spec/latest/json-ld-api/#value-compaction
		/// </summary>
		/// <param name="activeProperty"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		internal virtual GenericJsonToken CompactValue(string activeProperty, GenericJsonObject value)
		{
            var dict = (IDictionary<string, GenericJsonToken>)value;
			// 1)
			int numberMembers = value.Count;
			// 2)
            if (dict.ContainsKey("@index") && "@index".Equals(this.GetContainer(activeProperty
				)))
			{
				numberMembers--;
			}
			// 3)
			if (numberMembers > 2)
			{
				return value;
			}
			// 4)
			string typeMapping = GetTypeMapping(activeProperty);
			string languageMapping = GetLanguageMapping(activeProperty);
            if (dict.ContainsKey("@id"))
			{
				// 4.1)
				if (numberMembers == 1 && "@id".Equals(typeMapping))
				{
					return CompactIri((string)value["@id"]);
				}
				// 4.2)
				if (numberMembers == 1 && "@vocab".Equals(typeMapping))
				{
					return CompactIri((string)value["@id"], true);
				}
				// 4.3)
				return value;
			}
			GenericJsonToken valueValue = value["@value"];
			// 5)
            if (dict.ContainsKey("@type") && value["@type"].SafeCompare(typeMapping))
			{
				return valueValue;
			}
			// 6)
            if (dict.ContainsKey("@language"))
			{
				// TODO: SPEC: doesn't specify to check default language as well
                if (value["@language"].SafeCompare(languageMapping) || value["@language"].SafeCompare(this["@language"]))
				{
					return valueValue;
				}
			}
			// 7)
			if (numberMembers == 1 && (!(valueValue.Type == GenericJsonTokenType.String) || !((IDictionary<string,GenericJsonToken>)this).ContainsKey("@language"
				) || (GetTermDefinition(activeProperty).ContainsKey("@language") && languageMapping
				 == null)))
			{
				return valueValue;
			}
			// 8)
			return value;
		}

		/// <summary>
		/// Context Processing Algorithm
		/// http://json-ld.org/spec/latest/json-ld-api/#context-processing-algorithms
		/// </summary>
		/// <param name="localContext"></param>
		/// <param name="remoteContexts"></param>
		/// <returns></returns>
		/// <exception cref="JsonLdError">JsonLdError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		internal virtual JsonLD.Core.Context Parse(GenericJsonToken localContext, List<string> remoteContexts)
		{
			if (remoteContexts == null)
			{
				remoteContexts = new List<string>();
			}
			// 1. Initialize result to the result of cloning active context.
			JsonLD.Core.Context result = ((JsonLD.Core.Context)this.Clone());
			// TODO: clone?
			// 2)
			if (!(localContext is GenericJsonArray))
			{
				GenericJsonToken temp = localContext;
				localContext = new GenericJsonArray();
				((GenericJsonArray)localContext).Add(temp);
			}
			// 3)
			foreach (GenericJsonToken context in (GenericJsonArray)localContext)
			{
                var eachContext = context;
				// 3.1)
				if (eachContext.Type == GenericJsonTokenType.Null)
				{
					result = new JsonLD.Core.Context(this.options);
					continue;
				}
				else
				{
					if (eachContext is JsonLD.Core.Context)
					{
                        result = ((JsonLD.Core.Context)(eachContext as JsonLD.Core.Context).Clone());
					}
					else
					{
						// 3.2)
						if (eachContext.Type == GenericJsonTokenType.String)
						{
							string uri = (string)result["@base"];
							uri = URL.Resolve(uri, (string)eachContext);
							// 3.2.2
							if (remoteContexts.Contains(uri))
							{
								throw new JsonLdError(JsonLdError.Error.RecursiveContextInclusion, uri);
							}
							remoteContexts.Add(uri);
							// 3.2.3: Dereference context
                            RemoteDocument rd;
                            try
                            {
                                rd = this.options.documentLoader.LoadDocument(uri);
                            }
                            catch (JsonLdError err)
                            {
                                if (err.Message.StartsWith(JsonLdError.Error.LoadingDocumentFailed.ToString()))
                                {
                                    throw new JsonLdError(JsonLdError.Error.LoadingRemoteContextFailed);
                                }
                                else
                                    throw;
                            }
							GenericJsonToken remoteContext = rd.document;
                            if (!(remoteContext is GenericJsonObject) || !((GenericJsonObject)remoteContext
								).ContainsKey("@context"))
							{
								// If the dereferenced document has no top-level JSON object
								// with an @context member
								throw new JsonLdError(JsonLdError.Error.InvalidRemoteContext, eachContext);
							}
                            eachContext = ((GenericJsonObject)remoteContext)["@context"];
							// 3.2.4
							result = result.Parse(eachContext, remoteContexts);
							// 3.2.5
							continue;
						}
						else
						{
                            if (!(eachContext is GenericJsonObject))
							{
								// 3.3
								throw new JsonLdError(JsonLdError.Error.InvalidLocalContext, eachContext);
							}
						}
					}
				}
				// 3.4
                if (remoteContexts.IsEmpty() && ((GenericJsonObject)eachContext).ContainsKey
					("@base"))
				{
                    GenericJsonToken value = eachContext["@base"];
					if (value.IsNull())
					{
						JsonLD.Collections.Remove(result, "@base");
					}
					else
					{
						if (value.Type == GenericJsonTokenType.String)
						{
							if (JsonLdUtils.IsAbsoluteIri((string)value))
							{
								result["@base"] = value;
							}
							else
							{
								string baseUri = (string)result["@base"];
								if (!JsonLdUtils.IsAbsoluteIri(baseUri))
								{
									throw new JsonLdError(JsonLdError.Error.InvalidBaseIri, baseUri);
								}
								result["@base"] = URL.Resolve(baseUri, (string)value);
							}
						}
						else
						{
							throw new JsonLdError(JsonLdError.Error.InvalidBaseIri, "@base must be a string");
						}
					}
				}
				// 3.5
                if (((GenericJsonObject)eachContext).ContainsKey("@vocab"))
				{
					GenericJsonToken value = eachContext["@vocab"];
					if (value.IsNull())
					{
						JsonLD.Collections.Remove(result, "@vocab");
					}
					else
					{
						if (value.Type == GenericJsonTokenType.String)
						{
							if (JsonLdUtils.IsAbsoluteIri((string)value))
							{
								result["@vocab"] = value;
							}
							else
							{
								throw new JsonLdError(JsonLdError.Error.InvalidVocabMapping, "@value must be an absolute IRI"
									);
							}
						}
						else
						{
							throw new JsonLdError(JsonLdError.Error.InvalidVocabMapping, "@vocab must be a string or null"
								);
						}
					}
				}
				// 3.6
				if (((GenericJsonObject)eachContext).ContainsKey("@language"))
				{
					GenericJsonToken value = ((GenericJsonObject)eachContext)["@language"];
					if (value.IsNull())
					{
						JsonLD.Collections.Remove(result, "@language");
					}
					else
					{
						if (value.Type == GenericJsonTokenType.String)
						{
							result["@language"] = ((string)value).ToLower();
						}
						else
						{
							throw new JsonLdError(JsonLdError.Error.InvalidDefaultLanguage, value);
						}
					}
				}
				// 3.7
				IDictionary<string, bool> defined = new Dictionary<string, bool>();
				foreach (string key in eachContext.GetKeys())
				{
					if ("@base".Equals(key) || "@vocab".Equals(key) || "@language".Equals(key))
					{
						continue;
					}
                    result.CreateTermDefinition((GenericJsonObject)eachContext, key, defined);
				}
			}
			return result;
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		internal virtual JsonLD.Core.Context Parse(GenericJsonToken localContext)
		{
			return this.Parse(localContext, new List<string>());
		}

		/// <summary>
		/// Create Term Definition Algorithm
		/// http://json-ld.org/spec/latest/json-ld-api/#create-term-definition
		/// </summary>
		/// <param name="result"></param>
		/// <param name="context"></param>
		/// <param name="key"></param>
		/// <param name="defined"></param>
		/// <exception cref="JsonLdError">JsonLdError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		private void CreateTermDefinition(GenericJsonObject context, string term
			, IDictionary<string, bool> defined)
		{
			if (defined.ContainsKey(term))
			{
				if (defined[term] == true)
				{
					return;
				}
				throw new JsonLdError(JsonLdError.Error.CyclicIriMapping, term);
			}
			defined[term] = false;
			if (JsonLdUtils.IsKeyword(term))
			{
				throw new JsonLdError(JsonLdError.Error.KeywordRedefinition, term);
			}
			JsonLD.Collections.Remove(this.termDefinitions, term);
			GenericJsonToken value = context[term];
			if (value.IsNull() || (value is GenericJsonObject && ((IDictionary<string, GenericJsonToken>)value
				).ContainsKey("@id") && ((IDictionary<string, GenericJsonToken>)value)["@id"].IsNull()))
			{
				this.termDefinitions[term] = null;
				defined[term] = true;
				return;
			}
			if (value.Type == GenericJsonTokenType.String)
			{
                GenericJsonObject tmp = new GenericJsonObject();
				tmp["@id"] = value;
				value = tmp;
			}
			if (!(value is GenericJsonObject))
			{
				throw new JsonLdError(JsonLdError.Error.InvalidTermDefinition, value);
			}
			// casting the value so it doesn't have to be done below everytime
			GenericJsonObject val = (GenericJsonObject)value;
			// 9) create a new term definition
			GenericJsonObject definition = new GenericJsonObject();
			// 10)
			if (val.ContainsKey("@type"))
			{
				if (!(val["@type"].Type == GenericJsonTokenType.String))
				{
					throw new JsonLdError(JsonLdError.Error.InvalidTypeMapping, val["@type"]);
				}
				string type = (string)val["@type"];
				try
				{
					type = this.ExpandIri((string)val["@type"], false, true, context, defined);
				}
				catch (JsonLdError error)
				{
					if (error.GetType() != JsonLdError.Error.InvalidIriMapping)
					{
						throw;
					}
					throw new JsonLdError(JsonLdError.Error.InvalidTypeMapping, type);
				}
				// TODO: fix check for absoluteIri (blank nodes shouldn't count, at
				// least not here!)
				if ("@id".Equals(type) || "@vocab".Equals(type) || (!type.StartsWith("_:") && JsonLdUtils
					.IsAbsoluteIri(type)))
				{
					definition["@type"] = type;
				}
				else
				{
					throw new JsonLdError(JsonLdError.Error.InvalidTypeMapping, type);
				}
			}
			// 11)
			if (val.ContainsKey("@reverse"))
			{
				if (val.ContainsKey("@id"))
				{
					throw new JsonLdError(JsonLdError.Error.InvalidReverseProperty, val);
				}
				if (!(val["@reverse"].Type == GenericJsonTokenType.String))
				{
					throw new JsonLdError(JsonLdError.Error.InvalidIriMapping, "Expected String for @reverse value. got "
						 + (val["@reverse"].IsNull() ? "null" : val["@reverse"].GetType().ToString()));
				}
				string reverse = this.ExpandIri((string)val["@reverse"], false, true, context, defined
					);
				if (!JsonLdUtils.IsAbsoluteIri(reverse))
				{
					throw new JsonLdError(JsonLdError.Error.InvalidIriMapping, "Non-absolute @reverse IRI: "
						 + reverse);
				}
				definition["@id"] = reverse;
				if (val.ContainsKey("@container"))
				{
					string container = (string)val["@container"];
					if (container == null || "@set".Equals(container) || "@index".Equals(container))
					{
						definition["@container"] = container;
					}
					else
					{
						throw new JsonLdError(JsonLdError.Error.InvalidReverseProperty, "reverse properties only support set- and index-containers"
							);
					}
				}
				definition["@reverse"] = true;
				this.termDefinitions[term] = definition;
				defined[term] = true;
				return;
			}
			// 12)
			definition["@reverse"] = false;
			// 13)
			if (!val["@id"].IsNull() && !val["@id"].SafeCompare(term))
			{
				if (!(val["@id"].Type == GenericJsonTokenType.String))
				{
					throw new JsonLdError(JsonLdError.Error.InvalidIriMapping, "expected value of @id to be a string"
						);
				}
				string res = this.ExpandIri((string)val["@id"], false, true, context, defined);
				if (JsonLdUtils.IsKeyword(res) || JsonLdUtils.IsAbsoluteIri(res))
				{
					if ("@context".Equals(res))
					{
						throw new JsonLdError(JsonLdError.Error.InvalidKeywordAlias, "cannot alias @context"
							);
					}
					definition["@id"] = res;
				}
				else
				{
					throw new JsonLdError(JsonLdError.Error.InvalidIriMapping, "resulting IRI mapping should be a keyword, absolute IRI or blank node"
						);
				}
			}
			else
			{
				// 14)
				if (term.IndexOf(":") >= 0)
				{
					int colIndex = term.IndexOf(":");
					string prefix = JsonLD.JavaCompat.Substring(term, 0, colIndex);
					string suffix = JsonLD.JavaCompat.Substring(term, colIndex + 1);
					if (context.ContainsKey(prefix))
					{
						this.CreateTermDefinition(context, prefix, defined);
					}
					if (termDefinitions.ContainsKey(prefix))
					{
						definition["@id"] = (string)(((IDictionary<string, GenericJsonToken>)termDefinitions[prefix])["@id"]) + suffix;
					}
					else
					{
						definition["@id"] = term;
					}
				}
				else
				{
					// 15)
					if (this.ContainsKey("@vocab"))
					{
						definition["@id"] = (string)this["@vocab"] + term;
					}
					else
					{
						throw new JsonLdError(JsonLdError.Error.InvalidIriMapping, "relative term definition without vocab mapping"
							);
					}
				}
			}
			// 16)
			if (val.ContainsKey("@container"))
			{
				string container = (string)val["@container"];
				if (!"@list".Equals(container) && !"@set".Equals(container) && !"@index".Equals(container
					) && !"@language".Equals(container))
				{
					throw new JsonLdError(JsonLdError.Error.InvalidContainerMapping, "@container must be either @list, @set, @index, or @language"
						);
				}
				definition["@container"] = container;
			}
			// 17)
			if (val.ContainsKey("@language") && !val.ContainsKey("@type"))
			{
				if (val["@language"].IsNull() || val["@language"].Type == GenericJsonTokenType.String)
				{
					string language = (string)val["@language"];
					definition["@language"] = language != null ? language.ToLower() : null;
				}
				else
				{
					throw new JsonLdError(JsonLdError.Error.InvalidLanguageMapping, "@language must be a string or null"
						);
				}
			}
			// 18)
			this.termDefinitions[term] = definition;
			defined[term] = true;
		}

		/// <summary>
		/// IRI Expansion Algorithm
		/// http://json-ld.org/spec/latest/json-ld-api/#iri-expansion
		/// </summary>
		/// <param name="value"></param>
		/// <param name="relative"></param>
		/// <param name="vocab"></param>
		/// <param name="context"></param>
		/// <param name="defined"></param>
		/// <returns></returns>
		/// <exception cref="JsonLdError">JsonLdError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		internal virtual string ExpandIri(string value, bool relative, bool vocab, GenericJsonObject context, IDictionary<string, bool> defined)
		{
			// 1)
			if (value == null || JsonLdUtils.IsKeyword(value))
			{
				return value;
			}
			// 2)
			if (context != null && context.ContainsKey(value) && defined.ContainsKey(value) && !defined[value])
			{
				this.CreateTermDefinition(context, value, defined);
			}
			// 3)
			if (vocab && this.termDefinitions.ContainsKey(value))
			{
                GenericJsonToken td = this.termDefinitions[value];
				if (td.Type != GenericJsonTokenType.Null)
				{
					return (string)((GenericJsonObject)td)["@id"];
				}
				else
				{
					return null;
				}
			}
			// 4)
			int colIndex = value.IndexOf(":");
			if (colIndex >= 0)
			{
				// 4.1)
				string prefix = JsonLD.JavaCompat.Substring(value, 0, colIndex);
				string suffix = JsonLD.JavaCompat.Substring(value, colIndex + 1);
				// 4.2)
				if ("_".Equals(prefix) || suffix.StartsWith("//"))
				{
					return value;
				}
				// 4.3)
				if (context != null && context.ContainsKey(prefix) && (!defined.ContainsKey(prefix
					) || defined[prefix] == false))
				{
					this.CreateTermDefinition(context, prefix, defined);
				}
				// 4.4)
				if (this.termDefinitions.ContainsKey(prefix))
				{
                    return (string)((GenericJsonObject)this.termDefinitions[prefix])["@id"] 
						+ suffix;
				}
				// 4.5)
				return value;
			}
			// 5)
			if (vocab && this.ContainsKey("@vocab"))
			{
				return (string)this["@vocab"] + value;
			}
			else
			{
				// 6)
				if (relative)
				{
					return URL.Resolve((string)this["@base"], value);
				}
				else
				{
					if (context != null && JsonLdUtils.IsRelativeIri(value))
					{
						throw new JsonLdError(JsonLdError.Error.InvalidIriMapping, "not an absolute IRI: "
							 + value);
					}
				}
			}
			// 7)
			return value;
		}

		/// <summary>
		/// IRI Compaction Algorithm
		/// http://json-ld.org/spec/latest/json-ld-api/#iri-compaction
		/// Compacts an IRI or keyword into a term or prefix if it can be.
		/// </summary>
		/// <remarks>
		/// IRI Compaction Algorithm
		/// http://json-ld.org/spec/latest/json-ld-api/#iri-compaction
		/// Compacts an IRI or keyword into a term or prefix if it can be. If the IRI
		/// has an associated value it may be passed.
		/// </remarks>
		/// <param name="iri">the IRI to compact.</param>
		/// <param name="value">the value to check or null.</param>
		/// <param name="relativeTo">
		/// options for how to compact IRIs: vocab: true to split after
		/// false not to.
		/// </param>
		/// <param name="reverse">true if a reverse property is being compacted, false if not.
		/// 	</param>
		/// <returns>the compacted term, prefix, keyword alias, or the original IRI.</returns>
		internal virtual string CompactIri(string iri, GenericJsonToken value, bool relativeToVocab
			, bool reverse)
		{
			// 1)
			if (iri == null)
			{
				return null;
			}
			// 2)
			if (relativeToVocab && GetInverse().ContainsKey(iri))
			{
				// 2.1)
				string defaultLanguage = (string)this["@language"];
				if (defaultLanguage == null)
				{
					defaultLanguage = "@none";
				}
				// 2.2)
				GenericJsonArray containers = new GenericJsonArray();
				// 2.3)
				string typeLanguage = "@language";
				string typeLanguageValue = "@null";
				// 2.4)
				if (value is GenericJsonObject && ((IDictionary<string, GenericJsonToken>)value).ContainsKey("@index"
					))
				{
					containers.Add("@index");
				}
				// 2.5)
				if (reverse)
				{
					typeLanguage = "@type";
					typeLanguageValue = "@reverse";
					containers.Add("@set");
				}
				else
				{
					// 2.6)
                    if (value is GenericJsonObject && ((IDictionary<string, GenericJsonToken>)value).ContainsKey("@list"))
					{
						// 2.6.1)
						if (!((IDictionary<string, GenericJsonToken>)value).ContainsKey("@index"))
						{
							containers.Add("@list");
						}
						// 2.6.2)
                        GenericJsonArray list = (GenericJsonArray)((GenericJsonObject)value)["@list"];
						// 2.6.3)
						string commonLanguage = (list.Count == 0) ? defaultLanguage : null;
						string commonType = null;
						// 2.6.4)
						foreach (GenericJsonToken item in list)
						{
							// 2.6.4.1)
							string itemLanguage = "@none";
							string itemType = "@none";
							// 2.6.4.2)
							if (JsonLdUtils.IsValue(item))
							{
								// 2.6.4.2.1)
								if (((IDictionary<string, GenericJsonToken>)item).ContainsKey("@language"))
								{
									itemLanguage = (string)((GenericJsonObject)item)["@language"];
								}
								else
								{
									// 2.6.4.2.2)
									if (((IDictionary<string, GenericJsonToken>)item).ContainsKey("@type"))
									{
										itemType = (string)((GenericJsonObject)item)["@type"];
									}
									else
									{
										// 2.6.4.2.3)
										itemLanguage = "@null";
									}
								}
							}
							else
							{
								// 2.6.4.3)
								itemType = "@id";
							}
							// 2.6.4.4)
							if (commonLanguage == null)
							{
								commonLanguage = itemLanguage;
							}
							else
							{
								// 2.6.4.5)
								if (!commonLanguage.Equals(itemLanguage) && JsonLdUtils.IsValue(item))
								{
									commonLanguage = "@none";
								}
							}
							// 2.6.4.6)
							if (commonType == null)
							{
								commonType = itemType;
							}
							else
							{
								// 2.6.4.7)
								if (!commonType.Equals(itemType))
								{
									commonType = "@none";
								}
							}
							// 2.6.4.8)
							if ("@none".Equals(commonLanguage) && "@none".Equals(commonType))
							{
								break;
							}
						}
						// 2.6.5)
						commonLanguage = (commonLanguage != null) ? commonLanguage : "@none";
						// 2.6.6)
						commonType = (commonType != null) ? commonType : "@none";
						// 2.6.7)
						if (!"@none".Equals(commonType))
						{
							typeLanguage = "@type";
							typeLanguageValue = commonType;
						}
						else
						{
							// 2.6.8)
							typeLanguageValue = commonLanguage;
						}
					}
					else
					{
						// 2.7)
						// 2.7.1)
                        if (value is GenericJsonObject && ((IDictionary<string, GenericJsonToken>)value).ContainsKey("@value"
							))
						{
							// 2.7.1.1)
                            if (((IDictionary<string, GenericJsonToken>)value).ContainsKey("@language") && !((IDictionary
                                <string, GenericJsonToken>)value).ContainsKey("@index"))
							{
								containers.Add("@language");
                                typeLanguageValue = (string)((IDictionary<string, GenericJsonToken>)value)["@language"];
							}
							else
							{
								// 2.7.1.2)
                                if (((IDictionary<string, GenericJsonToken>)value).ContainsKey("@type"))
								{
									typeLanguage = "@type";
                                    typeLanguageValue = (string)((IDictionary<string, GenericJsonToken>)value)["@type"];
								}
							}
						}
						else
						{
							// 2.7.2)
							typeLanguage = "@type";
							typeLanguageValue = "@id";
						}
						// 2.7.3)
						containers.Add("@set");
					}
				}
				// 2.8)
				containers.Add("@none");
				// 2.9)
				if (typeLanguageValue == null)
				{
					typeLanguageValue = "@null";
				}
				// 2.10)
				GenericJsonArray preferredValues = new GenericJsonArray();
				// 2.11)
				if ("@reverse".Equals(typeLanguageValue))
				{
					preferredValues.Add("@reverse");
				}
				// 2.12)
				if (("@reverse".Equals(typeLanguageValue) || "@id".Equals(typeLanguageValue)) && 
					(value is GenericJsonObject) && ((GenericJsonObject)value).ContainsKey("@id"
					))
				{
					// 2.12.1)
                    string result = this.CompactIri((string)((IDictionary<string, GenericJsonToken>)value)["@id"
						], null, true, true);
                    if (termDefinitions.ContainsKey(result) && ((IDictionary<string, GenericJsonToken>)termDefinitions
                        [result]).ContainsKey("@id") && ((IDictionary<string, GenericJsonToken>)value)["@id"].SafeCompare
                        (((IDictionary<string, GenericJsonToken>)termDefinitions[result])["@id"]))
					{
						preferredValues.Add("@vocab");
						preferredValues.Add("@id");
					}
					else
					{
						// 2.12.2)
						preferredValues.Add("@id");
						preferredValues.Add("@vocab");
					}
				}
				else
				{
					// 2.13)
					preferredValues.Add(typeLanguageValue);
				}
				preferredValues.Add("@none");
				// 2.14)
				string term = SelectTerm(iri, containers, typeLanguage, preferredValues);
				// 2.15)
				if (term != null)
				{
					return term;
				}
			}
			// 3)
			if (relativeToVocab && this.ContainsKey("@vocab"))
			{
				// determine if vocab is a prefix of the iri
				string vocab = (string)this["@vocab"];
				// 3.1)
				if (iri.IndexOf(vocab) == 0 && !iri.Equals(vocab))
				{
					// use suffix as relative iri if it is not a term in the
					// active context
					string suffix = JsonLD.JavaCompat.Substring(iri, vocab.Length);
					if (!termDefinitions.ContainsKey(suffix))
					{
						return suffix;
					}
				}
			}
			// 4)
			string compactIRI = null;
			// 5)
			foreach (string term_1 in termDefinitions.GetKeys())
			{
				GenericJsonToken termDefinitionToken = termDefinitions[term_1];
				// 5.1)
				if (term_1.Contains(":"))
				{
					continue;
				}
				// 5.2)
                if (termDefinitionToken.Type == GenericJsonTokenType.Null)
                {
                    continue;
                }
                GenericJsonObject termDefinition = (GenericJsonObject)termDefinitionToken;
                if (termDefinition["@id"].SafeCompare(iri) || !iri.StartsWith
					((string)termDefinition["@id"]))
				{
					continue;
				}
				// 5.3)
				string candidate = term_1 + ":" + JsonLD.JavaCompat.Substring(iri, ((string)termDefinition
					["@id"]).Length);
				// 5.4)
				if ((compactIRI == null || JsonLdUtils.CompareShortestLeast(candidate, compactIRI
					) < 0) && (!termDefinitions.ContainsKey(candidate) || (((IDictionary<
                    string, GenericJsonToken>)termDefinitions[candidate])["@id"].SafeCompare(iri)) && value.IsNull()))
				{
					compactIRI = candidate;
				}
			}
			// 6)
			if (compactIRI != null)
			{
				return compactIRI;
			}
			// 7)
			if (!relativeToVocab)
			{
				return URL.RemoveBase(this["@base"], iri);
			}
			// 8)
			return iri;
		}

		internal virtual string CompactIri(string iri, bool relativeToVocab)
		{
			return CompactIri(iri, null, relativeToVocab, false);
		}

		internal virtual string CompactIri(string iri)
		{
			return CompactIri(iri, null, false, false);
		}

		public object Clone()
		{
			JsonLD.Core.Context rval = new Context(base.DeepClone(),options);
			rval.termDefinitions = (GenericJsonObject)termDefinitions.DeepClone();
			return rval;
		}

		/// <summary>
		/// Inverse Context Creation
		/// http://json-ld.org/spec/latest/json-ld-api/#inverse-context-creation
		/// Generates an inverse context for use in the compaction algorithm, if not
		/// already generated for the given active context.
		/// </summary>
		/// <remarks>
		/// Inverse Context Creation
		/// http://json-ld.org/spec/latest/json-ld-api/#inverse-context-creation
		/// Generates an inverse context for use in the compaction algorithm, if not
		/// already generated for the given active context.
		/// </remarks>
		/// <returns>the inverse context.</returns>
		internal virtual GenericJsonObject GetInverse()
		{
			// lazily create inverse
			if (inverse != null)
			{
				return inverse;
			}
			// 1)
			inverse = new GenericJsonObject();
			// 2)
			string defaultLanguage = (string)this["@language"];
			if (defaultLanguage == null)
			{
				defaultLanguage = "@none";
			}
			// create term selections for each mapping in the context, ordererd by
			// shortest and then lexicographically least
			GenericJsonArray terms = new GenericJsonArray(termDefinitions.GetKeys());
			((GenericJsonArray)terms).SortInPlace(new _IComparer_794());
			foreach (string term in terms)
			{
                GenericJsonToken definitionToken = termDefinitions[term];
                // 3.1)
                if (definitionToken.Type == GenericJsonTokenType.Null)
                {
                    continue;
                }

				GenericJsonObject definition = (GenericJsonObject)termDefinitions[term];
				// 3.2)
				string container = (string)definition["@container"];
				if (container == null)
				{
					container = "@none";
				}
				// 3.3)
				string iri = (string)definition["@id"];
				// 3.4 + 3.5)
				GenericJsonObject containerMap = (GenericJsonObject)inverse[iri];
				if (containerMap == null)
				{
					containerMap = new GenericJsonObject();
					inverse[iri] = containerMap;
				}
				// 3.6 + 3.7)
				GenericJsonObject typeLanguageMap = (GenericJsonObject)containerMap[container];
				if (typeLanguageMap == null)
				{
					typeLanguageMap = new GenericJsonObject();
					typeLanguageMap["@language"] = new GenericJsonObject();
					typeLanguageMap["@type"] = new GenericJsonObject();
					containerMap[container] = typeLanguageMap;
				}
				// 3.8)
				if (definition["@reverse"].SafeCompare(true))
				{
                    GenericJsonObject typeMap = (GenericJsonObject)typeLanguageMap
						["@type"];
					if (!typeMap.ContainsKey("@reverse"))
					{
						typeMap["@reverse"] = term;
					}
				}
				else
				{
					// 3.9)
					if (definition.ContainsKey("@type"))
					{
                        GenericJsonObject typeMap = (GenericJsonObject)typeLanguageMap["@type"];
						if (!typeMap.ContainsKey((string)definition["@type"]))
						{
							typeMap[(string)definition["@type"]] = term;
						}
					}
					else
					{
						// 3.10)
						if (definition.ContainsKey("@language"))
						{
                            GenericJsonObject languageMap = (GenericJsonObject)typeLanguageMap
								["@language"];
							string language = (string)definition["@language"];
							if (language == null)
							{
								language = "@null";
							}
							if (!languageMap.ContainsKey(language))
							{
								languageMap[language] = term;
							}
						}
						else
						{
							// 3.11)
							// 3.11.1)
                            GenericJsonObject languageMap = (GenericJsonObject)typeLanguageMap
								["@language"];
							// 3.11.2)
							if (!languageMap.ContainsKey("@language"))
							{
								languageMap["@language"] = term;
							}
							// 3.11.3)
							if (!languageMap.ContainsKey("@none"))
							{
								languageMap["@none"] = term;
							}
							// 3.11.4)
                            GenericJsonObject typeMap = (GenericJsonObject)typeLanguageMap
								["@type"];
							// 3.11.5)
							if (!typeMap.ContainsKey("@none"))
							{
								typeMap["@none"] = term;
							}
						}
					}
				}
			}
			// 4)
			return inverse;
		}

		private sealed class _IComparer_794 : IComparer<GenericJsonToken>
		{
			public _IComparer_794()
			{
			}

			public int Compare(GenericJsonToken a, GenericJsonToken b)
			{
				return JsonLdUtils.CompareShortestLeast((string)a, (string)b);
			}
		}

		/// <summary>
		/// Term Selection
		/// http://json-ld.org/spec/latest/json-ld-api/#term-selection
		/// This algorithm, invoked via the IRI Compaction algorithm, makes use of an
		/// active context's inverse context to find the term that is best used to
		/// compact an IRI.
		/// </summary>
		/// <remarks>
		/// Term Selection
		/// http://json-ld.org/spec/latest/json-ld-api/#term-selection
		/// This algorithm, invoked via the IRI Compaction algorithm, makes use of an
		/// active context's inverse context to find the term that is best used to
		/// compact an IRI. Other information about a value associated with the IRI
		/// is given, including which container mappings and which type mapping or
		/// language mapping would be best used to express the value.
		/// </remarks>
		/// <returns>the selected term.</returns>
		private string SelectTerm(string iri, GenericJsonArray containers, string typeLanguage
			, GenericJsonArray preferredValues)
		{
			GenericJsonObject inv = GetInverse();
			// 1)
			GenericJsonObject containerMap = (GenericJsonObject)inv[iri];
			// 2)
			foreach (string container in containers)
			{
				// 2.1)
				if (!containerMap.ContainsKey(container))
				{
					continue;
				}
				// 2.2)
				GenericJsonObject typeLanguageMap = (GenericJsonObject)containerMap
					[container];
				// 2.3)
				GenericJsonObject valueMap = (GenericJsonObject)typeLanguageMap
					[typeLanguage];
				// 2.4 )
				foreach (string item in preferredValues)
				{
					// 2.4.1
					if (!valueMap.ContainsKey(item))
					{
						continue;
					}
					// 2.4.2
					return (string)valueMap[item];
				}
			}
			// 3)
			return null;
		}

		/// <summary>Retrieve container mapping.</summary>
		/// <remarks>Retrieve container mapping.</remarks>
		/// <param name="property"></param>
		/// <returns></returns>
		internal virtual string GetContainer(string property)
		{
            // TODO(sblom): Do java semantics of get() on a Map return null if property is null?
            if (property == null)
            {
                return null;
            }

			if ("@graph".Equals(property))
			{
				return "@set";
			}
			if (JsonLdUtils.IsKeyword(property))
			{
				return property;
			}
            GenericJsonObject td = (GenericJsonObject)termDefinitions[property
				];
			if (td == null)
			{
				return null;
			}
			return (string)td["@container"];
		}

		internal virtual bool IsReverseProperty(string property)
		{
            if (property == null)
            {
                return false;
            }
            GenericJsonObject td = (GenericJsonObject)termDefinitions[property];
			if (td == null)
			{
				return false;
			}
			GenericJsonToken reverse = td["@reverse"];
			return !reverse.IsNull() && (bool)reverse;
		}

		private string GetTypeMapping(string property)
		{
            if (property == null)
            {
                return null;
            }
			GenericJsonToken td = termDefinitions[property];
			if (td.IsNull())
			{
				return null;
			}
			return (string)((GenericJsonObject)td)["@type"];
		}

		private string GetLanguageMapping(string property)
		{
            if (property == null)
            {
                return null;
            }
			GenericJsonObject td = (GenericJsonObject)termDefinitions[property];
			if (td == null)
			{
				return null;
			}
			return (string)td["@language"];
		}

		internal virtual GenericJsonObject GetTermDefinition(string key)
		{
			return (GenericJsonObject)termDefinitions[key];
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		internal virtual GenericJsonToken ExpandValue(string activeProperty, GenericJsonToken value)
		{
			GenericJsonObject rval = new GenericJsonObject();
			GenericJsonObject td = GetTermDefinition(activeProperty);
			// 1)
			if (td != null && td["@type"].SafeCompare("@id"))
			{
				// TODO: i'm pretty sure value should be a string if the @type is
				// @id
				rval["@id"] = ExpandIri((string)value, true, false, null, null);
				return rval;
			}
			// 2)
			if (td != null && td["@type"].SafeCompare("@vocab"))
			{
				// TODO: same as above
				rval["@id"] = ExpandIri((string)value, true, true, null, null);
				return rval;
			}
			// 3)
			rval["@value"] = value;
			// 4)
			if (td != null && td.ContainsKey("@type"))
			{
				rval["@type"] = td["@type"];
			}
			else
			{
				// 5)
				if (value.Type == GenericJsonTokenType.String)
				{
					// 5.1)
					if (td != null && td.ContainsKey("@language"))
					{
						string lang = (string)td["@language"];
						if (lang != null)
						{
							rval["@language"] = lang;
						}
					}
					else
					{
						// 5.2)
						if (!this["@language"].IsNull())
						{
							rval["@language"] = this["@language"];
						}
					}
				}
			}
			return rval;
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal virtual GenericJsonObject GetContextValue(string activeProperty, string @string)
		{
			throw new JsonLdError(JsonLdError.Error.NotImplemented, "getContextValue is only used by old code so far and thus isn't implemented"
				);
		}

		public virtual GenericJsonObject Serialize()
		{
			GenericJsonObject ctx = new GenericJsonObject();
			if (!this["@base"].IsNull() && !this["@base"].SafeCompare(options.GetBase()))
			{
				ctx["@base"] = this["@base"];
			}
			if (!this["@language"].IsNull())
			{
				ctx["@language"] = this["@language"];
			}
			if (!this["@vocab"].IsNull())
			{
				ctx["@vocab"] = this["@vocab"];
			}
			foreach (string term in termDefinitions.GetKeys())
			{
				GenericJsonObject definition = (GenericJsonObject)termDefinitions[term];
				if (definition["@language"].IsNull() && definition["@container"].IsNull() && definition
					["@type"].IsNull() && (definition["@reverse"].IsNull() || (definition["@reverse"].Type == GenericJsonTokenType.Boolean && (bool)definition["@reverse"] == false)))
				{
					string cid = this.CompactIri((string)definition["@id"]);
					ctx[term] = term.Equals(cid) ? (string)definition["@id"] : cid;
				}
				else
				{
					GenericJsonObject defn = new GenericJsonObject();
					string cid = this.CompactIri((string)definition["@id"]);
					bool reverseProperty = definition["@reverse"].SafeCompare(true);
					if (!(term.Equals(cid) && !reverseProperty))
					{
						defn[reverseProperty ? "@reverse" : "@id"] = cid;
					}
					string typeMapping = (string)definition["@type"];
					if (typeMapping != null)
					{
						defn["@type"] = JsonLdUtils.IsKeyword(typeMapping) ? typeMapping : CompactIri(typeMapping
							, true);
					}
					if (!definition["@container"].IsNull())
					{
						defn["@container"] = definition["@container"];
					}
					GenericJsonToken lang = definition["@language"];
					if (!definition["@language"].IsNull())
					{
						defn["@language"] = lang.SafeCompare(false) ? null : lang;
					}
					ctx[term] = defn;
				}
			}
			GenericJsonObject rval = new GenericJsonObject();
			if (!(ctx == null || ctx.IsEmpty()))
			{
				rval["@context"] = ctx;
			}
			return rval;
		}
	}
}
