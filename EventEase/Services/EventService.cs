using System.Collections.Concurrent;
using EventEase.Models;

namespace EventEase.Services;

public class EventService
{
    public const int MaxDatasetSize = 50_000;
    public const int MaxPageSize = 100;

    private static readonly string[] Kinds =
        { "Konferensi", "Workshop", "Seminar", "Gala Dinner", "Bazar", "Pelatihan", "Pameran", "Resepsi" };
    private static readonly string[] Topics =
        { "Teknologi", "Bisnis", "Kepemimpinan", "UMKM", "Kreatif", "Kesehatan", "Pendidikan", "Keuangan" };
    private static readonly string[] Cities =
        { "Surabaya", "Malang", "Sidoarjo", "Gresik", "Jakarta", "Bandung", "Yogyakarta", "Semarang" };

    private static readonly List<EventItem> Sample = new()
    {
        new() { Id = 1, Name = "Konferensi Teknologi Surabaya", Date = new DateTime(2026, 11, 12), Location = "Grand City Convention Hall, Surabaya", Capacity = 500,
                Description = "Konferensi tahunan tentang cloud, AI, dan pengembangan perangkat lunak." },
        new() { Id = 2, Name = "Workshop Blazor untuk Pemula", Date = new DateTime(2026, 11, 20), Location = "Co-working Space Tunjungan, Surabaya", Capacity = 60,
                Description = "Belajar membangun aplikasi web interaktif dengan Blazor dan C#." },
        new() { Id = 3, Name = "Gala Dinner Perusahaan", Date = new DateTime(2026, 12, 5), Location = "Hotel Majapahit, Surabaya", Capacity = 200,
                Description = "Malam apresiasi karyawan dan mitra bisnis akhir tahun." },
        new() { Id = 4, Name = "Pernikahan Sari & Budi", Date = new DateTime(2026, 12, 19), Location = "Balai Pemuda, Surabaya", Capacity = 300,
                Description = "Resepsi pernikahan dengan konsep taman outdoor." },
        new() { Id = 5, Name = "Bazar UMKM Jawa Timur", Date = new DateTime(2027, 1, 16), Location = "Pakuwon Mall, Surabaya", Capacity = 1000,
                Description = "Pameran produk lokal dari ratusan UMKM Jawa Timur." },
        new() { Id = 6, Name = "Seminar Kepemimpinan", Date = new DateTime(2027, 2, 6), Location = "Universitas Airlangga, Surabaya", Capacity = 150,
                Description = "Sesi inspiratif bersama para pemimpin industri." }
    };

    private static readonly Dictionary<int, EventItem> SampleById = Sample.ToDictionary(e => e.Id);

    // Cache dataset besar agar tidak dibuat ulang tiap permintaan.
    private readonly ConcurrentDictionary<int, Lazy<IReadOnlyList<EventItem>>> _datasets = new();

    /// <summary>Mengambil seluruh acara terurut tanggal. size &lt;= 6 = data contoh.</summary>
    public IReadOnlyList<EventItem> GetAll(int size = 0)
    {
        size = Math.Clamp(size, 0, MaxDatasetSize);
        if (size <= Sample.Count) return _datasets.GetOrAdd(0, _ => new Lazy<IReadOnlyList<EventItem>>(
            () => Sample.OrderBy(e => e.Date).ToList())).Value;

        return _datasets.GetOrAdd(size, s => new Lazy<IReadOnlyList<EventItem>>(() =>
            Enumerable.Range(1, s).Select(GetById).Where(e => e is not null).Select(e => e!)
                      .OrderBy(e => e.Date).ThenBy(e => e.Id).ToList())).Value;
    }

    /// <summary>Pencarian O(1). Mengembalikan null untuk id tidak valid / tidak ada.</summary>
    public EventItem? GetById(int id)
    {
        if (id <= 0 || id > MaxDatasetSize) return null;
        if (SampleById.TryGetValue(id, out var sample)) return sample;
        return CreateSynthetic(id);
    }

    /// <summary>Mencari (nama/lokasi) tanpa paging. Term null/kosong = semua data.</summary>
    public IReadOnlyList<EventItem> Search(string? term, int datasetSize = 0)
    {
        var source = GetAll(datasetSize);
        term = term?.Trim();
        if (string.IsNullOrEmpty(term)) return source;

        return source.Where(e =>
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Pencarian + paging. Nomor halaman di luar rentang otomatis dirapikan.</summary>
    public PagedResult<EventItem> Query(string? term, int page, int pageSize, int datasetSize = 0)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var matches = Search(term, datasetSize);

        var totalPages = Math.Max(1, (int)Math.Ceiling(matches.Count / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<EventItem>(items, matches.Count, page, pageSize, totalPages);
    }

    // Data sintetis deterministik untuk uji kinerja
    private static EventItem CreateSynthetic(int id)
    {
        var kind = Kinds[id % Kinds.Length];
        var topic = Topics[(id / 3) % Topics.Length];
        var city = Cities[(id / 7) % Cities.Length];
        return new EventItem
        {
            Id = id,
            Name = $"{kind} {topic} #{id}",
            Date = new DateTime(2027, 3, 1).AddDays(id % 365),
            Location = $"Gedung {(char)('A' + id % 26)}, {city}",
            Capacity = 50 + (id % 20) * 25,
            Description = $"Acara {kind.ToLowerInvariant()} bertema {topic.ToLowerInvariant()} di {city}."
        };
    }
}
