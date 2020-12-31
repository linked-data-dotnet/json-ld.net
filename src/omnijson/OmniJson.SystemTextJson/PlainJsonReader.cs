using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonLD.OmniJson
{
    public class PlainJsonReader
        : JsonConverter<object>
    {
        public override object Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True)
            {
                return true;
            }

            if (reader.TokenType == JsonTokenType.False)
            {
                return false;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long l))
                {
                    return l;
                }

                return reader.GetDouble();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                //if (reader.TryGetDateTime(out DateTime datetime))
                //{
                //    return datetime;
                //}

                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return dictionary;
                    }

                    // Get the key.
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }

                    string propertyName = reader.GetString();

                    // Get the value.
                    reader.Read();
                    object v = this.Read(ref reader, typeof(object), options);

                    // Add to dictionary.
                    dictionary[propertyName] = v;
                }
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<object> list = new List<object>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return list;
                    }

                    // Get the value.
                    object v = this.Read(ref reader, typeof(object), options);

                    // Add to dictionary.
                    list.Add(v);
                }
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // Use JsonElement as fallback.
            // Newtonsoft uses JArray or JObject.
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                return document.RootElement.Clone();
        }

        public override void Write(
            Utf8JsonWriter writer,
            object objectToWrite,
            JsonSerializerOptions options) =>
                throw new InvalidOperationException("Should not get here.");
    }
}
