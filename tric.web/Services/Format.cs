using System.Globalization;
using tric.lib;

namespace tric.web.Services;

/// <summary>
/// Display helpers shared by the pages.
/// </summary>
public static class Format
{
    public static readonly string[] Currencies =
    [
        "EUR", "USD", "CHF", "GBP", "JPY", "CAD", "AUD", "SEK", "NOK", "DKK", "PLN", "CZK", "HUF", "TRY", "THB", "TWD", "CNY", "HKD", "SGD", "NZD", "MXN", "BRL"
    ];

    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("de-DE");

    public static string Money(decimal value, string? currency)
    {
        var amount = value.ToString("N2", Culture);
        return string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase) ? $"{amount}€" : $"{amount} {currency}".Trim();
    }

    public static string Date(DateTime value) => value.ToString("dd.MM.yyyy", Culture);

    public static string MemberName(RegistryMembershipNonUser? member)
        => member?.Alias?.DisplayName
           ?? member?.Alias?.Pointer?.Name
           ?? member?.Alias?.Pointer?.Value
           ?? "?";

    public static string MemberName(RegistryMembershipWrapper? member) => MemberName(member?.RegistryMembershipNonUser);

    /// <summary>
    /// Standard categories of the api with their German label and Material icon.
    /// </summary>
    public static readonly (EntryCategory Key, string Label, string Icon)[] Categories =
    [
        (EntryCategory.Uncategorized, "Ohne Kategorie", MudBlazor.Icons.Material.Filled.Receipt),
        (EntryCategory.Food_and_Drink, "Essen & Trinken", MudBlazor.Icons.Material.Filled.Restaurant),
        (EntryCategory.Groceries, "Lebensmittel", MudBlazor.Icons.Material.Filled.LocalGroceryStore),
        (EntryCategory.Transport, "Transport", MudBlazor.Icons.Material.Filled.DirectionsCar),
        (EntryCategory.Travel, "Reisen", MudBlazor.Icons.Material.Filled.Flight),
        (EntryCategory.Entertainment, "Unterhaltung", MudBlazor.Icons.Material.Filled.Movie),
        (EntryCategory.Shopping, "Einkaufen", MudBlazor.Icons.Material.Filled.ShoppingBag),
        (EntryCategory.Rent_and_Utilities, "Miete & Nebenkosten", MudBlazor.Icons.Material.Filled.Home),
        (EntryCategory.Healthcare, "Gesundheit", MudBlazor.Icons.Material.Filled.MedicalServices),
        (EntryCategory.Insurance, "Versicherung", MudBlazor.Icons.Material.Filled.Security),
        (EntryCategory.Other, "Sonstiges", MudBlazor.Icons.Material.Filled.MoreHoriz)
    ];

    public static string CategoryIcon(EntryCategory? category)
        => Categories.FirstOrDefault(c => c.Key == category).Icon
           ?? MudBlazor.Icons.Material.Filled.Receipt;

    public static bool IsArchived(Registry registry) => string.Equals(registry.Status, "READ_ONLY", StringComparison.OrdinalIgnoreCase);

    public static IEnumerable<RegistryEntry> Entries(Registry registry)
        => (registry.AllRegistryEntry ?? []).Select(e => e.RegistryEntry).Where(e => e is not null).Select(e => e!);

    public static IEnumerable<RegistryMembershipNonUser> Members(Registry registry)
        => (registry.Memberships ?? []).Select(m => m.RegistryMembershipNonUser).Where(m => m is not null).Select(m => m!);

    /// <summary>
    /// Parses the api date format "yyyy-MM-dd HH:mm:ss.ffffff".
    /// </summary>
    public static DateTime ParseDate(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : DateTime.MinValue;
}
