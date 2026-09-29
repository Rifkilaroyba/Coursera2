using Bunit;
using EventEase.Components.Pages;
using EventEase.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventEase.Tests;

/// <summary>Uji komponen formulir pendaftaran: penanganan kesalahan & alur berhasil.</summary>
public class RegisterPageTests : TestContext
{
    public RegisterPageTests()
    {
        Services.AddSingleton<TimeProvider>(new TestTime());
        Services.AddSingleton<EventService>();
        Services.AddSingleton<RegistrationService>();
        Services.AddSingleton<ISessionStore, InMemorySessionStore>();
        Services.AddScoped<SessionState>();
    }

    [Fact]
    public void Empty_submit_shows_validation_messages_and_does_not_register()
    {
        var cut = RenderComponent<Register>(p => p.Add(c => c.Id, 3));
        cut.Find("form").Submit();

        Assert.Contains("Nama lengkap wajib diisi", cut.Markup);
        Assert.Contains("Email wajib diisi", cut.Markup);
        Assert.DoesNotContain("Pendaftaran Berhasil", cut.Markup);
    }

    [Fact]
    public void Invalid_email_is_reported()
    {
        var cut = RenderComponent<Register>(p => p.Add(c => c.Id, 3));
        var inputs = cut.FindAll("input");
        inputs[0].Change("Citra Ayu");
        inputs[1].Change("bukan-email");
        cut.Find("form").Submit();

        Assert.Contains("Format email tidak valid", cut.Markup);
    }

    [Fact]
    public void Valid_submit_shows_success_with_registration_code()
    {
        var cut = RenderComponent<Register>(p => p.Add(c => c.Id, 3));
        var inputs = cut.FindAll("input");
        inputs[0].Change("Citra Ayu");
        inputs[1].Change("citra@contoh.id");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Pendaftaran Berhasil", cut.Markup));
        Assert.Contains("Kode registrasi", cut.Markup);
    }

    [Fact]
    public void Duplicate_registration_shows_service_error()
    {
        var cut = RenderComponent<Register>(p => p.Add(c => c.Id, 1));   // andi@contoh.id sudah terdaftar
        var inputs = cut.FindAll("input");
        inputs[0].Change("Andi Pratama");
        inputs[1].Change("andi@contoh.id");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("sudah terdaftar", cut.Markup));
    }

    [Fact]
    public void Unknown_event_shows_not_found_message()
    {
        var cut = RenderComponent<Register>(p => p.Add(c => c.Id, 999_999));
        Assert.Contains("Acara tidak ditemukan", cut.Markup);
    }
}
