using System.ComponentModel.DataAnnotations;

namespace EventEase.Models;

public class RegistrationForm
{
    [Required(ErrorMessage = "Nama lengkap wajib diisi.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Nama harus 3-80 karakter.")]
    [RegularExpression(@"^[\p{L}][\p{L} .'\-]*$",
        ErrorMessage = "Nama hanya boleh berisi huruf, spasi, titik, apostrof, dan tanda hubung.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email wajib diisi.")]
    [StringLength(100, ErrorMessage = "Email maksimal 100 karakter.")]
    [EmailAddress(ErrorMessage = "Format email tidak valid.")]
    public string Email { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Jumlah tiket harus antara 1 dan 10.")]
    public int Tickets { get; set; } = 1;
}
