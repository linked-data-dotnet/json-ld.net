using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace JsonLD.GenericJson
{
    public enum GenericJsonTokenType
    {
        Object,
        Array,
        Integer,
        String,
        Float,
        Boolean,
        Null,
        Value,
        Property
    };

    public static class GenericJsonExtensions
    {
        static IEnumerable<GenericJsonToken> Where(this GenericJsonToken toks, Func<GenericJsonToken,bool> predicate)
        {
            foreach (var tok in toks)
            {
                if (predicate((GenericJsonToken)tok))
                {
                    yield return (GenericJsonToken)tok;
                }
            }
        }
    }

    public abstract class GenericJsonToken : IEnumerable
    {
        public virtual GenericJsonToken this[object index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public abstract object Unwrap();

        public abstract GenericJsonTokenType Type { get; }

        public virtual T Value<T>() => throw new NotImplementedException();

        public virtual GenericJsonToken Wrap(object obj)
        {
            switch (obj)
            {
                case IList<object> o:
                    return new GenericJsonArray(o);
                case IDictionary<string, object> d:
                    return new GenericJsonObject(d);
                case var x:
                    return new GenericJsonValue(x);
            }
        }

        public static explicit operator string(GenericJsonToken t)
        {
            return (string)(t?.Unwrap());
        }

        public static explicit operator bool(GenericJsonToken t)
        {
            return (bool)(t.Unwrap());
        }

        public static explicit operator double(GenericJsonToken t)
        {
            return (double)(t.Unwrap());
        }

        public static explicit operator int(GenericJsonToken t)
        {
            return (int)(t.Unwrap());
        }

        public static explicit operator long(GenericJsonToken t)
        {
            return (long)(t.Unwrap());
        }

        public static implicit operator GenericJsonToken(string s)
        {
            return new GenericJsonValue(s);
        }

        public static implicit operator GenericJsonToken(bool b)
        {
            return new GenericJsonValue(b);
        }

        public static implicit operator GenericJsonToken(double d)
        {
            return new GenericJsonValue(d);
        }

        public static GenericJsonToken CreateGenericJsonToken(object obj)
        {
            switch (obj)
            {
                case IList<object> o:
                    return new GenericJsonArray(o);
                case IDictionary<string, object> d:
                    return new GenericJsonObject(d);
                case var x:
                    return new GenericJsonValue(x);
            }
        }

        public static GenericJsonToken Parse(string str)
        {
            var deserializeOptions = new JsonSerializerOptions();
            deserializeOptions.Converters.Add(new GenericConverter());

            return GenericJsonToken.CreateGenericJsonToken(JsonSerializer.Deserialize<object>(str, deserializeOptions));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this is GenericJsonArray arr)
                foreach (var item in arr) yield return item;

            else if (this is GenericJsonObject obj)
                foreach (var item in obj) yield return item;
        }

        public abstract GenericJsonToken DeepClone();
    }

    public class GenericJsonValue : GenericJsonToken
    {
        object _wrapped;

        public override object Unwrap() => _wrapped;

        public override GenericJsonTokenType Type { 
            get
            {
                switch (_wrapped)
                {
                    case string _:
                        return GenericJsonTokenType.String;
                    case int _:
                    case long _:
                        return GenericJsonTokenType.Integer;
                    case double _:
                        return GenericJsonTokenType.Float;
                    case bool _:
                        return GenericJsonTokenType.Boolean;
                    case var _:
                        return GenericJsonTokenType.Null;
                }
            }
        }

        public GenericJsonValue(object obj)
        {
            _wrapped = obj;
        }

        public static explicit operator string(GenericJsonValue v)
        {
            return (string)(v._wrapped);
        }

        public static implicit operator GenericJsonValue(string s)
        {
            return new GenericJsonValue(s);
        }

        public static explicit operator GenericJsonValue(int i)
        {
            return new GenericJsonValue(i);
        }

        public static explicit operator GenericJsonValue(double d)
        {
            return new GenericJsonValue(d);
        }

        public override GenericJsonToken DeepClone()
        {
            return Wrap(_wrapped);
        }

        public override T Value<T>()
        {
            return (T)_wrapped;
        }
    }

    public class GenericJsonProperty : GenericJsonToken
    {
        public string Name { get; private set; }
        public GenericJsonToken Value { get; private set; }

        public override GenericJsonTokenType Type => GenericJsonTokenType.Property;

        public override GenericJsonToken DeepClone()
        {
            throw new NotImplementedException();
        }

        public override object Unwrap()
        {
            throw new NotImplementedException();
        }

        public GenericJsonProperty(string name, GenericJsonToken value)
        {
            Name = name;
            Value = value;
        }

        public static explicit operator GenericJsonProperty(KeyValuePair<string,GenericJsonToken> kvp)
        {
            return new GenericJsonProperty(kvp.Key, kvp.Value);
        }
    }

    public class GenericJsonArray : GenericJsonToken, IList<GenericJsonToken>, IList
    {
        IList<object> _wrapped;

        public override object Unwrap() => _wrapped;

        public GenericJsonArray()
        {
            _wrapped = new List<object>();
        }

        public GenericJsonArray(IList<object> list)
        {
            _wrapped = list;
        }

        public GenericJsonArray(IEnumerable<object> list)
        {
            _wrapped = list.ToList();
        }

        public override GenericJsonToken this[object index] { get => this[(int)index]; set => this[(int)index] = value; }

        public virtual GenericJsonToken this[int index] { get => Wrap(_wrapped[index]); set => _wrapped[index] = value?.Unwrap(); }

        public virtual int Count => _wrapped.Count;

        public virtual bool IsReadOnly => throw new NotImplementedException();

        public override GenericJsonTokenType Type => GenericJsonTokenType.Array;

        public bool IsFixedSize => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        object IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual void Add(GenericJsonToken item)
        {
            _wrapped.Add(item?.Unwrap());
        }

        public virtual void Clear()
        {
            _wrapped.Clear();
        }

        public virtual bool Contains(GenericJsonToken item)
        {
            return _wrapped.Contains(item.Unwrap());
        }

        public virtual void CopyTo(GenericJsonToken[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public override GenericJsonToken DeepClone()
        {
            var clone = new List<object>();

            foreach (var element in _wrapped)
            {
                clone.Add(Wrap(element).DeepClone().Unwrap());
            }

            return Wrap(clone);
        }

        public virtual IEnumerator<GenericJsonToken> GetEnumerator()
        {
            foreach (var tok in _wrapped)
            {
                yield return Wrap(tok);
            }
        }

        public virtual int IndexOf(GenericJsonToken item)
        {
            throw new NotImplementedException();
        }

        public virtual void Insert(int index, GenericJsonToken item)
        {
            throw new NotImplementedException();
        }

        public virtual bool Remove(GenericJsonToken item)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerable Enumerate()
        {
            foreach (var val in _wrapped)
            {
                yield return Wrap(val);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Enumerate().GetEnumerator();
        }

        public override T Value<T>()
        {
            throw new NotImplementedException();
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }
    }

    public class GenericJsonObject : GenericJsonToken, IDictionary<string, GenericJsonToken>
    {
        IDictionary<string, object> _wrapped;

        public override object Unwrap() => _wrapped;

        public GenericJsonObject(IDictionary<string,object> dict)
        {
            _wrapped = dict;
        }

        public GenericJsonObject()
        {
            _wrapped = new Dictionary<string,object>();
        }

        public bool IsEmpty() => _wrapped.Count == 0;

        public override GenericJsonToken this[object index] { get => this[(string)index]; set => this[(string)index] = value; }

        public virtual GenericJsonToken this[string key] { get => _wrapped.ContainsKey(key) ? Wrap(_wrapped[key]) : null; set => _wrapped[key] = value?.Unwrap(); }

        public virtual ICollection<string> Keys => throw new NotImplementedException();

        public virtual ICollection<GenericJsonToken> Values => throw new NotImplementedException();

        public int Count => _wrapped.Count;

        public virtual bool IsReadOnly => throw new NotImplementedException();

        public override GenericJsonTokenType Type => GenericJsonTokenType.Object;

        public virtual void Add(string key, GenericJsonToken value)
        {
            throw new NotImplementedException();
        }

        public virtual void Add(KeyValuePair<string, GenericJsonToken> item)
        {
            throw new NotImplementedException();
        }

        public virtual void Clear()
        {
            throw new NotImplementedException();
        }

        public virtual bool Contains(KeyValuePair<string, GenericJsonToken> item)
        {
            throw new NotImplementedException();
        }

        public virtual bool ContainsKey(string key)
        {
            return _wrapped.ContainsKey(key);
        }

        public virtual void CopyTo(KeyValuePair<string, GenericJsonToken>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerator<KeyValuePair<string, GenericJsonToken>> GetEnumerator()
        {
            foreach (var kvp in _wrapped)
            {
                yield return new KeyValuePair<string,GenericJsonToken>(kvp.Key, Wrap(kvp.Value));
            }
        }

        public virtual bool Remove(string key)
        {
            return _wrapped.Remove(key);
        }

        public virtual bool Remove(KeyValuePair<string, GenericJsonToken> item)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryGetValue(string key, out GenericJsonToken value)
        {
            var result = _wrapped.TryGetValue(key, out var obj);
            value = Wrap(obj) as GenericJsonToken;

            return result;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var kvp in _wrapped)
            {
                yield return new KeyValuePair<string, GenericJsonToken>(kvp.Key, Wrap(kvp.Value));
            }
        }

        public override GenericJsonToken DeepClone()
        {
            var clone = new Dictionary<string, object>();

            foreach (var element in _wrapped)
            {
                clone.Add(element.Key,Wrap(element.Value).DeepClone().Unwrap());
            }

            return Wrap(clone);

        }
    }

    public class GenericConverter
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
