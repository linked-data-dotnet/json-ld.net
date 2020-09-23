using System;
using System.Collections;
using System.IO;
using System.Linq;
using JsonLD.Core;
using JsonLD.Util;
using System.Net;
using System.Collections.Generic;

namespace JsonLD.Core
{
    public class DocumentLoader
    {
        /// <exception cref="JsonLDNet.Core.JsonLdError"></exception>
        public virtual RemoteDocument LoadDocument(string url)
        {
            if (_documentLoader != null)
            {
                return _documentLoader(url);
            }
#if !PORTABLE && !IS_CORECLR
            RemoteDocument doc = new RemoteDocument(url, null);
            HttpWebResponse resp;

            try
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.Accept = AcceptHeader;
                resp = (HttpWebResponse)req.GetResponse();
                bool isJsonld = resp.Headers[HttpResponseHeader.ContentType] == "application/ld+json";
                if (!resp.Headers[HttpResponseHeader.ContentType].Contains("json"))
                {
                    throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, url);
                }

                string[] linkHeaders = resp.Headers.GetValues("Link");
                if (!isJsonld && linkHeaders != null)
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
                    string resolvedUrl = URL.Resolve(url, linkedUrl);
                    var remoteContext = this.LoadDocument(resolvedUrl);
                    doc.contextUrl = remoteContext.documentUrl;
                    doc.context = remoteContext.document;
                }

                Stream stream = resp.GetResponseStream();

                doc.DocumentUrl = req.Address.ToString();
                doc.Document = JSONUtils.FromInputStream(stream);
            }
            catch (JsonLdError)
            {
                throw;
            }
            catch (WebException webException)
            {
                try
                {
                    resp = (HttpWebResponse)webException.Response;
                    int baseStatusCode = (int)(Math.Floor((double)resp.StatusCode / 100)) * 100;
                    if (baseStatusCode == 300)
                    {
                        string location = resp.Headers[HttpResponseHeader.Location];
                        if (!string.IsNullOrWhiteSpace(location))
                        {
                            // TODO: Add recursion break or simply switch to HttpClient so we don't have to recurse on HTTP redirects.
                            return LoadDocument(location);
                        }
                    }
                }
                catch (Exception innerException)
                {
                    throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, url, innerException);
                }

                throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, url, webException);
            }
            catch (Exception exception)
            {
                throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, url, exception);
            }
            return doc;
#else
            throw new PlatformNotSupportedException();
#endif
        }

        /// <summary>An HTTP Accept header that prefers JSONLD.</summary>
        /// <remarks>An HTTP Accept header that prefers JSONLD.</remarks>
        public const string AcceptHeader = "application/ld+json, application/json;q=0.9, application/javascript;q=0.5, text/javascript;q=0.5, text/plain;q=0.2, */*;q=0.1";

        private Func<string, RemoteDocument> _documentLoader;

        public DocumentLoader() { }

        public DocumentLoader(Func<string, RemoteDocument> documentLoader) => _documentLoader = documentLoader;

//        private static volatile IHttpClient httpClient;

//        /// <summary>
//        /// Returns a Map, List, or String containing the contents of the JSON
//        /// resource resolved from the URL.
//        /// </summary>
//        /// <remarks>
//        /// Returns a Map, List, or String containing the contents of the JSON
//        /// resource resolved from the URL.
//        /// </remarks>
//        /// <param name="url">The URL to resolve</param>
//        /// <returns>
//        /// The Map, List, or String that represent the JSON resource
//        /// resolved from the URL
//        /// </returns>
//        /// <exception cref="Com.Fasterxml.Jackson.Core.JsonParseException">If the JSON was not valid.
//        /// 	</exception>
//        /// <exception cref="System.IO.IOException">If there was an error resolving the resource.
//        /// 	</exception>
//        public static object FromURL(URL url)
//        {
//            MappingJsonFactory jsonFactory = new MappingJsonFactory();
//            InputStream @in = OpenStreamFromURL(url);
//            try
//            {
//                JsonParser parser = jsonFactory.CreateParser(@in);
//                try
//                {
//                    JsonToken token = parser.NextToken();
//                    Type type;
//                    if (token == JsonToken.StartObject)
//                    {
//                        type = typeof(IDictionary);
//                    }
//                    else
//                    {
//                        if (token == JsonToken.StartArray)
//                        {
//                            type = typeof(IList);
//                        }
//                        else
//                        {
//                            type = typeof(string);
//                        }
//                    }
//                    return parser.ReadValueAs(type);
//                }
//                finally
//                {
//                    parser.Close();
//                }
//            }
//            finally
//            {
//                @in.Close();
//            }
//        }

//        /// <summary>
//        /// Opens an
//        /// <see cref="Java.IO.InputStream">Java.IO.InputStream</see>
//        /// for the given
//        /// <see cref="Java.Net.URL">Java.Net.URL</see>
//        /// , including support
//        /// for http and https URLs that are requested using Content Negotiation with
//        /// application/ld+json as the preferred content type.
//        /// </summary>
//        /// <param name="url">The URL identifying the source.</param>
//        /// <returns>An InputStream containing the contents of the source.</returns>
//        /// <exception cref="System.IO.IOException">If there was an error resolving the URL.</exception>
//        public static InputStream OpenStreamFromURL(URL url)
//        {
//            string protocol = url.GetProtocol();
//            if (!JsonLDNet.Shims.EqualsIgnoreCase(protocol, "http") && !JsonLDNet.Shims.EqualsIgnoreCase
//                (protocol, "https"))
//            {
//                // Can't use the HTTP client for those!
//                // Fallback to Java's built-in URL handler. No need for
//                // Accept headers as it's likely to be file: or jar:
//                return url.OpenStream();
//            }
//            IHttpUriRequest request = new HttpGet(url.ToExternalForm());
//            // We prefer application/ld+json, but fallback to application/json
//            // or whatever is available
//            request.AddHeader("Accept", AcceptHeader);
//            IHttpResponse response = GetHttpClient().Execute(request);
//            int status = response.GetStatusLine().GetStatusCode();
//            if (status != 200 && status != 203)
//            {
//                throw new IOException("Can't retrieve " + url + ", status code: " + status);
//            }
//            return response.GetEntity().GetContent();
//        }

//        public static IHttpClient GetHttpClient()
//        {
//            IHttpClient result = httpClient;
//            if (result == null)
//            {
//                lock (typeof(JSONUtils))
//                {
//                    result = httpClient;
//                    if (result == null)
//                    {
//                        // Uses Apache SystemDefaultHttpClient rather than
//                        // DefaultHttpClient, thus the normal proxy settings for the
//                        // JVM will be used
//                        DefaultHttpClient client = new SystemDefaultHttpClient();
//                        // Support compressed data
//                        // http://hc.apache.org/httpcomponents-client-ga/tutorial/html/httpagent.html#d5e1238
//                        client.AddRequestInterceptor(new RequestAcceptEncoding());
//                        client.AddResponseInterceptor(new ResponseContentEncoding());
//                        CacheConfig cacheConfig = new CacheConfig();
//                        cacheConfig.SetMaxObjectSize(1024 * 128);
//                        // 128 kB
//                        cacheConfig.SetMaxCacheEntries(1000);
//                        // and allow caching
//                        httpClient = new CachingHttpClient(client, cacheConfig);
//                        result = httpClient;
//                    }
//                }
//            }
//            return result;
//        }

//        public static void SetHttpClient(IHttpClient nextHttpClient)
//        {
//            lock (typeof(JSONUtils))
//            {
//                httpClient = nextHttpClient;
//            }
//        }
    }
}
