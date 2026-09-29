using System.Text.Json.Serialization;

namespace EventEase.Models;

/// <summary>Status sesi pengguna yang disimpan (terenkripsi) di sessionStorage browser.</summary>
public class UserSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    public int PageViews { get; set; }
    public List<int> VisitedEventIds { get; set; } = new();
    public List<int> FavoriteEventIds { get; set; } = new();
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }

    [JsonIgnore]
    public string ShortId => SessionId.Length >= 8 ? SessionId[..8] : SessionId;
}
