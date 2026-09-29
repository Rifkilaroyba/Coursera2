# Laporan Debugging & Optimasi - EventEase (Kegiatan 2)

## 1. Ringkasan masalah dan perbaikan

| # | Masalah (Kegiatan 1) | Penyebab | Perbaikan | Berkas |
|---|---|---|---|---|
| 1 | Event Card crash bila data `null` / kosong / tidak lengkap | Komponen langsung memakai `Event.Name` tanpa validasi | Validasi terpusat `EventItemValidator`; data tidak valid menampilkan kartu peringatan berisi daftar error, bukan exception | `Models/EventItemValidator.cs`, `Components/Shared/EventCard.razor` |
| 2 | Pengikatan data gagal pada input tidak valid | `favorites[item.Id]` (Dictionary) dapat melempar `KeyNotFoundException`; `InputNumber` menampilkan pesan bawaan bila diisi huruf/kosong; kuota tidak dicek | Diganti `HashSet<int>` + callback; `ParsingErrorMessage`, `ValidationMessage` per kolom, `maxlength`, cek kuota, cegah klik ganda | `Events.razor`, `Register.razor`, `RegistrationForm.cs` |
| 3 | Routing error pada jalur tidak ada | Alamat tak dikenal (mis. `/abc`) tidak punya halaman; id `0`/negatif lolos ke halaman detail | Route constraint `{Id:int:min(1)}`; halaman catch-all `/{*path}`; komponen `NotFoundMessage` yang mengatur status HTTP 404; `ErrorBoundary` di layout untuk error tak terduga | `EventDetails.razor`, `Register.razor`, `NotFoundPage.razor`, `NotFoundMessage.razor`, `MainLayout.razor` |
| 4 | Daftar acara lambat pada data besar | Semua kartu dirender sekaligus; filter dihitung ulang tiap render dan tiap ketikan; setiap kartu dirender ulang | Paging 12 kartu/halaman; filter hanya saat input/halaman berubah; debounce 300 ms; dataset di-cache & terurut sekali; `@key` + `ShouldRender` pada kartu; `GetById` O(1) | `EventService.cs`, `Events.razor`, `EventCard.razor` |

## 2. Prompt Copilot yang disarankan
1. "Analyze EventCard.razor and list every place a null or invalid EventItem could throw. Suggest a validation approach."
2. "Why can `@bind-IsFavorite=\"favorites[item.Id]\"` throw in a foreach loop? Suggest a safer pattern."
3. "My Blazor router shows an error for unknown URLs. Suggest a catch-all route and how to return HTTP 404."
4. "This list renders 10,000 cards and typing in the search box is slow. Suggest paging, debounce, and ShouldRender optimizations."
5. "Generate xUnit and bUnit tests for the edge cases above."

## 3. Rencana & hasil uji

### Otomatis (`dotnet test`)
- Validasi model: data valid, `null`, nama kosong/spasi, nama terlalu panjang, banyak error sekaligus.
- Validasi form: nama kosong/pendek, email salah/kosong, tiket 0/11/-3.
- Service: id tidak valid (0, negatif, `int.MaxValue`), pencarian null/kosong/tidak ada hasil, nomor halaman di luar rentang, ukuran halaman ekstrem, halaman tidak saling tumpang tindih.
- Komponen (bUnit): kartu valid, `null`, data tidak lengkap, toggle favorit memicu callback.
- Kinerja: 50.000 acara, query paged vs tanpa paging.

### Manual
| Skenario | Langkah | Hasil yang diharapkan |
|---|---|---|
| Alamat tidak ada | Buka `/abc`, `/events/xyz`, `/events/0`, `/events/-5` | Halaman "tidak ditemukan" + tombol kembali |
| Id tidak ada | Buka `/events/999999` | "Acara tidak ditemukan" |
| Input tidak valid | Daftar: nama kosong, email "abc", tiket "abc" / 0 / 11 / melebihi kuota | Pesan error per kolom, form tidak terkirim |
| Pencarian kosong | Cari "zzzz" | "Tidak ada acara yang cocok" |
| Data besar | Pilih 50.000 acara, ketik cepat | Halaman tetap responsif, query hanya jalan setelah jeda 300 ms |

### Pengukuran kinerja (isi dengan hasil di komputer Anda)
| Kondisi | Query (ms) | Kartu di DOM | Catatan |
|---|---|---|---|
| 10.000 acara, paging | ... | 12 | lihat baris "Query" di halaman |
| 10.000 acara, "Tanpa paging" (dibatasi 5.000) | ... | 5.000 | amati kelambatan render |
| 50.000 acara, paging (output `dotnet test`) | ... | 12 | |

Ekspektasi: jumlah elemen DOM turun >99% dan waktu query tetap kecil karena dataset di-cache dan terurut.
