using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Integrations;

namespace MultiTenantEcommerce.Presentation.Controllers.Integrations;

[ApiController]
[Route("api/integrations/woocommerce")]
[Authorize]
public class WooCommerceController : ControllerBase
{
    private readonly IWooCommerceImportService _importService;

    public WooCommerceController(IWooCommerceImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(WooCommerceImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import([FromForm] WooCommerceImportRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("A WooCommerce export file must be provided.");
        }

        if (!TryResolveFormat(request, out var format, out var error))
        {
            return BadRequest(error);
        }

        await using var stream = request.File.OpenReadStream();
        var result = await _importService.ImportAsync(stream, format, cancellationToken);
        return Ok(result);
    }

    private static bool TryResolveFormat(WooCommerceImportRequest request, out WooCommerceImportFormat format, out string? error)
    {
        error = null;
        if (!string.IsNullOrWhiteSpace(request.Format) && Enum.TryParse<WooCommerceImportFormat>(request.Format, true, out format))
        {
            return true;
        }

        var extension = Path.GetExtension(request.File?.FileName ?? string.Empty);
        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            format = WooCommerceImportFormat.Json;
            return true;
        }

        if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            format = WooCommerceImportFormat.Csv;
            return true;
        }

        format = WooCommerceImportFormat.Csv;
        if (string.IsNullOrWhiteSpace(extension))
        {
            return true;
        }

        error = "Unable to determine the file format. Specify a format of 'csv' or 'json'.";
        return false;
    }

    public class WooCommerceImportRequest
    {
        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }

        [FromForm(Name = "format")]
        public string? Format { get; set; }
    }
}
