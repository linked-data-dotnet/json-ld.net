using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JsonLD.OmniJson
{
    public class OmniJsonArray : OmniJsonToken, IList<OmniJsonToken>, IList
    {
        IList<object> _wrapped;

        public override object Unwrap() => _wrapped;

        public OmniJsonArray()
        {
            _wrapped = new List<object>();
        }

        public OmniJsonArray(IList<object> list)
        {
            _wrapped = list;
        }

        public OmniJsonArray(IEnumerable<object> list)
        {
            _wrapped = list.ToList();
        }

        public override OmniJsonToken this[object index] { get => this[(int)index]; set => this[(int)index] = value; }

        public virtual OmniJsonToken this[int index] { get => Wrap(_wrapped[index]); set => _wrapped[index] = value?.Unwrap(); }

        public virtual int Count => _wrapped.Count;

        public virtual bool IsReadOnly => throw new NotImplementedException();

        public override OmniJsonTokenType Type => OmniJsonTokenType.Array;

        public bool IsFixedSize => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        object IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual void Add(OmniJsonToken item)
        {
            _wrapped.Add(item?.Unwrap());
        }

        public virtual void Clear()
        {
            _wrapped.Clear();
        }

        public virtual bool Contains(OmniJsonToken item)
        {
            return _wrapped.Contains(item.Unwrap());
        }

        public virtual void CopyTo(OmniJsonToken[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public override OmniJsonToken DeepClone()
        {
            var clone = new List<object>();

            foreach (var element in _wrapped)
            {
                clone.Add(Wrap(element).DeepClone().Unwrap());
            }

            return Wrap(clone);
        }

        public virtual IEnumerator<OmniJsonToken> GetEnumerator()
        {
            foreach (var tok in _wrapped)
            {
                yield return Wrap(tok);
            }
        }

        public virtual int IndexOf(OmniJsonToken item)
        {
            throw new NotImplementedException();
        }

        public virtual void Insert(int index, OmniJsonToken item)
        {
            throw new NotImplementedException();
        }

        public virtual bool Remove(OmniJsonToken item)
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
}
