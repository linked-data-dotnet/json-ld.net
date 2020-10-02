using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JsonLD.Util
{
    internal static class LDHttpClient
    {
        const string ACCEPT_HEADER = "application/ld+json, application/json;q=0.9, application/javascript;q=0.5, text/javascript;q=0.5, text/plain;q=0.2, */*;q=0.1";
        const int MAX_REDIRECTS = 20;

        static HttpClient _hc;

        static LDHttpClient()
        {
            _hc = new HttpClient();
            _hc.DefaultRequestHeaders.Add("Accept", ACCEPT_HEADER);
        }

        static public async Task<HttpResponseMessage> FetchAsync(string url)
        {
            int redirects = 0;
            int code;
            string redirectedUrl = url;

            HttpResponseMessage response;

            // Manually follow redirects because .NET Core refuses to auto-follow HTTPS->HTTP redirects.
            do
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, redirectedUrl);
                response = await _hc.SendAsync(httpRequestMessage);
                if (response.Headers.TryGetValues("Location", out var location))
                {
                    redirectedUrl = location.First();
                }

                code = (int)response.StatusCode;
            } while (redirects++ < MAX_REDIRECTS && code >= 300 && code < 400);

            if (redirects >= MAX_REDIRECTS)
            {
                throw new HttpRequestException("Too many redirects");
            }

            return response;
        }
    }
}
