using System;
using System.Collections.Generic;
using System.Text;

namespace JsonLD.Core.Raw
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

        public virtual string Document
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

        public virtual string Context
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

        internal string document;

        internal string contextUrl;

        internal string context;

        public RemoteDocument(string url, string document)
            : this(url, document, null)
        {
        }

        public RemoteDocument(string url, string document, string context)
        {
            this.documentUrl = url;
            this.document = document;
            this.contextUrl = context;
        }
    }
}
