using System.Collections;
using System.Collections.Generic;
using JsonLD.Core;
using JsonLD.Util;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
	public class JsonLdApi
	{
		//private static readonly ILogger Log = LoggerFactory.GetLogger(typeof(JsonLDNet.Core.JsonLdApi));

		internal JsonLdOptions opts;

        internal JToken value = null;

		internal Context context = null;

		public JsonLdApi()
		{
			opts = new JsonLdOptions(string.Empty);
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public JsonLdApi(JToken input, JsonLdOptions opts)
		{
			Initialize(input, null, opts);
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public JsonLdApi(JToken input, JToken context, JsonLdOptions opts)
		{
			Initialize(input, null, opts);
		}

		public JsonLdApi(JsonLdOptions opts)
		{
			if (opts == null)
			{
				opts = new JsonLdOptions(string.Empty);
			}
			else
			{
				this.opts = opts;
			}
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private void Initialize(JToken input, JToken context, JsonLdOptions opts)
		{
			// set option defaults (TODO: clone?)
			// NOTE: sane defaults should be set in JsonLdOptions constructor
			this.opts = opts;
			if (input is JArray || input is JObject)
			{
				this.value = JsonLdUtils.Clone(input);
			}
			// TODO: string/IO input
			this.context = new Context(opts);
			if (!context.IsNull())
			{
				this.context = this.context.Parse(context);
			}
		}

		/// <summary>
		/// Compaction Algorithm
		/// http://json-ld.org/spec/latest/json-ld-api/#compaction-algorithm
		/// </summary>
		/// <param name="activeCtx"></param>
		/// <param name="activeProperty"></param>
		/// <param name="element"></param>
		/// <param name="compactArrays"></param>
		/// <returns></returns>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		public virtual JToken Compact(Context activeCtx, string activeProperty, JToken element
			, bool compactArrays)
		{
			// 2)
			if (element is JArray)
			{
				// 2.1)
				JArray result = new JArray();
				// 2.2)
				foreach (JToken item in element)
				{
					// 2.2.1)
					JToken compactedItem = Compact(activeCtx, activeProperty, item, compactArrays);
					// 2.2.2)
					if (!compactedItem.IsNull())
					{
						result.Add(compactedItem);
					}
				}
				// 2.3)
				if (compactArrays && result.Count == 1 && activeCtx.GetContainer(activeProperty) 
					== null)
				{
					return result[0];
				}
				// 2.4)
				return result;
			}
			// 3)
			if (element is JObject)
			{
				// access helper
                IDictionary<string, JToken> elem = (IDictionary<string, JToken>)element;
				// 4
				if (elem.ContainsKey("@value") || elem.ContainsKey("@id"))
				{
					JToken compactedValue = activeCtx.CompactValue(activeProperty, (JObject)element);
					if (!(compactedValue is JObject || compactedValue is JArray))
					{
						return compactedValue;
					}
				}
				// 5)
				bool insideReverse = ("@reverse".Equals(activeProperty));
				// 6)
				JObject result = new JObject();
				// 7)
                JArray keys = new JArray(element.GetKeys());
				keys.SortInPlace();
				foreach (string expandedProperty in keys)
				{
					JToken expandedValue = elem[expandedProperty];
					// 7.1)
					if ("@id".Equals(expandedProperty) || "@type".Equals(expandedProperty))
					{
						JToken compactedValue;
						// 7.1.1)
						if (expandedValue.Type == JTokenType.String)
						{
							compactedValue = activeCtx.CompactIri((string)expandedValue, "@type".Equals(expandedProperty
								));
						}
						else
						{
							// 7.1.2)
                            JArray types = new JArray();
							// 7.1.2.2)
							foreach (string expandedType in (JArray)expandedValue)
							{
								types.Add(activeCtx.CompactIri(expandedType, true));
							}
							// 7.1.2.3)
							if (types.Count == 1)
							{
								compactedValue = types[0];
							}
							else
							{
								compactedValue = types;
							}
						}
						// 7.1.3)
						string alias = activeCtx.CompactIri(expandedProperty, true);
						// 7.1.4)
						result[alias] = compactedValue;
						continue;
					}
					// TODO: old add value code, see if it's still relevant?
					// addValue(rval, alias, compactedValue,
					// isArray(compactedValue)
					// && ((List<Object>) expandedValue).size() == 0);
					// 7.2)
					if ("@reverse".Equals(expandedProperty))
					{
						// 7.2.1)
						JObject compactedValue = (JObject)Compact(activeCtx, "@reverse", expandedValue, compactArrays);
						// 7.2.2)
                        List<string> properties = new List<string>(compactedValue.GetKeys());
						foreach (string property in properties)
						{
							JToken value = compactedValue[property];
							// 7.2.2.1)
							if (activeCtx.IsReverseProperty(property))
							{
								// 7.2.2.1.1)
								if (("@set".Equals(activeCtx.GetContainer(property)) || !compactArrays) && !(value
									 is JArray))
								{
									JArray tmp = new JArray();
									tmp.Add(value);
									result[property] = tmp;
								}
								// 7.2.2.1.2)
								if (!result.ContainsKey(property))
								{
									result[property] = value;
								}
								else
								{
									// 7.2.2.1.3)
									if (!(result[property] is JArray))
									{
                                        JArray tmp = new JArray();
                                        tmp.Add(result[property]);
										result[property] = tmp;
									}
									if (value is JArray)
									{
										JsonLD.Collections.AddAll(((JArray)result[property]), (JArray)value
											);
									}
									else
									{
										((JArray)result[property]).Add(value);
									}
								}
								// 7.2.2.1.4) TODO: this doesn't seem safe (i.e.
								// modifying the map being used to drive the loop)!
								JsonLD.Collections.Remove(compactedValue, property);
							}
						}
						// 7.2.3)
						if (compactedValue.Count != 0)
						{
							// 7.2.3.1)
							string alias = activeCtx.CompactIri("@reverse", true);
							// 7.2.3.2)
							result[alias] = compactedValue;
						}
						// 7.2.4)
						continue;
					}
					// 7.3)
					if ("@index".Equals(expandedProperty) && "@index".Equals(activeCtx.GetContainer(activeProperty
						)))
					{
						continue;
					}
					else
					{
						// 7.4)
						if ("@index".Equals(expandedProperty) || "@value".Equals(expandedProperty) || "@language"
							.Equals(expandedProperty))
						{
							// 7.4.1)
							string alias = activeCtx.CompactIri(expandedProperty, true);
							// 7.4.2)
							result[alias] = expandedValue;
							continue;
						}
					}
					// NOTE: expanded value must be an array due to expansion
					// algorithm.
					// 7.5)
					if (((JArray)expandedValue).Count == 0)
					{
						// 7.5.1)
						string itemActiveProperty = activeCtx.CompactIri(expandedProperty, expandedValue, 
							true, insideReverse);
						// 7.5.2)
						if (!result.ContainsKey(itemActiveProperty))
						{
							result[itemActiveProperty] = new JArray();
						}
						else
						{
							JToken value = result[itemActiveProperty];
							if (!(value is JArray))
							{
								JArray tmp = new JArray();
								tmp.Add(value);
								result[itemActiveProperty] = tmp;
							}
						}
					}
					// 7.6)
					foreach (JToken expandedItem in (JArray)expandedValue)
					{
						// 7.6.1)
						string itemActiveProperty = activeCtx.CompactIri(expandedProperty, expandedItem, 
							true, insideReverse);
						// 7.6.2)
						string container = activeCtx.GetContainer(itemActiveProperty);
						// get @list value if appropriate
						bool isList = (expandedItem is JObject && ((IDictionary<string, JToken>)expandedItem
							).ContainsKey("@list"));
						JToken list = null;
						if (isList)
						{
							list = ((IDictionary<string, JToken>)expandedItem)["@list"];
						}
						// 7.6.3)
						JToken compactedItem = Compact(activeCtx, itemActiveProperty, isList ? list : expandedItem
							, compactArrays);
						// 7.6.4)
						if (isList)
						{
							// 7.6.4.1)
							if (!(compactedItem is JArray))
							{
								JArray tmp = new JArray();
								tmp.Add(compactedItem);
								compactedItem = tmp;
							}
							// 7.6.4.2)
							if (!"@list".Equals(container))
							{
								// 7.6.4.2.1)
								JObject wrapper = new JObject();
								// TODO: SPEC: no mention of vocab = true
								wrapper[activeCtx.CompactIri("@list", true)] = compactedItem;
								compactedItem = wrapper;
								// 7.6.4.2.2)
								if (((IDictionary<string, JToken>)expandedItem).ContainsKey("@index"))
								{
									((IDictionary<string, JToken>)compactedItem)[activeCtx.CompactIri("@index", true)
										] = ((IDictionary<string, JToken>)expandedItem)["@index"];
								}
							}
							else
							{
								// TODO: SPEC: no mention of vocab =
								// true
								// 7.6.4.3)
								if (result.ContainsKey(itemActiveProperty))
								{
									throw new JsonLdError(JsonLdError.Error.CompactionToListOfLists, "There cannot be two list objects associated with an active property that has a container mapping"
										);
								}
							}
						}
						// 7.6.5)
						if ("@language".Equals(container) || "@index".Equals(container))
						{
							// 7.6.5.1)
							JObject mapObject;
							if (result.ContainsKey(itemActiveProperty))
							{
								mapObject = (JObject)result[itemActiveProperty];
							}
							else
							{
								mapObject = new JObject();
								result[itemActiveProperty] = mapObject;
							}
							// 7.6.5.2)
							if ("@language".Equals(container) && (compactedItem is JObject && ((IDictionary
								<string, JToken>)compactedItem).ContainsKey("@value")))
							{
								compactedItem = compactedItem["@value"];
							}
							// 7.6.5.3)
							string mapKey = (string)expandedItem[container];
							// 7.6.5.4)
							if (!mapObject.ContainsKey(mapKey))
							{
								mapObject[mapKey] = compactedItem;
							}
							else
							{
								JArray tmp;
								if (!(mapObject[mapKey] is JArray))
								{
									tmp = new JArray();
                                    tmp.Add(mapObject[mapKey]);
                                    mapObject[mapKey] = tmp;
								}
								else
								{
									tmp = (JArray)mapObject[mapKey];
								}
								tmp.Add(compactedItem);
							}
						}
						else
						{
							// 7.6.6)
							// 7.6.6.1)
							bool check = (!compactArrays || "@set".Equals(container) || "@list".Equals(container
								) || "@list".Equals(expandedProperty) || "@graph".Equals(expandedProperty)) && (
								!(compactedItem is JArray));
							if (check)
							{
								JArray tmp = new JArray();
								tmp.Add(compactedItem);
								compactedItem = tmp;
							}
							// 7.6.6.2)
							if (!result.ContainsKey(itemActiveProperty))
							{
								result[itemActiveProperty] = compactedItem;
							}
							else
							{
                                if (!(result[itemActiveProperty] is JArray))
								{
                                    JArray tmp = new JArray();
									tmp.Add(result[itemActiveProperty]);
                                    result[itemActiveProperty] = tmp;
								}
                                if (compactedItem is JArray)
								{
                                    JsonLD.Collections.AddAll(((JArray)result[itemActiveProperty]), (JArray)compactedItem);
								}
								else
								{
                                    ((JArray)result[itemActiveProperty]).Add(compactedItem);
								}
							}
						}
					}
				}
				// 8)
				return result;
			}
			// 2)
			return element;
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		public virtual JToken Compact(Context activeCtx, string activeProperty, JToken element
			)
		{
			return Compact(activeCtx, activeProperty, element, true);
		}

		/// <summary>
		/// Expansion Algorithm
		/// http://json-ld.org/spec/latest/json-ld-api/#expansion-algorithm
		/// </summary>
		/// <param name="activeCtx"></param>
		/// <param name="activeProperty"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		/// <exception cref="JsonLdError">JsonLdError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		public virtual JToken Expand(Context activeCtx, string activeProperty, JToken element
			)
		{
			// 1)
			if (element.IsNull())
			{
				return null;
			}
			// 3)
			if (element is JArray)
			{
				// 3.1)
                JArray result = new JArray();
				// 3.2)
                foreach (JToken item in (JArray)element)
				{
					// 3.2.1)
					JToken v = Expand(activeCtx, activeProperty, item);
					// 3.2.2)
					if (("@list".Equals(activeProperty) || "@list".Equals(activeCtx.GetContainer(activeProperty
						))) && (v is JArray || (v is JObject && ((IDictionary<string, JToken>)v).ContainsKey
						("@list"))))
					{
						throw new JsonLdError(JsonLdError.Error.ListOfLists, "lists of lists are not permitted."
							);
					}
					else
					{
						// 3.2.3)
						if (!v.IsNull())
						{
							if (v is JArray)
							{
								JsonLD.Collections.AddAll(result, (JArray)v);
							}
							else
							{
								result.Add(v);
							}
						}
					}
				}
				// 3.3)
				return result;
			}
			else
			{
				// 4)
				if (element is JObject)
				{
					// access helper
                    IDictionary<string, JToken> elem = (JObject)element;
					// 5)
					if (elem.ContainsKey("@context"))
					{
						activeCtx = activeCtx.Parse(elem["@context"]);
					}
					// 6)
                    JObject result = new JObject();
					// 7)
                    JArray keys = new JArray(element.GetKeys());
					keys.SortInPlace();
					foreach (string key in keys)
					{
						JToken value = elem[key];
						// 7.1)
						if (key.Equals("@context"))
						{
							continue;
						}
						// 7.2)
						string expandedProperty = activeCtx.ExpandIri(key, false, true, null, null);
                        JToken expandedValue = null;
						// 7.3)
						if (expandedProperty == null || (!expandedProperty.Contains(":") && !JsonLdUtils.IsKeyword
							(expandedProperty)))
						{
							continue;
						}
						// 7.4)
						if (JsonLdUtils.IsKeyword(expandedProperty))
						{
							// 7.4.1)
							if ("@reverse".Equals(activeProperty))
							{
								throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyMap, "a keyword cannot be used as a @reverse propery"
									);
							}
							// 7.4.2)
							if (result.ContainsKey(expandedProperty))
							{
								throw new JsonLdError(JsonLdError.Error.CollidingKeywords, expandedProperty + " already exists in result"
									);
							}
							// 7.4.3)
							if ("@id".Equals(expandedProperty))
							{
								if (!(value.Type == JTokenType.String))
								{
									throw new JsonLdError(JsonLdError.Error.InvalidIdValue, "value of @id must be a string"
										);
								}
								expandedValue = activeCtx.ExpandIri((string)value, true, false, null, null);
							}
							else
							{
								// 7.4.4)
								if ("@type".Equals(expandedProperty))
								{
                                    if (value is JArray)
									{
										expandedValue = new JArray();
										foreach (JToken v in (JArray)value)
										{
											if (v.Type != JTokenType.String)
											{
												throw new JsonLdError(JsonLdError.Error.InvalidTypeValue, "@type value must be a string or array of strings"
													);
											}
											((JArray)expandedValue).Add(activeCtx.ExpandIri((string)v, true, true, null
												, null));
										}
									}
									else
									{
										if (value.Type == JTokenType.String)
										{
											expandedValue = activeCtx.ExpandIri((string)value, true, true, null, null);
										}
										else
										{
											// TODO: SPEC: no mention of empty map check
											if (value is JObject)
											{
												if (((JObject)value).Count != 0)
												{
													throw new JsonLdError(JsonLdError.Error.InvalidTypeValue, "@type value must be a an empty object for framing"
														);
												}
												expandedValue = value;
											}
											else
											{
												throw new JsonLdError(JsonLdError.Error.InvalidTypeValue, "@type value must be a string or array of strings"
													);
											}
										}
									}
								}
								else
								{
									// 7.4.5)
									if ("@graph".Equals(expandedProperty))
									{
										expandedValue = Expand(activeCtx, "@graph", value);
									}
									else
									{
										// 7.4.6)
										if ("@value".Equals(expandedProperty))
										{
											if (!value.IsNull() && (value is JObject || value is JArray))
											{
												throw new JsonLdError(JsonLdError.Error.InvalidValueObjectValue, "value of " + expandedProperty
													 + " must be a scalar or null");
											}
											expandedValue = value;
											if (expandedValue.IsNull())
											{
												result["@value"] = null;
												continue;
											}
										}
										else
										{
											// 7.4.7)
											if ("@language".Equals(expandedProperty))
											{
												if (!(value.Type == JTokenType.String))
												{
													throw new JsonLdError(JsonLdError.Error.InvalidLanguageTaggedString, "Value of " 
														+ expandedProperty + " must be a string");
												}
												expandedValue = ((string)value).ToLower();
											}
											else
											{
												// 7.4.8)
												if ("@index".Equals(expandedProperty))
												{
													if (!(value.Type == JTokenType.String))
													{
														throw new JsonLdError(JsonLdError.Error.InvalidIndexValue, "Value of " + expandedProperty
															 + " must be a string");
													}
													expandedValue = value;
												}
												else
												{
													// 7.4.9)
													if ("@list".Equals(expandedProperty))
													{
														// 7.4.9.1)
														if (activeProperty == null || "@graph".Equals(activeProperty))
														{
															continue;
														}
														// 7.4.9.2)
														expandedValue = Expand(activeCtx, activeProperty, value);
														// NOTE: step not in the spec yet
                                                        if (!(expandedValue is JArray))
														{
                                                            JArray tmp = new JArray();
															tmp.Add(expandedValue);
															expandedValue = tmp;
														}
														// 7.4.9.3)
                                                        foreach (JToken o in (JArray)expandedValue)
														{
                                                            if (o is JObject && ((JObject)o).ContainsKey("@list"))
															{
																throw new JsonLdError(JsonLdError.Error.ListOfLists, "A list may not contain another list"
																	);
															}
														}
													}
													else
													{
														// 7.4.10)
														if ("@set".Equals(expandedProperty))
														{
															expandedValue = Expand(activeCtx, activeProperty, value);
														}
														else
														{
															// 7.4.11)
															if ("@reverse".Equals(expandedProperty))
															{
																if (!(value is JObject))
																{
																	throw new JsonLdError(JsonLdError.Error.InvalidReverseValue, "@reverse value must be an object"
																		);
																}
																// 7.4.11.1)
																expandedValue = Expand(activeCtx, "@reverse", value);
																// NOTE: algorithm assumes the result is a map
																// 7.4.11.2)
                                                                if (((IDictionary<string, JToken>)expandedValue).ContainsKey("@reverse"))
																{
                                                                    JObject reverse = (JObject)((JObject)expandedValue)["@reverse"];
																	foreach (string property in reverse.GetKeys())
																	{
                                                                        JToken item = reverse[property];
																		// 7.4.11.2.1)
																		if (!result.ContainsKey(property))
																		{
																			result[property] = new JArray();
																		}
																		// 7.4.11.2.2)
																		if (item is JArray)
																		{
                                                                            JsonLD.Collections.AddAll(((JArray)result[property]), (JArray)item);
																		}
																		else
																		{
                                                                            ((JArray)result[property]).Add(item);
																		}
																	}
																}
																// 7.4.11.3)
                                                                if (((JObject)expandedValue).Count > (((JObject)expandedValue).ContainsKey("@reverse") ? 1 : 0))
																{
																	// 7.4.11.3.1)
																	if (!result.ContainsKey("@reverse"))
																	{
                                                                        result["@reverse"] = new JObject();
																	}
																	// 7.4.11.3.2)
                                                                    JObject reverseMap = (JObject)result["@reverse"];
																	// 7.4.11.3.3)
                                                                    foreach (string property in expandedValue.GetKeys())
																	{
																		if ("@reverse".Equals(property))
																		{
																			continue;
																		}
																		// 7.4.11.3.3.1)
                                                                        JArray items = (JArray)((JObject)expandedValue)[property];
																		foreach (JToken item in items)
																		{
																			// 7.4.11.3.3.1.1)
                                                                            if (item is JObject && (((JObject)item).ContainsKey("@value") || ((JObject)item).ContainsKey("@list")))
																			{
																				throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyValue);
																			}
																			// 7.4.11.3.3.1.2)
																			if (!reverseMap.ContainsKey(property))
																			{
																				reverseMap[property] = new JArray();
																			}
																			// 7.4.11.3.3.1.3)
                                                                            ((JArray)reverseMap[property]).Add(item);
																		}
																	}
																}
																// 7.4.11.4)
																continue;
															}
															else
															{
																// TODO: SPEC no mention of @explicit etc in spec
																if ("@explicit".Equals(expandedProperty) || "@default".Equals(expandedProperty) ||
																	 "@embed".Equals(expandedProperty) || "@embedChildren".Equals(expandedProperty) 
																	|| "@omitDefault".Equals(expandedProperty))
																{
																	expandedValue = Expand(activeCtx, expandedProperty, value);
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
							// 7.4.12)
							if (!expandedValue.IsNull())
							{
								result[expandedProperty] = expandedValue;
							}
							// 7.4.13)
							continue;
						}
						else
						{
							// 7.5
							if ("@language".Equals(activeCtx.GetContainer(key)) && value is JObject)
							{
								// 7.5.1)
                                expandedValue = new JArray();
								// 7.5.2)
								foreach (string language in value.GetKeys())
								{
                                    JToken languageValue = ((IDictionary<string, JToken>)value)[language];
									// 7.5.2.1)
									if (!(languageValue is JArray))
									{
                                        JToken tmp = languageValue;
                                        languageValue = new JArray();
                                        ((JArray)languageValue).Add(tmp);
									}
									// 7.5.2.2)
                                    foreach (JToken item in (JArray)languageValue)
									{
										// 7.5.2.2.1)
										if (!(item.Type == JTokenType.String))
										{
											throw new JsonLdError(JsonLdError.Error.InvalidLanguageMapValue, "Expected " + item
												.ToString() + " to be a string");
										}
										// 7.5.2.2.2)
                                        JObject tmp = new JObject();
										tmp["@value"] = item;
										tmp["@language"] = language.ToLower();
										((JArray)expandedValue).Add(tmp);
									}
								}
							}
							else
							{
								// 7.6)
								if ("@index".Equals(activeCtx.GetContainer(key)) && value is JObject)
								{
									// 7.6.1)
									expandedValue = new JArray();
									// 7.6.2)
                                    JArray indexKeys = new JArray(value.GetKeys());
									indexKeys.SortInPlace();
									foreach (string index in indexKeys)
									{
										JToken indexValue = ((JObject)value)[index];
										// 7.6.2.1)
										if (!(indexValue is JArray))
										{
											JToken tmp = indexValue;
											indexValue = new JArray();
											((JArray)indexValue).Add(tmp);
										}
										// 7.6.2.2)
										indexValue = Expand(activeCtx, key, indexValue);
										// 7.6.2.3)
										foreach (JObject item in (JArray)indexValue)
										{
											// 7.6.2.3.1)
											if (!item.ContainsKey("@index"))
											{
												item["@index"] = index;
											}
											// 7.6.2.3.2)
											((JArray)expandedValue).Add(item);
										}
									}
								}
								else
								{
									// 7.7)
									expandedValue = Expand(activeCtx, key, value);
								}
							}
						}
						// 7.8)
						if (expandedValue.IsNull())
						{
							continue;
						}
						// 7.9)
						if ("@list".Equals(activeCtx.GetContainer(key)))
						{
							if (!(expandedValue is JObject) || !((JObject)expandedValue).ContainsKey("@list"))
							{
								JToken tmp = expandedValue;
								if (!(tmp is JArray))
								{
									tmp = new JArray();
									((JArray)tmp).Add(expandedValue);
								}
								expandedValue = new JObject();
								((JObject)expandedValue)["@list"] = tmp;
							}
						}
						// 7.10)
						if (activeCtx.IsReverseProperty(key))
						{
							// 7.10.1)
							if (!result.ContainsKey("@reverse"))
							{
								result["@reverse"] = new JObject();
							}
							// 7.10.2)
							JObject reverseMap = (JObject)result["@reverse"];
							// 7.10.3)
                            if (!(expandedValue is JArray))
							{
                                JToken tmp = expandedValue;
								expandedValue = new JArray();
								((JArray)expandedValue).Add(tmp);
							}
							// 7.10.4)
							foreach (JToken item in (JArray)expandedValue)
							{
								// 7.10.4.1)
								if (item is JObject && (((JObject)item).ContainsKey("@value") || ((JObject)item).ContainsKey("@list")))
								{
									throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyValue);
								}
								// 7.10.4.2)
								if (!reverseMap.ContainsKey(expandedProperty))
								{
									reverseMap[expandedProperty] = new JArray();
								}
								// 7.10.4.3)
                                if (item is JArray)
								{
                                    JsonLD.Collections.AddAll(((JArray)reverseMap[expandedProperty]), (JArray)item);
								}
								else
								{
									((JArray)reverseMap[expandedProperty]).Add(item);
								}
							}
						}
						else
						{
							// 7.11)
							// 7.11.1)
							if (!result.ContainsKey(expandedProperty))
							{
								result[expandedProperty] = new JArray();
							}
							// 7.11.2)
                            if (expandedValue is JArray)
							{
                                JsonLD.Collections.AddAll(((JArray)result[expandedProperty]), (JArray)expandedValue);
							}
							else
							{
								((JArray)result[expandedProperty]).Add(expandedValue);
							}
						}
					}
					// 8)
					if (result.ContainsKey("@value"))
					{
						// 8.1)
						// TODO: is this method faster than just using containsKey for
						// each?
						ICollection<string> keySet = new HashSet<string>(result.GetKeys());
						keySet.Remove("@value");
						keySet.Remove("@index");
						bool langremoved = keySet.Remove("@language");
						bool typeremoved = keySet.Remove("@type");
						if ((langremoved && typeremoved) || !keySet.IsEmpty())
						{
							throw new JsonLdError(JsonLdError.Error.InvalidValueObject, "value object has unknown keys"
								);
						}
						// 8.2)
                        JToken rval = result["@value"];
						if (rval.IsNull())
						{
							// nothing else is possible with result if we set it to
							// null, so simply return it
							return null;
						}
						// 8.3)
						if (!(rval.Type == JTokenType.String) && result.ContainsKey("@language"))
						{
							throw new JsonLdError(JsonLdError.Error.InvalidLanguageTaggedValue, "when @language is used, @value must be a string"
								);
						}
						else
						{
							// 8.4)
							if (result.ContainsKey("@type"))
							{
								// TODO: is this enough for "is an IRI"
								if (!(result["@type"].Type == JTokenType.String) || ((string)result["@type"]).StartsWith("_:") ||
									 !((string)result["@type"]).Contains(":"))
								{
									throw new JsonLdError(JsonLdError.Error.InvalidTypedValue, "value of @type must be an IRI"
										);
								}
							}
						}
					}
					else
					{
						// 9)
						if (result.ContainsKey("@type"))
						{
                            JToken rtype = result["@type"];
							if (!(rtype is JArray))
							{
								JArray tmp = new JArray();
								tmp.Add(rtype);
								result["@type"] = tmp;
							}
						}
						else
						{
							// 10)
							if (result.ContainsKey("@set") || result.ContainsKey("@list"))
							{
								// 10.1)
								if (result.Count > (result.ContainsKey("@index") ? 2 : 1))
								{
									throw new JsonLdError(JsonLdError.Error.InvalidSetOrListObject, "@set or @list may only contain @index"
										);
								}
								// 10.2)
								if (result.ContainsKey("@set"))
								{
									// result becomes an array here, thus the remaining checks
									// will never be true from here on
									// so simply return the value rather than have to make
									// result an object and cast it with every
									// other use in the function.
									return result["@set"];
								}
							}
						}
					}
					// 11)
					if (result.ContainsKey("@language") && result.Count == 1)
					{
						result = null;
					}
					// 12)
					if (activeProperty == null || "@graph".Equals(activeProperty))
					{
						// 12.1)
						if (result != null && (result.Count == 0 || result.ContainsKey("@value") || result
							.ContainsKey("@list")))
						{
							result = null;
						}
						else
						{
							// 12.2)
							if (result != null && result.ContainsKey("@id") && result.Count == 1)
							{
								result = null;
							}
						}
					}
					// 13)
					return result;
				}
				else
				{
					// 2) If element is a scalar
					// 2.1)
					if (activeProperty == null || "@graph".Equals(activeProperty))
					{
						return null;
					}
					return activeCtx.ExpandValue(activeProperty, element);
				}
			}
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		public virtual JToken Expand(Context activeCtx, JToken element)
		{
			return Expand(activeCtx, null, element);
		}

		/// <summary>
		/// _____ _ _ _ _ _ _ _ _ | ___| | __ _| |_| |_ ___ _ __ / \ | | __ _ ___ _
		/// __(_) |_| |__ _ __ ___ | |_ | |/ _` | __| __/ _ \ '_ \ / _ \ | |/ _` |/ _
		/// \| '__| | __| '_ \| '_ ` _ \ | _| | | (_| | |_| || __/ | | | / ___ \| |
		/// (_| | (_) | | | | |_| | | | | | | | | |_| |_|\__,_|\__|\__\___|_| |_| /_/
		/// \_\_|\__, |\___/|_| |_|\__|_| |_|_| |_| |_| |___/
		/// </summary>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		internal virtual void GenerateNodeMap(JToken element, JObject
			 nodeMap)
		{
			GenerateNodeMap(element, nodeMap, "@default", null, null, null);
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal virtual void GenerateNodeMap(JToken element, JObject
			 nodeMap, string activeGraph)
		{
			GenerateNodeMap(element, nodeMap, activeGraph, null, null, null);
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal virtual void GenerateNodeMap(JToken element, JObject
             nodeMap, string activeGraph, JToken activeSubject, string activeProperty, JObject list)
		{
			// 1)
			if (element is JArray)
			{
				// 1.1)
				foreach (JToken item in (JArray)element)
				{
					GenerateNodeMap(item, nodeMap, activeGraph, activeSubject, activeProperty, list);
				}
				return;
			}
			// for convenience
            IDictionary<string, JToken> elem = (IDictionary<string, JToken>)element;
			// 2)
			if (!((IDictionary<string,JToken>)nodeMap).ContainsKey(activeGraph))
			{
				nodeMap[activeGraph] = new JObject();
			}
            JObject graph = (JObject)nodeMap[activeGraph
				];
            JObject node = (JObject)((activeSubject.IsNull() || activeSubject.Type != JTokenType.String) 
                ? null : graph[(string)activeSubject]);
			// 3)
			if (elem.ContainsKey("@type"))
			{
				// 3.1)
				JArray oldTypes;
                JArray newTypes = new JArray();
                if (elem["@type"] is JArray)
				{
                    oldTypes = (JArray)elem["@type"];
				}
				else
				{
                    oldTypes = new JArray();
					oldTypes.Add((string)elem["@type"]);
				}
				foreach (string item in oldTypes)
				{
					if (item.StartsWith("_:"))
					{
						newTypes.Add(GenerateBlankNodeIdentifier(item));
					}
					else
					{
						newTypes.Add(item);
					}
				}
				if (elem["@type"] is JArray)
				{
					elem["@type"] = newTypes;
				}
				else
				{
					elem["@type"] = newTypes[0];
				}
			}
			// 4)
			if (elem.ContainsKey("@value"))
			{
				// 4.1)
				if (list == null)
				{
					JsonLdUtils.MergeValue(node, activeProperty, (JObject)elem);
				}
				else
				{
					// 4.2)
					JsonLdUtils.MergeValue(list, "@list", (JObject)elem);
				}
			}
			else
			{
				// 5)
				if (elem.ContainsKey("@list"))
				{
					// 5.1)
					JObject result = new JObject();
					result["@list"] = new JArray();
					// 5.2)
					//for (final Object item : (List<Object>) elem.get("@list")) {
					//    generateNodeMap(item, nodeMap, activeGraph, activeSubject, activeProperty, result);
					//}
					GenerateNodeMap(elem["@list"], nodeMap, activeGraph, activeSubject, activeProperty
						, result);
					// 5.3)
					JsonLdUtils.MergeValue(node, activeProperty, result);
				}
				else
				{
					// 6)
					// 6.1)
					string id = (string)JsonLD.Collections.Remove(elem, "@id");
					if (id != null)
					{
						if (id.StartsWith("_:"))
						{
							id = GenerateBlankNodeIdentifier(id);
						}
					}
					else
					{
						// 6.2)
						id = GenerateBlankNodeIdentifier(null);
					}
					// 6.3)
					if (!graph.ContainsKey(id))
					{
						JObject tmp = new JObject();
						tmp["@id"] = id;
						graph[id] = tmp;
					}
					// 6.4) TODO: SPEC this line is asked for by the spec, but it breaks various tests
					//node = (Map<String, Object>) graph.get(id);
					// 6.5)
					if (activeSubject is JObject)
					{
						// 6.5.1)
						JsonLdUtils.MergeValue((JObject)graph[id], activeProperty, activeSubject
							);
					}
					else
					{
						// 6.6)
						if (activeProperty != null)
						{
                            JObject reference = new JObject();
							reference["@id"] = id;
							// 6.6.2)
							if (list == null)
							{
								// 6.6.2.1+2)
								JsonLdUtils.MergeValue(node, activeProperty, reference);
							}
							else
							{
								// 6.6.3) TODO: SPEC says to add ELEMENT to @list member, should
								// be REFERENCE
								JsonLdUtils.MergeValue(list, "@list", reference);
							}
						}
					}
					// TODO: SPEC this is removed in the spec now, but it's still needed (see 6.4)
                    node = (JObject)graph[id];
					// 6.7)
					if (elem.ContainsKey("@type"))
					{
						foreach (JToken type in (JArray)JsonLD.Collections.Remove(elem, "@type"
							))
						{
							JsonLdUtils.MergeValue(node, "@type", type);
						}
					}
					// 6.8)
					if (elem.ContainsKey("@index"))
					{
						JToken elemIndex = JsonLD.Collections.Remove(elem, "@index");
						if (node.ContainsKey("@index"))
						{
							if (!JsonLdUtils.DeepCompare(node["@index"], elemIndex))
							{
								throw new JsonLdError(JsonLdError.Error.ConflictingIndexes);
							}
						}
						else
						{
							node["@index"] = elemIndex;
						}
					}
					// 6.9)
					if (elem.ContainsKey("@reverse"))
					{
						// 6.9.1)
                        JObject referencedNode = new JObject();
						referencedNode["@id"] = id;
						// 6.9.2+6.9.4)
                        JObject reverseMap = (JObject)JsonLD.Collections.Remove
							(elem, "@reverse");
						// 6.9.3)
						foreach (string property in reverseMap.GetKeys())
						{
							JArray values = (JArray)reverseMap[property];
							// 6.9.3.1)
							foreach (JToken value in values)
							{
								// 6.9.3.1.1)
								GenerateNodeMap(value, nodeMap, activeGraph, referencedNode, property, null);
							}
						}
					}
					// 6.10)
					if (elem.ContainsKey("@graph"))
					{
						GenerateNodeMap(JsonLD.Collections.Remove(elem, "@graph"), nodeMap, id, null, 
							null, null);
					}
					// 6.11)
					JArray keys = new JArray(element.GetKeys());
					keys.SortInPlace();
					foreach (string property_1 in keys)
					{
                        var eachProperty_1 = property_1;
						JToken value = elem[eachProperty_1];
						// 6.11.1)
						if (eachProperty_1.StartsWith("_:"))
						{
							eachProperty_1 = GenerateBlankNodeIdentifier(eachProperty_1);
						}
						// 6.11.2)
						if (!node.ContainsKey(eachProperty_1))
						{
							node[eachProperty_1] = new JArray();
						}
						// 6.11.3)
						GenerateNodeMap(value, nodeMap, activeGraph, id, eachProperty_1, null);
					}
				}
			}
		}

		private readonly JObject blankNodeIdentifierMap = new JObject();

		private int blankNodeCounter = 0;

		internal virtual string GenerateBlankNodeIdentifier(string id)
		{
			if (id != null && blankNodeIdentifierMap.ContainsKey(id))
			{
				return (string)blankNodeIdentifierMap[id];
			}
			string bnid = "_:b" + blankNodeCounter++;
			if (id != null)
			{
				blankNodeIdentifierMap[id] = bnid;
			}
			return bnid;
		}

		internal virtual string GenerateBlankNodeIdentifier()
		{
			return GenerateBlankNodeIdentifier(null);
		}

		/// <summary>
		/// _____ _ _ _ _ _ _ | ___| __ __ _ _ __ ___ (_)_ __ __ _ / \ | | __ _ ___ _
		/// __(_) |_| |__ _ __ ___ | |_ | '__/ _` | '_ ` _ \| | '_ \ / _` | / _ \ |
		/// |/ _` |/ _ \| '__| | __| '_ \| '_ ` _ \ | _|| | | (_| | | | | | | | | | |
		/// (_| | / ___ \| | (_| | (_) | | | | |_| | | | | | | | | |_| |_| \__,_|_|
		/// |_| |_|_|_| |_|\__, | /_/ \_\_|\__, |\___/|_| |_|\__|_| |_|_| |_| |_|
		/// |___/ |___/
		/// </summary>
		private class FramingContext
		{
			public bool embed;

			public bool @explicit;

			public bool omitDefault;

			public FramingContext(JsonLdApi _enclosing)
			{
				this._enclosing = _enclosing;
				this.embed = true;
				this.@explicit = false;
				this.omitDefault = false;
				this.embeds = null;
			}

			public IDictionary<string, JsonLdApi.EmbedNode> embeds = null;

			private readonly JsonLdApi _enclosing;
		}

		private class EmbedNode
		{
			public JToken parent = null;

			public string property = null;

			internal EmbedNode(JsonLdApi _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly JsonLdApi _enclosing;
		}

		private JObject nodeMap;

		/// <summary>Performs JSON-LD framing.</summary>
		/// <remarks>Performs JSON-LD framing.</remarks>
		/// <param name="input">the expanded JSON-LD to frame.</param>
		/// <param name="frame">the expanded JSON-LD frame to use.</param>
		/// <param name="options">the framing options.</param>
		/// <returns>the framed output.</returns>
		/// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		public virtual JArray Frame(JToken input, JArray frame)
		{
			// create framing state
			JsonLdApi.FramingContext state = new JsonLdApi.FramingContext(this);
			if (this.opts.GetEmbed() != null)
			{
				state.embed = this.opts.GetEmbed().Value;
			}
			if (this.opts.GetExplicit() != null)
			{
				state.@explicit = this.opts.GetExplicit().Value;
			}
			if (this.opts.GetOmitDefault() != null)
			{
				state.omitDefault = this.opts.GetOmitDefault().Value;
			}
			// use tree map so keys are sotred by default
            // XXX BUG BUG BUG XXX (sblom) Figure out where this needs to be sorted and use extension methods to return sorted enumerators or something!
			JObject nodes = new JObject();
			GenerateNodeMap(input, nodes);
			this.nodeMap = (JObject)nodes["@default"];
			JArray framed = new JArray();
			// NOTE: frame validation is done by the function not allowing anything
			// other than list to me passed
			Frame(state, this.nodeMap, (frame != null && frame.Count > 0 ? (JObject)frame[0] : new JObject()), framed, null);
			return framed;
		}

		/// <summary>Frames subjects according to the given frame.</summary>
		/// <remarks>Frames subjects according to the given frame.</remarks>
		/// <param name="state">the current framing state.</param>
		/// <param name="subjects">the subjects to filter.</param>
		/// <param name="frame">the frame.</param>
		/// <param name="parent">the parent subject or top-level array.</param>
		/// <param name="property">the parent property, initialized to null.</param>
		/// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		private void Frame(JsonLdApi.FramingContext state, JObject nodes
			, JObject frame, JToken parent, string property)
		{
			// filter out subjects that match the frame
			JObject matches = FilterNodes(state, nodes, frame);
			// get flags for current frame
			bool embedOn = GetFrameFlag(frame, "@embed", state.embed);
			bool explicitOn = GetFrameFlag(frame, "@explicit", state.@explicit);
			// add matches to output
			JArray ids = new JArray(matches.GetKeys());
			ids.SortInPlace();
			foreach (string id in ids)
			{
				if (property == null)
				{
					state.embeds = new Dictionary<string, JsonLdApi.EmbedNode>();
				}
				// start output
				JObject output = new JObject();
				output["@id"] = id;
				// prepare embed meta info
				JsonLdApi.EmbedNode embeddedNode = new JsonLdApi.EmbedNode(this);
				embeddedNode.parent = parent;
				embeddedNode.property = property;
				// if embed is on and there is an existing embed
				if (embedOn && state.embeds.ContainsKey(id))
				{
					JsonLdApi.EmbedNode existing = state.embeds[id];
					embedOn = false;
					if (existing.parent is JArray)
					{
						foreach (JToken p in (JArray)(existing.parent))
						{
							if (JsonLdUtils.CompareValues(output, p))
							{
								embedOn = true;
								break;
							}
						}
					}
					else
					{
						// existing embed's parent is an object
						if (((JObject)existing.parent).ContainsKey(existing.property))
						{
							foreach (JToken v in (JArray)((JObject)existing.parent)[existing.property])
							{
								if (v is JObject && ((JObject)v)["@id"].SafeCompare(id))
								{
									embedOn = true;
									break;
								}
							}
						}
					}
					// existing embed has already been added, so allow an overwrite
					if (embedOn)
					{
						RemoveEmbed(state, id);
					}
				}
				// not embedding, add output without any other properties
				if (!embedOn)
				{
					AddFrameOutput(state, parent, property, output);
				}
				else
				{
					// add embed meta info
					state.embeds[id] = embeddedNode;
					// iterate over subject properties
					JObject element = (JObject)matches[id];
					JArray props = new JArray(element.GetKeys());
					props.SortInPlace();
					foreach (string prop in props)
					{
						// copy keywords to output
						if (JsonLdUtils.IsKeyword(prop))
						{
							output[prop] = JsonLdUtils.Clone(element[prop]);
							continue;
						}
						// if property isn't in the frame
						if (!frame.ContainsKey(prop))
						{
							// if explicit is off, embed values
							if (!explicitOn)
							{
								EmbedValues(state, element, prop, output);
							}
							continue;
						}
						// add objects
						JArray value = (JArray)element[prop];
						foreach (JToken item in value)
						{
							// recurse into list
                            if ((item is JObject) && ((JObject)item).ContainsKey("@list"))
							{
								// add empty list
								JObject list = new JObject();
								list["@list"] = new JArray();
								AddFrameOutput(state, output, prop, list);
								// add list objects
                                foreach (JToken listitem in (JArray)((JObject)item)["@list"
									])
								{
									// recurse into subject reference
									if (JsonLdUtils.IsNodeReference(listitem))
									{
										JObject tmp = new JObject();
                                        string itemid = (string)((IDictionary<string, JToken>)listitem)["@id"];
										// TODO: nodes may need to be node_map,
										// which is global
										tmp[itemid] = this.nodeMap[itemid];
                                        Frame(state, tmp, (JObject)((JArray)frame[prop])[0], list
											, "@list");
									}
									else
									{
										// include other values automatcially (TODO:
										// may need JsonLdUtils.clone(n))
										AddFrameOutput(state, list, "@list", listitem);
									}
								}
							}
							else
							{
								// recurse into subject reference
								if (JsonLdUtils.IsNodeReference(item))
								{
                                    JObject tmp = new JObject();
                                    string itemid = (string)((JObject)item)["@id"];
									// TODO: nodes may need to be node_map, which is
									// global
									tmp[itemid] = this.nodeMap[itemid];
									Frame(state, tmp, (JObject)((JArray)frame[prop])[0], output
										, prop);
								}
								else
								{
									// include other values automatically (TODO: may
									// need JsonLdUtils.clone(o))
									AddFrameOutput(state, output, prop, item);
								}
							}
						}
					}
					// handle defaults
					props = new JArray(frame.GetKeys());
					props.SortInPlace();
					foreach (string prop_1 in props)
					{
						// skip keywords
						if (JsonLdUtils.IsKeyword(prop_1))
						{
							continue;
						}
						JArray pf = (JArray)frame[prop_1];
						JObject propertyFrame = pf.Count > 0 ? (JObject)pf[0] : null;
						if (propertyFrame == null)
						{
							propertyFrame = new JObject();
						}
						bool omitDefaultOn = GetFrameFlag(propertyFrame, "@omitDefault", state.omitDefault
							);
						if (!omitDefaultOn && !output.ContainsKey(prop_1))
						{
							JToken def = "@null";
							if (propertyFrame.ContainsKey("@default"))
							{
								def = JsonLdUtils.Clone(propertyFrame["@default"]);
							}
							if (!(def is JArray))
							{
								JArray tmp = new JArray();
								tmp.Add(def);
								def = tmp;
							}
							JObject tmp1 = new JObject();
							tmp1["@preserve"] = def;
							JArray tmp2 = new JArray();
							tmp2.Add(tmp1);
							output[prop_1] = tmp2;
						}
					}
					// add output to parent
					AddFrameOutput(state, parent, property, output);
				}
			}
		}

		private bool GetFrameFlag(JObject frame, string name, bool thedefault
			)
		{
			JToken value = frame[name];
			if (value is JArray)
			{
				if (((JArray)value).Count > 0)
				{
					value = ((JArray)value)[0];
				}
			}
			if (value is JObject && ((JObject)value).ContainsKey("@value"
				))
			{
				value = ((JObject)value)["@value"];
			}
			if (value != null && value.Type == JTokenType.Boolean)
			{
				return (bool)value;
			}
			return thedefault;
		}

		/// <summary>Removes an existing embed.</summary>
		/// <remarks>Removes an existing embed.</remarks>
		/// <param name="state">the current framing state.</param>
		/// <param name="id">the @id of the embed to remove.</param>
		private static void RemoveEmbed(JsonLdApi.FramingContext state, string id)
		{
			// get existing embed
			IDictionary<string, JsonLdApi.EmbedNode> embeds = state.embeds;
			JsonLdApi.EmbedNode embed = embeds[id];
            JToken parent = embed.parent;
			string property = embed.property;
			// create reference to replace embed
			JObject node = new JObject();
			node["@id"] = id;
			// remove existing embed
			if (JsonLdUtils.IsNode(parent))
			{
				// replace subject with reference
				JArray newvals = new JArray();
				JArray oldvals = (JArray)((JObject)parent)[property
					];
				foreach (JToken v in oldvals)
				{
					if (v is JObject && ((JObject)v)["@id"].SafeCompare(id))
					{
						newvals.Add(node);
					}
					else
					{
						newvals.Add(v);
					}
				}
				((JObject)parent)[property] = newvals;
			}
			// recursively remove dependent dangling embeds
			RemoveDependents(embeds, id);
		}

		private static void RemoveDependents(IDictionary<string, JsonLdApi.EmbedNode> embeds
			, string id)
		{
			// get embed keys as a separate array to enable deleting keys in map
			foreach (string id_dep in embeds.Keys)
			{
				JsonLdApi.EmbedNode e = embeds[id_dep];
                JToken p = !e.parent.IsNull() ? e.parent : new JObject();
				if (!(p is JObject))
				{
					continue;
				}
				string pid = (string)((JObject)p)["@id"];
				if (Obj.Equals(id, pid))
				{
					JsonLD.Collections.Remove(embeds, id_dep);
					RemoveDependents(embeds, id_dep);
				}
			}
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		private JObject FilterNodes(JsonLdApi.FramingContext state, JObject nodes, JObject frame)
		{
			JObject rval = new JObject();
			foreach (string id in nodes.GetKeys())
			{
				JObject element = (JObject)nodes[id];
				if (element != null && FilterNode(state, element, frame))
				{
					rval[id] = element;
				}
			}
			return rval;
		}

		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		private bool FilterNode(JsonLdApi.FramingContext state, JObject node, JObject frame)
		{
			JToken types = frame["@type"];
			if (!types.IsNull())
			{
				if (!(types is JArray))
				{
					throw new JsonLdError(JsonLdError.Error.SyntaxError, "frame @type must be an array"
						);
				}
                JToken nodeTypes = node["@type"];
				if (nodeTypes.IsNull())
				{
					nodeTypes = new JArray();
				}
				else
				{
					if (!(nodeTypes is JArray))
					{
						throw new JsonLdError(JsonLdError.Error.SyntaxError, "node @type must be an array"
							);
					}
				}
				if (((JArray)types).Count == 1 && ((JArray)types)[0] is JObject
					 && ((JObject)((JArray)types)[0]).Count == 0)
				{
					return !((JArray)nodeTypes).IsEmpty();
				}
				else
				{
					foreach (JToken i in (JArray)nodeTypes)
					{
                        foreach (JToken j in (JArray)types)
						{
							if (JsonLdUtils.DeepCompare(i, j))
							{
								return true;
							}
						}
					}
					return false;
				}
			}
			else
			{
				foreach (string key in frame.GetKeys())
				{
					if ("@id".Equals(key) || !JsonLdUtils.IsKeyword(key) && !(node.ContainsKey(key)))
					{
						return false;
					}
				}
				return true;
			}
		}

		/// <summary>Adds framing output to the given parent.</summary>
		/// <remarks>Adds framing output to the given parent.</remarks>
		/// <param name="state">the current framing state.</param>
		/// <param name="parent">the parent to add to.</param>
		/// <param name="property">the parent property.</param>
		/// <param name="output">the output to add.</param>
		private static void AddFrameOutput(JsonLdApi.FramingContext state, JToken parent, 
			string property, JToken output)
		{
            if (parent is JObject)
			{
				JArray prop = (JArray)((JObject)parent)[property];
				if (prop == null)
				{
					prop = new JArray();
					((JObject)parent)[property] = prop;
				}
				prop.Add(output);
			}
			else
			{
				((JArray)parent).Add(output);
			}
		}

		/// <summary>
		/// Embeds values for the given subject and property into the given output
		/// during the framing algorithm.
		/// </summary>
		/// <remarks>
		/// Embeds values for the given subject and property into the given output
		/// during the framing algorithm.
		/// </remarks>
		/// <param name="state">the current framing state.</param>
		/// <param name="element">the subject.</param>
		/// <param name="property">the property.</param>
		/// <param name="output">the output.</param>
		private void EmbedValues(JsonLdApi.FramingContext state, JObject element, string property, JToken output)
		{
			// embed subject properties in output
			JArray objects = (JArray)element[property];
			foreach (JToken o in objects)
			{
                var eachObj = o;

                if (eachObj is JObject && ((JObject)eachObj).ContainsKey("@list"))
                {
                    JObject list = new JObject { { "@list", new JArray() } };
                    if (output is JArray)
                    {
                        ((JArray)output).Add(list);
                    }
                    else
                    {
                        output[property] = new JArray(list);
                    }
                    EmbedValues(state, (JObject)eachObj, "@list", list["@list"]);
                }
                // handle subject reference
                else if (JsonLdUtils.IsNodeReference(eachObj))
				{
					string sid = (string)((JObject)eachObj)["@id"];
					// embed full subject if isn't already embedded
					if (!state.embeds.ContainsKey(sid))
					{
						// add embed
						JsonLdApi.EmbedNode embed = new JsonLdApi.EmbedNode(this);
						embed.parent = output;
						embed.property = property;
						state.embeds[sid] = embed;
						// recurse into subject
						eachObj = new JObject();
						JObject s = (JObject)this.nodeMap[sid];
						if (s == null)
						{
							s = new JObject();
							s["@id"] = sid;
						}
						foreach (string prop in s.GetKeys())
						{
							// copy keywords
							if (JsonLdUtils.IsKeyword(prop))
							{
								((JObject)eachObj)[prop] = JsonLdUtils.Clone(s[prop]);
								continue;
							}
							EmbedValues(state, s, prop, eachObj);
						}
					}
					AddFrameOutput(state, output, property, eachObj);
				}
				else
				{
					// copy non-subject value
					AddFrameOutput(state, output, property, JsonLdUtils.Clone(eachObj));
				}
			}
		}

		/// <summary>Helper class for node usages</summary>
		/// <author>tristan</author>
		private class UsagesNode
		{
			public UsagesNode(JsonLdApi _enclosing, JsonLdApi.NodeMapNode node, string property
				, JObject value)
			{
				this._enclosing = _enclosing;
				this.node = node;
				this.property = property;
				this.value = value;
			}

			public JsonLdApi.NodeMapNode node = null;

			public string property = null;

			public JObject value = null;

			private readonly JsonLdApi _enclosing;
		}

		//[System.Serializable]
		private class NodeMapNode : JObject
		{
            public IList<UsagesNode> usages = new List<UsagesNode>();

			public NodeMapNode(JsonLdApi _enclosing, string id) : base()
			{
				this._enclosing = _enclosing;
				this["@id"] = id;
			}

			// helper fucntion for 4.3.3
			public virtual bool IsWellFormedListNode()
			{
				if (this.usages.Count != 1)
				{
					return false;
				}
				int keys = 0;
				if (this.ContainsKey(JSONLDConsts.RdfFirst))
				{
					keys++;
					if (!(this[JSONLDConsts.RdfFirst] is JArray && ((JArray)this[JSONLDConsts.RdfFirst
						]).Count == 1))
					{
						return false;
					}
				}
				if (this.ContainsKey(JSONLDConsts.RdfRest))
				{
					keys++;
					if (!(this[JSONLDConsts.RdfRest] is JArray && ((JArray)this[JSONLDConsts.RdfRest
						]).Count == 1))
					{
						return false;
					}
				}
				if (this.ContainsKey("@type"))
				{
					keys++;
					if (!(this["@type"] is JArray && ((JArray)this["@type"]).Count == 1) && JSONLDConsts
						.RdfList.Equals(((JArray)this["@type"])[0]))
					{
						return false;
					}
				}
				// TODO: SPEC: 4.3.3 has no mention of @id
				if (this.ContainsKey("@id"))
				{
					keys++;
				}
				if (keys < Count)
				{
					return false;
				}
				return true;
			}

			// return this node without the usages variable
			public virtual JObject Serialize()
			{
				return new JObject(this);
			}

			private readonly JsonLdApi _enclosing;
		}

		/// <summary>Converts RDF statements into JSON-LD.</summary>
		/// <remarks>Converts RDF statements into JSON-LD.</remarks>
		/// <param name="statements">the RDF statements.</param>
		/// <param name="options">the RDF conversion options.</param>
		/// <param name="callback">(err, output) called once the operation completes.</param>
		/// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		public virtual JArray FromRDF(RDFDataset dataset)
		{
			// 1)
			JObject defaultGraph = new JObject();
			// 2)
			JObject graphMap = new JObject();
			graphMap["@default"] = defaultGraph;
			// 3/3.1)
			foreach (string name in dataset.GraphNames())
			{
				IList<RDFDataset.Quad> graph = dataset.GetQuads(name);
				// 3.2+3.4)
				JObject nodeMap;
				if (!graphMap.ContainsKey(name))
				{
					nodeMap = new JObject();
					graphMap[name] = nodeMap;
				}
				else
				{
					nodeMap = (JObject)graphMap[name];
				}
				// 3.3)
				if (!"@default".Equals(name) && !Obj.Contains(defaultGraph, name))
				{
					defaultGraph[name] = new JsonLdApi.NodeMapNode(this, name);
				}
				// 3.5)
				foreach (RDFDataset.Quad triple in graph)
				{
					string subject = triple.GetSubject().GetValue();
					string predicate = triple.GetPredicate().GetValue();
					RDFDataset.Node @object = triple.GetObject();
					// 3.5.1+3.5.2)
					JsonLdApi.NodeMapNode node;
					if (!nodeMap.ContainsKey(subject))
					{
						node = new JsonLdApi.NodeMapNode(this, subject);
						nodeMap[subject] = node;
					}
					else
					{
						node = (NodeMapNode)nodeMap[subject];
					}
					// 3.5.3)
					if ((@object.IsIRI() || @object.IsBlankNode()) && !nodeMap.ContainsKey(@object.GetValue
						()))
					{
						nodeMap[@object.GetValue()] = new JsonLdApi.NodeMapNode(this, @object.GetValue());
					}
					// 3.5.4)
					if (JSONLDConsts.RdfType.Equals(predicate) && (@object.IsIRI() || @object.IsBlankNode
						()) && !opts.GetUseRdfType())
					{
						JsonLdUtils.MergeValue(node, "@type", @object.GetValue());
						continue;
					}
					// 3.5.5)
					JObject value = @object.ToObject(opts.GetUseNativeTypes());
					// 3.5.6+7)
					JsonLdUtils.MergeValue(node, predicate, value);
					// 3.5.8)
					if (@object.IsBlankNode() || @object.IsIRI())
					{
						// 3.5.8.1-3)
						((NodeMapNode)nodeMap[@object.GetValue()]).usages.Add(new JsonLdApi.UsagesNode(this, node, predicate
							, value));
					}
				}
			}
			// 4)
			foreach (string name_1 in graphMap.GetKeys())
			{
				JObject graph = (JObject)graphMap[name_1];
				// 4.1)
				if (!graph.ContainsKey(JSONLDConsts.RdfNil))
				{
					continue;
				}
				// 4.2)
				JsonLdApi.NodeMapNode nil = (NodeMapNode)graph[JSONLDConsts.RdfNil];
				// 4.3)
				foreach (JsonLdApi.UsagesNode usage in nil.usages)
				{
					// 4.3.1)
					JsonLdApi.NodeMapNode node = usage.node;
					string property = usage.property;
					JObject head = usage.value;
					// 4.3.2)
					JArray list = new JArray();
					JArray listNodes = new JArray();
					// 4.3.3)
					while (JSONLDConsts.RdfRest.Equals(property) && node.IsWellFormedListNode())
					{
						// 4.3.3.1)
						list.Add(((JArray)node[JSONLDConsts.RdfFirst])[0]);
						// 4.3.3.2)
						listNodes.Add((string)node["@id"]);
						// 4.3.3.3)
						JsonLdApi.UsagesNode nodeUsage = node.usages[0];
						// 4.3.3.4)
						node = nodeUsage.node;
						property = nodeUsage.property;
						head = nodeUsage.value;
						// 4.3.3.5)
						if (!JsonLdUtils.IsBlankNode(node))
						{
							break;
						}
					}
					// 4.3.4)
					if (JSONLDConsts.RdfFirst.Equals(property))
					{
						// 4.3.4.1)
						if (JSONLDConsts.RdfNil.Equals(node["@id"]))
						{
							continue;
						}
						// 4.3.4.3)
						string headId = (string)head["@id"];
						// 4.3.4.4-5)
						head = (JObject)((JArray)graph[headId][JSONLDConsts.RdfRest
							])[0];
						// 4.3.4.6)
						list.RemoveAt(list.Count - 1);
						listNodes.RemoveAt(listNodes.Count - 1);
					}
					// 4.3.5)
					JsonLD.Collections.Remove(head, "@id");
					// 4.3.6)
					JsonLD.Collections.Reverse(list);
					// 4.3.7)
					head["@list"] = list;
					// 4.3.8)
					foreach (string nodeId in listNodes)
					{
						JsonLD.Collections.Remove(graph, nodeId);
					}
				}
			}
			// 5)
			JArray result = new JArray();
			// 6)
            JArray ids = new JArray(defaultGraph.GetKeys());
			ids.SortInPlace();
			foreach (string subject_1 in ids)
			{
				JsonLdApi.NodeMapNode node = (NodeMapNode)defaultGraph[subject_1];
				// 6.1)
				if (graphMap.ContainsKey(subject_1))
				{
					// 6.1.1)
                    node["@graph"] = new JArray();
					// 6.1.2)
                    JArray keys = new JArray(graphMap[subject_1].GetKeys());
					keys.SortInPlace();
					foreach (string s in keys)
					{
						JsonLdApi.NodeMapNode n = (NodeMapNode)graphMap[subject_1][s];
						if (n.Count == 1 && n.ContainsKey("@id"))
						{
							continue;
						}
						((JArray)node["@graph"]).Add(n.Serialize());
					}
				}
				// 6.2)
				if (node.Count == 1 && node.ContainsKey("@id"))
				{
					continue;
				}
				result.Add(node.Serialize());
			}
			return result;
		}

		/// <summary>Adds RDF triples for each graph in the given node map to an RDF dataset.
		/// 	</summary>
		/// <remarks>Adds RDF triples for each graph in the given node map to an RDF dataset.
		/// 	</remarks>
		/// <returns>the RDF dataset.</returns>
		/// <exception cref="JsonLdError">JsonLdError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		public virtual RDFDataset ToRDF()
		{
			// TODO: make the default generateNodeMap call (i.e. without a
			// graphName) create and return the nodeMap
			JObject nodeMap = new JObject();
			nodeMap["@default"] = new JObject();
			GenerateNodeMap(this.value, nodeMap);
			RDFDataset dataset = new RDFDataset(this);
			foreach (string graphName in nodeMap.GetKeys())
			{
				// 4.1)
				if (JsonLdUtils.IsRelativeIri(graphName))
				{
					continue;
				}
				JObject graph = (JObject)nodeMap[graphName
					];
				dataset.GraphToRDF(graphName, graph);
			}
			return dataset;
		}

#if !PORTABLE
		/// <summary>Performs RDF normalization on the given JSON-LD input.</summary>
		/// <remarks>Performs RDF normalization on the given JSON-LD input.</remarks>
		/// <param name="input">the expanded JSON-LD object to normalize.</param>
		/// <param name="options">the normalization options.</param>
		/// <param name="callback">(err, normalized) called once the operation completes.</param>
		/// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
		/// <exception cref="JsonLD.Core.JsonLdError"></exception>
		public virtual object Normalize(RDFDataset dataset)
		{
			// create quads and map bnodes to their associated quads
			IList<RDFDataset.Quad> quads = new List<RDFDataset.Quad>();
			IDictionary<string,IDictionary<string,object>> bnodes = new Dictionary<string,IDictionary<string,object>>();
			foreach (string graphName in dataset.Keys)
			{
                var eachGraphName = graphName;
                IList<RDFDataset.Quad> triples = (IList<RDFDataset.Quad>)dataset[eachGraphName];
				if ("@default".Equals(eachGraphName))
				{
					eachGraphName = null;
				}
                foreach (RDFDataset.Quad quad in triples)
				{
					if (eachGraphName != null)
					{
						if (eachGraphName.IndexOf("_:") == 0)
						{
                            IDictionary<string, object> tmp = new Dictionary<string, object>();
							tmp["type"] = "blank node";
							tmp["value"] = eachGraphName;
							quad["name"] = tmp;
						}
						else
						{
                            IDictionary<string, object> tmp = new Dictionary<string, object>();
							tmp["type"] = "IRI";
							tmp["value"] = eachGraphName;
							quad["name"] = tmp;
						}
					}
					quads.Add(quad);
					string[] attrs = new string[] { "subject", "object", "name" };
					foreach (string attr in attrs)
					{
						if (quad.ContainsKey(attr) && (string)((IDictionary<string,object>)quad[attr])["type"] == "blank node")
						{
                            string id = (string)((IDictionary<string,object>)quad[attr])["value"];
							if (!bnodes.ContainsKey(id))
							{
								bnodes[id] = new Dictionary<string,object> { {"quads", new List<RDFDataset.Quad>()} };
							}
							((IList<RDFDataset.Quad>)bnodes[id]["quads"]).Add(quad);
						}
					}
				}
			}
			// mapping complete, start canonical naming
			NormalizeUtils normalizeUtils = new NormalizeUtils(quads, bnodes, new UniqueNamer
				("_:c14n"), opts);
			return normalizeUtils.HashBlankNodes(bnodes.Keys);
		}
#endif
	}
}
