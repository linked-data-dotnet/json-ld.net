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
				string documentUrl = value;
				this.documentUrl = documentUrl;
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
				JToken document = value;
				this.document = document;
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
				string contextUrl = value;
				this.contextUrl = contextUrl;
			}
		}

		internal string documentUrl;

		internal JToken document;

		internal string contextUrl;

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
