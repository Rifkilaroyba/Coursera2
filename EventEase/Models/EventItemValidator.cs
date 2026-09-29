namespace EventEase.Models;

/// <summary>
/// Validasi terpusat untuk data acara. Aman dipanggil dengan null
/// sehingga komponen tidak melempar NullReferenceException.
/// </summary>
public static class EventItemValidator
{
    public const int MaxNameLength = 120;

    public static IReadOnlyList<string> GetErrors(EventItem? item)
    {
        if (item is null)
            return new[] { "Data acara tidak tersedia (null)." };

        var errors = new List<string>();

        if (item.Id <= 0)
            errors.Add("Id acara harus lebih besar dari 0.");
        if (string.IsNullOrWhiteSpace(item.Name))
            errors.Add("Nama acara wajib diisi.");
        else if (item.Name.Length > MaxNameLength)
            errors.Add($"Nama acara maksimal {MaxNameLength} karakter.");
        if (string.IsNullOrWhiteSpace(item.Location))
            errors.Add("Lokasi acara wajib diisi.");
        if (item.Date == default)
            errors.Add("Tanggal acara tidak valid.");
        if (item.Capacity < 0)
            errors.Add("Kapasitas tidak boleh negatif.");

        return errors;
    }

    public static bool IsValid(EventItem? item) => GetErrors(item).Count == 0;
}
