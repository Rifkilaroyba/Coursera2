# EventEase - Kegiatan 3 (Versi Akhir)

Aplikasi manajemen acara berbasis Blazor (.NET 8, Interactive Server) - tanpa paket NuGet pihak ketiga.

## Menjalankan
```
cd EventEase
dotnet run
```
## Pengujian
```
cd EventEase.Tests
dotnet test --logger "console;verbosity=detailed"
```
## Docker
```
docker build -t eventease .
docker run -p 8080:8080 eventease      # http://localhost:8080  (health check: /health)
```

## Fitur
| Fitur | Halaman | Berkas utama |
|---|---|---|
| Daftar acara, cari, paging, favorit | `/events` | `Events.razor`, `EventCard.razor` |
| Detail acara + sisa kuota | `/events/{id}` | `EventDetails.razor` |
| **Formulir pendaftaran + validasi** (klien & server, kuota, email ganda, isi otomatis dari sesi) | `/events/{id}/register` | `Register.razor`, `RegistrationForm.cs`, `RegistrationService.cs` |
| **Pelacak sesi** (persisten saat refresh, timeout 30 menit, gabung aktivitas sebelum dimuat) | `/session` + badge di footer | `SessionState.cs`, `ProtectedSessionStore.cs` |
| **Pelacak kehadiran** (check-in/batal, ringkasan, pembaruan langsung antar-tab) | `/attendance`, `/events/{id}/attendance` | `Attendance.razor`, `EventAttendance.razor` |
| 404 / error ditangani | `/apa-saja`, `/Error` | `NotFoundPage.razor`, `Error.razor`, `ErrorBoundary` |

## Kesiapan produksi
- Tanpa dependensi pihak ketiga di aplikasi (paket bUnit/xUnit hanya di proyek tes); `_Imports` dirapikan.
- Health check `/health`, header keamanan, HSTS, exception handler, `DetailedErrors` hanya di Development, batas retensi sirkuit.
- Validasi diulang di server; input dibatasi panjangnya; nama dibatasi karakter (mencegah markup); Razor otomatis meng-encode output.
- Dockerfile multi-stage, berjalan sebagai user non-root.

## Batasan yang perlu diketahui
- **Data pendaftaran disimpan di memori** (hilang saat aplikasi restart, satu instance saja). Untuk produksi ganti isi `RegistrationService` dengan database (mis. EF Core) - antarmuka publiknya tidak berubah.
- Sesi memakai Data Protection; pada beberapa instance/kontainer, simpan key ring di lokasi bersama agar sesi tetap bisa dibaca setelah restart.
- Data contoh (7 pendaftar pada acara 1 dan 2) dibuat otomatis untuk demonstrasi.

## Daftar periksa uji manual
1. Daftar dengan form kosong / email salah / tiket "abc", 0, 11 -> pesan error per kolom.
2. Daftar dengan email yang sama dua kali -> ditolak. Daftar melebihi kuota -> ditolak.
3. Berhasil daftar -> buka form acara lain: nama & email terisi otomatis.
4. Favoritkan beberapa acara, lihat detail beberapa acara, **refresh (F5)** -> `/session` tetap menampilkan data yang sama.
5. Buka `/attendance` di dua tab; lakukan check-in di tab 1 -> tab 2 berubah tanpa refresh.
6. Buka `/abc`, `/events/0`, `/events/999999` -> halaman "tidak ditemukan".
7. Pilih 10.000 / 50.000 acara -> antarmuka tetap responsif (lihat waktu query).
