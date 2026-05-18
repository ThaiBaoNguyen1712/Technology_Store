using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tech_Store.Models.DTO;
using Tech_Store.Services.Recommendation;

namespace Tech_Store.Controllers;

[ApiController]
[Route("api/v1/user-interactions")]
public class UserInteractionController : ControllerBase
{
    private readonly IUserProductEventTrackingService _trackingService;

    public UserInteractionController(IUserProductEventTrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] UserInteractionTrackRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.EventType))
        {
            return BadRequest(new { success = false, message = "EventType is required." });
        }

        var userId = TryGetCurrentUserId();
        var sessionId = Request.Cookies["guest_id"];
        var metadataJson = BuildMetadataJson(request.Metadata);

        await _trackingService.TrackAsync(new UserProductEventWriteRequest
        {
            UserId = userId,
            SessionId = userId.HasValue ? null : sessionId,
            ProductId = request.ProductId,
            ProductSysId = request.ProductSysId,
            EventType = request.EventType,
            Source = request.Source,
            MetadataJson = metadataJson
        }, cancellationToken);

        return Accepted(new { success = true });
    }

    private int? TryGetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    private string? BuildMetadataJson(JsonElement? clientMetadata)
    {
        var serverMetadata = new Dictionary<string, object?>
        {
            ["path"] = Request.Path.Value,
            ["query"] = Request.QueryString.Value,
            ["referrer"] = Request.Headers.Referer.ToString(),
            ["userAgent"] = Request.Headers.UserAgent.ToString(),
            ["capturedAtUtc"] = DateTime.UtcNow
        };

        if (clientMetadata.HasValue && clientMetadata.Value.ValueKind != JsonValueKind.Undefined && clientMetadata.Value.ValueKind != JsonValueKind.Null)
        {
            serverMetadata["client"] = clientMetadata.Value;
        }

        return JsonSerializer.Serialize(serverMetadata);
    }
}
