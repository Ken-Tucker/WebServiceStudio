using System.ComponentModel;
using System.Xml.Serialization;

namespace WebServiceStudio
{
    [TypeConverter(typeof (ExpandableObjectConverter))]
    public class WsdlProperties
    {
        [XmlAttribute]
        public string Domain { get; set; }

        [XmlAttribute]
        public string Password { get; set; }

        [XmlAttribute]
        public string ProxyDomain { get; set; }

        [XmlAttribute]
        public string ProxyPassword { get; set; }

        [XmlAttribute]
        public string ProxyServer { get; set; }

        [XmlAttribute]
        public string ProxyUserName { get; set; }

        [XmlAttribute]
        public string UserName { get; set; }

        public override string ToString()
        {
            return "";
        }
    }
}