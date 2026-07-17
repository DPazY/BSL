using Microsoft.AspNetCore.Builder;

namespace BSL.Security
{
    public static class ApiKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKey(this IApplicationBuilder builder, string keysFilePath)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>(keysFilePath);
        }
    }
}