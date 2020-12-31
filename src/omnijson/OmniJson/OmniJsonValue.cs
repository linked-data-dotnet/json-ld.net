namespace JsonLD.OmniJson
{
    public class OmniJsonValue : OmniJsonToken
    {
        object _wrapped;

        public override object Unwrap() => _wrapped;

        public override OmniJsonTokenType Type { 
            get
            {
                switch (_wrapped)
                {
                    case string _:
                        return OmniJsonTokenType.String;
                    case int _:
                    case long _:
                        return OmniJsonTokenType.Integer;
                    case double _:
                        return OmniJsonTokenType.Float;
                    case bool _:
                        return OmniJsonTokenType.Boolean;
                    case var _:
                        return OmniJsonTokenType.Null;
                }
            }
        }

        public OmniJsonValue(object obj)
        {
            _wrapped = obj;
        }

        public static explicit operator string(OmniJsonValue v)
        {
            return (string)(v._wrapped);
        }

        public static implicit operator OmniJsonValue(string s)
        {
            return new OmniJsonValue(s);
        }

        public static explicit operator OmniJsonValue(int i)
        {
            return new OmniJsonValue(i);
        }

        public static explicit operator OmniJsonValue(double d)
        {
            return new OmniJsonValue(d);
        }

        public override OmniJsonToken DeepClone()
        {
            return Wrap(_wrapped);
        }

        public override T Value<T>()
        {
            return (T)_wrapped;
        }
    }
}
