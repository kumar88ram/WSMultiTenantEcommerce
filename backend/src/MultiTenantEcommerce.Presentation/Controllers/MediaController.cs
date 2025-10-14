using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Media;

namespace MultiTenantEcommerce.Presentation.Controllers;

[ApiController]
[Route("api/media")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpPost("images")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<MediaUploadResult>> UploadImage([FromForm] UploadImageRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("An image file must be provided.");
        }

        var resizeDefinitions = ParseResizeDefinitions(request.Resizes);

        await using var stream = request.File.OpenReadStream();
        var result = await _mediaService.UploadImageAsync(
            stream,
            request.File.FileName,
            request.File.ContentType,
            resizeDefinitions,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("signed-url")]
    public async Task<ActionResult<SignedUrlResult>> GetSignedUrl([FromQuery] SignedUrlRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
        {
            return BadRequest("A file path must be provided.");
        }

        var expires = request.ExpiresInSeconds > 0
            ? TimeSpan.FromSeconds(request.ExpiresInSeconds)
            : TimeSpan.FromMinutes(5);

        var result = await _mediaService.GetSignedUrlAsync(request.Path, expires, request.Adapter, cancellationToken);
        return Ok(result);
    }

    private static IReadOnlyCollection<ImageResizeDefinition> ParseResizeDefinitions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<ImageResizeDefinition>();
        }

        var definitions = new List<ImageResizeDefinition>();
        var entries = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var entry in entries)
        {
            var nameAndSize = entry.Split(':', 2, StringSplitOptions.TrimEntries);
            string sizePart;
            string name;

            if (nameAndSize.Length == 2)
            {
                name = nameAndSize[0];
                sizePart = nameAndSize[1];
            }
            else
            {
                name = $"size{definitions.Count + 1}";
                sizePart = nameAndSize[0];
            }

            var dimensions = sizePart.Split('x', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (dimensions.Length != 2)
            {
                continue;
            }

            if (int.TryParse(dimensions[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) &&
                int.TryParse(dimensions[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
            {
                definitions.Add(new ImageResizeDefinition(name, width, height));
            }
        }

        return definitions;
    }

    public sealed class UploadImageRequest
    {
        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }

        [FromForm(Name = "resizes")]
        public string? Resizes { get; set; }
    }

    public sealed class SignedUrlRequest
    {
        [FromQuery(Name = "path")]
        public string? Path { get; set; }

        [FromQuery(Name = "adapter")]
        public string? Adapter { get; set; }

        [FromQuery(Name = "expiresInSeconds")]
        public int ExpiresInSeconds { get; set; } = 300;
    }
}
