using EventEase.Services;

namespace EventEase.Tests;

public class SessionStateTests
{
    private readonly InMemorySessionStore store = new();
    private readonly TestTime time = new();

    private SessionState Create() => new(store, time);

    [Fact]
    public void New_state_has_an_active_session_even_before_initialize()
    {
        var s = Create();
        Assert.False(s.IsInitialized);
        Assert.Equal(8, s.Current.ShortId.Length);
    }

    [Fact]
    public async Task Session_survives_refresh()
    {
        var first = Create();
        await first.InitializeAsync();
        await first.TrackVisitAsync(3);
        await first.SetFavoriteAsync(3, true);
        var id = first.Current.SessionId;

        time.Advance(TimeSpan.FromMinutes(5));
        var afterRefresh = Create();          // sirkuit baru = refresh halaman
        await afterRefresh.InitializeAsync();

        Assert.Equal(id, afterRefresh.Current.SessionId);
        Assert.Contains(3, afterRefresh.Current.VisitedEventIds);
        Assert.True(afterRefresh.IsFavorite(3));
        Assert.False(afterRefresh.RenewedAfterTimeout);
    }

    [Fact]
    public async Task Idle_session_expires_on_restore()
    {
        var first = Create();
        await first.InitializeAsync();
        var oldId = first.Current.SessionId;

        time.Advance(SessionState.IdleTimeout + TimeSpan.FromMinutes(1));
        var later = Create();
        await later.InitializeAsync();

        Assert.NotEqual(oldId, later.Current.SessionId);
        Assert.True(later.RenewedAfterTimeout);
    }

    [Fact]
    public async Task Session_expires_while_circuit_stays_open()
    {
        var s = Create();
        await s.InitializeAsync();
        var oldId = s.Current.SessionId;

        time.Advance(SessionState.IdleTimeout + TimeSpan.FromMinutes(1));
        await s.TrackVisitAsync(1);

        Assert.NotEqual(oldId, s.Current.SessionId);
        Assert.True(s.RenewedAfterTimeout);
        Assert.Equal(1, s.Current.PageViews);
    }

    [Fact]
    public async Task Activity_before_initialize_is_merged_with_stored_session()
    {
        var first = Create();
        await first.InitializeAsync();
        await first.TrackVisitAsync(3);

        var second = Create();
        await second.TrackVisitAsync(5);       // terjadi sebelum sesi tersimpan dimuat
        await second.InitializeAsync();

        Assert.Equal(first.Current.SessionId, second.Current.SessionId);
        Assert.Equal(new[] { 3, 5 }, second.Current.VisitedEventIds);
        Assert.Equal(2, second.Current.PageViews);
    }

    [Fact]
    public async Task Favorite_toggle_is_idempotent()
    {
        var s = Create();
        await s.SetFavoriteAsync(1, true);
        await s.SetFavoriteAsync(1, true);
        Assert.Single(s.Current.FavoriteEventIds);

        await s.SetFavoriteAsync(1, false);
        await s.SetFavoriteAsync(1, false);
        Assert.Empty(s.Current.FavoriteEventIds);
    }

    [Fact]
    public async Task Reset_starts_new_session_and_persists_it()
    {
        var s = Create();
        await s.InitializeAsync();
        await s.SetFavoriteAsync(2, true);
        var oldId = s.Current.SessionId;

        await s.ResetAsync();

        Assert.NotEqual(oldId, s.Current.SessionId);
        Assert.Empty(s.Current.FavoriteEventIds);
        Assert.Equal(s.Current.SessionId, (await store.LoadAsync())!.SessionId);
    }

    [Fact]
    public async Task Storage_failure_does_not_throw_and_keeps_memory_state()
    {
        store.ThrowOnLoad = true;
        var s = Create();

        await s.InitializeAsync();          // tidak boleh melempar exception
        await s.TrackVisitAsync(1);

        Assert.False(s.IsInitialized);
        Assert.Equal(1, s.Current.PageViews);
    }

    [Fact]
    public async Task OnChange_is_raised_on_mutation()
    {
        var s = Create();
        var count = 0;
        s.OnChange += () => count++;

        await s.TrackVisitAsync(1);
        await s.SetContactAsync("Budi", "budi@contoh.id");

        Assert.Equal(2, count);
        Assert.Equal("Budi", s.Current.ContactName);
    }

    [Fact]
    public async Task Contact_values_are_trimmed_and_length_limited()
    {
        var s = Create();
        await s.SetContactAsync("  " + new string('a', 200) + "  ", "  x@y.id ");
        Assert.Equal(80, s.Current.ContactName!.Length);
        Assert.Equal("x@y.id", s.Current.ContactEmail);
    }
}
