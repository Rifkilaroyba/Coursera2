using System.Security.Cryptography;
using System.Text.Json;
using EventEase.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace EventEase.Services;

/// <summary>sessionStorage browser yang dienkripsi (Data Protection). Hanya bisa dipakai setelah render interaktif.</summary>
public sealed class ProtectedSessionStore : ISessionStore
{
    private const string Key = "eventease.session";
    private readonly ProtectedSessionStorage _storage;

    public ProtectedSessionStore(ProtectedSessionStorage storage) => _storage = storage;

    public async Task<UserSession?> LoadAsync()
    {
        try
        {
            var result = await _storage.GetAsync<UserSession>(Key);
            return result.Success ? result.Value : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            return null; // kunci enkripsi berubah atau data rusak -> mulai sesi baru
        }
    }

    public async Task SaveAsync(UserSession session) => await _storage.SetAsync(Key, session);
}
