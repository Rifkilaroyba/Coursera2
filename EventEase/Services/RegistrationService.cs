using System.ComponentModel.DataAnnotations;
using EventEase.Models;

namespace EventEase.Services;

/// <summary>
/// Menyimpan pendaftaran & kehadiran di memori (thread-safe).
/// Untuk produksi, ganti penyimpanan ini dengan database tanpa mengubah antarmuka publiknya.
/// </summary>
public class RegistrationService
{
    private readonly EventService _events;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private readonly Dictionary<int, List<Registration>> _byEvent = new();

    /// <summary>Dipicu setelah data berubah (argumen = id acara). Dipakai untuk pembaruan langsung antar-pengguna.</summary>
    public event Action<int>? Changed;

    public RegistrationService(EventService events, TimeProvider time)
    {
        _events = events;
        _time = time;
        SeedDemoData();
    }

    public RegistrationResult Register(int eventId, RegistrationForm? form)
    {
        if (form is null)
            return RegistrationResult.Fail("Data pendaftaran kosong.");

        // Validasi ulang di sisi server (defense in depth).
        var problems = new List<ValidationResult>();
        if (!Validator.TryValidateObject(form, new ValidationContext(form), problems, validateAllProperties: true))
            return RegistrationResult.Fail(problems[0].ErrorMessage ?? "Data pendaftaran tidak valid.");

        var ev = _events.GetById(eventId);
        if (ev is null || !EventItemValidator.IsValid(ev))
            return RegistrationResult.Fail("Acara tidak ditemukan.");

        var email = form.Email.Trim().ToLowerInvariant();
        Registration registration;

        lock (_gate)
        {
            var list = GetOrCreateList(eventId);

            if (list.Any(r => r.Email == email))
                return RegistrationResult.Fail("Email ini sudah terdaftar pada acara tersebut.");

            var left = ev.Capacity - list.Sum(r => r.Tickets);
            if (form.Tickets > left)
                return RegistrationResult.Fail(left <= 0
                    ? "Kuota acara sudah penuh."
                    : $"Sisa kuota hanya {left} tiket.");

            registration = new Registration(Guid.NewGuid(), eventId, form.FullName.Trim(), email,
                form.Tickets, _time.GetUtcNow());
            list.Add(registration);
        }

        RaiseChanged(eventId);
        return RegistrationResult.Ok(registration);
    }

    public IReadOnlyList<Registration> GetByEvent(int eventId)
    {
        lock (_gate)
        {
            return _byEvent.TryGetValue(eventId, out var list)
                ? list.OrderBy(r => r.RegisteredAt).ThenBy(r => r.FullName).ToList()
                : new List<Registration>();
        }
    }

    public int RegisteredTickets(int eventId)
    {
        lock (_gate)
        {
            return _byEvent.TryGetValue(eventId, out var list) ? list.Sum(r => r.Tickets) : 0;
        }
    }

    /// <summary>Menandai hadir / batal hadir. False bila pendaftaran tidak ditemukan.</summary>
    public bool SetAttendance(Guid registrationId, bool attended)
    {
        int eventId = 0;
        var found = false;

        lock (_gate)
        {
            foreach (var (id, list) in _byEvent)
            {
                var index = list.FindIndex(r => r.Id == registrationId);
                if (index < 0) continue;

                list[index] = list[index] with { CheckedInAt = attended ? _time.GetUtcNow() : null };
                eventId = id;
                found = true;
                break;
            }
        }

        if (found) RaiseChanged(eventId);
        return found;
    }

    public AttendanceSummary GetSummary(int eventId)
    {
        var ev = _events.GetById(eventId);
        var regs = GetByEvent(eventId);
        return new AttendanceSummary(
            eventId,
            ev?.Name ?? $"Acara #{eventId}",
            ev?.Capacity ?? 0,
            regs.Count,
            regs.Sum(r => r.Tickets),
            regs.Where(r => r.Attended).Sum(r => r.Tickets));
    }

    /// <summary>Ringkasan semua acara yang memiliki minimal satu pendaftaran.</summary>
    public IReadOnlyList<AttendanceSummary> GetSummaries()
    {
        List<int> ids;
        lock (_gate)
        {
            ids = _byEvent.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToList();
        }
        return ids.OrderBy(i => i).Select(GetSummary).ToList();
    }

    private List<Registration> GetOrCreateList(int eventId)
    {
        if (!_byEvent.TryGetValue(eventId, out var list))
            _byEvent[eventId] = list = new List<Registration>();
        return list;
    }

    private void RaiseChanged(int eventId)
    {
        var handlers = Changed;
        if (handlers is null) return;

        foreach (Action<int> handler in handlers.GetInvocationList())
        {
            try { handler(eventId); }
            catch { /* satu pelanggan yang gagal (mis. sirkuit ditutup) tidak boleh mengganggu yang lain */ }
        }
    }

    private void SeedDemoData()
    {
        var demo = new (int EventId, string Name, string Email, int Tickets, bool Attended)[]
        {
            (1, "Andi Pratama", "andi@contoh.id", 2, true),
            (1, "Siti Rahma", "siti@contoh.id", 1, true),
            (1, "Budi Hartono", "budi@contoh.id", 3, false),
            (1, "Dewi Lestari", "dewi@contoh.id", 1, false),
            (2, "Rina Wijaya", "rina@contoh.id", 1, true),
            (2, "Agus Salim", "agus@contoh.id", 2, false),
            (2, "Maya Putri", "maya@contoh.id", 1, false),
        };

        foreach (var d in demo)
        {
            var result = Register(d.EventId, new RegistrationForm { FullName = d.Name, Email = d.Email, Tickets = d.Tickets });
            if (result.Success && d.Attended) SetAttendance(result.Registration!.Id, true);
        }
    }
}
