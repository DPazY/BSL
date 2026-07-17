using BSL.Models;
using BSL.Models.Interface;
using System.Net.Http.Headers;

public class WebXmlService : IXmlService
{
    private readonly IHttpClientFactory httpClientFactory;

    public WebXmlService(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<Stream> Export(Stream stream, IEnumerable<Book>? filteredBooks = null)
    {
        var client = httpClientFactory.CreateClient("BslWebClient");

        var response = await client.GetAsync("/dumps/full", HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode) 
        {
            throw new Exception($"Ошибка при скачивании дампа: {response.StatusCode}");
        }

        using var netStream = await response.Content.ReadAsStreamAsync();

        await netStream.CopyToAsync(stream);

        await stream.FlushAsync();

        Console.WriteLine($"[Debug] Скачано из сети и записано: {stream.Length} байт.");

        stream.Position = 0;
        return stream;
    }

    public async Task Import(Stream stream)
    {
        var client = httpClientFactory.CreateClient("BSL.Web Client");

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

        content.Add(fileContent, "formFile", "upload.xml");

        var response = await client.PostAsync("/books/import", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Ошибка сервера при импорте: {response.StatusCode} - {error}");
        }

        Console.WriteLine("[Web Client] Файл успешно отправлен на сервер!");
    }
}