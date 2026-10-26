using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Services.Description;
using System.Web.Services.Discovery;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace WebServiceStudio
{
    internal class Wsdl
    {
        private static readonly string duplicateSchema =
            "Warning: Ignoring duplicate schema description with TargetNamespace='{0}' from '{1}'.";

        private static readonly string duplicateService =
            "Warning: Ignoring duplicate service description with TargetNamespace='{0}' from '{1}'.";

        private readonly StringCollection paths = new StringCollection();
        private readonly StringCollection wsdls = new StringCollection();
        private readonly StringCollection xsds = new StringCollection();
        private readonly List<WsdlOperation> operations = new List<WsdlOperation>();
        private bool cancelled;

        public StringCollection Paths
        {
            get { return paths; }
        }

        public WsdlProperties WsdlProperties
        {
            get { return Configuration.MasterConfig.WsdlSettings; }
        }

        public StringCollection Wsdls
        {
            get { return wsdls; }
        }

        public StringCollection Xsds
        {
            get { return xsds; }
        }

        public IList<WsdlOperation> Operations
        {
            get { return operations; }
        }

        private void AddDocument(string path, object document, XmlSchemas schemas,
            ServiceDescriptionCollection descriptions)
        {
            var serviceDescription = document as ServiceDescription;
            if (serviceDescription != null)
            {
                if (descriptions[serviceDescription.TargetNamespace] == null)
                {
                    descriptions.Add(serviceDescription);
                    var w = new StringWriter();
                    var writer = new XmlTextWriter(w);
                    writer.Formatting = Formatting.Indented;
                    serviceDescription.Write(writer);
                    wsdls.Add(w.ToString());
                }
                else
                {
                    CheckPoint(MessageType.Warning,
                        string.Format(duplicateService, serviceDescription.TargetNamespace, path));
                }
            }
            else
            {
                var schema = document as XmlSchema;
                if (schema != null)
                {
                    if (schemas[schema.TargetNamespace] == null)
                    {
                        schemas.Add(schema);
                        var writer3 = new StringWriter();
                        var writer4 = new XmlTextWriter(writer3);
                        writer4.Formatting = Formatting.Indented;
                        schema.Write(writer4);
                        xsds.Add(writer3.ToString());
                    }
                    else
                    {
                        CheckPoint(MessageType.Warning,
                            string.Format(duplicateSchema, schema.TargetNamespace, path));
                    }
                }
            }
        }

        public void Cancel()
        {
            cancelled = true;
        }

        private void CheckPoint(MessageType status, string message)
        {
            if (!cancelled)
            {
                MainForm.ShowMessage(this, status, message);
            }
        }

        private DiscoveryClientProtocol CreateDiscoveryClient()
        {
            var protocol = new DiscoveryClientProtocol();
            protocol.AllowAutoRedirect = true;
            if ((WsdlProperties.UserName != null) && (WsdlProperties.UserName.Length != 0))
            {
                protocol.Credentials = new NetworkCredential(WsdlProperties.UserName, WsdlProperties.Password,
                    WsdlProperties.Domain);
            }
            else
            {
                protocol.Credentials = CredentialCache.DefaultCredentials;
            }
            if ((WsdlProperties.ProxyServer != null) && (WsdlProperties.ProxyServer.Length != 0))
            {
                IWebProxy proxy = null;
                proxy = new WebProxy(WsdlProperties.ProxyServer);
                proxy.Credentials = new NetworkCredential(WsdlProperties.ProxyUserName, WsdlProperties.ProxyPassword,
                    WsdlProperties.ProxyDomain);
                protocol.Proxy = proxy;
            }
            return protocol;
        }

        private static XmlSchema CreateFakeSoapEncodingSchema(string ns, string name)
        {
            var schema = new XmlSchema();
            schema.TargetNamespace = ns;
            var item = new XmlSchemaGroup();
            item.Name = "Array";
            var sequence = new XmlSchemaSequence();
            var any = new XmlSchemaAny();
            any.MinOccurs = 0M;
            any.MaxOccurs = 79228162514264337593543950335M;
            sequence.Items.Add(any);
            any.Namespace = "##any";
            any.ProcessContents = XmlSchemaContentProcessing.Lax;
            item.Particle = sequence;
            schema.Items.Add(item);
            var type = new XmlSchemaComplexType();
            type.Name = name;
            var ref2 = new XmlSchemaGroupRef();
            ref2.RefName = new XmlQualifiedName("Array", ns);
            type.Particle = ref2;
            var attribute = new XmlSchemaAttribute();
            attribute.RefName = new XmlQualifiedName("arrayType", ns);
            type.Attributes.Add(attribute);
            schema.Items.Add(type);
            attribute = new XmlSchemaAttribute();
            attribute.Use = XmlSchemaUse.None;
            attribute.Name = "arrayType";
            schema.Items.Add(attribute);
            AddSimpleType(schema, "base64", "base64Binary");
            AddElementAndType(schema, "anyURI", ns);
            AddElementAndType(schema, "base64Binary", ns);
            AddElementAndType(schema, "boolean", ns);
            AddElementAndType(schema, "byte", ns);
            AddElementAndType(schema, "date", ns);
            AddElementAndType(schema, "dateTime", ns);
            AddElementAndType(schema, "decimal", ns);
            AddElementAndType(schema, "double", ns);
            AddElementAndType(schema, "duration", ns);
            AddElementAndType(schema, "ENTITIES", ns);
            AddElementAndType(schema, "ENTITY", ns);
            AddElementAndType(schema, "float", ns);
            AddElementAndType(schema, "gDay", ns);
            AddElementAndType(schema, "gMonth", ns);
            AddElementAndType(schema, "gMonthDay", ns);
            AddElementAndType(schema, "gYear", ns);
            AddElementAndType(schema, "gYearMonth", ns);
            AddElementAndType(schema, "hexBinary", ns);
            AddElementAndType(schema, "ID", ns);
            AddElementAndType(schema, "IDREF", ns);
            AddElementAndType(schema, "IDREFS", ns);
            AddElementAndType(schema, "int", ns);
            AddElementAndType(schema, "integer", ns);
            AddElementAndType(schema, "language", ns);
            AddElementAndType(schema, "long", ns);
            AddElementAndType(schema, "Name", ns);
            AddElementAndType(schema, "NCName", ns);
            AddElementAndType(schema, "negativeInteger", ns);
            AddElementAndType(schema, "NMTOKEN", ns);
            AddElementAndType(schema, "NMTOKENS", ns);
            AddElementAndType(schema, "nonNegativeInteger", ns);
            AddElementAndType(schema, "nonPositiveInteger", ns);
            AddElementAndType(schema, "normalizedString", ns);
            AddElementAndType(schema, "positiveInteger", ns);
            AddElementAndType(schema, "QName", ns);
            AddElementAndType(schema, "short", ns);
            AddElementAndType(schema, "string", ns);
            AddElementAndType(schema, "time", ns);
            AddElementAndType(schema, "token", ns);
            AddElementAndType(schema, "unsignedByte", ns);
            AddElementAndType(schema, "unsignedInt", ns);
            AddElementAndType(schema, "unsignedLong", ns);
            AddElementAndType(schema, "unsignedShort", ns);
            return schema;
        }

        private static XmlSchema CreateFakeWsdlSchema(string ns)
        {
            var schema = new XmlSchema();
            schema.TargetNamespace = ns;
            var item = new XmlSchemaAttribute();
            item.Use = XmlSchemaUse.None;
            item.Name = "arrayType";
            item.SchemaTypeName = new XmlQualifiedName("QName", "http://www.w3.org/2001/XMLSchema");
            schema.Items.Add(item);
            return schema;
        }

        private static XmlSchema CreateFakeXsdSchema(string ns, string name)
        {
            var schema = new XmlSchema();
            schema.TargetNamespace = ns;
            var item = new XmlSchemaElement();
            item.Name = name;
            var type = new XmlSchemaComplexType();
            item.SchemaType = type;
            schema.Items.Add(item);
            return schema;
        }

        private static bool FileExists(string path)
        {
            bool flag = false;
            if ((path == null) || (path.Length == 0))
            {
                return false;
            }
            try
            {
                flag = (path.LastIndexOf('?') == -1) && File.Exists(path);
            }
            catch
            {
            }
            return flag;
        }

        public void Generate()
        {
            CheckPoint(MessageType.Begin, "Initializing");
            var descriptions = new ServiceDescriptionCollection();
            var schemas = new XmlSchemas();
            var urls = new StringCollection();
            var localPaths = new StringCollection();
            GetPaths(localPaths, urls);
            operations.Clear();
            wsdls.Clear();
            xsds.Clear();
            CheckPoint(MessageType.Begin, "Reading WSDL");
            try
            {
                DiscoveryClientProtocol client = CreateDiscoveryClient();
                ProcessLocalPaths(client, localPaths, schemas, descriptions);
                ProcessRemoteUrls(client, urls, schemas, descriptions);
            }
            catch (Exception exception)
            {
                CheckPoint(MessageType.Failure, exception.ToString());
                return;
            }
            try
            {
                BuildOperations(descriptions, schemas);
                if (operations.Count == 0)
                {
                    CheckPoint(MessageType.Failure, "No SOAP operations were found in the WSDL.");
                    return;
                }
                CheckPoint(MessageType.Success, "Read " + operations.Count + " SOAP operation(s)");
            }
            catch (Exception exception2)
            {
                CheckPoint(MessageType.Failure, exception2.ToString());
            }
        }

        private void BuildOperations(ServiceDescriptionCollection descriptions, XmlSchemas schemas)
        {
            var schemaSet = new XmlSchemaSet();
            schemaSet.ValidationEventHandler += (sender, args) =>
                CheckPoint(MessageType.Warning, "Schema parsing warning: " + args.Message);
            foreach (XmlSchema schema in schemas)
            {
                schemaSet.Add(schema);
            }
            foreach (ServiceDescription description in descriptions)
            {
                foreach (XmlSchema schema in description.Types.Schemas)
                {
                    schemaSet.Add(schema);
                }
            }
            try
            {
                schemaSet.Compile();
            }
            catch (Exception exception)
            {
                CheckPoint(MessageType.Warning, "Some XML schema types could not be resolved: " + exception.Message);
            }

            foreach (ServiceDescription serviceDescription in descriptions)
            {
                foreach (Service service in serviceDescription.Services)
                {
                    foreach (Port port in service.Ports)
                    {
                        Binding binding = FindBinding(descriptions, port.Binding);
                        if (binding == null)
                        {
                            continue;
                        }
                        SoapBinding soapBinding = FindExtension<SoapBinding>(binding.Extensions);
                        Soap12Binding soap12Binding = FindExtension<Soap12Binding>(binding.Extensions);
                        if (soapBinding == null && soap12Binding == null)
                        {
                            continue;
                        }
                        bool isSoap12 = soap12Binding != null;
                        PortType portType = FindPortType(descriptions, binding.Type);
                        if (portType == null)
                        {
                            continue;
                        }
                        string endpoint = GetEndpoint(port, isSoap12);
                        foreach (OperationBinding operationBinding in binding.Operations)
                        {
                            Operation portOperation = FindOperation(portType, operationBinding.Name);
                            if (portOperation == null || portOperation.Messages.Input == null ||
                                operationBinding.Input == null)
                            {
                                continue;
                            }
                            OperationInput operationInput = portOperation.Messages.Input;
                            Message message = FindMessage(descriptions, operationInput.Message);
                            if (message == null)
                            {
                                continue;
                            }
                            SoapBodyBinding bodyBinding = FindExtension<SoapBodyBinding>(operationBinding.Input.Extensions);
                            Soap12BodyBinding body12Binding = FindExtension<Soap12BodyBinding>(operationBinding.Input.Extensions);
                            string[] selectedParts = bodyBinding != null ? bodyBinding.Parts :
                                (body12Binding != null ? body12Binding.Parts : null);
                            string style = GetOperationStyle(operationBinding, soapBinding, soap12Binding);
                            string bodyNamespace = bodyBinding != null ? bodyBinding.Namespace :
                                (body12Binding != null ? body12Binding.Namespace : binding.Type.Namespace);
                            var operation = new WsdlOperation
                            {
                                Name = operationBinding.Name,
                                ServiceName = service.Name,
                                Endpoint = endpoint,
                                SoapAction = GetSoapAction(operationBinding),
                                TargetNamespace = bodyNamespace,
                                IsSoap12 = isSoap12
                            };

                            XmlElement rpcElement = null;
                            if (string.Equals(style, "Rpc", StringComparison.OrdinalIgnoreCase))
                            {
                                var document = new XmlDocument();
                                rpcElement = document.CreateElement(operation.Name, bodyNamespace ?? "");
                            }
                            foreach (MessagePart part in message.Parts)
                            {
                                if (selectedParts != null && selectedParts.Length > 0 &&
                                    Array.IndexOf(selectedParts, part.Name) < 0)
                                {
                                    continue;
                                }
                                XmlElement requestElement = CreateRequestElement(part, schemaSet, bodyNamespace);
                                if (requestElement != null)
                                {
                                    if (rpcElement != null)
                                    {
                                        rpcElement.AppendChild(rpcElement.OwnerDocument.ImportNode(requestElement, true));
                                    }
                                    else
                                    {
                                        operation.RequestElements.Add(requestElement);
                                    }
                                }
                            }
                            if (rpcElement != null)
                            {
                                operation.RequestElements.Add(rpcElement);
                            }
                            operations.Add(operation);
                        }
                    }
                }
            }
        }

        private static T FindExtension<T>(ServiceDescriptionFormatExtensionCollection extensions) where T : class
        {
            foreach (object extension in extensions)
            {
                T result = extension as T;
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private static Operation FindOperation(PortType portType, string name)
        {
            foreach (Operation operation in portType.Operations)
            {
                if (operation.Name == name)
                {
                    return operation;
                }
            }
            return null;
        }

        private static Binding FindBinding(ServiceDescriptionCollection descriptions, XmlQualifiedName name)
        {
            foreach (ServiceDescription description in descriptions)
            {
                foreach (Binding binding in description.Bindings)
                {
                    if (binding.Name == name.Name && description.TargetNamespace == name.Namespace)
                    {
                        return binding;
                    }
                }
            }
            return null;
        }

        private static PortType FindPortType(ServiceDescriptionCollection descriptions, XmlQualifiedName name)
        {
            foreach (ServiceDescription description in descriptions)
            {
                foreach (PortType portType in description.PortTypes)
                {
                    if (portType.Name == name.Name && description.TargetNamespace == name.Namespace)
                    {
                        return portType;
                    }
                }
            }
            return null;
        }

        private static Message FindMessage(ServiceDescriptionCollection descriptions, XmlQualifiedName name)
        {
            foreach (ServiceDescription description in descriptions)
            {
                foreach (Message message in description.Messages)
                {
                    if (message.Name == name.Name && description.TargetNamespace == name.Namespace)
                    {
                        return message;
                    }
                }
            }
            return null;
        }

        private static string GetEndpoint(Port port, bool isSoap12)
        {
            foreach (object extension in port.Extensions)
            {
                var address11 = extension as SoapAddressBinding;
                if (address11 != null && !isSoap12)
                {
                    return address11.Location;
                }
                var address12 = extension as Soap12AddressBinding;
                if (address12 != null && isSoap12)
                {
                    return address12.Location;
                }
            }
            return null;
        }

        private static string GetSoapAction(OperationBinding operation)
        {
            foreach (object extension in operation.Extensions)
            {
                var soap = extension as SoapOperationBinding;
                if (soap != null)
                {
                    return soap.SoapAction;
                }
                var soap12 = extension as Soap12OperationBinding;
                if (soap12 != null)
                {
                    return soap12.SoapAction;
                }
            }
            return "";
        }

        private static string GetOperationStyle(OperationBinding operation, SoapBinding soap, Soap12Binding soap12)
        {
            foreach (object extension in operation.Extensions)
            {
                var soapOperation = extension as SoapOperationBinding;
                if (soapOperation != null && soapOperation.Style != SoapBindingStyle.Default)
                {
                    return soapOperation.Style.ToString();
                }
                var soap12Operation = extension as Soap12OperationBinding;
                if (soap12Operation != null && soap12Operation.Style != SoapBindingStyle.Default)
                {
                    return soap12Operation.Style.ToString();
                }
            }
            if (soap12 != null && soap12.Style != SoapBindingStyle.Default)
            {
                return soap12.Style.ToString();
            }
            return soap != null ? soap.Style.ToString() : "Document";
        }

        private static XmlElement CreateRequestElement(MessagePart part, XmlSchemaSet schemas, string targetNamespace)
        {
            var document = new XmlDocument();
            if (!part.Element.IsEmpty)
            {
                var declaration = schemas.GlobalElements[part.Element] as XmlSchemaElement;
                return declaration == null ? document.CreateElement(part.Element.Name, part.Element.Namespace) :
                    CreateElement(document, declaration, schemas, 0);
            }

            XmlElement element = document.CreateElement(part.Name, targetNamespace ?? "");
            XmlSchemaType type = schemas.GlobalTypes[part.Type] as XmlSchemaType;
            if (type != null)
            {
                AppendTypeContent(document, element, type, schemas, 0);
            }
            return element;
        }

        private static XmlElement CreateElement(XmlDocument document, XmlSchemaElement declaration,
            XmlSchemaSet schemas, int depth)
        {
            if (depth > 12)
            {
                return document.CreateElement(declaration.Name ?? "Item");
            }
            if (!declaration.RefName.IsEmpty)
            {
                declaration = schemas.GlobalElements[declaration.RefName] as XmlSchemaElement ?? declaration;
            }
            string name = declaration.Name ?? declaration.RefName.Name ?? "Item";
            string ns = declaration.QualifiedName.Namespace ?? "";
            XmlElement element = document.CreateElement(name, ns);
            XmlSchemaType type = declaration.ElementSchemaType;
            if (type != null)
            {
                AppendTypeContent(document, element, type, schemas, depth + 1);
            }
            return element;
        }

        private static void AppendTypeContent(XmlDocument document, XmlElement element, XmlSchemaType type,
            XmlSchemaSet schemas, int depth)
        {
            var complexType = type as XmlSchemaComplexType;
            if (complexType == null || depth > 12)
            {
                return;
            }
            XmlSchemaParticle particle = complexType.ContentTypeParticle;
            AppendParticle(document, element, particle, schemas, depth + 1);
            foreach (XmlSchemaAttribute attribute in complexType.Attributes)
            {
                if (!string.IsNullOrEmpty(attribute.Name) && attribute.Use == XmlSchemaUse.Required)
                {
                    element.SetAttribute(attribute.Name, "");
                }
            }
        }

        private static void AppendParticle(XmlDocument document, XmlElement parent, XmlSchemaParticle particle,
            XmlSchemaSet schemas, int depth)
        {
            if (particle == null || depth > 12)
            {
                return;
            }
            var element = particle as XmlSchemaElement;
            if (element != null)
            {
                parent.AppendChild(CreateElement(document, element, schemas, depth));
                return;
            }
            var group = particle as XmlSchemaGroupBase;
            if (group != null)
            {
                int count = group is XmlSchemaChoice ? Math.Min(1, group.Items.Count) : group.Items.Count;
                for (int i = 0; i < count; i++)
                {
                    AppendParticle(document, parent, group.Items[i] as XmlSchemaParticle, schemas, depth + 1);
                }
            }
        }

        private void GetPaths(StringCollection localPaths, StringCollection urls)
        {
            StringEnumerator enumerator = paths.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string current = enumerator.Current;
                if (FileExists(current))
                {
                    if (!Path.HasExtension(current))
                    {
                        throw new InvalidOperationException(current + " has no extensions");
                    }
                    localPaths.Add(current);
                }
                else
                {
                    Uri uri = null;
                    try
                    {
                        uri = new Uri(current, true);
                    }
                    catch (Exception)
                    {
                        uri = null;
                    }
                    if (uri == null)
                    {
                        throw new InvalidOperationException(current + " is invalid URI");
                    }
                    urls.Add(uri.AbsoluteUri);
                }
            }
        }

        private void ProcessLocalPaths(DiscoveryClientProtocol client, StringCollection localPaths, XmlSchemas schemas,
            ServiceDescriptionCollection descriptions)
        {
            StringEnumerator enumerator = localPaths.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string current = enumerator.Current;
                string extension = Path.GetExtension(current);
                if (string.Compare(extension, ".discomap", true) == 0)
                {
                    client.ReadAll(current);
                }
                else
                {
                    object document = null;
                    if (string.Compare(extension, ".wsdl", true) == 0)
                    {
                        document = ReadLocalDocument(false, current);
                    }
                    else
                    {
                        if (string.Compare(extension, ".xsd", true) != 0)
                        {
                            throw new InvalidOperationException("Unknown file type " + current);
                        }
                        document = ReadLocalDocument(true, current);
                    }
                    if (document != null)
                    {
                        AddDocument(current, document, schemas, descriptions);
                    }
                }
            }
        }

        private void ProcessRemoteUrls(DiscoveryClientProtocol client, StringCollection urls, XmlSchemas schemas,
            ServiceDescriptionCollection descriptions)
        {
            StringEnumerator enumerator = urls.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string current = enumerator.Current;
                try
                {
                    _ = client.DiscoverAny(current);
                    client.ResolveAll();
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("General Error " + current, exception);
                }
            }
            IDictionaryEnumerator enumerator2 = client.Documents.GetEnumerator();
            while (enumerator2.MoveNext())
            {
                var entry = (DictionaryEntry)enumerator2.Current;
                AddDocument((string)entry.Key, entry.Value, schemas, descriptions);
            }
        }

        private object ReadLocalDocument(bool isSchema, string path)
        {
            object obj2 = null;
            StreamReader input = null;
            try
            {
                input = new StreamReader(path);
                if (isSchema)
                {
                    return XmlSchema.Read(new XmlTextReader(input), null);
                }
                obj2 = ServiceDescription.Read(input.BaseStream);
            }
            catch
            {
                throw;
            }
            finally
            {
                if (input != null)
                {
                    input.Close();
                }
            }
            return obj2;
        }

        public static XmlSchema ReadSchema(string filename)
        {
            var reader = new XmlTextReader(filename);
            if (reader.IsStartElement("schema", "http://www.w3.org/2001/XMLSchema"))
            {
                return XmlSchema.Read(reader, null);
            }
            return null;
        }

        public void Reset()
        {
            paths.Clear();
            wsdls.Clear();
            xsds.Clear();
            operations.Clear();
            cancelled = false;
        }

        private void ValidationCallbackWithErrorCode(object sender, ValidationEventArgs args)
        {
            CheckPoint(MessageType.Warning, "Schema parsing error " + args.Message);
        }

        internal class Namespace
        {
            public const string SoapEncoding = "http://schemas.xmlsoap.org/soap/encoding/";
            public const string Wsdl = "http://schemas.xmlsoap.org/wsdl/";
        }
    }
}