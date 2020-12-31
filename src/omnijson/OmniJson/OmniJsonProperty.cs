using System;
using System.Collections.Generic;

namespace JsonLD.OmniJson
{
    public class OmniJsonProperty : OmniJsonToken
    {
        public string Name { get; private set; }
        public OmniJsonToken Value { get; private set; }

        public override OmniJsonTokenType Type => OmniJsonTokenType.Property;

        public override OmniJsonToken DeepClone()
        {
            throw new NotImplementedException();
        }

        public override object Unwrap()
        {
            throw new NotImplementedException();
        }

        public OmniJsonProperty(string name, OmniJsonToken value)
        {
            Name = name;
            Value = value;
        }

        public static explicit operator OmniJsonProperty(KeyValuePair<string,OmniJsonToken> kvp)
        {
            return new OmniJsonProperty(kvp.Key, kvp.Value);
        }
    }
}
