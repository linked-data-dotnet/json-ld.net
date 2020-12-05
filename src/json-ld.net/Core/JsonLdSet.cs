using System;
using System.Collections.Generic;
using JsonLD.GenericJson;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    internal sealed class JsonLdSet
    {
        private readonly Lazy<HashSet<string>> _objects;

        internal JsonLdSet()
        {
            _objects = new Lazy<HashSet<string>>(() => new HashSet<string>(StringComparer.Ordinal));
        }

        internal bool Add(GenericJsonToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (token is GenericJsonObject)
            {
                var id = token["@id"];

                return id != null && _objects.Value.Add(id.Value<string>());
            }

            return false;
        }
    }
}