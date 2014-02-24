using JsonLD.Core;

namespace JsonLD.Core
{
	/// <summary>URI Constants used in the JSON-LD parser.</summary>
	/// <remarks>URI Constants used in the JSON-LD parser.</remarks>
	public sealed class JSONLDConsts
	{
		public const string RdfSyntaxNs = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

		public const string RdfSchemaNs = "http://www.w3.org/2000/01/rdf-schema#";

		public const string XsdNs = "http://www.w3.org/2001/XMLSchema#";

		public const string XsdAnytype = XsdNs + "anyType";

		public const string XsdBoolean = XsdNs + "boolean";

		public const string XsdDouble = XsdNs + "double";

		public const string XsdInteger = XsdNs + "integer";

		public const string XsdFloat = XsdNs + "float";

		public const string XsdDecimal = XsdNs + "decimal";

		public const string XsdAnyuri = XsdNs + "anyURI";

		public const string XsdString = XsdNs + "string";

		public const string RdfType = RdfSyntaxNs + "type";

		public const string RdfFirst = RdfSyntaxNs + "first";

		public const string RdfRest = RdfSyntaxNs + "rest";

		public const string RdfNil = RdfSyntaxNs + "nil";

		public const string RdfPlainLiteral = RdfSyntaxNs + "PlainLiteral";

		public const string RdfXmlLiteral = RdfSyntaxNs + "XMLLiteral";

		public const string RdfObject = RdfSyntaxNs + "object";

		public const string RdfLangstring = RdfSyntaxNs + "langString";

		public const string RdfList = RdfSyntaxNs + "List";
	}
}
