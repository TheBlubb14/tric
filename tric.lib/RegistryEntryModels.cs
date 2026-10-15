using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tric.lib;

[DebuggerDisplay("{RegistryEntry}")]
public sealed class RegistryEntryItem
{
    [JsonPropertyName("RegistryEntry")]
    public RegistryEntry? RegistryEntry { get; set; }

    public override string ToString() => $"{RegistryEntry}";
}

[DebuggerDisplay("{Description} {Amount} {Date}")]
public sealed class RegistryEntry
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("status")]
    public EntryStatus? Status { get; set; }

    [JsonPropertyName("amount")]
    public Amount? Amount { get; set; }

    [JsonPropertyName("amount_local")]
    public Amount? AmountLocal { get; set; }

    [JsonPropertyName("exchange_rate")]
    public string? ExchangeRate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("type_transaction")]
    public string? TypeTransaction { get; set; }

    [JsonPropertyName("membership_owned")]
    public RegistryMembershipWrapper? MembershipOwned { get; set; }

    [JsonPropertyName("allocations")]
    public List<RegistryEntryAllocation>? Allocations { get; set; }

    [JsonPropertyName("attachment")]
    public List<JsonElement>? Attachment { get; set; }

    [JsonPropertyName("category")]
    public EntryCategory? Category { get; set; }

    [JsonPropertyName("category_custom")]
    public string? CategoryCustom { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    public override string ToString() => $"{Description} {Amount} {Date}";
}

[JsonConverter(typeof(JsonStringEnumConverter<EntryCategory>))]
public enum EntryCategory
{
    [JsonStringEnumMemberName("GENERAL")]
    General,

    [JsonStringEnumMemberName("SUBSCRIPTIONS")]
    Subscriptions,

    [JsonStringEnumMemberName("CASH")]
    Cash,

    [JsonStringEnumMemberName("SAVINGS")]
    Savings,

    [JsonStringEnumMemberName("FINANCE")]
    Finance,

    [JsonStringEnumMemberName("TRANSPORT")]
    Transport,

    [JsonStringEnumMemberName("FAMILY")]
    Family,

    [JsonStringEnumMemberName("SHOPPING")]
    Shopping,

    [JsonStringEnumMemberName("ENTERTAINMENT")]
    Entertainment,

    [JsonStringEnumMemberName("FOOD_AND_DRINK")]
    Food_and_Drink,

    [JsonStringEnumMemberName("GROCERIES")]
    Groceries,

    [JsonStringEnumMemberName("PERSONAL_CARE")]
    Personal_Care,

    [JsonStringEnumMemberName("HOUSEHOLD_EXPENSES")]
    Household_Expenses,

    [JsonStringEnumMemberName("TRAVEL")]
    Travel,

    [JsonStringEnumMemberName("BUSINESS_EXPENSES")]
    Business_Expenses,

    [JsonStringEnumMemberName("CAR_EXPENSES")]
    Car_Expenses,

    [JsonStringEnumMemberName("UNCATEGORIZED")]
    Uncategorized,

    [JsonStringEnumMemberName("ELECTRONICS")]
    Electronics,

    [JsonStringEnumMemberName("INVESTMENTS")]
    Investments,

    [JsonStringEnumMemberName("CULTURE")]
    Culture,

    [JsonStringEnumMemberName("HEALTHCARE")]
    Healthcare,

    [JsonStringEnumMemberName("PETS")]
    Pets,

    [JsonStringEnumMemberName("CLOTHING")]
    Clothing,

    [JsonStringEnumMemberName("SPORTS")]
    Sports,

    [JsonStringEnumMemberName("GIFTS")]
    Gifts,

    [JsonStringEnumMemberName("PAYROLL")]
    Payroll,

    [JsonStringEnumMemberName("HR")]
    Hr,

    [JsonStringEnumMemberName("MARKETING")]
    Marketing,

    [JsonStringEnumMemberName("RENT_AND_UTILITIES")]
    Rent_and_Utilities,

    [JsonStringEnumMemberName("INSURANCE")]
    Insurance,

    [JsonStringEnumMemberName("EMPLOYEE_BENEFITS")]
    Employee_Benefits,

    [JsonStringEnumMemberName("OFFICE_SUPPLIES")]
    Office_Supplies,

    [JsonStringEnumMemberName("ASSETS")]
    Assets,

    [JsonStringEnumMemberName("PROFESSIONAL_SERVICES")]
    Professional_Services,

    [JsonStringEnumMemberName("OTHER")]
    Other,

    [JsonStringEnumMemberName("USER_DEFINED_1")]
    User_Defined_1,

    [JsonStringEnumMemberName("USER_DEFINED_2")]
    User_Defined_2,

    [JsonStringEnumMemberName("USER_DEFINED_3")]
    User_Defined_3,

    [JsonStringEnumMemberName("USER_DEFINED_4")]
    User_Defined_4,

    [JsonStringEnumMemberName("USER_DEFINED_5")]
    User_Defined_5,

    [JsonStringEnumMemberName("USER_DEFINED_6")]
    User_Defined_6,

    [JsonStringEnumMemberName("USER_DEFINED_7")]
    User_Defined_7,

    [JsonStringEnumMemberName("USER_DEFINED_8")]
    User_Defined_8,

    [JsonStringEnumMemberName("USER_DEFINED_9")]
    User_Defined_9,

    [JsonStringEnumMemberName("INCOME")]
    Income,
}

[DebuggerDisplay("{Amount} {Membership} {Type} {ShareRatio}")]
public sealed class RegistryEntryAllocation
{
    [JsonPropertyName("amount")]
    public Amount? Amount { get; set; }

    [JsonPropertyName("amount_local")]
    public Amount? AmountLocal { get; set; }

    [JsonPropertyName("membership")]
    public RegistryMembershipWrapper? Membership { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("share_ratio")]
    public decimal? ShareRatio { get; set; }

    override public string ToString() => $"{Amount} {Membership} {Type} {ShareRatio}";
}

[DebuggerDisplay("{RegistryMembershipNonUser}")]
public sealed class RegistryMembershipWrapper
{
    [JsonPropertyName("RegistryMembershipNonUser")]
    public RegistryMembershipNonUser? RegistryMembershipNonUser { get; set; }

    public override string ToString() => $"{RegistryMembershipNonUser}";
}

[DebuggerDisplay("{Alias}")]
public sealed class RegistryMembershipNonUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("alias")]
    public RegistryMembershipAlias? Alias { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("setting")]
    public RegistryMembershipSetting? Setting { get; set; }

    [JsonPropertyName("auto_add_card_transaction")]
    public string? AutoAddCardTransaction { get; set; }

    [JsonPropertyName("remover")]
    public Remover? Remover { get; set; }

    public override string ToString() => $"{Alias}";
}

[DebuggerDisplay("{DisplayName}")]
public sealed class Remover
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    public override string ToString() => DisplayName;
}

[DebuggerDisplay("{DisplayName}")]
public sealed class RegistryMembershipAlias
{
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("pointer")]
    public RegistryMembershipPointer? Pointer { get; set; }

    override public string ToString() => DisplayName ?? Pointer?.Value ?? "";
}

public sealed class RegistryMembershipPointer
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class RegistryMembershipSetting
{
    [JsonPropertyName("auto_add_card_transaction")]
    public string? AutoAddCardTransaction { get; set; }

    [JsonPropertyName("time_auto_add_card_transaction_end")]
    public string? TimeAutoAddCardTransactionEnd { get; set; }

    [JsonPropertyName("time_auto_add_card_transaction_start")]
    public string? TimeAutoAddCardTransactionStart { get; set; }

    [JsonPropertyName("card_ids")]
    public List<long>? CardIds { get; set; }

    [JsonPropertyName("card_labels")]
    public List<string>? CardLabels { get; set; }
}
