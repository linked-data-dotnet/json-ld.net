using JsonLD.Core;

namespace JsonLD.Core
{
    internal class Regex
    {
        //public static readonly Pattern TrickyUtfChars = Pattern.Compile("[\uD800\uDC00-\uDB7F\uDFFF]"
        //	);

        public static readonly Pattern PnCharsBase = Pattern.Compile("[a-zA-Z]|[\\u00C0-\\u00D6]|[\\u00D8-\\u00F6]|[\\u00F8-\\u02FF]|[\\u0370-\\u037D]|[\\u037F-\\u1FFF]|"
             + "[\\u200C-\\u200D]|[\\u2070-\\u218F]|[\\u2C00-\\u2FEF]|[\\u3001-\\uD7FF]|[\\uF900-\\uFDCF]|[\\uFDF0-\\uFFFD]|"
            );

        public static readonly Pattern PnCharsU = Pattern.Compile(PnCharsBase + "|[_]");

        public static readonly Pattern PnChars = Pattern.Compile(PnCharsU + "|[-0-9]|[\\u00B7]|[\\u0300-\\u036F]|[\\u203F-\\u2040]"
            );

        public static readonly Pattern PnPrefix = Pattern.Compile("(?:(?:" + PnCharsBase 
            + ")(?:(?:" + PnChars + "|[\\.])*(?:" + PnChars + "))?)");

        public static readonly Pattern Hex = Pattern.Compile("[0-9A-Fa-f]");

        public static readonly Pattern PnLocalEsc = Pattern.Compile("[\\\\][_~\\.\\-!$&'\\(\\)*+,;=/?#@%]"
            );

        public static readonly Pattern Percent = Pattern.Compile("%" + Hex + Hex);

        public static readonly Pattern Plx = Pattern.Compile(Percent + "|" + PnLocalEsc);

        public static readonly Pattern PnLocal = Pattern.Compile("((?:" + PnCharsU + "|[:]|[0-9]|"
             + Plx + ")(?:(?:" + PnChars + "|[.]|[:]|" + Plx + ")*(?:" + PnChars + "|[:]|" +
             Plx + "))?)");

        public static readonly Pattern PnameNs = Pattern.Compile("((?:" + PnPrefix + ")?):"
            );

        public static readonly Pattern PnameLn = Pattern.Compile(string.Empty + PnameNs +
             PnLocal);

        public static readonly Pattern Uchar = Pattern.Compile("\\u005Cu" + Hex + Hex + Hex
             + Hex + "|\\u005CU" + Hex + Hex + Hex + Hex + Hex + Hex + Hex + Hex);

        public static readonly Pattern Echar = Pattern.Compile("\\u005C[tbnrf\\u005C\"']"
            );

        public static readonly Pattern Iriref = Pattern.Compile("(?:<((?:[^\\x00-\\x20<>\"{}|\\^`\\\\]|"
             + Uchar + ")*)>)");

        public static readonly Pattern BlankNodeLabel = Pattern.Compile("(?:_:((?:" + PnCharsU
             + "|[0-9])(?:(?:" + PnChars + "|[\\.])*(?:" + PnChars + "))?))");

        public static readonly Pattern Ws = Pattern.Compile("[ \t\r\n]");

        public static readonly Pattern Ws0N = Pattern.Compile(Ws + "*");

        public static readonly Pattern Ws01 = Pattern.Compile(Ws + "?");

        public static readonly Pattern Ws1N = Pattern.Compile(Ws + "+");

        public static readonly Pattern StringLiteralQuote = Pattern.Compile("\"(?:[^\\u0022\\u005C\\u000A\\u000D]|(?:"
             + Echar + ")|(?:" + Uchar + "))*\"");

        public static readonly Pattern StringLiteralSingleQuote = Pattern.Compile("'(?:[^\\u0027\\u005C\\u000A\\u000D]|(?:"
             + Echar + ")|(?:" + Uchar + "))*'");

        public static readonly Pattern StringLiteralLongSingleQuote = Pattern.Compile("'''(?:(?:(?:'|'')?[^'\\\\])|"
             + Echar + "|" + Uchar + ")*'''");

        public static readonly Pattern StringLiteralLongQuote = Pattern.Compile("\"\"\"(?:(?:(?:\"|\"\")?[^\\\"\\\\])|"
             + Echar + "|" + Uchar + ")*\"\"\"");

        public static readonly Pattern Langtag = Pattern.Compile("(?:@([a-zA-Z]+(?:-[a-zA-Z0-9]+)*))"
            );

        public static readonly Pattern Integer = Pattern.Compile("[+-]?[0-9]+");

        public static readonly Pattern Decimal = Pattern.Compile("[+-]?[0-9]*\\.[0-9]+");

        public static readonly Pattern Exponent = Pattern.Compile("[eE][+-]?[0-9]+");

        public static readonly Pattern Double = Pattern.Compile("[+-]?(?:(?:[0-9]+\\.[0-9]*"
             + Exponent + ")|(?:\\.[0-9]+" + Exponent + ")|(?:[0-9]+" + Exponent + "))");
        // ("1.7".equals(System.getProperty("java.specification.version")) ?
        // "[\\x{10000}-\\x{EFFFF}]" :
        // this seems to work with jdk1.6
        // for ttl
    }
}
