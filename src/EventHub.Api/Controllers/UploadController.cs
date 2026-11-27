using EventHub.Api.Models;
using EventHub.Domain.Authorization;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Models;
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

    // 40 MP, e.g. 8000 × 5000. A few MB of compressed PNG can claim far more pixels than that,
    // and SKBitmap.Decode would allocate width × height × 4 bytes before anything is checked.
    private const long MaxPixels = 40_000_000;

    [HttpPost("images")]
    [Authorize(Roles = RoleName.Organizer)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType<UploadResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<UploadResult>> UploadAsync(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) throw new BusinessRuleException(ErrorCodes.FileMissing, "No file was provided");
        await using var input = file.OpenReadStream();
        // The codec only reads the header – the size is known before any pixel memory is allocated.
        using var codec = SKCodec.Create(input)
                          ?? throw new BusinessRuleException(ErrorCodes.FileUnsupported, "The file is not a supported image.");
        if ((long)codec.Info.Width * codec.Info.Height > MaxPixels)
            throw new BusinessRuleException(ErrorCodes.FileTooManyPixels,
                $"The image has more than {MaxPixels / 1_000_000} megapixels.");
        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null) throw new BusinessRuleException(ErrorCodes.FileUnsupported, "The file is not a supported image.");
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