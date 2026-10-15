using System.Diagnostics;
using System.Text.Json.Serialization;

namespace tric.lib;

#region Membership update

public sealed class UpdateMembershipRequest
{
    [JsonPropertyName("alias")]
    public MembershipAlias? Alias { get; set; }
}

#endregion

#region Registry entry (transaction) requests

public sealed class RegistryEntryRequest
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("amount")]
    public Amount? Amount { get; set; }

    [JsonPropertyName("amount_local")]
    public Amount? AmountLocal { get; set; }

    [JsonPropertyName("exchange_rate")]
    public string? ExchangeRate { get; set; }

    [JsonPropertyName("membership_uuid_owner")]
    public string? MembershipUuidOwner { get; set; }

    [JsonPropertyName("allocations")]
    public List<RegistryEntryAllocationRequest>? Allocations { get; set; }

    /// <summary>NORMAL (expense), INCOME or BALANCE (reimbursement).</summary>
    [JsonPropertyName("type_transaction")]
    public string? TypeTransaction { get; set; }

    [JsonPropertyName("status")]
    public EntryStatus? Status { get; set; }

    /// <summary>Format: YYYY-MM-DD HH:MM:SS.ffffff</summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("category")]
    public EntryCategory? Category { get; set; }

    [JsonPropertyName("category_custom")]
    public string? CategoryCustom { get; set; }

    [JsonPropertyName("attachment")]
    public List<EntryAttachmentRef>? Attachment { get; set; }
}

public sealed class EntryAttachmentRef
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<EntryStatus>))]
public enum EntryStatus
{
    [JsonStringEnumMemberName("ACTIVE")]
    Active,
    [JsonStringEnumMemberName("SETTLED")]
    Settled,
    [JsonStringEnumMemberName("INACTIVE")]
    Inactive
}
public sealed class RegistryEntryAllocationRequest
{
    [JsonPropertyName("membership_uuid")]
    public string MembershipUuid { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public Amount? Amount { get; set; }

    [JsonPropertyName("amount_local")]
    public Amount? AmountLocal { get; set; }

    /// <summary>AMOUNT or RATIO.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "AMOUNT";

    [JsonPropertyName("share_ratio")]
    public decimal? ShareRatio { get; set; }
}

#endregion

#region Gallery attachments

public sealed class RegistryGalleryAttachmentItem
{
    [JsonPropertyName("RegistryGalleryAttachment")]
    public RegistryGalleryAttachment? RegistryGalleryAttachment { get; set; }
}

public sealed class RegistryGalleryAttachment
{
    [JsonPropertyName("attachment")]
    public AttachmentInfo? Attachment { get; set; }

    [JsonPropertyName("membership_uuid")]
    public string? MembershipUuid { get; set; }
}

[DebuggerDisplay("{Uuid} {ContentType}")]
public sealed class AttachmentInfo
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("urls")]
    public List<AttachmentUrl>? Urls { get; set; }
}

public sealed class AttachmentUrl
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public sealed class UuidItem
{
    [JsonPropertyName("UUID")]
    public UuidValue? Uuid { get; set; }
}

public sealed class UuidValue
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;
}

#endregion

#region Exchange rate

public sealed class ExchangeRateItem
{
    [JsonPropertyName("ExchangeRate")]
    public ExchangeRate? ExchangeRate { get; set; }
}

[DebuggerDisplay("{CurrencySource} -> {CurrencyTarget} {Rate}")]
public sealed class ExchangeRate
{
    [JsonPropertyName("currency_source")]
    public string? CurrencySource { get; set; }

    [JsonPropertyName("currency_target")]
    public string? CurrencyTarget { get; set; }

    [JsonPropertyName("rate")]
    public string? Rate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("number_of_decimal")]
    public int? NumberOfDecimal { get; set; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
}

#endregion
