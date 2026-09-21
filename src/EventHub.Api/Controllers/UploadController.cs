using System.Net.Mime;
using EventHub.Api.Models;
using EventHub.Domain.Authorization;
using EventHub.Domain.Exceptions;
using EventHub.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace EventHub.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public sealed class UploadController(IImageStorage storage) : ControllerBase
{
    [HttpPost("images")]
    [Authorize(Roles = RoleName.Organizer)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<string>> UploadAsync(IFormFile file, CancellationToken ct)
    {
        try
        {
            if (file is null || file.Length == 0) throw new BusinessRuleException("No file was provided");
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input, ct);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(1600, 1600)
            }));
            using var output = new MemoryStream();
            await image.SaveAsWebpAsync(output, ct);
            output.Position = 0;

            var url = await storage.SaveAsync(output, "image/webp", ct);
            return Ok(new UploadResult(url));
        }
        catch (ImageFormatException)
        {
            throw new BusinessRuleException("The file is not a supported image");
        }
    }
}