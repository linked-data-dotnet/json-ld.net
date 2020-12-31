using System;
using System.Collections.Generic;

namespace JsonLD.OmniJson
{
    public static class OmniJsonExtensions
    {
        static IEnumerable<OmniJsonToken> Where(this OmniJsonToken toks, Func<OmniJsonToken,bool> predicate)
        {
            foreach (var tok in toks)
            {
                if (predicate((OmniJsonToken)tok))
                {
                    yield return (OmniJsonToken)tok;
                }
            }
        }
    }
}
