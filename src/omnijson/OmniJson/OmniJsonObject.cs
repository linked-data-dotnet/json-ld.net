using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace JsonLD.OmniJson
{
    public class OmniJsonObject : OmniJsonToken, IDictionary<string, OmniJsonToken>
    {
        IDictionary<string, object> _wrapped;

        public override object Unwrap() => _wrapped;

        public OmniJsonObject(IDictionary<string,object> dict)
        {
            _wrapped = dict;
        }

        public OmniJsonObject()
        {
            _wrapped = new Dictionary<string,object>();
        }

        public bool IsEmpty() => _wrapped.Count == 0;

        public override OmniJsonToken this[object index] { get => this[(string)index]; set => this[(string)index] = value; }

        public virtual OmniJsonToken this[string key] { get => _wrapped.ContainsKey(key) ? Wrap(_wrapped[key]) : null; set => _wrapped[key] = value?.Unwrap(); }

        public virtual ICollection<string> Keys => throw new NotImplementedException();

        public virtual ICollection<OmniJsonToken> Values => throw new NotImplementedException();

        public int Count => _wrapped.Count;

        public virtual bool IsReadOnly => throw new NotImplementedException();

        public override OmniJsonTokenType Type => OmniJsonTokenType.Object;

        public virtual void Add(string key, OmniJsonToken value)
        {
            _wrapped.Add(key, value.Unwrap());
        }

        public virtual void Add(KeyValuePair<string, OmniJsonToken> item)
        {
            throw new NotImplementedException();
        }

        public virtual void Clear()
        {
            throw new NotImplementedException();
        }

        public virtual bool Contains(KeyValuePair<string, OmniJsonToken> item)
        {
            throw new NotImplementedException();
        }

        public virtual bool ContainsKey(string key)
        {
            return _wrapped.ContainsKey(key);
        }

        public virtual void CopyTo(KeyValuePair<string, OmniJsonToken>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerator<KeyValuePair<string, OmniJsonToken>> GetEnumerator()
        {
            foreach (var kvp in _wrapped)
            {
                yield return new KeyValuePair<string,OmniJsonToken>(kvp.Key, Wrap(kvp.Value));
            }
        }

        public virtual bool Remove(string key)
        {
            return _wrapped.Remove(key);
        }

        public virtual bool Remove(KeyValuePair<string, OmniJsonToken> item)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryGetValue(string key, out OmniJsonToken value)
        {
            var result = _wrapped.TryGetValue(key, out var obj);
            value = Wrap(obj) as OmniJsonToken;

            return result;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var kvp in _wrapped)
            {
                yield return new KeyValuePair<string, OmniJsonToken>(kvp.Key, Wrap(kvp.Value));
            }
        }

        public override OmniJsonToken DeepClone()
        {
            var clone = new Dictionary<string, object>();

            foreach (var element in _wrapped)
            {
                Debug.Assert(!(element.Value is OmniJsonToken));
                clone.Add(element.Key,Wrap(element.Value).DeepClone().Unwrap());
            }

            return Wrap(clone);

        }
    }
}
