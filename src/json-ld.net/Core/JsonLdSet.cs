using System;
using System.Collections.Generic;
using JsonLD.OmniJson;

namespace JsonLD.Core
{
    internal sealed class JsonLdSet
    {
        private readonly Lazy<HashSet<string>> _objects;

        internal JsonLdSet()
        {
            _objects = new Lazy<HashSet<string>>(() => new HashSet<string>(StringComparer.Ordinal));
        }

        internal bool Add(OmniJsonToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (token is OmniJsonObject)
            {
                var id = token["@id"];

                return id != null && _objects.Value.Add(id.Value<string>());
            }

            return false;
        }
    }
}