using System.ComponentModel.DataAnnotations;
using EventEase.Models;

namespace EventEase.Tests;

public class ValidationTests
{
    private static EventItem Valid() => new()
    {
        Id = 1, Name = "Acara", Date = new DateTime(2027, 1, 1), Location = "Surabaya", Capacity = 10
    };

    [Fact] public void Valid_event_has_no_errors() => Assert.True(EventItemValidator.IsValid(Valid()));

    [Fact] public void Null_event_is_invalid() => Assert.False(EventItemValidator.IsValid(null));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_name_is_invalid(string name)
    {
        var e = Valid(); e.Name = name;
        Assert.False(EventItemValidator.IsValid(e));
    }

    [Fact]
    public void Multiple_problems_are_all_reported()
    {
        var e = new EventItem { Id = 0, Name = "", Location = "", Date = default, Capacity = -1 };
        Assert.Equal(5, EventItemValidator.GetErrors(e).Count);
    }

    [Fact]
    public void Too_long_name_is_invalid()
    {
        var e = Valid(); e.Name = new string('x', EventItemValidator.MaxNameLength + 1);
        Assert.False(EventItemValidator.IsValid(e));
    }

    private static bool FormValid(RegistrationForm f) =>
        Validator.TryValidateObject(f, new ValidationContext(f), new List<ValidationResult>(), true);

    [Theory]
    [InlineData("Budi Santoso")]
    [InlineData("Siti Nur-Aini")]
    [InlineData("Dr. O'Neil")]
    [InlineData("Ñandú Pérez")]
    public void Realistic_names_are_valid(string name) =>
        Assert.True(FormValid(new() { FullName = name, Email = "a@b.com", Tickets = 1 }));

    [Fact] public void Good_form_is_valid() =>
        Assert.True(FormValid(new() { FullName = "Budi Santoso", Email = "budi@mail.com", Tickets = 2 }));

    [Theory]
    [InlineData("", "a@b.com", 1)]
    [InlineData("  ", "a@b.com", 1)]
    [InlineData("Bu", "a@b.com", 1)]
    [InlineData("Budi", "", 1)]
    [InlineData("Budi", "bukan-email", 1)]
    [InlineData("Budi", "a@b.com", 0)]
    [InlineData("Budi", "a@b.com", 11)]
    [InlineData("Budi", "a@b.com", -3)]
    [InlineData("B0di", "a@b.com", 1)]
    [InlineData("Budi<script>", "a@b.com", 1)]
    [InlineData("  Budi", "a@b.com", 1)]
    public void Bad_forms_are_rejected(string name, string email, int tickets) =>
        Assert.False(FormValid(new() { FullName = name, Email = email, Tickets = tickets }));
}
