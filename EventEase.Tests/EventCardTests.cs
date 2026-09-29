using Bunit;
using EventEase.Components.Shared;
using EventEase.Models;
using Microsoft.AspNetCore.Components;

namespace EventEase.Tests;

public class EventCardTests : TestContext
{
    private static EventItem Valid() => new()
    {
        Id = 7, Name = "Workshop Uji", Date = new DateTime(2027, 5, 1), Location = "Malang", Capacity = 20
    };

    [Fact]
    public void Renders_name_and_location_for_valid_event()
    {
        var cut = RenderComponent<EventCard>(p => p.Add(c => c.Event, Valid()));
        Assert.Contains("Workshop Uji", cut.Markup);
        Assert.Contains("Malang", cut.Markup);
        Assert.Contains("/events/7/register", cut.Markup);
    }

    [Fact]
    public void Null_event_shows_warning_instead_of_throwing()
    {
        var cut = RenderComponent<EventCard>(p => p.Add(c => c.Event, null));
        Assert.Contains("Data acara tidak valid", cut.Markup);
    }

    [Fact]
    public void Incomplete_event_lists_validation_errors()
    {
        var bad = new EventItem { Id = 1, Name = "", Location = "", Date = default };
        var cut = RenderComponent<EventCard>(p => p.Add(c => c.Event, bad));
        Assert.Contains("Nama acara wajib diisi", cut.Markup);
        Assert.Contains("Lokasi acara wajib diisi", cut.Markup);
    }

    [Fact]
    public void Toggling_favorite_raises_IsFavoriteChanged()
    {
        bool? received = null;
        var cut = RenderComponent<EventCard>(p => p
            .Add(c => c.Event, Valid())
            .Add(c => c.IsFavoriteChanged, EventCallback.Factory.Create<bool>(this, v => received = v)));

        cut.Find("input[type=checkbox]").Change(true);

        Assert.True(received);
        Assert.Contains("favorite", cut.Find("article").ClassList);
    }
}
