using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tric.lib;

public sealed class SessionRegistrationRequest
{
    [JsonPropertyName("app_installation_uuid")]
    public string AppInstallationUuid { get; set; } = string.Empty;

    [JsonPropertyName("client_public_key")]
    public string ClientPublicKey { get; set; } = string.Empty;

    [JsonPropertyName("device_description")]
    public string DeviceDescription { get; set; } = "Android";
}

public sealed class SessionRegistrationItem
{
    [JsonPropertyName("Id")]
    public IdValue? Id { get; set; }

    [JsonPropertyName("Token")]
    public SessionToken? Token { get; set; }

    [JsonPropertyName("EncryptionKey")]
    public EncryptionKey? EncryptionKey { get; set; }

    [JsonPropertyName("UserPerson")]
    public UserPerson? UserPerson { get; set; }

    [JsonPropertyName("RegistryOverview")]
    public RegistryOverview? RegistryOverview { get; set; }

    [JsonPropertyName("RegistryArchivedOverview")]
    public RegistryOverview? RegistryArchivedOverview { get; set; }

    [JsonPropertyName("TokenDevice")]
    public DeviceToken? TokenDevice { get; set; }

    [JsonPropertyName("ServerPublicKey")]
    public ServerPublicKey? ServerPublicKey { get; set; }

    [JsonPropertyName("DeviceBrowser")]
    public DeviceBrowser? DeviceBrowser { get; set; }
}

public sealed class SessionToken
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("token")]
    public string Value { get; set; } = string.Empty;
}

public sealed class DeviceToken
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("token")]
    public string Value { get; set; } = string.Empty;
}

public sealed class EncryptionKey
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("encryption_key")]
    public string? Key { get; set; }
}

public sealed class ServerPublicKey
{
    [JsonPropertyName("server_public_key")]
    public string? Value { get; set; }
}

public sealed class DeviceBrowser
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }

    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class RegistryOverview
{
    [JsonPropertyName("registry")]
    public List<Registry> Registry { get; set; } = new();
}

[DebuggerDisplay("{Value} {Currency}")]
public sealed class Amount
{
    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    public override string ToString() => $"{Value} {Currency}";
}

public sealed class Avatar
{
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("image")]
    public List<AvatarImage>? Image { get; set; }

    [JsonPropertyName("anchor_uuid")]
    public string? AnchorUuid { get; set; }

    [JsonPropertyName("style")]
    public string? Style { get; set; }
}

public sealed class AvatarImage
{
    [JsonPropertyName("attachment_public_uuid")]
    public string? AttachmentPublicUuid { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("urls")]
    public List<AvatarImageUrl>? Urls { get; set; }
}

public sealed class AvatarImageUrl
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public sealed class CustomerLimit
{
    [JsonPropertyName("limit_monetary_account")]
    public int? LimitMonetaryAccount { get; set; }

    [JsonPropertyName("limit_monetary_account_remaining")]
    public int? LimitMonetaryAccountRemaining { get; set; }

    [JsonPropertyName("limit_card_debit_maestro")]
    public int? LimitCardDebitMaestro { get; set; }

    [JsonPropertyName("limit_card_debit_mastercard")]
    public int? LimitCardDebitMastercard { get; set; }

    [JsonPropertyName("limit_card_wildcard")]
    public int? LimitCardWildcard { get; set; }

    [JsonPropertyName("limit_card_debit_wildcard")]
    public int? LimitCardDebitWildcard { get; set; }

    [JsonPropertyName("limit_card_debit_maestro_virtual_subscription")]
    public int? LimitCardDebitMaestroVirtualSubscription { get; set; }

    [JsonPropertyName("limit_card_debit_maestro_virtual_total")]
    public int? LimitCardDebitMaestroVirtualTotal { get; set; }

    [JsonPropertyName("limit_card_debit_mastercard_virtual_subscription")]
    public int? LimitCardDebitMastercardVirtualSubscription { get; set; }

    [JsonPropertyName("limit_card_debit_mastercard_virtual_total")]
    public int? LimitCardDebitMastercardVirtualTotal { get; set; }

    [JsonPropertyName("limit_card_replacement")]
    public int? LimitCardReplacement { get; set; }

    [JsonPropertyName("limit_amount_monthly")]
    public Amount? LimitAmountMonthly { get; set; }

    [JsonPropertyName("spent_amount_monthly")]
    public Amount? SpentAmountMonthly { get; set; }

    [JsonPropertyName("limit_card_credit_mastercard")]
    public int? LimitCardCreditMastercard { get; set; }
}

public sealed class Fulfillment
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("reason_translated")]
    public string? ReasonTranslated { get; set; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("time_mandatory")]
    public string? TimeMandatory { get; set; }

    [JsonPropertyName("all_status_allowed")]
    public List<string>? AllStatusAllowed { get; set; }

    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("created_timestamp")]
    public string? CreatedTimestamp { get; set; }

    [JsonPropertyName("updated_timestamp")]
    public string? UpdatedTimestamp { get; set; }

    [JsonPropertyName("user_information_inquiry_id")]
    public long? UserInformationInquiryId { get; set; }
}

public sealed class UserPerson
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("updated")]
    public string? Updated { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("sub_status")]
    public string? SubStatus { get; set; }

    [JsonPropertyName("public_uuid")]
    public string? PublicUuid { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("public_nick_name")]
    public string? PublicNickName { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("session_timeout")]
    public int? SessionTimeout { get; set; }

    [JsonPropertyName("daily_limit_without_confirmation_login")]
    public Amount? DailyLimitWithoutConfirmationLogin { get; set; }

    [JsonPropertyName("avatar")]
    public Avatar? Avatar { get; set; }

    [JsonPropertyName("relations")]
    public List<JsonElement>? Relations { get; set; }

    [JsonPropertyName("alias")]
    public List<JsonElement>? Alias { get; set; }

    [JsonPropertyName("tax_resident")]
    public List<JsonElement>? TaxResident { get; set; }

    [JsonPropertyName("notification_filters")]
    public List<JsonElement>? NotificationFilters { get; set; }

    [JsonPropertyName("user_onboarding")]
    public JsonElement? UserOnboarding { get; set; }

    [JsonPropertyName("address_main")]
    public JsonElement? AddressMain { get; set; }

    [JsonPropertyName("address_postal")]
    public JsonElement? AddressPostal { get; set; }

    [JsonPropertyName("address_shipping")]
    public JsonElement? AddressShipping { get; set; }

    [JsonPropertyName("freeze_user")]
    public JsonElement? FreezeUser { get; set; }

    [JsonPropertyName("user_offboarding")]
    public JsonElement? UserOffboarding { get; set; }

    [JsonPropertyName("debt_collection")]
    public JsonElement? DebtCollection { get; set; }

    [JsonPropertyName("recovery_requirements")]
    public List<JsonElement>? RecoveryRequirements { get; set; }

    [JsonPropertyName("deny_reason")]
    public string? DenyReason { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("middle_name")]
    public string? MiddleName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("legal_name")]
    public string? LegalName { get; set; }

    [JsonPropertyName("date_of_birth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("place_of_birth")]
    public string? PlaceOfBirth { get; set; }

    [JsonPropertyName("country_of_banking_license")]
    public string? CountryOfBankingLicense { get; set; }

    [JsonPropertyName("country_of_banking_license_preferred")]
    public string? CountryOfBankingLicensePreferred { get; set; }

    [JsonPropertyName("country_of_birth")]
    public string? CountryOfBirth { get; set; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; set; }

    [JsonPropertyName("all_nationality")]
    public List<string>? AllNationality { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("verification_status")]
    public string? VerificationStatus { get; set; }

    [JsonPropertyName("version_terms_of_service")]
    public string? VersionTermsOfService { get; set; }

    [JsonPropertyName("customer")]
    public JsonElement? Customer { get; set; }

    [JsonPropertyName("customer_limit")]
    public CustomerLimit? CustomerLimit { get; set; }

    [JsonPropertyName("billing_contract")]
    public List<JsonElement>? BillingContract { get; set; }

    [JsonPropertyName("pack_membership")]
    public JsonElement? PackMembership { get; set; }

    [JsonPropertyName("premium_trial")]
    public JsonElement? PremiumTrial { get; set; }

    [JsonPropertyName("fulfillments")]
    public List<Fulfillment>? Fulfillments { get; set; }
}
