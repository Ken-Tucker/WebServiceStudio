using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;

namespace WebServiceStudio
{
    internal class WsdlOperation
    {
        public string Name { get; set; }

        public string ServiceName { get; set; }

        public string Endpoint { get; set; }

        public string SoapAction { get; set; }

        public string TargetNamespace { get; set; }

        public bool IsSoap12 { get; set; }

        public List<XmlElement> RequestElements { get; } = new List<XmlElement>();
    }

    internal class WsdlOperationProperty : TreeNodeProperty
    {
        private readonly WsdlOperation operation;

        public WsdlOperationProperty(WsdlOperation operation)
            : base(new[] { typeof(XmlElement) }, operation.Name)
        {
            this.operation = operation;
        }

        protected override void CreateChildren()
        {
            TreeNode bodyNode = TreeNode.Nodes.Add("Body");
            foreach (XmlElement element in operation.RequestElements)
            {
                XmlElement copy = (XmlElement)element.CloneNode(true);
                TreeNodeProperty.CreateTreeNodeProperty(new[] { typeof(XmlElement) }, element.Name, copy)
                    .RecreateSubtree(bodyNode);
            }
            bodyNode.ExpandAll();
        }

        public XmlElement[] ReadRequestElements()
        {
            var elements = new List<XmlElement>();
            if (TreeNode.Nodes.Count == 0)
            {
                return elements.ToArray();
            }
            foreach (TreeNode node in TreeNode.Nodes[0].Nodes)
            {
                var property = node.Tag as TreeNodeProperty;
                if (property != null)
                {
                    var element = property.ReadChildren() as XmlElement;
                    if (element != null)
                    {
                        elements.Add(element);
                    }
                }
            }
            return elements.ToArray();
        }

        public WsdlOperation Operation
        {
            get { return operation; }
        }
    }
}
