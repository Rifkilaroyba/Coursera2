using System.Diagnostics;
using EventEase.Services;
using Xunit.Abstractions;

namespace EventEase.Tests;

public class EventServiceTests
{
    private readonly EventService svc = new();
    private readonly ITestOutputHelper output;

    public EventServiceTests(ITestOutputHelper output) => this.output = output;

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(EventService.MaxDatasetSize + 1)]
    [InlineData(int.MaxValue)]
    public void GetById_with_invalid_id_returns_null(int id) => Assert.Null(svc.GetById(id));

    [Fact] public void GetById_known_id_returns_event() => Assert.Equal(1, svc.GetById(1)!.Id);

    [Fact] public void GetAll_default_returns_six_sorted_by_date()
    {
        var all = svc.GetAll();
        Assert.Equal(6, all.Count);
        Assert.Equal(all.OrderBy(e => e.Date), all);
    }

    [Fact] public void Dataset_size_is_clamped()
    {
        Assert.Equal(6, svc.GetAll(-5).Count);
        Assert.Equal(EventService.MaxDatasetSize, svc.GetAll(int.MaxValue).Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_search_returns_everything(string? term) => Assert.Equal(6, svc.Search(term).Count);

    [Fact] public void Search_is_case_insensitive_and_matches_location() =>
        Assert.Single(svc.Search("HOTEL majapahit"));

    [Fact] public void Search_without_match_returns_empty_and_page_is_one()
    {
        var r = svc.Query("zzz-tidak-ada", 5, 12);
        Assert.Empty(r.Items);
        Assert.Equal(1, r.Page);
        Assert.Equal(1, r.TotalPages);
    }

    [Theory]
    [InlineData(-10, 1)]
    [InlineData(0, 1)]
    [InlineData(999, 84)]   // 1000 data / 12 per halaman = 84 halaman
    public void Out_of_range_page_is_clamped(int page, int expected) =>
        Assert.Equal(expected, svc.Query(null, page, 12, 1000).Page);

    [Fact] public void Page_size_is_clamped()
    {
        Assert.Single(svc.Query(null, 1, 0).Items.Take(1));
        Assert.True(svc.Query(null, 1, 100_000, 10_000).Items.Count <= EventService.MaxPageSize);
    }

    [Fact] public void Pages_do_not_overlap()
    {
        var p1 = svc.Query(null, 1, 12, 1000).Items.Select(e => e.Id);
        var p2 = svc.Query(null, 2, 12, 1000).Items.Select(e => e.Id);
        Assert.Empty(p1.Intersect(p2));
    }

    [Fact]
    public void Performance_paged_vs_unpaged_on_50k_events()
    {
        const int n = 50_000;
        svc.GetAll(n); // pemanasan / cache dataset

        var sw = Stopwatch.StartNew();
        const int loops = 100;
        for (var i = 0; i < loops; i++) svc.Query("konferensi", 1, 12, n);
        sw.Stop();
        var pagedMs = sw.Elapsed.TotalMilliseconds / loops;

        var page = svc.Query("konferensi", 1, 12, n);
        var unpaged = svc.Search("konferensi", n);

        output.WriteLine($"Query paged (rata-rata {loops}x): {pagedMs:0.000} ms");
        output.WriteLine($"Kartu dirender: paged = {page.Items.Count}, tanpa paging = {unpaged.Count}");
        output.WriteLine($"Pengurangan elemen DOM: {100.0 * (1 - (double)page.Items.Count / unpaged.Count):0.0}%");

        Assert.Equal(12, page.Items.Count);
        Assert.True(unpaged.Count > 1000);
        Assert.True(pagedMs < 100, $"Query terlalu lambat: {pagedMs} ms");
    }
}
