using EventEase.Models;
using Microsoft.JSInterop;

namespace EventEase.Services;

/// <summary>
/// Pelacak sesi pengguna (scoped per sirkuit Blazor).
/// - Selalu punya sesi aktif di memori (aman dipakai saat prerender).
/// - Setelah render interaktif, InitializeAsync memulihkan sesi dari sessionStorage
///   sehingga sesi bertahan saat halaman di-refresh.
/// - Sesi berakhir setelah IdleTimeout tanpa aktivitas.
/// </summary>
public class SessionState
{
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);
    private const int MaxTracked = 200;

    private readonly ISessionStore _store;
    private readonly TimeProvider _time;

    public SessionState(ISessionStore store, TimeProvider time)
    {
        _store = store;
        _time = time;
        Current = NewSession();
    }

    public UserSession Current { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool RenewedAfterTimeout { get; private set; }

    public event Action? OnChange;

    public bool IsFavorite(int eventId) => Current.FavoriteEventIds.Contains(eventId);

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        UserSession? stored;
        try
        {
            stored = await _store.LoadAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException)
        {
            return; // JS belum tersedia / sirkuit terputus: coba lagi pada render berikutnya
        }

        var now = _time.GetUtcNow();

        if (stored is not null && IsUsable(stored, now))
        {
            Merge(stored, Current, now);
            Current = stored;
        }
        else if (stored is not null)
        {
            RenewedAfterTimeout = true;
        }

        IsInitialized = true;
        await SaveAsync();
        OnChange?.Invoke();
    }

    public async Task TrackVisitAsync(int eventId)
    {
        Touch();
        Current.PageViews++;
        AddUnique(Current.VisitedEventIds, eventId);
        await CommitAsync();
    }

    public async Task SetFavoriteAsync(int eventId, bool isFavorite)
    {
        Touch();
        var changed = isFavorite
            ? AddUnique(Current.FavoriteEventIds, eventId)
            : Current.FavoriteEventIds.Remove(eventId);

        if (changed) await CommitAsync();
    }

    public async Task SetContactAsync(string name, string email)
    {
        Touch();
        Current.ContactName = Truncate(name.Trim(), 80);
        Current.ContactEmail = Truncate(email.Trim(), 100);
        await CommitAsync();
    }

    public async Task ResetAsync()
    {
        Current = NewSession();
        RenewedAfterTimeout = false;
        await CommitAsync();
    }

    // ---- internal ----

    private UserSession NewSession()
    {
        var now = _time.GetUtcNow();
        return new UserSession { StartedAt = now, LastActivity = now };
    }

    private void Touch()
    {
        var now = _time.GetUtcNow();
        if (now - Current.LastActivity > IdleTimeout)
        {
            Current = NewSession();
            RenewedAfterTimeout = true;
        }
        Current.LastActivity = now;
    }

    private async Task CommitAsync()
    {
        if (IsInitialized) await SaveAsync();
        OnChange?.Invoke();
    }

    private async Task SaveAsync()
    {
        try
        {
            await _store.SaveAsync(Current);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or InvalidOperationException)
        {
            // Browser terputus: status di memori tetap benar, penyimpanan dilewati.
        }
    }

    private static bool IsUsable(UserSession s, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(s.SessionId)) return false;
        s.VisitedEventIds ??= new List<int>();
        s.FavoriteEventIds ??= new List<int>();
        return now - s.LastActivity <= IdleTimeout;
    }

    // Gabungkan aktivitas yang terjadi sebelum sesi tersimpan berhasil dimuat.
    private static void Merge(UserSession target, UserSession pending, DateTimeOffset now)
    {
        target.PageViews += pending.PageViews;
        foreach (var id in pending.VisitedEventIds) AddUnique(target.VisitedEventIds, id);
        foreach (var id in pending.FavoriteEventIds) AddUnique(target.FavoriteEventIds, id);
        target.ContactName ??= pending.ContactName;
        target.ContactEmail ??= pending.ContactEmail;
        target.LastActivity = now;
    }

    private static bool AddUnique(List<int> list, int id)
    {
        if (list.Contains(id)) return false;
        if (list.Count >= MaxTracked) list.RemoveAt(0);
        list.Add(id);
        return true;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
