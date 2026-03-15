namespace BSL.Models.Interface
{
    public interface IXmlService
    {
        void Import(Stream stream);
        Stream Export(Stream stream);
    }
}
