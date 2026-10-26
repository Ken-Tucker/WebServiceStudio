using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;

namespace WebServiceStudio
{
    internal class SoapHttpResult
    {
        public string Request { get; set; }

        public string Response { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }

    internal static class SoapHttpClient
    {
        private const string Soap11Namespace = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string Soap12Namespace = "http://www.w3.org/2003/05/soap-envelope";

        public static SoapHttpResult Invoke(WsdlOperation operation, XmlElement[] requestElements,
            RequestProperties properties, CookieContainer cookies)
        {
            string envelope = BuildEnvelope(operation, requestElements);
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = properties.AllowAutoRedirect,
                PreAuthenticate = properties.PreAuthenticate,
                UseCookies = properties.UseCookieContainer,
                CookieContainer = cookies
            };
            if (!string.IsNullOrEmpty(properties.HttpProxy))
            {
                handler.Proxy = new WebProxy(properties.HttpProxy);
                handler.UseProxy = true;
            }
            if (!string.IsNullOrEmpty(properties.BasicAuthUserName))
            {
                handler.Credentials = new NetworkCredential(properties.BasicAuthUserName, properties.BasicAuthPassword);
            }
            else if (properties.UseDefaultCredential)
            {
                handler.Credentials = CredentialCache.DefaultCredentials;
            }

            using (var client = new HttpClient(handler))
            {
                client.Timeout = properties.Timeout > 0
                    ? TimeSpan.FromMilliseconds(properties.Timeout)
                    : System.Threading.Timeout.InfiniteTimeSpan;
                using (var request = new HttpRequestMessage(HttpMethod.Post, properties.Url))
                {
                    string contentType = string.IsNullOrEmpty(properties.ContentType)
                        ? (operation.IsSoap12 ? "application/soap+xml; charset=utf-8" : "text/xml; charset=utf-8")
                        : properties.ContentType;
                    request.Content = new StringContent(envelope, Encoding.UTF8);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                    if (operation.IsSoap12)
                    {
                        if (!string.IsNullOrEmpty(properties.SOAPAction))
                        {
                            request.Content.Headers.ContentType.Parameters.Add(
                                new NameValueHeaderValue("action", "\"" + properties.SOAPAction.Replace("\"", "") + "\""));
                        }
                    }
                    else
                    {
                        request.Headers.Add("SOAPAction", "\"" + (properties.SOAPAction ?? "") + "\"");
                    }

                    using (HttpResponseMessage response =
                        client.SendAsync(request).GetAwaiter().GetResult())
                    {
                        return new SoapHttpResult
                        {
                            Request = envelope,
                            Response = response.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                            StatusCode = response.StatusCode
                        };
                    }
                }
            }
        }

        private static string BuildEnvelope(WsdlOperation operation, XmlElement[] requestElements)
        {
            var document = new XmlDocument();
            string soapNamespace = operation.IsSoap12 ? Soap12Namespace : Soap11Namespace;
            XmlElement envelope = document.CreateElement("soap", "Envelope", soapNamespace);
            document.AppendChild(envelope);
            XmlElement body = document.CreateElement("soap", "Body", soapNamespace);
            envelope.AppendChild(body);
            foreach (XmlElement element in requestElements)
            {
                body.AppendChild(document.ImportNode(element, true));
            }
            return document.OuterXml;
        }
    }
}
