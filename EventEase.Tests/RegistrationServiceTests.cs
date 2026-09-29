using EventEase.Models;
using EventEase.Services;

namespace EventEase.Tests;

public class RegistrationServiceTests
{
    private readonly TestTime time = new();
    private readonly RegistrationService svc;

    public RegistrationServiceTests() => svc = new RegistrationService(new EventService(), time);

    private static RegistrationForm Form(string email = "peserta@contoh.id", int tickets = 1, string name = "Peserta Uji") =>
        new() { FullName = name, Email = email, Tickets = tickets };

    [Fact]
    public void Valid_registration_succeeds_and_is_normalized()
    {
        var r = svc.Register(3, Form("  BUDI.Baru@Contoh.ID ", 2, "  Budi Baru  "));
        Assert.True(r.Success);
        Assert.Equal("budi.baru@contoh.id", r.Registration!.Email);
        Assert.Equal("Budi Baru", r.Registration.FullName);
        Assert.Equal(2, svc.RegisteredTickets(3));
    }

    [Fact]
    public void Null_form_fails() => Assert.False(svc.Register(3, null).Success);

    [Theory]
    [InlineData("", "a@b.com", 1)]
    [InlineData("Budi", "bukan-email", 1)]
    [InlineData("Budi", "a@b.com", 0)]
    [InlineData("Budi", "a@b.com", 11)]
    [InlineData("B0di", "a@b.com", 1)]
    [InlineData("Budi<script>", "a@b.com", 1)]
    public void Invalid_form_fails_on_server_side_too(string name, string email, int tickets)
    {
        var r = svc.Register(3, Form(email, tickets, name));
        Assert.False(r.Success);
        Assert.NotNull(r.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999_999)]
    public void Unknown_event_fails(int eventId) => Assert.False(svc.Register(eventId, Form()).Success);

    [Fact]
    public void Duplicate_email_same_event_fails_case_insensitively()
    {
        Assert.True(svc.Register(3, Form("dup@contoh.id")).Success);
        var second = svc.Register(3, Form("DUP@contoh.id"));
        Assert.False(second.Success);
        Assert.Contains("sudah terdaftar", second.Error);
    }

    [Fact]
    public void Same_email_on_different_events_is_allowed()
    {
        Assert.True(svc.Register(3, Form("sama@contoh.id")).Success);
        Assert.True(svc.Register(4, Form("sama@contoh.id")).Success);
    }

    [Fact]
    public void Capacity_is_enforced()
    {
        // Acara 3 berkapasitas 200 -> 20 pendaftar x 10 tiket
        for (var i = 0; i < 20; i++)
            Assert.True(svc.Register(3, Form($"p{i}@contoh.id", 10)).Success);

        var full = svc.Register(3, Form("terlambat@contoh.id"));
        Assert.False(full.Success);
        Assert.Contains("penuh", full.Error);
    }

    [Fact]
    public void Partial_capacity_message_shows_remaining()
    {
        for (var i = 0; i < 19; i++) svc.Register(3, Form($"p{i}@contoh.id", 10));  // 190 tiket
        Assert.True(svc.Register(3, Form("p19@contoh.id", 5)).Success);              // 195 tiket

        var r = svc.Register(3, Form("x@contoh.id", 10));                           // sisa 5
        Assert.False(r.Success);
        Assert.Contains("5", r.Error);
    }

    [Fact]
    public async Task Concurrent_registrations_never_exceed_capacity()
    {
        // Acara 20 (sintetis) berkapasitas 50 tiket
        var tasks = Enumerable.Range(0, 200)
            .Select(i => Task.Run(() => svc.Register(20, Form($"p{i}@contoh.id"))));
        var results = await Task.WhenAll(tasks);

        Assert.Equal(50, results.Count(r => r.Success));
        Assert.Equal(50, svc.GetSummary(20).RegisteredTickets);
    }

    [Fact]
    public void Attendance_can_be_toggled_and_records_time()
    {
        var reg = svc.Register(3, Form("hadir@contoh.id")).Registration!;
        Assert.True(svc.SetAttendance(reg.Id, true));
        var checkedIn = svc.GetByEvent(3).Single(r => r.Id == reg.Id);
        Assert.True(checkedIn.Attended);
        Assert.Equal(time.Now, checkedIn.CheckedInAt);

        Assert.True(svc.SetAttendance(reg.Id, false));
        Assert.False(svc.GetByEvent(3).Single(r => r.Id == reg.Id).Attended);
    }

    [Fact] public void Attendance_on_unknown_registration_returns_false() =>
        Assert.False(svc.SetAttendance(Guid.NewGuid(), true));

    [Fact]
    public void Seeded_summary_is_correct()
    {
        var s = svc.GetSummary(1);
        Assert.Equal(4, s.RegistrantCount);
        Assert.Equal(7, s.RegisteredTickets);
        Assert.Equal(3, s.CheckedInTickets);
        Assert.Equal(43, s.AttendancePercent);
    }

    [Fact]
    public void Summary_of_event_without_registrations_has_zero_rate()
    {
        var s = svc.GetSummary(5);
        Assert.Equal(0, s.AttendancePercent);
        Assert.Equal(1000, s.Remaining);
    }

    [Fact]
    public void Summaries_list_only_events_with_registrations()
    {
        var ids = svc.GetSummaries().Select(s => s.EventId).ToList();
        Assert.Equal(new[] { 1, 2 }, ids);
    }

    [Fact]
    public void Changed_event_is_raised_with_event_id()
    {
        var raised = new List<int>();
        svc.Changed += raised.Add;
        var reg = svc.Register(3, Form()).Registration!;
        svc.SetAttendance(reg.Id, true);
        Assert.Equal(new[] { 3, 3 }, raised);
    }

    [Fact]
    public void Failing_subscriber_does_not_break_registration()
    {
        svc.Changed += _ => throw new InvalidOperationException("sirkuit ditutup");
        Assert.True(svc.Register(3, Form()).Success);
    }
}
