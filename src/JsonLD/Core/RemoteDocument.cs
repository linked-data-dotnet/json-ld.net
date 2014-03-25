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

		public virtual JToken Document
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

        public virtual JToken Context
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

		internal JToken document;

		internal string contextUrl;

        internal JToken context;

        public RemoteDocument(string url, JToken document)
            : this(url, document, null)
		{
		}

		public RemoteDocument(string url, JToken document, string context)
		{
			this.documentUrl = url;
			this.document = document;
			this.contextUrl = context;
		}
	}
}
