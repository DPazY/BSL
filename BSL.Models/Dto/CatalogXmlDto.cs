using System.Xml.Serialization;
namespace BSL.Models.Dto
{
    [XmlRoot("catalog", Namespace = "http://library.by/catalog")]
    public class CatalogXmlDto
    {
        [XmlAttribute("date")]
        public string Date { get; set; }

        [XmlElement("book")]
        public List<BookXmlDto> Books { get; set; }
    }
}