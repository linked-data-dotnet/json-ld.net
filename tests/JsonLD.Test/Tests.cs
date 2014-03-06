using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Xunit;
using JsonLD.Core;
using System.Net;

namespace JsonLD.Test
{
    public class Tests
    {
        [Fact]
        public static void This_crazy_thing_doesnt_crash()
        {
            JToken @in = JToken.Parse(new WebClient().DownloadString("http://linked.blob.core.windows.net/nuget/package/dotnetrdf"));
            JsonLdProcessor.Frame(@in, new JObject(), new JsonLdOptions());
        }
    }
}
