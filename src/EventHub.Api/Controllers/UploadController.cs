using System.Net.Mime;
using EventHub.Api.Models;
using EventHub.Domain.Authorization;
using EventHub.Domain.Exceptions;
using EventHub.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public sealed class UploadController(IImageStorage storage) : ControllerBase
{
    private const int MaxEdge = 1600;

    [HttpPost("images")]
    [Authorize(Roles = RoleName.Organizer)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<string>> UploadAsync(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) throw new BusinessRuleException("No file was provided");
        await using var input = file.OpenReadStream();
        using var decoded = SKBitmap.Decode(input);
        if (decoded is null) throw new BusinessRuleException("The file is not a supported image.");
        var scale = Math.Min(1.0, (double)MaxEdge / Math.Max(decoded.Width, decoded.Height));
        using var resized = scale < 1.0
            ? decoded.Resize(new SKImageInfo((int)(decoded.Width * scale), (int)(decoded.Height * scale)),
                SKSamplingOptions.Default)
            : decoded.Copy();
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Webp, 85);
        using var output = new MemoryStream();
        data.SaveTo(output);
        output.Position = 0;

        var url = await storage.SaveAsync(output, "image/webp", ct);
        return Ok(new UploadResult(url));
    }
}