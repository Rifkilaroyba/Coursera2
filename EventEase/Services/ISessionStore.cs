using EventEase.Models;

namespace EventEase.Services;

/// <summary>Abstraksi penyimpanan sesi agar logika sesi dapat diuji tanpa browser.</summary>
public interface ISessionStore
{
    Task<UserSession?> LoadAsync();
    Task SaveAsync(UserSession session);
}
