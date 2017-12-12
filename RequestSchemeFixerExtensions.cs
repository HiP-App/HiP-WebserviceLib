using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PaderbornUniversity.SILab.Hip.Webservice
{
    public static class RequestSchemeFixerExtensions
    {
        /// <summary>
        /// Fixes the current request's scheme if an "X-Forwarded-Proto"-header is set. This call should
        /// ideally be the first when configuring the request pipeline so that all other middlewares have
        /// access to the correct request scheme.
        /// </summary>
        /// <remarks>
        /// Root issue: Clients send requests to our services via HTTPS, but nginx forwards these requests
        /// to the Docker services via HTTP. In order for our services to know that the request was originally
        /// using HTTPS, nginx adds an HTTP header "X-Forwarded-Proto: https" to the requests. This method
        /// registers a middleware which detects this header and sets the <see cref="HttpRequest.Scheme"/>
        /// property of the current request to "https" accordingly.
        /// </remarks>
        public static IApplicationBuilder UseRequestSchemeFixer(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var xproto))
                    context.Request.Scheme = xproto;
                await next();
            });
        }
    }
}
