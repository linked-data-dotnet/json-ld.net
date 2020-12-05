using System.Collections;
using System.Collections.Generic;
using JsonLD.Core;
using JsonLD.Util;
using Newtonsoft.Json.Linq;
using System;
using JsonLD.GenericJson;

namespace JsonLD.Core
{
    public class JsonLdApi
    {
        //private static readonly ILogger Log = LoggerFactory.GetLogger(typeof(JsonLDNet.Core.JsonLdApi));

        internal JsonLdOptions opts;

        internal GenericJsonToken value = null;

        internal Context context = null;

        public JsonLdApi()
        {
            opts = new JsonLdOptions(string.Empty);
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public JsonLdApi(GenericJsonToken input, JsonLdOptions opts)
        {
            Initialize(input, null, opts);
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public JsonLdApi(GenericJsonToken input, GenericJsonToken context, JsonLdOptions opts)
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
        private void Initialize(GenericJsonToken input, GenericJsonToken context, JsonLdOptions opts)
        {
            // set option defaults (TODO: clone?)
            // NOTE: sane defaults should be set in JsonLdOptions constructor
            this.opts = opts;
            if (input is GenericJsonArray || input is GenericJsonObject)
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
        public virtual GenericJsonToken Compact(Context activeCtx, string activeProperty, GenericJsonToken element
            , bool compactArrays)
        {
            // 2)
            if (element is GenericJsonArray)
            {
                // 2.1)
                GenericJsonArray result = new GenericJsonArray();
                // 2.2)
                foreach (GenericJsonToken item in element)
                {
                    // 2.2.1)
                    GenericJsonToken compactedItem = Compact(activeCtx, activeProperty, item, compactArrays);
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
            if (element is GenericJsonObject)
            {
                // access helper
                IDictionary<string, GenericJsonToken> elem = (IDictionary<string, GenericJsonToken>)element;
                // 4
                if (elem.ContainsKey("@value") || elem.ContainsKey("@id"))
                {
                    GenericJsonToken compactedValue = activeCtx.CompactValue(activeProperty, (GenericJsonObject)element);
                    if (!(compactedValue is GenericJsonObject || compactedValue is GenericJsonArray))
                    {
                        return compactedValue;
                    }
                }
                // 5)
                bool insideReverse = ("@reverse".Equals(activeProperty));
                // 6)
                GenericJsonObject result = new GenericJsonObject();
                // 7)
                GenericJsonArray keys = new GenericJsonArray(element.GetKeys());
                keys.SortInPlace();
                foreach (string expandedProperty in keys)
                {
                    GenericJsonToken expandedValue = elem[expandedProperty];
                    // 7.1)
                    if ("@id".Equals(expandedProperty) || "@type".Equals(expandedProperty))
                    {
                        GenericJsonToken compactedValue;
                        // 7.1.1)
                        if (expandedValue.Type == GenericJsonTokenType.String)
                        {
                            compactedValue = activeCtx.CompactIri((string)expandedValue, "@type".Equals(expandedProperty
                                ));
                        }
                        else
                        {
                            // 7.1.2)
                            GenericJsonArray types = new GenericJsonArray();
                            // 7.1.2.2)
                            foreach (string expandedType in (GenericJsonArray)expandedValue)
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
                        GenericJsonObject compactedValue = (GenericJsonObject)Compact(activeCtx, "@reverse", expandedValue, compactArrays);
                        // 7.2.2)
                        List<string> properties = new List<string>(compactedValue.GetKeys());
                        foreach (string property in properties)
                        {
                            GenericJsonToken value = compactedValue[property];
                            // 7.2.2.1)
                            if (activeCtx.IsReverseProperty(property))
                            {
                                // 7.2.2.1.1)
                                if (("@set".Equals(activeCtx.GetContainer(property)) || !compactArrays) && !(value
                                     is GenericJsonArray))
                                {
                                    GenericJsonArray tmp = new GenericJsonArray();
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
                                    if (!(result[property] is GenericJsonArray))
                                    {
                                        GenericJsonArray tmp = new GenericJsonArray();
                                        tmp.Add(result[property]);
                                        result[property] = tmp;
                                    }
                                    if (value is GenericJsonArray)
                                    {
                                        JsonLD.Collections.AddAll(((GenericJsonArray)result[property]), (GenericJsonArray)value
                                            );
                                    }
                                    else
                                    {
                                        ((GenericJsonArray)result[property]).Add(value);
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
                    if (((GenericJsonArray)expandedValue).Count == 0)
                    {
                        // 7.5.1)
                        string itemActiveProperty = activeCtx.CompactIri(expandedProperty, expandedValue, 
                            true, insideReverse);
                        // 7.5.2)
                        if (!result.ContainsKey(itemActiveProperty))
                        {
                            result[itemActiveProperty] = new GenericJsonArray();
                        }
                        else
                        {
                            GenericJsonToken value = result[itemActiveProperty];
                            if (!(value is GenericJsonArray))
                            {
                                GenericJsonArray tmp = new GenericJsonArray();
                                tmp.Add(value);
                                result[itemActiveProperty] = tmp;
                            }
                        }
                    }
                    // 7.6)
                    foreach (GenericJsonToken expandedItem in (GenericJsonArray)expandedValue)
                    {
                        // 7.6.1)
                        string itemActiveProperty = activeCtx.CompactIri(expandedProperty, expandedItem, 
                            true, insideReverse);
                        // 7.6.2)
                        string container = activeCtx.GetContainer(itemActiveProperty);
                        // get @list value if appropriate
                        bool isList = (expandedItem is GenericJsonObject && ((IDictionary<string, GenericJsonToken>)expandedItem
                            ).ContainsKey("@list"));
                        GenericJsonToken list = null;
                        if (isList)
                        {
                            list = ((IDictionary<string, GenericJsonToken>)expandedItem)["@list"];
                        }
                        // 7.6.3)
                        GenericJsonToken compactedItem = Compact(activeCtx, itemActiveProperty, isList ? list : expandedItem
                            , compactArrays);
                        // 7.6.4)
                        if (isList)
                        {
                            // 7.6.4.1)
                            if (!(compactedItem is GenericJsonArray))
                            {
                                GenericJsonArray tmp = new GenericJsonArray();
                                tmp.Add(compactedItem);
                                compactedItem = tmp;
                            }
                            // 7.6.4.2)
                            if (!"@list".Equals(container))
                            {
                                // 7.6.4.2.1)
                                GenericJsonObject wrapper = new GenericJsonObject();
                                // TODO: SPEC: no mention of vocab = true
                                wrapper[activeCtx.CompactIri("@list", true)] = compactedItem;
                                compactedItem = wrapper;
                                // 7.6.4.2.2)
                                if (((IDictionary<string, GenericJsonToken>)expandedItem).ContainsKey("@index"))
                                {
                                    ((IDictionary<string, GenericJsonToken>)compactedItem)[activeCtx.CompactIri("@index", true)
                                        ] = ((IDictionary<string, GenericJsonToken>)expandedItem)["@index"];
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
                            GenericJsonObject mapObject;
                            if (result.ContainsKey(itemActiveProperty))
                            {
                                mapObject = (GenericJsonObject)result[itemActiveProperty];
                            }
                            else
                            {
                                mapObject = new GenericJsonObject();
                                result[itemActiveProperty] = mapObject;
                            }
                            // 7.6.5.2)
                            if ("@language".Equals(container) && (compactedItem is GenericJsonObject && ((IDictionary
                                <string, GenericJsonToken>)compactedItem).ContainsKey("@value")))
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
                                GenericJsonArray tmp;
                                if (!(mapObject[mapKey] is GenericJsonArray))
                                {
                                    tmp = new GenericJsonArray();
                                    tmp.Add(mapObject[mapKey]);
                                    mapObject[mapKey] = tmp;
                                }
                                else
                                {
                                    tmp = (GenericJsonArray)mapObject[mapKey];
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
                                !(compactedItem is GenericJsonArray));
                            if (check)
                            {
                                GenericJsonArray tmp = new GenericJsonArray();
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
                                if (!(result[itemActiveProperty] is GenericJsonArray))
                                {
                                    GenericJsonArray tmp = new GenericJsonArray();
                                    tmp.Add(result[itemActiveProperty]);
                                    result[itemActiveProperty] = tmp;
                                }
                                if (compactedItem is GenericJsonArray)
                                {
                                    JsonLD.Collections.AddAll(((GenericJsonArray)result[itemActiveProperty]), (GenericJsonArray)compactedItem);
                                }
                                else
                                {
                                    ((GenericJsonArray)result[itemActiveProperty]).Add(compactedItem);
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
        public virtual GenericJsonToken Compact(Context activeCtx, string activeProperty, GenericJsonToken element
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
        public virtual GenericJsonToken Expand(Context activeCtx, string activeProperty, GenericJsonToken element)
        {
            // 1)
            if (element.IsNull())
            {
                return null;
            }
            // 3)
            if (element is GenericJsonArray)
            {
                // 3.1)
                GenericJsonArray result = new GenericJsonArray();
                // 3.2)
                foreach (GenericJsonToken item in (GenericJsonArray)element)
                {
                    // 3.2.1)
                    GenericJsonToken v = Expand(activeCtx, activeProperty, item);
                    // 3.2.2)
                    if (("@list".Equals(activeProperty) || "@list".Equals(activeCtx.GetContainer(activeProperty
                        ))) && (v is GenericJsonArray || (v is GenericJsonObject && ((IDictionary<string, GenericJsonToken>)v).ContainsKey
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
                            if (v is GenericJsonArray)
                            {
                                JsonLD.Collections.AddAll(result, (GenericJsonArray)v);
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
                if (element is GenericJsonObject)
                {
                    // access helper
                    IDictionary<string, GenericJsonToken> elem = (GenericJsonObject)element;
                    // 5)
                    if (elem.ContainsKey("@context"))
                    {
                        activeCtx = activeCtx.Parse(elem["@context"]);
                    }
                    // 6)
                    GenericJsonObject result = new GenericJsonObject();
                    // 7)
                    GenericJsonArray keys = new GenericJsonArray(element.GetKeys());
                    keys.SortInPlace();
                    foreach (string key in keys)
                    {
                        GenericJsonToken value = elem[key];
                        // 7.1)
                        if (key.Equals("@context"))
                        {
                            continue;
                        }
                        // 7.2)
                        string expandedProperty = activeCtx.ExpandIri(key, false, true, null, null);
                        GenericJsonToken expandedValue = null;
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
                                if (!(value.Type == GenericJsonTokenType.String))
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
                                    if (value is GenericJsonArray)
                                    {
                                        expandedValue = new GenericJsonArray();
                                        foreach (GenericJsonToken v in (GenericJsonArray)value)
                                        {
                                            if (v.Type != GenericJsonTokenType.String)
                                            {
                                                throw new JsonLdError(JsonLdError.Error.InvalidTypeValue, "@type value must be a string or array of strings"
                                                    );
                                            }
                                            ((GenericJsonArray)expandedValue).Add(activeCtx.ExpandIri((string)v, true, true, null
                                                , null));
                                        }
                                    }
                                    else
                                    {
                                        if (value.Type == GenericJsonTokenType.String)
                                        {
                                            expandedValue = activeCtx.ExpandIri((string)value, true, true, null, null);
                                        }
                                        else
                                        {
                                            // TODO: SPEC: no mention of empty map check
                                            if (value is GenericJsonObject)
                                            {
                                                if (((GenericJsonObject)value).Count != 0)
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
                                            if (!value.IsNull() && (value is GenericJsonObject || value is GenericJsonArray))
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
                                                if (!(value.Type == GenericJsonTokenType.String))
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
                                                    if (!(value.Type == GenericJsonTokenType.String))
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
                                                        if (!(expandedValue is GenericJsonArray))
                                                        {
                                                            GenericJsonArray tmp = new GenericJsonArray();
                                                            tmp.Add(expandedValue);
                                                            expandedValue = tmp;
                                                        }
                                                        // 7.4.9.3)
                                                        foreach (GenericJsonToken o in (GenericJsonArray)expandedValue)
                                                        {
                                                            if (o is GenericJsonObject && ((GenericJsonObject)o).ContainsKey("@list"))
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
                                                                if (!(value is GenericJsonObject))
                                                                {
                                                                    throw new JsonLdError(JsonLdError.Error.InvalidReverseValue, "@reverse value must be an object"
                                                                        );
                                                                }
                                                                // 7.4.11.1)
                                                                expandedValue = Expand(activeCtx, "@reverse", value);
                                                                // NOTE: algorithm assumes the result is a map
                                                                // 7.4.11.2)
                                                                if (((IDictionary<string, GenericJsonToken>)expandedValue).ContainsKey("@reverse"))
                                                                {
                                                                    GenericJsonObject reverse = (GenericJsonObject)((GenericJsonObject)expandedValue)["@reverse"];
                                                                    foreach (string property in reverse.GetKeys())
                                                                    {
                                                                        GenericJsonToken item = reverse[property];
                                                                        // 7.4.11.2.1)
                                                                        if (!result.ContainsKey(property))
                                                                        {
                                                                            result[property] = new GenericJsonArray();
                                                                        }
                                                                        // 7.4.11.2.2)
                                                                        if (item is GenericJsonArray)
                                                                        {
                                                                            JsonLD.Collections.AddAll(((GenericJsonArray)result[property]), (GenericJsonArray)item);
                                                                        }
                                                                        else
                                                                        {
                                                                            ((GenericJsonArray)result[property]).Add(item);
                                                                        }
                                                                    }
                                                                }
                                                                // 7.4.11.3)
                                                                if (((GenericJsonObject)expandedValue).Count > (((GenericJsonObject)expandedValue).ContainsKey("@reverse") ? 1 : 0))
                                                                {
                                                                    // 7.4.11.3.1)
                                                                    if (!result.ContainsKey("@reverse"))
                                                                    {
                                                                        result["@reverse"] = new GenericJsonObject();
                                                                    }
                                                                    // 7.4.11.3.2)
                                                                    GenericJsonObject reverseMap = (GenericJsonObject)result["@reverse"];
                                                                    // 7.4.11.3.3)
                                                                    foreach (string property in expandedValue.GetKeys())
                                                                    {
                                                                        if ("@reverse".Equals(property))
                                                                        {
                                                                            continue;
                                                                        }
                                                                        // 7.4.11.3.3.1)
                                                                        GenericJsonArray items = (GenericJsonArray)((GenericJsonObject)expandedValue)[property];
                                                                        foreach (GenericJsonToken item in items)
                                                                        {
                                                                            // 7.4.11.3.3.1.1)
                                                                            if (item is GenericJsonObject && (((GenericJsonObject)item).ContainsKey("@value") || ((GenericJsonObject)item).ContainsKey("@list")))
                                                                            {
                                                                                throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyValue);
                                                                            }
                                                                            // 7.4.11.3.3.1.2)
                                                                            if (!reverseMap.ContainsKey(property))
                                                                            {
                                                                                reverseMap[property] = new GenericJsonArray();
                                                                            }
                                                                            // 7.4.11.3.3.1.3)
                                                                            ((GenericJsonArray)reverseMap[property]).Add(item);
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
                            if ("@language".Equals(activeCtx.GetContainer(key)) && value is GenericJsonObject)
                            {
                                // 7.5.1)
                                expandedValue = new GenericJsonArray();
                                // 7.5.2)
                                foreach (string language in value.GetKeys())
                                {
                                    GenericJsonToken languageValue = ((IDictionary<string, GenericJsonToken>)value)[language];
                                    // 7.5.2.1)
                                    if (!(languageValue is GenericJsonArray))
                                    {
                                        GenericJsonToken tmp = languageValue;
                                        languageValue = new GenericJsonArray();
                                        ((GenericJsonArray)languageValue).Add(tmp);
                                    }
                                    // 7.5.2.2)
                                    foreach (GenericJsonToken item in (GenericJsonArray)languageValue)
                                    {
                                        // 7.5.2.2.1)
                                        if (!(item.Type == GenericJsonTokenType.String))
                                        {
                                            throw new JsonLdError(JsonLdError.Error.InvalidLanguageMapValue, "Expected " + item
                                                .ToString() + " to be a string");
                                        }
                                        // 7.5.2.2.2)
                                        GenericJsonObject tmp = new GenericJsonObject();
                                        tmp["@value"] = item;
                                        tmp["@language"] = language.ToLower();
                                        ((GenericJsonArray)expandedValue).Add(tmp);
                                    }
                                }
                            }
                            else
                            {
                                // 7.6)
                                if ("@index".Equals(activeCtx.GetContainer(key)) && value is GenericJsonObject)
                                {
                                    // 7.6.1)
                                    expandedValue = new GenericJsonArray();
                                    // 7.6.2)
                                    GenericJsonArray indexKeys = new GenericJsonArray(value.GetKeys());
                                    indexKeys.SortInPlace();
                                    foreach (string index in indexKeys)
                                    {
                                        GenericJsonToken indexValue = ((GenericJsonObject)value)[index];
                                        // 7.6.2.1)
                                        if (!(indexValue is GenericJsonArray))
                                        {
                                            GenericJsonToken tmp = indexValue;
                                            indexValue = new GenericJsonArray();
                                            ((GenericJsonArray)indexValue).Add(tmp);
                                        }
                                        // 7.6.2.2)
                                        indexValue = Expand(activeCtx, key, indexValue);
                                        // 7.6.2.3)
                                        foreach (GenericJsonObject item in (GenericJsonArray)indexValue)
                                        {
                                            // 7.6.2.3.1)
                                            if (!item.ContainsKey("@index"))
                                            {
                                                item["@index"] = index;
                                            }
                                            // 7.6.2.3.2)
                                            ((GenericJsonArray)expandedValue).Add(item);
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
                            if (!(expandedValue is GenericJsonObject) || !((GenericJsonObject)expandedValue).ContainsKey("@list"))
                            {
                                GenericJsonToken tmp = expandedValue;
                                if (!(tmp is GenericJsonArray))
                                {
                                    tmp = new GenericJsonArray();
                                    ((GenericJsonArray)tmp).Add(expandedValue);
                                }
                                expandedValue = new GenericJsonObject();
                                ((GenericJsonObject)expandedValue)["@list"] = tmp;
                            }
                        }
                        // 7.10)
                        if (activeCtx.IsReverseProperty(key))
                        {
                            // 7.10.1)
                            if (!result.ContainsKey("@reverse"))
                            {
                                result["@reverse"] = new GenericJsonObject();
                            }
                            // 7.10.2)
                            GenericJsonObject reverseMap = (GenericJsonObject)result["@reverse"];
                            // 7.10.3)
                            if (!(expandedValue is GenericJsonArray))
                            {
                                GenericJsonToken tmp = expandedValue;
                                expandedValue = new GenericJsonArray();
                                ((GenericJsonArray)expandedValue).Add(tmp);
                            }
                            // 7.10.4)
                            foreach (GenericJsonToken item in (GenericJsonArray)expandedValue)
                            {
                                // 7.10.4.1)
                                if (item is GenericJsonObject && (((GenericJsonObject)item).ContainsKey("@value") || ((GenericJsonObject)item).ContainsKey("@list")))
                                {
                                    throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyValue);
                                }
                                // 7.10.4.2)
                                if (!reverseMap.ContainsKey(expandedProperty))
                                {
                                    reverseMap[expandedProperty] = new GenericJsonArray();
                                }
                                // 7.10.4.3)
                                if (item is GenericJsonArray)
                                {
                                    JsonLD.Collections.AddAll(((GenericJsonArray)reverseMap[expandedProperty]), (GenericJsonArray)item);
                                }
                                else
                                {
                                    ((GenericJsonArray)reverseMap[expandedProperty]).Add(item);
                                }
                            }
                        }
                        else
                        {
                            // 7.11)
                            // 7.11.1)
                            if (!result.ContainsKey(expandedProperty))
                            {
                                result[expandedProperty] = new GenericJsonArray();
                            }
                            // 7.11.2)
                            if (expandedValue is GenericJsonArray)
                            {
                                JsonLD.Collections.AddAll(((GenericJsonArray)result[expandedProperty]), (GenericJsonArray)expandedValue);
                            }
                            else
                            {
                                ((GenericJsonArray)result[expandedProperty]).Add(expandedValue);
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
                        GenericJsonToken rval = result["@value"];
                        if (rval.IsNull())
                        {
                            // nothing else is possible with result if we set it to
                            // null, so simply return it
                            return null;
                        }
                        // 8.3)
                        if (!(rval.Type == GenericJsonTokenType.String) && result.ContainsKey("@language"))
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
                                if (!(result["@type"].Type == GenericJsonTokenType.String) || ((string)result["@type"]).StartsWith("_:") ||
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
                            GenericJsonToken rtype = result["@type"];
                            if (!(rtype is GenericJsonArray))
                            {
                                GenericJsonArray tmp = new GenericJsonArray();
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
        public virtual GenericJsonToken Expand(Context activeCtx, GenericJsonToken element)
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
        internal virtual void GenerateNodeMap(GenericJsonToken element, GenericJsonObject
             nodeMap)
        {
            GenerateNodeMap(element, nodeMap, "@default", null, null, null);
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal virtual void GenerateNodeMap(GenericJsonToken element, GenericJsonObject
             nodeMap, string activeGraph)
        {
            GenerateNodeMap(element, nodeMap, activeGraph, null, null, null);
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        internal virtual void GenerateNodeMap(GenericJsonToken element, GenericJsonObject
             nodeMap, string activeGraph, GenericJsonToken activeSubject, string activeProperty, GenericJsonObject list)
        {
            GenerateNodeMap(element, nodeMap, activeGraph, activeSubject, activeProperty, list, skipSetContainsCheck: false);
        }

        private void GenerateNodeMap(GenericJsonToken element, GenericJsonObject nodeMap,
            string activeGraph, GenericJsonToken activeSubject, string activeProperty, GenericJsonObject list, bool skipSetContainsCheck)
        {
            // 1)
            if (element is GenericJsonArray)
            {
                JsonLdSet set = null;

                if (list == null)
                {
                    set = new JsonLdSet();
                }

                // 1.1)
                foreach (GenericJsonToken item in (GenericJsonArray)element)
                {
                    skipSetContainsCheck = false;

                    if (set != null)
                    {
                        skipSetContainsCheck = set.Add(item);
                    }

                    GenerateNodeMap(item, nodeMap, activeGraph, activeSubject, activeProperty, list, skipSetContainsCheck);
                }
                return;
            }
            // for convenience
            IDictionary<string, GenericJsonToken> elem = (IDictionary<string, GenericJsonToken>)element;
            // 2)
            if (!((IDictionary<string,GenericJsonToken>)nodeMap).ContainsKey(activeGraph))
            {
                nodeMap[activeGraph] = new GenericJsonObject();
            }
            GenericJsonObject graph = (GenericJsonObject)nodeMap[activeGraph
                ];
            GenericJsonObject node = (GenericJsonObject)((activeSubject.IsNull() || activeSubject.Type != GenericJsonTokenType.String) 
                ? null : graph[(string)activeSubject]);
            // 3)
            if (elem.ContainsKey("@type"))
            {
                // 3.1)
                GenericJsonArray oldTypes;
                GenericJsonArray newTypes = new GenericJsonArray();
                if (elem["@type"] is GenericJsonArray)
                {
                    oldTypes = (GenericJsonArray)elem["@type"];
                }
                else
                {
                    oldTypes = new GenericJsonArray();
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
                if (elem["@type"] is GenericJsonArray)
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
                    JsonLdUtils.MergeValue(node, activeProperty, (GenericJsonObject)elem);
                }
                else
                {
                    // 4.2)
                    JsonLdUtils.MergeValue(list, "@list", (GenericJsonObject)elem);
                }
            }
            else
            {
                // 5)
                if (elem.ContainsKey("@list"))
                {
                    // 5.1)
                    GenericJsonObject result = new GenericJsonObject();
                    result["@list"] = new GenericJsonArray();
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
                        GenericJsonObject tmp = new GenericJsonObject();
                        tmp["@id"] = id;
                        graph[id] = tmp;
                    }
                    // 6.4) TODO: SPEC this line is asked for by the spec, but it breaks various tests
                    //node = (Map<String, Object>) graph.get(id);
                    // 6.5)
                    if (activeSubject is GenericJsonObject)
                    {
                        // 6.5.1)
                        JsonLdUtils.MergeValue((GenericJsonObject)graph[id], activeProperty, activeSubject
                            );
                    }
                    else
                    {
                        // 6.6)
                        if (activeProperty != null)
                        {
                            GenericJsonObject reference = new GenericJsonObject();
                            reference["@id"] = id;
                            // 6.6.2)
                            if (list == null)
                            {
                                // 6.6.2.1+2)
                                JsonLdUtils.MergeValue(node, activeProperty, reference, skipSetContainsCheck);
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
                    node = (GenericJsonObject)graph[id];
                    // 6.7)
                    if (elem.ContainsKey("@type"))
                    {
                        foreach (GenericJsonToken type in (GenericJsonArray)JsonLD.Collections.Remove(elem, "@type"
                            ))
                        {
                            JsonLdUtils.MergeValue(node, "@type", type);
                        }
                    }
                    // 6.8)
                    if (elem.ContainsKey("@index"))
                    {
                        GenericJsonToken elemIndex = JsonLD.Collections.Remove(elem, "@index");
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
                        GenericJsonObject referencedNode = new GenericJsonObject();
                        referencedNode["@id"] = id;
                        // 6.9.2+6.9.4)
                        GenericJsonObject reverseMap = (GenericJsonObject)JsonLD.Collections.Remove
                            (elem, "@reverse");
                        // 6.9.3)
                        foreach (string property in reverseMap.GetKeys())
                        {
                            GenericJsonArray values = (GenericJsonArray)reverseMap[property];
                            // 6.9.3.1)
                            foreach (GenericJsonToken value in values)
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
                    GenericJsonArray keys = new GenericJsonArray(element.GetKeys());
                    keys.SortInPlace();
                    foreach (string property_1 in keys)
                    {
                        var eachProperty_1 = property_1;
                        GenericJsonToken value = elem[eachProperty_1];
                        // 6.11.1)
                        if (eachProperty_1.StartsWith("_:"))
                        {
                            eachProperty_1 = GenerateBlankNodeIdentifier(eachProperty_1);
                        }
                        // 6.11.2)
                        if (!node.ContainsKey(eachProperty_1))
                        {
                            node[eachProperty_1] = new GenericJsonArray();
                        }
                        // 6.11.3)
                        GenerateNodeMap(value, nodeMap, activeGraph, id, eachProperty_1, null);
                    }
                }
            }
        }

        private readonly GenericJsonObject blankNodeIdentifierMap = new GenericJsonObject();

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
            public GenericJsonToken parent = null;

            public string property = null;

            internal EmbedNode(JsonLdApi _enclosing)
            {
                this._enclosing = _enclosing;
            }

            private readonly JsonLdApi _enclosing;
        }

        private GenericJsonObject nodeMap;

        /// <summary>Performs JSON-LD framing.</summary>
        /// <remarks>Performs JSON-LD framing.</remarks>
        /// <param name="input">the expanded JSON-LD to frame.</param>
        /// <param name="frame">the expanded JSON-LD frame to use.</param>
        /// <param name="options">the framing options.</param>
        /// <returns>the framed output.</returns>
        /// <exception cref="JSONLDProcessingError">JSONLDProcessingError</exception>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual GenericJsonArray Frame(GenericJsonToken input, GenericJsonArray frame)
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
            // use tree map so keys are sorted by default
            // XXX BUG BUG BUG XXX (sblom) Figure out where this needs to be sorted and use extension methods to return sorted enumerators or something!
            GenericJsonObject nodes = new GenericJsonObject();
            GenerateNodeMap(input, nodes);
            this.nodeMap = (GenericJsonObject)nodes["@default"];
            GenericJsonArray framed = new GenericJsonArray();
            // NOTE: frame validation is done by the function not allowing anything
            // other than list to me passed
            Frame(state, this.nodeMap, (frame != null && frame.Count > 0 ? (GenericJsonObject)frame[0] : new GenericJsonObject()), framed, null);
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
        private void Frame(JsonLdApi.FramingContext state, GenericJsonObject nodes
            , GenericJsonObject frame, GenericJsonToken parent, string property)
        {
            // filter out subjects that match the frame
            GenericJsonObject matches = FilterNodes(state, nodes, frame);
            // get flags for current frame
            bool embedOn = GetFrameFlag(frame, "@embed", state.embed);
            bool explicitOn = GetFrameFlag(frame, "@explicit", state.@explicit);
            // add matches to output
            GenericJsonArray ids = new GenericJsonArray(matches.GetKeys());
            ids.SortInPlace();
            foreach (string id in ids)
            {
                if (property == null)
                {
                    state.embeds = new Dictionary<string, JsonLdApi.EmbedNode>();
                }
                // start output
                GenericJsonObject output = new GenericJsonObject();
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
                    if (existing.parent is GenericJsonArray)
                    {
                        foreach (GenericJsonToken p in (GenericJsonArray)(existing.parent))
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
                        if (((GenericJsonObject)existing.parent).ContainsKey(existing.property))
                        {
                            foreach (GenericJsonToken v in (GenericJsonArray)((GenericJsonObject)existing.parent)[existing.property])
                            {
                                if (v is GenericJsonObject && ((GenericJsonObject)v)["@id"].SafeCompare(id))
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
                    GenericJsonObject element = (GenericJsonObject)matches[id];
                    GenericJsonArray props = new GenericJsonArray(element.GetKeys());
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
                        GenericJsonArray value = (GenericJsonArray)element[prop];
                        foreach (GenericJsonToken item in value)
                        {
                            // recurse into list
                            if ((item is GenericJsonObject) && ((GenericJsonObject)item).ContainsKey("@list"))
                            {
                                // add empty list
                                GenericJsonObject list = new GenericJsonObject();
                                list["@list"] = new GenericJsonArray();
                                AddFrameOutput(state, output, prop, list);
                                // add list objects
                                foreach (GenericJsonToken listitem in (GenericJsonArray)((GenericJsonObject)item)["@list"
                                    ])
                                {
                                    // recurse into subject reference
                                    if (JsonLdUtils.IsNodeReference(listitem))
                                    {
                                        GenericJsonObject tmp = new GenericJsonObject();
                                        string itemid = (string)((IDictionary<string, GenericJsonToken>)listitem)["@id"];
                                        // TODO: nodes may need to be node_map,
                                        // which is global
                                        tmp[itemid] = this.nodeMap[itemid];
                                        Frame(state, tmp, (GenericJsonObject)((GenericJsonArray)frame[prop])[0], list
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
                                    GenericJsonObject tmp = new GenericJsonObject();
                                    string itemid = (string)((GenericJsonObject)item)["@id"];
                                    // TODO: nodes may need to be node_map, which is
                                    // global
                                    tmp[itemid] = this.nodeMap[itemid];
                                    Frame(state, tmp, (GenericJsonObject)((GenericJsonArray)frame[prop])[0], output
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
                    props = new GenericJsonArray(frame.GetKeys());
                    props.SortInPlace();
                    foreach (string prop_1 in props)
                    {
                        // skip keywords
                        if (JsonLdUtils.IsKeyword(prop_1))
                        {
                            continue;
                        }
                        GenericJsonArray pf = (GenericJsonArray)frame[prop_1];
                        GenericJsonObject propertyFrame = pf.Count > 0 ? (GenericJsonObject)pf[0] : null;
                        if (propertyFrame == null)
                        {
                            propertyFrame = new GenericJsonObject();
                        }
                        bool omitDefaultOn = GetFrameFlag(propertyFrame, "@omitDefault", state.omitDefault
                            );
                        if (!omitDefaultOn && !output.ContainsKey(prop_1))
                        {
                            GenericJsonToken def = "@null";
                            if (propertyFrame.ContainsKey("@default"))
                            {
                                def = JsonLdUtils.Clone(propertyFrame["@default"]);
                            }
                            if (!(def is GenericJsonArray))
                            {
                                GenericJsonArray tmp = new GenericJsonArray();
                                tmp.Add(def);
                                def = tmp;
                            }
                            GenericJsonObject tmp1 = new GenericJsonObject();
                            tmp1["@preserve"] = def;
                            GenericJsonArray tmp2 = new GenericJsonArray();
                            tmp2.Add(tmp1);
                            output[prop_1] = tmp2;
                        }
                    }
                    // add output to parent
                    AddFrameOutput(state, parent, property, output);
                }
            }
        }

        private bool GetFrameFlag(GenericJsonObject frame, string name, bool thedefault
            )
        {
            GenericJsonToken value = frame[name];
            if (value is GenericJsonArray)
            {
                if (((GenericJsonArray)value).Count > 0)
                {
                    value = ((GenericJsonArray)value)[0];
                }
            }
            if (value is GenericJsonObject && ((GenericJsonObject)value).ContainsKey("@value"
                ))
            {
                value = ((GenericJsonObject)value)["@value"];
            }
            if (value != null && value.Type == GenericJsonTokenType.Boolean)
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
            GenericJsonToken parent = embed.parent;
            string property = embed.property;
            // create reference to replace embed
            GenericJsonObject node = new GenericJsonObject();
            node["@id"] = id;
            // remove existing embed
            if (JsonLdUtils.IsNode(parent))
            {
                // replace subject with reference
                GenericJsonArray newvals = new GenericJsonArray();
                GenericJsonArray oldvals = (GenericJsonArray)((GenericJsonObject)parent)[property
                    ];
                foreach (GenericJsonToken v in oldvals)
                {
                    if (v is GenericJsonObject && ((GenericJsonObject)v)["@id"].SafeCompare(id))
                    {
                        newvals.Add(node);
                    }
                    else
                    {
                        newvals.Add(v);
                    }
                }
                ((GenericJsonObject)parent)[property] = newvals;
            }
            // recursively remove dependent dangling embeds
            RemoveDependents(embeds, id);
        }

        private static void RemoveDependents(IDictionary<string, JsonLdApi.EmbedNode> embeds
            , string id)
        {
            // get embed keys as a separate array to enable deleting keys in map
            List<string> embedsKeys = new List<string>(embeds.Keys);
            foreach (string id_dep in embedsKeys)
            {
                JsonLdApi.EmbedNode e;
                if (!embeds.TryGetValue(id_dep, out e))
                {
                    continue;
                }
                GenericJsonToken p = !e.parent.IsNull() ? e.parent : new GenericJsonObject();
                if (!(p is GenericJsonObject))
                {
                    continue;
                }
                string pid = (string)((GenericJsonObject)p)["@id"];
                if (Obj.Equals(id, pid))
                {
                    JsonLD.Collections.Remove(embeds, id_dep);
                    RemoveDependents(embeds, id_dep);
                }
            }
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private GenericJsonObject FilterNodes(JsonLdApi.FramingContext state, GenericJsonObject nodes, GenericJsonObject frame)
        {
            GenericJsonObject rval = new GenericJsonObject();
            foreach (string id in nodes.GetKeys())
            {
                GenericJsonObject element = (GenericJsonObject)nodes[id];
                if (element != null && FilterNode(state, element, frame))
                {
                    rval[id] = element;
                }
            }
            return rval;
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        private bool FilterNode(JsonLdApi.FramingContext state, GenericJsonObject node, GenericJsonObject frame)
        {
            GenericJsonToken types = frame["@type"];
            if (!types.IsNull())
            {
                if (!(types is GenericJsonArray))
                {
                    throw new JsonLdError(JsonLdError.Error.SyntaxError, "frame @type must be an array"
                        );
                }
                GenericJsonToken nodeTypes = node["@type"];
                if (nodeTypes.IsNull())
                {
                    nodeTypes = new GenericJsonArray();
                }
                else
                {
                    if (!(nodeTypes is GenericJsonArray))
                    {
                        throw new JsonLdError(JsonLdError.Error.SyntaxError, "node @type must be an array"
                            );
                    }
                }
                if (((GenericJsonArray)types).Count == 1 && ((GenericJsonArray)types)[0] is GenericJsonObject
                     && ((GenericJsonObject)((GenericJsonArray)types)[0]).Count == 0)
                {
                    return !((GenericJsonArray)nodeTypes).IsEmpty();
                }
                else
                {
                    foreach (GenericJsonToken i in (GenericJsonArray)nodeTypes)
                    {
                        foreach (GenericJsonToken j in (GenericJsonArray)types)
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
        private static void AddFrameOutput(JsonLdApi.FramingContext state, GenericJsonToken parent, 
            string property, GenericJsonToken output)
        {
            if (parent is GenericJsonObject)
            {
                GenericJsonArray prop = (GenericJsonArray)((GenericJsonObject)parent)[property];
                if (prop == null)
                {
                    prop = new GenericJsonArray();
                    ((GenericJsonObject)parent)[property] = prop;
                }
                prop.Add(output);
            }
            else
            {
                ((GenericJsonArray)parent).Add(output);
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
        private void EmbedValues(JsonLdApi.FramingContext state, GenericJsonObject element, string property, GenericJsonToken output)
        {
            // embed subject properties in output
            GenericJsonArray objects = (GenericJsonArray)element[property];
            foreach (GenericJsonToken o in objects)
            {
                var eachObj = o;

                if (eachObj is GenericJsonObject && ((GenericJsonObject)eachObj).ContainsKey("@list"))
                {
                    GenericJsonObject list = new GenericJsonObject { { "@list", new GenericJsonArray() } };
                    if (output is GenericJsonArray)
                    {
                        ((GenericJsonArray)output).Add(list);
                    }
                    else
                    {
                        // TODO(sblom): What the hell does this even mean in JSON.NET?
                        //output[property] = new GenericJsonArray(list);
                        output[property] = new GenericJsonArray();
                    }
                    EmbedValues(state, (GenericJsonObject)eachObj, "@list", list["@list"]);
                }
                // handle subject reference
                else if (JsonLdUtils.IsNodeReference(eachObj))
                {
                    string sid = (string)((GenericJsonObject)eachObj)["@id"];
                    // embed full subject if isn't already embedded
                    if (!state.embeds.ContainsKey(sid))
                    {
                        // add embed
                        JsonLdApi.EmbedNode embed = new JsonLdApi.EmbedNode(this);
                        embed.parent = output;
                        embed.property = property;
                        state.embeds[sid] = embed;
                        // recurse into subject
                        eachObj = new GenericJsonObject();
                        GenericJsonObject s = (GenericJsonObject)this.nodeMap[sid];
                        if (s == null)
                        {
                            s = new GenericJsonObject();
                            s["@id"] = sid;
                        }
                        foreach (string prop in s.GetKeys())
                        {
                            // copy keywords
                            if (JsonLdUtils.IsKeyword(prop))
                            {
                                ((GenericJsonObject)eachObj)[prop] = JsonLdUtils.Clone(s[prop]);
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
                , GenericJsonObject value)
            {
                this._enclosing = _enclosing;
                this.node = node;
                this.property = property;
                this.value = value;
            }

            public JsonLdApi.NodeMapNode node = null;

            public string property = null;

            public GenericJsonObject value = null;

            private readonly JsonLdApi _enclosing;
        }

        //[System.Serializable]
        private class NodeMapNode : GenericJsonObject
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
                    if (!(this[JSONLDConsts.RdfFirst] is GenericJsonArray && ((GenericJsonArray)this[JSONLDConsts.RdfFirst
                        ]).Count == 1))
                    {
                        return false;
                    }
                }
                if (this.ContainsKey(JSONLDConsts.RdfRest))
                {
                    keys++;
                    if (!(this[JSONLDConsts.RdfRest] is GenericJsonArray && ((GenericJsonArray)this[JSONLDConsts.RdfRest
                        ]).Count == 1))
                    {
                        return false;
                    }
                }
                if (this.ContainsKey("@type"))
                {
                    keys++;
                    if (!(this["@type"] is GenericJsonArray && ((GenericJsonArray)this["@type"]).Count == 1) && JSONLDConsts
                        .RdfList.Equals(((GenericJsonArray)this["@type"])[0]))
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
            public virtual GenericJsonObject Serialize()
            {
                return new GenericJsonObject((IDictionary<string, object>)this.Unwrap());
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
        public virtual GenericJsonArray FromRDF(RDFDataset dataset)
        {
            // 1)
            GenericJsonObject defaultGraph = new GenericJsonObject();
            // 2)
            GenericJsonObject graphMap = new GenericJsonObject();
            graphMap["@default"] = defaultGraph;
            // 3/3.1)
            foreach (string name in dataset.GraphNames())
            {
                IList<RDFDataset.Quad> graph = dataset.GetQuads(name);
                // 3.2+3.4)
                GenericJsonObject nodeMap;
                if (!graphMap.ContainsKey(name))
                {
                    nodeMap = new GenericJsonObject();
                    graphMap[name] = nodeMap;
                }
                else
                {
                    nodeMap = (GenericJsonObject)graphMap[name];
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
                    GenericJsonObject value = @object.ToObject(opts.GetUseNativeTypes());
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
                GenericJsonObject graph = (GenericJsonObject)graphMap[name_1];
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
                    GenericJsonObject head = usage.value;
                    // 4.3.2)
                    GenericJsonArray list = new GenericJsonArray();
                    GenericJsonArray listNodes = new GenericJsonArray();
                    // 4.3.3)
                    while (JSONLDConsts.RdfRest.Equals(property) && node.IsWellFormedListNode())
                    {
                        // 4.3.3.1)
                        list.Add(((GenericJsonArray)node[JSONLDConsts.RdfFirst])[0]);
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
                        head = (GenericJsonObject)((GenericJsonArray)graph[headId][JSONLDConsts.RdfRest
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
            GenericJsonArray result = new GenericJsonArray();
            // 6)
            GenericJsonArray ids = new GenericJsonArray(defaultGraph.GetKeys());

            if (opts.GetSortGraphsFromRdf())
            {
                ids.SortInPlace();
            }

            foreach (string subject_1 in ids)
            {
                JsonLdApi.NodeMapNode node = (NodeMapNode)defaultGraph[subject_1];
                // 6.1)
                if (graphMap.ContainsKey(subject_1))
                {
                    // 6.1.1)
                    node["@graph"] = new GenericJsonArray();
                    // 6.1.2)
                    GenericJsonArray keys = new GenericJsonArray(graphMap[subject_1].GetKeys());

                    if (opts.GetSortGraphNodesFromRdf())
                    {
                        keys.SortInPlace();
                    }

                    foreach (string s in keys)
                    {
                        JsonLdApi.NodeMapNode n = (NodeMapNode)graphMap[subject_1][s];
                        if (n.Count == 1 && n.ContainsKey("@id"))
                        {
                            continue;
                        }
                        ((GenericJsonArray)node["@graph"]).Add(n.Serialize());
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
            GenericJsonObject nodeMap = new GenericJsonObject();
            nodeMap["@default"] = new GenericJsonObject();
            GenerateNodeMap(this.value, nodeMap);
            RDFDataset dataset = new RDFDataset(this);
            foreach (string graphName in nodeMap.GetKeys())
            {
                // 4.1)
                if (JsonLdUtils.IsRelativeIri(graphName))
                {
                    continue;
                }
                GenericJsonObject graph = (GenericJsonObject)nodeMap[graphName
                    ];
                dataset.GraphToRDF(graphName, graph);
            }
            return dataset;
        }


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
    }
}
