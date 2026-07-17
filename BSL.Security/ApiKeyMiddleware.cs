using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BSL.Security
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HashSet<Guid> _allowedKeys = new HashSet<Guid>();

        public ApiKeyMiddleware(RequestDelegate next, string keysFilePath)
        {
            _next = next;
            _allowedKeys = new HashSet<Guid>();

            if (File.Exists(keysFilePath))
            {
                var jsonFile = File.ReadAllText(keysFilePath);
                if (!string.IsNullOrWhiteSpace(jsonFile))
                {
                    _allowedKeys = JsonSerializer.Deserialize<HashSet<Guid>>(jsonFile) ?? new HashSet<Guid>();
                }
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            if (Guid.TryParse(request.Query["api_key"], out var id))
            {
                if (!_allowedKeys.Contains(id))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
            }
            else if (Guid.TryParse(request.Headers["X-API-Key"], out id))
            {
                if (!_allowedKeys.Contains(id))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
            }
            else if (Guid.TryParse(request.Cookies["X-API-Key"], out id))
            {
                if (!_allowedKeys.Contains(id))
                {
                    context.Response.StatusCode = 401;
                    return;
                }
            }
            else
            {
                context.Response.StatusCode = 401;
                return;
            }

            await _next(context);
        }
    }
}
