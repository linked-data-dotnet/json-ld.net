using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace JsonLD.OmniJson
{
    public enum OmniJsonTokenType
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

    public abstract class OmniJsonToken : IEnumerable
    {
        public virtual OmniJsonToken this[object index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public abstract object Unwrap();

        public abstract OmniJsonTokenType Type { get; }

        public virtual T Value<T>() => throw new NotImplementedException();

        public virtual OmniJsonToken Wrap(object obj)
        {
            switch (obj)
            {
                case IList<object> o:
                    return new OmniJsonArray(o);
                case IDictionary<string, object> d:
                    return new OmniJsonObject(d);
                case var x:
                    return new OmniJsonValue(x);
            }
        }

        public static explicit operator string(OmniJsonToken t)
        {
            return (string)(t?.Unwrap());
        }

        public static explicit operator bool(OmniJsonToken t)
        {
            return (bool)(t.Unwrap());
        }

        public static explicit operator double(OmniJsonToken t)
        {
            return (double)(t.Unwrap());
        }

        public static explicit operator int(OmniJsonToken t)
        {
            return (int)(t.Unwrap());
        }

        public static explicit operator long(OmniJsonToken t)
        {
            return (long)(t.Unwrap());
        }

        public static implicit operator OmniJsonToken(string s)
        {
            return new OmniJsonValue(s);
        }

        public static implicit operator OmniJsonToken(bool b)
        {
            return new OmniJsonValue(b);
        }

        public static implicit operator OmniJsonToken(double d)
        {
            return new OmniJsonValue(d);
        }

        public static OmniJsonToken CreateGenericJsonToken(object obj)
        {
            switch (obj)
            {
                case IList<object> o:
                    return new OmniJsonArray(o);
                case IDictionary<string, object> d:
                    return new OmniJsonObject(d);
                case var x:
                    return new OmniJsonValue(x);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this is OmniJsonArray arr)
                foreach (var item in arr) yield return item;

            else if (this is OmniJsonObject obj)
                foreach (var item in obj) yield return item;
        }

        public abstract OmniJsonToken DeepClone();
    }
}
