# Ringkasan: Peran Microsoft Copilot dalam Pengembangan EventEase

| Tahap | Cara Copilot membantu | Contoh prompt | Hasil |
|---|---|---|---|
| **Kegiatan 1 - Fondasi** | Menyarankan struktur komponen Blazor, sintaks `@bind`, parameter komponen, dan template route | "Create a Blazor EventCard component with name, date, location parameters" | `EventCard`, halaman daftar/detail/pendaftaran, routing dasar |
| **Kegiatan 2 - Debugging** | Menemukan titik `NullReferenceException`/`KeyNotFoundException`, menjelaskan route constraint & catch-all, menyarankan paging, debounce, `ShouldRender`, `@key` | "List every place a null EventItem could throw" / "My list of 10,000 cards is slow - suggest optimizations" | Validasi kartu, halaman 404, paging + debounce, tes xUnit/bUnit |
| **Kegiatan 3 - Fitur lanjutan** | Merancang alur form dengan `EditForm` + DataAnnotations, pola state (service scoped + ProtectedSessionStorage), model kehadiran, dan tes untuk kasus tepi | "Design a session tracker in Blazor Server that survives page refresh" / "Generate tests for capacity and duplicate-email rules" | Form pendaftaran tervalidasi, `SessionState`, pelacak kehadiran, tes konkurensi |
| **Produksi** | Meninjau praktik terbaik: validasi ulang di server, header keamanan, health check, Dockerfile, penghapusan `using`/dependensi tak terpakai | "Review this Blazor app for production readiness and unnecessary dependencies" | `Program.cs` yang diperkuat, Dockerfile, `_Imports` ramping |

## Pelajaran
- Copilot mempercepat kode boilerplate dan memberi pilihan pola, tetapi setiap saran perlu diuji (lihat proyek `EventEase.Tests`).
- Prompt yang spesifik (menyebut framework, masalah, dan kendala) menghasilkan saran yang jauh lebih tepat.
- Debugging paling efektif bila Copilot diminta menjelaskan *penyebab* masalah, bukan hanya menambal gejalanya.
