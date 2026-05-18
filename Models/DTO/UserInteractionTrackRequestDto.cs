using System.Text.Json;

namespace Tech_Store.Models.DTO;

public class UserInteractionTrackRequestDto
{
    public int? ProductId { get; set; }

    public string? ProductSysId { get; set; }

    public string EventType { get; set; } = null!;

    public string? Source { get; set; }

    public JsonElement? Metadata { get; set; }
}
