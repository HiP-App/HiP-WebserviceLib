using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using NJsonSchema;
using NSwag.AspNetCore.Middlewares;
using NSwag.SwaggerGeneration;
using NSwag.SwaggerGeneration.Processors.Security;
using NSwag.SwaggerGeneration.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

// ReSharper disable All
namespace NSwag.AspNetCore
{
    /// <summary>
    /// This class defines an extension method "UseSwaggerUiHip" which works around two
    /// (temporary) issues in NSwag's "UseSwaggerUi"-method.
    /// </summary>
    /// <remarks>
    /// NSwag expects users to navigate to "foo.com/swagger" which redirects to something like
    /// "foo.com/swagger/index.html?url=/swagger/v1/swagger.json". 
    /// 
    /// Problems which we work around:
    /// 1) The redirection doesn't work correctly with our nginx configuration:
    ///    Navigating to "docker-hip.cs.upb.de/develop/datastore/swagger"
    ///    redirects to  "docker-hip.cs.upb.de/swagger/index.html", eliminating the middle part of the URL
    ///    
    /// 2) We are used to navigating directly to ".../swagger/index.html". In this case however, the URL-box on the
    ///    page will point to a sample API (Swagger Petstore sample) rather than our service's "swagger.json"-file.
    /// 
    /// The code in this file is mostly copy-pasted from the NSwag repository with some minor modifications.
    /// </remarks>
    public static class NSwagExtensions
    {
        /// <summary>
        /// Integrates Swagger and Swagger UI into the pipeline using some HiP-related customizations.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="webApiAssembly">
        /// The assembly to be analyzed to generate the Swagger specification. If null (default value), the
        /// entry assembly of the current process is used which should be the desired assembly in most cases.
        /// </param>
        /// <param name="configureSettings">
        /// A callback to configure Swagger and Swagger UI. By default, the following settings are applied:
        /// (1) 'Title' is set to the name of the assembly (<paramref name="webApiAssembly"/>),
        /// (2) 'DefaultEnumHandling' is set to 'String',
        /// (3) 'DocExpansion' is set to "list",
        /// (4) A parameter for the HTTP "Authorization"-header is added to each operation which needs
        ///     authorization. The parameter is marked as 'required' unless the operation supports anonymous
        ///     access (via [AllowAnonymous]-attribute).
        /// (5) The repository's "README.md"-file is included as the API description.
        /// </param>
        /// <returns></returns>
        public static IApplicationBuilder UseSwaggerUiHip(this IApplicationBuilder app, Assembly webApiAssembly = null, Action<SwaggerUiSettings> configureSettings = null)
        {
            webApiAssembly = webApiAssembly ?? Assembly.GetEntryAssembly();

            // Create Swagger and Swagger UI configuration
            var mvcRouting = app.ApplicationServices.GetService<IActionDescriptorCollectionProvider>();
            var settings = CreateDefaultSwaggerUiSettings(webApiAssembly, mvcRouting);
            configureSettings?.Invoke(settings);

            // Set up middlewares (code taken from NSwag)
            var controllerTypes = WebApiToSwaggerGenerator.GetControllerClasses(webApiAssembly);
            var schemaGenerator = new SwaggerJsonSchemaGenerator(settings);
            var actualSwaggerRoute = settings.SwaggerRoute.Substring(settings.MiddlewareBasePath?.Length ?? 0);
            var actualSwaggerUiRoute = settings.SwaggerUiRoute.Substring(settings.MiddlewareBasePath?.Length ?? 0);

            if (controllerTypes != null)
                app.UseMiddleware<SwaggerMiddleware>(actualSwaggerRoute, controllerTypes, settings, schemaGenerator);

            app.UseMiddleware<RedirectMiddleware>(actualSwaggerUiRoute, actualSwaggerRoute);
            app.UseMiddleware<SwaggerUiIndexMiddleware>(actualSwaggerUiRoute + "/index.html", settings, "NSwag.AspNetCore.SwaggerUi.index.html");
            app.UseFileServer(new FileServerOptions
            {
                RequestPath = new PathString(actualSwaggerUiRoute),
                FileProvider = new EmbeddedFileProvider(typeof(SwaggerExtensions).GetTypeInfo().Assembly, "NSwag.AspNetCore.SwaggerUi")
            });

            return app;
        }

        private static SwaggerUiSettings CreateDefaultSwaggerUiSettings(Assembly webApiAssembly, IActionDescriptorCollectionProvider mvcRouting)
        {
            return new SwaggerUiSettings
            {
                Title = webApiAssembly.GetName().Name,
                DefaultEnumHandling = EnumHandling.String,
                DocExpansion = "list",
                PostProcess = doc =>
                {
                    // Add a parameter for the "Authorization"-header to every operation which needs
                    // authorization. If there's an [AllowAnonymous]-attribute, make the parameter optional.
                    foreach (var op in doc.Operations)
                    {
                        // Find the MVC controller action corresponding to the current Swagger operation
                        var mvcAction = mvcRouting.ActionDescriptors.Items
                            .OfType<ControllerActionDescriptor>()
                            .FirstOrDefault(a =>
                            {
                                var constraint = a.ActionConstraints
                                    .OfType<HttpMethodActionConstraint>()
                                    .FirstOrDefault();

                                var actionName = a.ActionName.EndsWith("Async")
                                    ? a.ActionName.Substring(0, a.ActionName.Length - "Async".Length)
                                    : a.ActionName;

                                return
                                    $"{a.ControllerName}_{actionName}" == op.Operation.OperationId &&
                                    constraint?.HttpMethods != null &&
                                    constraint.HttpMethods.Contains(op.Method.ToString().ToUpper());
                            });

                        if (mvcAction != null)
                        {
                            var needsAuth = mvcAction.FilterDescriptors.Any(f => f.Filter is AuthorizeFilter);
                            var allowAnonymous = mvcAction.FilterDescriptors.Any(f => f.Filter is AllowAnonymousFilter);

                            if (needsAuth && !allowAnonymous)
                            {
                                var requirement = new SwaggerSecurityRequirement() { { "Bearer", Enumerable.Empty<string>() } };

                                if (op.Operation.Security == null)
                                {
                                    op.Operation.Security = new List<SwaggerSecurityRequirement>() { requirement };
                                }
                                else
                                {
                                    op.Operation.Security.Add(requirement);
                                }
                            }
                        }
                    }

                    // If available, include the repository's "README.md" file as the API description
                    var readmeFile = new[] { "README.md", "../README.md" }
                            .FirstOrDefault(file => File.Exists(file));

                    if (readmeFile != null)
                    {
                        try
                        {
                            doc.Info.Description = File.ReadAllText(readmeFile);
                        }
                        catch
                        { // Couldn't read "README.md"-file
                        }
                    }

                },
                DocumentProcessors =
                {
                    new SecurityDefinitionAppender("Bearer", new SwaggerSecurityScheme()
                    {
                        In = SwaggerSecurityApiKeyLocation.Header,
                        Type = SwaggerSecuritySchemeType.ApiKey,
                        Name= "Authorization",
                        Description = "Please insert JWT with Bearer into field"
                    })
                }
            };
        }

        /// <summary>
        /// Workaround 1): Redirect to the correct ".../swagger/index.html"-URL when ".../swagger" is requested.
        /// </summary>
        internal class RedirectMiddleware
        {
            private readonly RequestDelegate _nextDelegate;
            private readonly string _fromPath;
            private readonly string _swaggerPath;

            public RedirectMiddleware(RequestDelegate nextDelegate, string fromPath, string swaggerPath)
            {
                _nextDelegate = nextDelegate;
                _fromPath = fromPath;
                _swaggerPath = swaggerPath;
            }

            public async Task Invoke(HttpContext context)
            {
                if (context.Request.Path.HasValue &&
                    string.Equals(context.Request.Path.Value.Trim('/'), _fromPath.Trim('/'), StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 302;
                    var targetUrl = context.Request.GetEncodedUrl().TrimEnd('/') + "/index.html";
                    context.Response.Headers.Add("Location", targetUrl);
                }
                else
                {
                    await _nextDelegate.Invoke(context);
                }
            }
        }

        /// <summary>
        /// Workaround 2): Replace the default sample URL (Swagger Petstore) with the correct URL to our service
        /// when serving the Swagger UI page.
        /// </summary>
        internal class SwaggerUiIndexMiddleware
        {
            private readonly RequestDelegate _nextDelegate;
            private readonly string _indexPath;
            private readonly SwaggerUiSettingsBase _settings;
            private readonly string _resourcePath;

            public SwaggerUiIndexMiddleware(RequestDelegate nextDelegate, string indexPath, SwaggerUiSettingsBase settings, string resourcePath)
            {
                _nextDelegate = nextDelegate;
                _indexPath = indexPath;
                _settings = settings;
                _resourcePath = resourcePath;
            }

            public async Task Invoke(HttpContext context)
            {
                if (context.Request.Path.HasValue && context.Request.Path.Value.Trim('/').StartsWith(_indexPath.Trim('/'), StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";
                    context.Response.StatusCode = 200;

                    var html = _htmlTemplate;

                    var swaggerUrl = context.Request.GetEncodedUrl()
                        .TrimEnd('/')
                        .Replace("/index.html", "") + "/v1/swagger.json";

                    var transformHtmlMethod = _settings.GetType().GetMethod("TransformHtml", BindingFlags.Instance | BindingFlags.NonPublic);
                    html = (string)transformHtmlMethod.Invoke(_settings, new[] { html });

                    // Here's the important part:
                    html = html.Replace("http://petstore.swagger.io/v2/swagger.json", swaggerUrl);

                    await context.Response.WriteAsync(html);
                }
                else
                {
                    await _nextDelegate(context);
                }
            }

            private static readonly string _htmlTemplate = @"<!DOCTYPE html>
<html>
<head>
  <meta charset=""UTF-8"">
  <title>Swagger UI</title>
  <link rel=""icon"" type=""image/png"" href=""images/favicon-32x32.png"" sizes=""32x32"" />
  <link rel=""icon"" type=""image/png"" href=""images/favicon-16x16.png"" sizes=""16x16"" />
  <link href='css/typography.css' media='screen' rel='stylesheet' type='text/css'/>
  <link href='css/reset.css' media='screen' rel='stylesheet' type='text/css'/>
  <link href='css/screen.css' media='screen' rel='stylesheet' type='text/css'/>
  <link href='css/reset.css' media='print' rel='stylesheet' type='text/css'/>
  <link href='css/print.css' media='print' rel='stylesheet' type='text/css'/>

  <script src='lib/object-assign-pollyfill.js' type='text/javascript'></script>
  <script src='lib/jquery-1.8.0.min.js' type='text/javascript'></script>
  <script src='lib/jquery.slideto.min.js' type='text/javascript'></script>
  <script src='lib/jquery.wiggle.min.js' type='text/javascript'></script>
  <script src='lib/jquery.ba-bbq.min.js' type='text/javascript'></script>
  <script src='lib/handlebars-4.0.5.js' type='text/javascript'></script>
  <script src='lib/lodash.min.js' type='text/javascript'></script>
  <script src='lib/backbone-min.js' type='text/javascript'></script>
  <script src='swagger-ui.js' type='text/javascript'></script>
  <script src='lib/highlight.9.1.0.pack.js' type='text/javascript'></script>
  <script src='lib/highlight.9.1.0.pack_extended.js' type='text/javascript'></script>
  <script src='lib/jsoneditor.min.js' type='text/javascript'></script>
  <script src='lib/marked.js' type='text/javascript'></script>
  <script src='lib/swagger-oauth.js' type='text/javascript'></script>

  <!-- Some basic translations -->
  <!-- <script src='lang/translator.js' type='text/javascript'></script> -->
  <!-- <script src='lang/ru.js' type='text/javascript'></script> -->
  <!-- <script src='lang/en.js' type='text/javascript'></script> -->

  <script type=""text/javascript"">
    $(function () {
      var url = window.location.search.match(/url=([^&]+)/);
      if (url && url.length > 1) {
        url = decodeURIComponent(url[1]);
      } else {
        url = ""http://petstore.swagger.io/v2/swagger.json"";
      }

      hljs.configure({
        highlightSizeThreshold: 5000
      });

      // Pre load translate...
      if(window.SwaggerTranslator) {
        window.SwaggerTranslator.translate();
      }
      window.swaggerUi = new SwaggerUi({
        url: url,
        dom_id: ""swagger-ui-container"",
        supportedSubmitMethods: {SupportedSubmitMethods},
        validatorUrl: {ValidatorUrl}, 
        onComplete: function(swaggerApi, swaggerUi){
          if(typeof initOAuth == ""function"") {
            initOAuth({
              clientId: ""{ClientId}"",
              clientSecret: ""{ClientSecret}"",
              realm: ""{Realm}"",
              appName: ""{AppName}"",
              scopeSeparator: ""{ScopeSeparator}"",
              additionalQueryStringParams: {AdditionalQueryStringParameters}
            });
          }

          if(window.SwaggerTranslator) {
            window.SwaggerTranslator.translate();
          }
        },
        onFailure: function(data) {
          log(""Unable to Load SwaggerUI"");
        },
        docExpansion: ""{DocExpansion}"",
        jsonEditor: {UseJsonEditor},
        defaultModelRendering: ""{DefaultModelRendering}"",
        showRequestHeaders: {ShowRequestHeaders}
      });

      window.swaggerUi.load();

      function log() {
        if ('console' in window) {
          console.log.apply(console, arguments);
        }
      }
  });
  </script>
</head>

<body class=""swagger-section"">
<div id='header'>
  <div class=""swagger-ui-wrap"">
    <a id=""logo"" href=""http://swagger.io""><img class=""logo__img"" alt=""swagger"" height=""30"" width=""30"" src=""images/logo_small.png"" /><span class=""logo__title"">swagger</span></a>
    <form id='api_selector'>
      <div class='input'><input placeholder=""http://example.com/api"" id=""input_baseUrl"" name=""baseUrl"" type=""text""/></div>
      <div id='auth_container'></div>
      <div class='input'><a id=""explore"" class=""header__btn"" href=""#"" data-sw-translate>Explore</a></div>
    </form>
  </div>
</div>

<div id=""message-bar"" class=""swagger-ui-wrap"" data-sw-translate>&nbsp;</div>
<div id=""swagger-ui-container"" class=""swagger-ui-wrap""></div>
</body>
</html>";
        }
    }
}
