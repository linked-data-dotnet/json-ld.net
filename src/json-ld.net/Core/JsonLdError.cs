using System;
using System.Collections.Generic;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    //[System.Serializable]
    public class JsonLdError : Exception
    {
        private JsonLdError.Error type;
        internal JObject details = null;

        internal JsonLdError(JsonLdError.Error type, object detail, Exception innerException)
            : base(detail == null ? string.Empty : detail.ToString(), innerException)
        {
            // TODO: pretty toString (e.g. print whole json objects)
            this.type = type;
        }

        internal JsonLdError(JsonLdError.Error type, object detail)
            : base(detail == null ? string.Empty : detail.ToString())
        {
            // TODO: pretty toString (e.g. print whole json objects)
            this.type = type;
        }

        internal JsonLdError(JsonLdError.Error type)
            : base(string.Empty)
        {
            this.type = type;
        }

        //[System.Serializable]
        public sealed class Error
        {
            public static readonly JsonLdError.Error LoadingDocumentFailed = new JsonLdError.Error
                ("loading document failed");

            public static readonly JsonLdError.Error ListOfLists = new JsonLdError.Error("list of lists"
                );

            public static readonly JsonLdError.Error InvalidIndexValue = new JsonLdError.Error
                ("invalid @index value");

            public static readonly JsonLdError.Error ConflictingIndexes = new JsonLdError.Error
                ("conflicting indexes");

            public static readonly JsonLdError.Error InvalidIdValue = new JsonLdError.Error("invalid @id value"
                );

            public static readonly JsonLdError.Error InvalidLocalContext = new JsonLdError.Error
                ("invalid local context");

            public static readonly JsonLdError.Error MultipleContextLinkHeaders = new JsonLdError.Error
                ("multiple context link headers");

            public static readonly JsonLdError.Error LoadingRemoteContextFailed = new JsonLdError.Error
                ("loading remote context failed");

            public static readonly JsonLdError.Error InvalidRemoteContext = new JsonLdError.Error
                ("invalid remote context");

            public static readonly JsonLdError.Error RecursiveContextInclusion = new JsonLdError.Error
                ("recursive context inclusion");

            public static readonly JsonLdError.Error InvalidBaseIri = new JsonLdError.Error("invalid base IRI"
                );

            public static readonly JsonLdError.Error InvalidVocabMapping = new JsonLdError.Error
                ("invalid vocab mapping");

            public static readonly JsonLdError.Error InvalidDefaultLanguage = new JsonLdError.Error
                ("invalid default language");

            public static readonly JsonLdError.Error KeywordRedefinition = new JsonLdError.Error
                ("keyword redefinition");

            public static readonly JsonLdError.Error InvalidTermDefinition = new JsonLdError.Error
                ("invalid term definition");

            public static readonly JsonLdError.Error InvalidReverseProperty = new JsonLdError.Error
                ("invalid reverse property");

            public static readonly JsonLdError.Error InvalidIriMapping = new JsonLdError.Error
                ("invalid IRI mapping");

            public static readonly JsonLdError.Error CyclicIriMapping = new JsonLdError.Error
                ("cyclic IRI mapping");

            public static readonly JsonLdError.Error InvalidKeywordAlias = new JsonLdError.Error
                ("invalid keyword alias");

            public static readonly JsonLdError.Error InvalidTypeMapping = new JsonLdError.Error
                ("invalid type mapping");

            public static readonly JsonLdError.Error InvalidLanguageMapping = new JsonLdError.Error
                ("invalid language mapping");

            public static readonly JsonLdError.Error CollidingKeywords = new JsonLdError.Error
                ("colliding keywords");

            public static readonly JsonLdError.Error InvalidContainerMapping = new JsonLdError.Error
                ("invalid container mapping");

            public static readonly JsonLdError.Error InvalidTypeValue = new JsonLdError.Error
                ("invalid type value");

            public static readonly JsonLdError.Error InvalidValueObject = new JsonLdError.Error
                ("invalid value object");

            public static readonly JsonLdError.Error InvalidValueObjectValue = new JsonLdError.Error
                ("invalid value object value");

            public static readonly JsonLdError.Error InvalidLanguageTaggedString = new JsonLdError.Error
                ("invalid language-tagged string");

            public static readonly JsonLdError.Error InvalidLanguageTaggedValue = new JsonLdError.Error
                ("invalid language-tagged value");

            public static readonly JsonLdError.Error InvalidTypedValue = new JsonLdError.Error
                ("invalid typed value");

            public static readonly JsonLdError.Error InvalidSetOrListObject = new JsonLdError.Error
                ("invalid set or list object");

            public static readonly JsonLdError.Error InvalidLanguageMapValue = new JsonLdError.Error
                ("invalid language map value");

            public static readonly JsonLdError.Error CompactionToListOfLists = new JsonLdError.Error
                ("compaction to list of lists");

            public static readonly JsonLdError.Error InvalidReversePropertyMap = new JsonLdError.Error
                ("invalid reverse property map");

            public static readonly JsonLdError.Error InvalidReverseValue = new JsonLdError.Error
                ("invalid @reverse value");

            public static readonly JsonLdError.Error InvalidReversePropertyValue = new JsonLdError.Error
                ("invalid reverse property value");

            public static readonly JsonLdError.Error SyntaxError = new JsonLdError.Error("syntax error"
                );

            public static readonly JsonLdError.Error NotImplemented = new JsonLdError.Error("not implemnted"
                );

            public static readonly JsonLdError.Error UnknownFormat = new JsonLdError.Error("unknown format"
                );

            public static readonly JsonLdError.Error InvalidInput = new JsonLdError.Error("invalid input"
                );

            public static readonly JsonLdError.Error ParseError = new JsonLdError.Error("parse error"
                );

            public static readonly JsonLdError.Error UnknownError = new JsonLdError.Error("unknown error"
                );

            private readonly string error;

            private Error(string error)
            {
                // non spec related errors
                this.error = error;
            }

            public override string ToString()
            {
                return error;
            }
        }

        internal virtual JsonLdError SetType(JsonLdError.Error error)
        {
            this.type = error;
            return this;
        }

        public new virtual JsonLdError.Error GetType()
        {
            return type;
        }

        public virtual JObject GetDetails()
        {
            return details;
        }

        public override string Message
        {
            get
            {
                string msg = base.Message;
                if (msg != null && !string.Empty.Equals(msg))
                {
                    return type.ToString() + ": " + msg;
                }
                return type.ToString();
            }
        }
    }
}
