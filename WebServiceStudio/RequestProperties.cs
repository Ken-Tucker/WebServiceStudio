using System.Net;

namespace WebServiceStudio
{
    internal class RequestProperties
    {
        public enum HttpMethod
        {
            GET,
            POST
        }

        public bool allowAutoRedirect = true;
        public bool allowWriteStreamBuffering = true;
        public string basicAuthPassword;
        public string basicAuthUserName;
        public string contentType;
        public bool keepAlive;
        public HttpMethod method;
        public bool pipelined;
        public bool preAuthenticate;
        public string proxy;
        public string requestPayLoad;
        public string responsePayLoad;
        public bool sendChunked;
        public string soapAction;
        public int timeout = 0x2710;
        public string url;
        public bool useCookieContainer;
        public bool useDefaultCredential;

        public RequestProperties()
        {
        }

        public RequestProperties(WsdlOperation operation)
        {
            Method = HttpMethod.POST;
            Url = operation.Endpoint;
            SOAPAction = operation.SoapAction;
            ContentType = operation.IsSoap12
                ? "application/soap+xml; charset=utf-8"
                : "text/xml; charset=utf-8";
            UseCookieContainer = true;
            UseDefaultCredential = true;
            HttpProxy = Configuration.MasterConfig.WsdlSettings.ProxyServer;
        }

        public string __RequestProperties__
        {
            get { return ""; }
        }

        public bool AllowAutoRedirect
        {
            get { return allowAutoRedirect; }
            set { allowAutoRedirect = value; }
        }

        public bool AllowWriteStreamBuffering
        {
            get { return allowWriteStreamBuffering; }
            set { allowWriteStreamBuffering = value; }
        }

        public string BasicAuthPassword
        {
            get { return basicAuthPassword; }
            set { basicAuthPassword = value; }
        }

        public string BasicAuthUserName
        {
            get { return basicAuthUserName; }
            set { basicAuthUserName = value; }
        }

        public string ContentType
        {
            get { return contentType; }
            set { contentType = value; }
        }

        public string HttpProxy
        {
            get { return proxy; }
            set { proxy = ((value == null) || (value.Length == 0)) ? null : new WebProxy(value).Address.ToString(); }
        }

        public bool KeepAlive
        {
            get { return keepAlive; }
            set { keepAlive = value; }
        }

        public HttpMethod Method
        {
            get { return method; }
            set { method = value; }
        }

        public bool Pipelined
        {
            get { return pipelined; }
            set { pipelined = value; }
        }

        public bool PreAuthenticate
        {
            get { return preAuthenticate; }
            set { preAuthenticate = value; }
        }

        public bool SendChunked
        {
            get { return sendChunked; }
            set { sendChunked = value; }
        }

        public string SOAPAction
        {
            get { return soapAction; }
            set { soapAction = value; }
        }

        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        public string Url
        {
            get { return url; }
            set { url = value; }
        }

        public bool UseCookieContainer
        {
            get { return useCookieContainer; }
            set { useCookieContainer = value; }
        }

        public bool UseDefaultCredential
        {
            get { return useDefaultCredential; }
            set { useDefaultCredential = value; }
        }
    }
}