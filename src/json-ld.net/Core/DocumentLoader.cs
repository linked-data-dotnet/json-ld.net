using System;
using System.Collections;
using System.IO;
using System.Linq;
using JsonLD.Core;
using JsonLD.Util;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace JsonLD.Core
{
    public class DocumentLoader
    {
        const int MAX_REDIRECTS = 20;

        /// <summary>An HTTP Accept header that prefers JSONLD.</summary>
        /// <remarks>An HTTP Accept header that prefers JSONLD.</remarks>
        public const string AcceptHeader = "application/ld+json, application/json;q=0.9, application/javascript;q=0.5, text/javascript;q=0.5, text/plain;q=0.2, */*;q=0.1";

        /// <exception cref="JsonLDNet.Core.JsonLdError"></exception>
        public RemoteDocument LoadDocument(string url)
        {
            return LoadDocumentAsync(url).GetAwaiter().GetResult();
        }

        /// <exception cref="JsonLDNet.Core.JsonLdError"></exception>
        public async Task<RemoteDocument> LoadDocumentAsync(string url)
        {
            RemoteDocument doc = new RemoteDocument(url, null);

            try
            {
                HttpResponseMessage httpResponseMessage;

                int redirects = 0;
                int code;
                string redirectedUrl = url;

                // Manually follow redirects because .NET Core refuses to auto-follow HTTPS->HTTP redirects.
                do
                {
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, redirectedUrl);
                    httpRequestMessage.Headers.Add("Accept", AcceptHeader);
                    httpResponseMessage = await JSONUtils._HttpClient.SendAsync(httpRequestMessage);
                    if (httpResponseMessage.Headers.TryGetValues("Location", out var location))
                    {
                        redirectedUrl = location.First();
                    }

                    code = (int)httpResponseMessage.StatusCode;
                } while (redirects++ < MAX_REDIRECTS && code >= 300 && code < 400);

                if (redirects >= MAX_REDIRECTS)
                {
                    throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, $"Too many redirects - {url}");
                }

                if (code >= 400)
                {
                    throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, $"HTTP {code} {url}");
                }

                bool isJsonld = httpResponseMessage.Content.Headers.ContentType.MediaType == "application/ld+json";

                // From RFC 6839, it looks like we should accept application/json and any MediaType ending in "+json".
                if (httpResponseMessage.Content.Headers.ContentType.MediaType != "application/json" && !httpResponseMessage.Content.Headers.ContentType.MediaType.EndsWith("+json"))
                {
                    throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, url);
                }

                if (!isJsonld && httpResponseMessage.Headers.TryGetValues("Link", out var linkHeaders))
                {
                    linkHeaders = linkHeaders.SelectMany((h) => h.Split(",".ToCharArray()))
                                                .Select(h => h.Trim()).ToArray();
                    IEnumerable<string> linkedContexts = linkHeaders.Where(v => v.EndsWith("rel=\"http://www.w3.org/ns/json-ld#context\""));
                    if (linkedContexts.Count() > 1)
                    {
                        throw new JsonLdError(JsonLdError.Error.MultipleContextLinkHeaders);
                    }
                    string header = linkedContexts.First();
                    string linkedUrl = header.Substring(1, header.IndexOf(">") - 1);
                    string resolvedUrl = URL.Resolve(redirectedUrl, linkedUrl);
                    var remoteContext = this.LoadDocument(resolvedUrl);
                    doc.contextUrl = remoteContext.documentUrl;
                    doc.context = remoteContext.document;
                }

                Stream stream = await httpResponseMessage.Content.ReadAsStreamAsync();

                doc.DocumentUrl = redirectedUrl;
                doc.Document = JSONUtils.FromInputStream(stream);
            }
            catch (JsonLdError)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, url, exception);
            }
            return doc;
        }

    }
}
