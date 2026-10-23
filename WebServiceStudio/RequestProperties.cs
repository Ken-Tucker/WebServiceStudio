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
        public string basicAuthPassword;
        public string basicAuthUserName;
        public string contentType;
        public HttpMethod method;
        public bool preAuthenticate;
        public string proxy;
        public string requestPayLoad;
        public string responsePayLoad;
        public string soapAction;
        public int timeout = 0x2710;
        public string url;
        public bool useCookieContainer;
        public bool useDefaultCredential;
        internal CookieContainer CookieContainer { get; } = new CookieContainer();

        public RequestProperties()
        {
            Method = HttpMethod.POST;
            contentType = "text/xml; charset=utf-8";
            timeout = 10000;
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

        public HttpMethod Method
        {
            get { return method; }
            set { method = value; }
        }

        public bool PreAuthenticate
        {
            get { return preAuthenticate; }
            set { preAuthenticate = value; }
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