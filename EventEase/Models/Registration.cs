namespace EventEase.Models;

/// <summary>Satu pendaftaran peserta. Immutable: perubahan status dilakukan dengan `with`.</summary>
public record Registration(
    Guid Id,
    int EventId,
    string FullName,
    string Email,
    int Tickets,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? CheckedInAt = null)
{
    public bool Attended => CheckedInAt.HasValue;
    public string Code => Id.ToString("N")[..8].ToUpperInvariant();
}

public record RegistrationResult(bool Success, string? Error = null, Registration? Registration = null)
{
    public static RegistrationResult Ok(Registration registration) => new(true, null, registration);
    public static RegistrationResult Fail(string error) => new(false, error);
}

public record AttendanceSummary(
    int EventId,
    string EventName,
    int Capacity,
    int RegistrantCount,
    int RegisteredTickets,
    int CheckedInTickets)
{
    public int AttendancePercent =>
        RegisteredTickets == 0 ? 0 : (int)Math.Round(100.0 * CheckedInTickets / RegisteredTickets);

    public int Remaining => Math.Max(0, Capacity - RegisteredTickets);
}
