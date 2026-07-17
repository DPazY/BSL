using BSL.Security;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
namespace BSL.Test.Security;

public class ApiKeyMiddlewareTests
{
    private string _tempKeysFilePath;
    private Guid _validKey;

    [SetUp]
    public void Setup()
    {
        _validKey = Guid.NewGuid(); 
        _tempKeysFilePath = Path.GetTempFileName(); 

        var keys = new HashSet<Guid> { _validKey };
        File.WriteAllText(_tempKeysFilePath, JsonSerializer.Serialize(keys));
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_tempKeysFilePath))
        {
            File.Delete(_tempKeysFilePath);
        }
    }

    [Test]
    public async Task InvokeAsync_WithValidKeyInQuery_CallsNextDelegate()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString($"?api_key={_validKey}");

        bool nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiKeyMiddleware(next, _tempKeysFilePath);

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True, "Middleware должен был вызвать следующий делегат (next).");
        Assert.That(context.Response.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task InvokeAsync_WithInvalidKey_Returns401Unauthorized()
    {
       var context = new DefaultHttpContext();
        var invalidKey = Guid.NewGuid();
        context.Request.QueryString = new QueryString($"?api_key={invalidKey}");

        bool nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new ApiKeyMiddleware(next, _tempKeysFilePath);

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.False, "Middleware пропустил неверный ключ!");
        Assert.That(context.Response.StatusCode, Is.EqualTo(401));
    }

    [Test]
    public async Task InvokeAsync_WithoutKey_Returns401Unauthorized()
    {
        var context = new DefaultHttpContext();
        
        bool nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new ApiKeyMiddleware(next, _tempKeysFilePath);

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.False, "Middleware пропустил запрос без ключа!");
        Assert.That(context.Response.StatusCode, Is.EqualTo(401));
    }

    [Test]
    public async Task InvokeAsync_WithValidKeyInHeader_CallsNextDelegate()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = _validKey.ToString();

        bool nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

        var middleware = new ApiKeyMiddleware(next, _tempKeysFilePath);

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True, "Middleware не нашел валидный ключ в заголовках (Headers).");
        Assert.That(context.Response.StatusCode, Is.EqualTo(200));
    }
}