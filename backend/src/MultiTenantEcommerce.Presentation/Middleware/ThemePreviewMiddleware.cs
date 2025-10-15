using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Presentation.Middleware;

public class ThemePreviewMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _environment;
    private readonly IContentTypeProvider _contentTypeProvider = new FileExtensionContentTypeProvider();

    public ThemePreviewMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, IThemePreviewService previewService)
    {
        if (!context.Request.Path.StartsWithSegments("/theme-preview", out var remaining))
        {
            await _next(context);
            return;
        }

        var token = context.Request.Query["token"].ToString();
        if (!previewService.TryValidateToken(token, out var themeId, out var expiresAt))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or expired preview token.");
            return;
        }

        var segments = remaining.Value.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || !Guid.TryParse(segments[0], out var requestedThemeId) || requestedThemeId != themeId)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid theme preview request.");
            return;
        }

        var assetPath = segments.Length > 1
            ? string.Join('/', segments.Skip(1))
            : "index.html";

        var themeDirectory = Path.Combine(_environment.ContentRootPath, "themes", themeId.ToString(), "dist");
        var fileProvider = new PhysicalFileProvider(themeDirectory);
        var fileInfo = fileProvider.GetFileInfo(assetPath);

        if (!fileInfo.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Preview asset not found.");
            return;
        }

        if (!_contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        context.Response.Headers["Cache-Control"] = "no-store";
        context.Response.Headers["X-Preview-Expires"] = expiresAt.ToString("O", CultureInfo.InvariantCulture);
        context.Response.ContentType = contentType;

        await using var stream = fileInfo.CreateReadStream();
        await stream.CopyToAsync(context.Response.Body);
    }
}
