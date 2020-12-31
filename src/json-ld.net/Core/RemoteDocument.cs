using JsonLD.OmniJson;

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

        public virtual OmniJsonToken Document
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

        public virtual OmniJsonToken Context
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

        internal OmniJsonToken document;

        internal string contextUrl;

        internal OmniJsonToken context;

        public RemoteDocument(string url, OmniJsonToken document)
            : this(url, document, null)
        {
        }

        public RemoteDocument(string url, OmniJsonToken document, string context)
        {
            this.documentUrl = url;
            this.document = document;
            this.contextUrl = context;
        }
    }
}
