using System.Text.Json;
using EventEase.Models;
using EventEase.Services;

namespace EventEase.Tests;

/// <summary>Penyimpanan sesi di memori. Menyimpan salinan JSON agar perilakunya sama seperti browser.</summary>
public class InMemorySessionStore : ISessionStore
{
    private string? json;
    public bool ThrowOnLoad { get; set; }

    public Task<UserSession?> LoadAsync()
    {
        if (ThrowOnLoad) throw new InvalidOperationException("JS interop belum tersedia.");
        return Task.FromResult(json is null ? null : JsonSerializer.Deserialize<UserSession>(json));
    }

    public Task SaveAsync(UserSession session)
    {
        json = JsonSerializer.Serialize(session);
        return Task.CompletedTask;
    }
}

public class TestTime : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 9, 29, 8, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
    public void Advance(TimeSpan span) => Now += span;
}
