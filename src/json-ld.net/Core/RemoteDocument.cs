using JsonLD.GenericJson;
using Newtonsoft.Json.Linq;

namespace JsonLD.Core
{
    public class RemoteDocument
    {
        public virtual string DocumentUrl
        {
            get
            {
                return documentUrl;
            }
            set
            {
                this.documentUrl = value;
            }
        }

        public virtual GenericJsonToken Document
        {
            get
            {
                return document;
            }
            set
            {
                this.document = value;
            }
        }

        public virtual string ContextUrl
        {
            get
            {
                return contextUrl;
            }
            set
            {
                this.contextUrl = value;
            }
        }

        public virtual GenericJsonToken Context
        {
            get
            {
                return context;
            }
            set
            {
                this.context = value;
            }
        }

        internal string documentUrl;

        internal GenericJsonToken document;

        internal string contextUrl;

        internal GenericJsonToken context;

        public RemoteDocument(string url, GenericJsonToken document)
            : this(url, document, null)
        {
        }

        public RemoteDocument(string url, GenericJsonToken document, string context)
        {
            this.documentUrl = url;
            this.document = document;
            this.contextUrl = context;
        }
    }
}
