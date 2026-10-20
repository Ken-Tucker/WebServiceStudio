using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;

namespace WebServiceStudio
{
    internal sealed class Wsdl
    {
        private static readonly XNamespace WsdlNamespace = "http://schemas.xmlsoap.org/wsdl/";
        private static readonly XNamespace XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
        private readonly List<ServiceOperation> operations = new List<ServiceOperation>();
        private readonly StringCollection paths = new StringCollection();
        private readonly StringCollection wsdls = new StringCollection();
        private readonly StringCollection xsds = new StringCollection();

        public StringCollection Paths
        {
            get { return paths; }
        }

        public IList<ServiceOperation> Operations
        {
            get { return operations; }
        }

        public StringCollection Wsdls
        {
            get { return wsdls; }
        }

        public StringCollection Xsds
        {
            get { return xsds; }
        }

        public WsdlProperties WsdlProperties
        {
            get { return Configuration.MasterConfig.WsdlSettings; }
        }

        public void Reset()
        {
            paths.Clear();
            operations.Clear();
            wsdls.Clear();
            xsds.Clear();
        }

        public void Generate()
        {
            MainForm.ShowMessage(this, MessageType.Begin, "Loading WSDL");
            try
            {
                var documents = LoadDocuments();
                foreach (var document in documents)
                {
                    var root = document.Xml.Root;
                    if (root != null && root.Name == WsdlNamespace + "definitions")
                    {
                        wsdls.Add(document.Xml.ToString());
                        foreach (var schema in root.Descendants(XmlSchemaNamespace + "schema"))
                        {
                            xsds.Add(schema.ToString());
                        }
                    }
                    else if (root != null && root.Name == XmlSchemaNamespace + "schema")
                    {
                        xsds.Add(document.Xml.ToString());
                    }
                }

                BuildOperations(documents);
                if (operations.Count == 0)
                {
                    throw new InvalidOperationException("No SOAP operations were found in the WSDL.");
                }
                MainForm.ShowMessage(this, MessageType.Success, "Loaded " + operations.Count + " SOAP operations");
            }
            catch (Exception exception)
            {
                MainForm.ShowMessage(this, MessageType.Failure, exception.ToString());
            }
        }

        private List<MetadataDocument> LoadDocuments()
        {
            var documents = new List<MetadataDocument>();
            var pending = new Queue<MetadataDocument>();
            foreach (string path in paths)
            {
                pending.Enqueue(LoadInitialDocument(path));
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (pending.Count > 0)
            {
                MetadataDocument document = pending.Dequeue();
                if (!seen.Add(document.Location) || documents.Count >= 64)
                {
                    continue;
                }
                documents.Add(document);
                foreach (XElement import in document.Xml.Descendants()
                             .Where(element => element.Name == WsdlNamespace + "import" ||
                                               element.Name == XmlSchemaNamespace + "import" ||
                                               element.Name == XmlSchemaNamespace + "include" ||
                                               element.Name == XmlSchemaNamespace + "redefine"))
                {
                    string location = (string)import.Attribute("location") ?? (string)import.Attribute("schemaLocation");
                    if (string.IsNullOrWhiteSpace(location))
                    {
                        continue;
                    }
                    Uri resolved;
                    if (!Uri.TryCreate(new Uri(document.Location), location, out resolved) ||
                        (resolved.Scheme != Uri.UriSchemeHttp && resolved.Scheme != Uri.UriSchemeHttps &&
                         resolved.Scheme != Uri.UriSchemeFile))
                    {
                        continue;
                    }
                    pending.Enqueue(LoadDocument(resolved.AbsoluteUri, null));
                }
            }
            return documents;
        }

        private MetadataDocument LoadInitialDocument(string path)
        {
            bool isHttpUrl = Uri.TryCreate(path, UriKind.Absolute, out Uri uri) &&
                             (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            MetadataDocument document;
            try
            {
                document = LoadDocument(path, null);
            }
            catch when (isHttpUrl && string.IsNullOrEmpty(uri.Query))
            {
                return LoadDocument(new UriBuilder(uri) { Query = "wsdl" }.Uri.AbsoluteUri, null);
            }

            if (isHttpUrl && string.IsNullOrEmpty(uri.Query) &&
                document.Xml.Root?.Name != WsdlNamespace + "definitions")
            {
                return LoadDocument(new UriBuilder(uri) { Query = "wsdl" }.Uri.AbsoluteUri, null);
            }
            return document;
        }

        private MetadataDocument LoadDocument(string location, Uri baseUri)
        {
            Uri uri;
            if (baseUri == null && !Uri.TryCreate(location, UriKind.Absolute, out uri))
            {
                location = Path.GetFullPath(location);
                uri = new Uri(location);
            }
            else if (baseUri != null)
            {
                uri = new Uri(baseUri, location);
            }
            else
            {
                uri = new Uri(location);
            }

            if (uri.IsFile)
            {
                using (var stream = File.OpenRead(uri.LocalPath))
                {
                    return new MetadataDocument(uri.AbsoluteUri, ReadXml(stream));
                }
            }

            using (var handler = new HttpClientHandler())
            {
                WsdlProperties settings = WsdlProperties;
                if (!string.IsNullOrEmpty(settings.UserName))
                {
                    handler.Credentials = new NetworkCredential(settings.UserName, settings.Password, settings.Domain);
                }
                else
                {
                    handler.UseDefaultCredentials = true;
                }
                if (!string.IsNullOrEmpty(settings.ProxyServer))
                {
                    handler.Proxy = new WebProxy(settings.ProxyServer)
                    {
                        Credentials = new NetworkCredential(settings.ProxyUserName, settings.ProxyPassword,
                            settings.ProxyDomain)
                    };
                    handler.UseProxy = true;
                }
                using (var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) })
                using (HttpResponseMessage response = client.GetAsync(uri).GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentLength > 4 * 1024 * 1024)
                    {
                        throw new InvalidDataException("WSDL metadata exceeds the 4 MB limit.");
                    }
                    using (Stream stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    {
                        return new MetadataDocument(uri.AbsoluteUri, ReadXml(stream));
                    }
                }
            }
        }

        private static XDocument ReadXml(Stream stream)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 4 * 1024 * 1024
            };
            using (XmlReader reader = XmlReader.Create(stream, settings))
            {
                return XDocument.Load(reader);
            }
        }

        private void BuildOperations(IList<MetadataDocument> documents)
        {
            foreach (MetadataDocument document in documents)
            {
                XElement definitions = document.Xml.Root;
                if (definitions == null || definitions.Name != WsdlNamespace + "definitions")
                {
                    continue;
                }
                XNamespace targetNamespace = (string)definitions.Attribute("targetNamespace") ?? "";
                var bindings = definitions.Elements(WsdlNamespace + "binding")
                    .ToDictionary(binding => (string)binding.Attribute("name"), StringComparer.Ordinal);
                var portTypes = definitions.Elements(WsdlNamespace + "portType")
                    .ToDictionary(portType => (string)portType.Attribute("name"), StringComparer.Ordinal);
                var messages = definitions.Elements(WsdlNamespace + "message")
                    .ToDictionary(message => (string)message.Attribute("name"), StringComparer.Ordinal);

                foreach (XElement service in definitions.Elements(WsdlNamespace + "service"))
                {
                    foreach (XElement port in service.Elements(WsdlNamespace + "port"))
                    {
                        string endpoint = (string)port.Elements().FirstOrDefault(element =>
                            element.Name.LocalName == "address")?.Attribute("location");
                        XElement bindingElement = ResolveNamedElement(port, (string)port.Attribute("binding"),
                            bindings, targetNamespace);
                        if (bindingElement == null || string.IsNullOrEmpty(endpoint))
                        {
                            continue;
                        }
                        XElement bindingTypeElement = ResolveNamedElement(bindingElement,
                            (string)bindingElement.Attribute("type"), portTypes, targetNamespace);
                        if (bindingTypeElement == null)
                        {
                            continue;
                        }
                        XElement soapBinding = bindingElement.Elements().FirstOrDefault(element =>
                            element.Name.LocalName == "binding" && element.Name.NamespaceName.Contains("soap"));
                        if (soapBinding == null ||
                            string.Equals((string)soapBinding.Attribute("style"), "rpc",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        string soapNamespace = soapBinding.Name.NamespaceName;
                        bool soap12 = soapNamespace == "http://schemas.xmlsoap.org/wsdl/soap12/";
                        foreach (XElement bindingOperation in bindingElement.Elements(WsdlNamespace + "operation"))
                        {
                            string operationName = (string)bindingOperation.Attribute("name");
                            XElement portOperation = bindingTypeElement.Elements(WsdlNamespace + "operation")
                                .FirstOrDefault(element => (string)element.Attribute("name") == operationName);
                            string inputMessage = (string)portOperation?.Element(WsdlNamespace + "input")?
                                .Attribute("message");
                            XElement message = ResolveNamedElement(portOperation ?? bindingOperation, inputMessage,
                                messages, targetNamespace);
                            if (message == null)
                            {
                                continue;
                            }
                            XElement soapOperation = bindingOperation.Elements().FirstOrDefault(element =>
                                element.Name.LocalName == "operation" && element.Name.NamespaceName.Contains("soap"));
                            XElement soapBody = bindingOperation.Elements().SelectMany(element => element.Elements())
                                .FirstOrDefault(element => element.Name.LocalName == "body" &&
                                                           element.Name.NamespaceName.Contains("soap"));
                            if (string.Equals((string)soapBody?.Attribute("use"), "encoded",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            string action = (string)soapOperation?.Attribute("soapAction") ?? "";
                            XElement body = new XElement("Body");
                            foreach (XElement part in message.Elements(WsdlNamespace + "part"))
                            {
                                body.Add(CreatePartElement(part, documents));
                            }
                            string envelopeNamespace = soap12
                                ? "http://www.w3.org/2003/05/soap-envelope"
                                : "http://schemas.xmlsoap.org/soap/envelope/";
                            string bodyXml = new XDocument(new XElement(XName.Get("Envelope", envelopeNamespace),
                                new XAttribute(XNamespace.Xmlns + "soap", envelopeNamespace),
                                new XElement(XName.Get("Body", envelopeNamespace),
                                    body.Elements().ToArray()))).ToString();
                            operations.Add(new ServiceOperation(service.Name.LocalName, operationName, endpoint, action,
                                soap12, bodyXml));
                        }
                    }
                }
            }
        }

        private static XElement ResolveNamedElement(XElement context, string qualifiedName,
            IDictionary<string, XElement> candidates, XNamespace targetNamespace)
        {
            if (string.IsNullOrEmpty(qualifiedName))
            {
                return null;
            }
            int separator = qualifiedName.IndexOf(':');
            string name = separator < 0 ? qualifiedName : qualifiedName.Substring(separator + 1);
            XNamespace ns = separator < 0
                ? targetNamespace
                : context.GetNamespaceOfPrefix(qualifiedName.Substring(0, separator));
            return ns == targetNamespace && candidates.TryGetValue(name, out XElement element) ? element : null;
        }

        private static XElement CreatePartElement(XElement part, IList<MetadataDocument> documents)
        {
            string elementReference = (string)part.Attribute("element");
            if (!string.IsNullOrEmpty(elementReference))
            {
                return CreateGlobalElement(part, elementReference, documents, 0);
            }
            string name = (string)part.Attribute("name") ?? "part";
            string typeReference = (string)part.Attribute("type");
            XElement result = new XElement(name);
            if (!string.IsNullOrEmpty(typeReference))
            {
                AddTypeChildren(result, part, typeReference, documents, 0);
            }
            return result;
        }

        private static XElement CreateGlobalElement(XElement context, string reference,
            IList<MetadataDocument> documents, int depth)
        {
            if (depth > 16)
            {
                return new XElement("element");
            }
            XNamespace ns = ResolveNamespace(context, reference);
            string name = LocalName(reference);
            XElement declaration = documents.SelectMany(document => document.Xml.Descendants(XmlSchemaNamespace + "element"))
                .FirstOrDefault(element => (string)element.Attribute("name") == name &&
                                           (string)element.Parent?.Attribute("targetNamespace") == ns.NamespaceName);
            if (declaration == null)
            {
                return new XElement(XName.Get(name, ns.NamespaceName));
            }
            XElement result = new XElement(XName.Get(name, ns.NamespaceName));
            string type = (string)declaration.Attribute("type");
            if (!string.IsNullOrEmpty(type))
            {
                AddTypeChildren(result, declaration, type, documents, depth + 1);
            }
            else
            {
                AddInlineTypeChildren(result, declaration, documents, depth + 1);
            }
            return result;
        }

        private static void AddTypeChildren(XElement parent, XElement context, string reference,
            IList<MetadataDocument> documents, int depth)
        {
            if (depth > 16 || string.IsNullOrEmpty(reference) ||
                ResolveNamespace(context, reference) == XmlSchemaNamespace)
            {
                return;
            }
            string name = LocalName(reference);
            string ns = ResolveNamespace(context, reference).NamespaceName;
            XElement type = documents.SelectMany(document => document.Xml.Descendants())
                .FirstOrDefault(element => (element.Name == XmlSchemaNamespace + "complexType" ||
                                            element.Name == XmlSchemaNamespace + "simpleType") &&
                                           (string)element.Attribute("name") == name &&
                                           (string)element.Parent?.Attribute("targetNamespace") == ns);
            if (type != null)
            {
                AddInlineTypeChildren(parent, type, documents, depth + 1);
            }
        }

        private static void AddInlineTypeChildren(XElement parent, XElement declaration,
            IList<MetadataDocument> documents, int depth)
        {
            if (depth > 16)
            {
                return;
            }
            XElement complexType = declaration.Name == XmlSchemaNamespace + "complexType"
                ? declaration
                : declaration.Elements(XmlSchemaNamespace + "complexType").FirstOrDefault();
            if (complexType == null)
            {
                return;
            }
            XElement content = complexType.Elements().FirstOrDefault(element =>
                element.Name == XmlSchemaNamespace + "complexContent" ||
                element.Name == XmlSchemaNamespace + "simpleContent") ?? complexType;
            XElement extension = content.Elements().FirstOrDefault(element =>
                element.Name == XmlSchemaNamespace + "extension" ||
                element.Name == XmlSchemaNamespace + "restriction");
            if (extension != null)
            {
                string baseType = (string)extension.Attribute("base");
                if (!string.IsNullOrEmpty(baseType))
                {
                    AddTypeChildren(parent, extension, baseType, documents, depth + 1);
                }
                content = extension;
            }
            XElement particle = content.Elements().FirstOrDefault(element =>
                element.Name == XmlSchemaNamespace + "sequence" ||
                element.Name == XmlSchemaNamespace + "all" ||
                element.Name == XmlSchemaNamespace + "choice");
            AddParticleElements(parent, particle, documents, depth + 1);
        }

        private static void AddParticleElements(XElement parent, XElement particle,
            IList<MetadataDocument> documents, int depth)
        {
            if (particle == null || depth > 16)
            {
                return;
            }
            IEnumerable<XElement> items = particle.Elements();
            if (particle.Name == XmlSchemaNamespace + "choice")
            {
                items = items.Take(1);
            }
            foreach (XElement item in items.Take(64))
            {
                if (item.Name == XmlSchemaNamespace + "sequence" ||
                    item.Name == XmlSchemaNamespace + "all" ||
                    item.Name == XmlSchemaNamespace + "choice")
                {
                    AddParticleElements(parent, item, documents, depth + 1);
                }
                else if (item.Name == XmlSchemaNamespace + "element")
                {
                    string reference = (string)item.Attribute("ref");
                    if (!string.IsNullOrEmpty(reference))
                    {
                        parent.Add(CreateGlobalElement(item, reference, documents, depth + 1));
                        continue;
                    }
                    string name = (string)item.Attribute("name");
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }
                    XElement schema = item.Ancestors(XmlSchemaNamespace + "schema").FirstOrDefault();
                    bool qualified = string.Equals((string)item.Attribute("form"), "qualified",
                                        StringComparison.Ordinal) ||
                                     (item.Attribute("form") == null &&
                                      string.Equals((string)schema?.Attribute("elementFormDefault"), "qualified",
                                          StringComparison.Ordinal));
                    string elementNamespace = qualified ? (string)schema?.Attribute("targetNamespace") ?? "" : "";
                    var element = new XElement(XName.Get(name, elementNamespace));
                    parent.Add(element);
                    string type = (string)item.Attribute("type");
                    if (!string.IsNullOrEmpty(type))
                    {
                        AddTypeChildren(element, item, type, documents, depth + 1);
                    }
                    else
                    {
                        AddInlineTypeChildren(element, item, documents, depth + 1);
                    }
                }
            }
        }

        private static XNamespace ResolveNamespace(XElement context, string reference)
        {
            int separator = reference.IndexOf(':');
            return separator < 0
                ? (string)context.Ancestors(XmlSchemaNamespace + "schema").FirstOrDefault()?
                    .Attribute("targetNamespace") ?? ""
                : context.GetNamespaceOfPrefix(reference.Substring(0, separator));
        }

        private static string LocalName(string qualifiedName)
        {
            int separator = qualifiedName.IndexOf(':');
            return separator < 0 ? qualifiedName : qualifiedName.Substring(separator + 1);
        }

        private sealed class MetadataDocument
        {
            public MetadataDocument(string location, XDocument xml)
            {
                Location = location;
                Xml = xml;
            }

            public string Location { get; }
            public XDocument Xml { get; }
        }
    }

    internal sealed class ServiceOperation
    {
        public ServiceOperation(string serviceName, string name, string endpoint, string action, bool soap12,
            string bodyXml)
        {
            ServiceName = serviceName;
            Name = name;
            Endpoint = endpoint;
            Action = action;
            Soap12 = soap12;
            BodyXml = bodyXml;
        }

        public string ServiceName { get; }
        public string Name { get; }
        public string Endpoint { get; }
        public string Action { get; }
        public bool Soap12 { get; }
        public string BodyXml { get; }

        public string CreateEnvelope()
        {
            XNamespace envelope = Soap12
                ? "http://www.w3.org/2003/05/soap-envelope"
                : "http://schemas.xmlsoap.org/soap/envelope/";
            XElement body = XDocument.Parse(BodyXml).Root.Element(envelope + "Body");
            return new XDocument(new XElement(envelope + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", envelope),
                new XElement(envelope + "Body", body.Elements()))).ToString();
        }

        public override string ToString()
        {
            return ServiceName + "." + Name;
        }
    }
}
