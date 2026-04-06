namespace BSL.Models.Interface
{
    public interface IXmlService
    {
        Task Import(Stream stream);
        Task<Stream> Export(Stream stream, IEnumerable<Book>? filteredBooks = null);
    }
}
