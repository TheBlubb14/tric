using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace tric.lib;

public sealed class Api : IDisposable
{
    private const string BaseUrl = "https://api.tricount.bunq.com";
    private const string ClientRequestId = "049bfcdf-6ae4-4cee-af7b-45da31ea85d0";
    private const string UserAgent = "com.bunq.tricount.android:RELEASE:7.0.7:3174:ANDROID:13:C";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    public string? AuthToken { get; private set; }
    public long? UserId { get; private set; }

    private readonly ILogger<Api> _logger;
    private readonly bool _readOnly;

    public Api(ILogger<Api> logger, HttpClient? httpClient = null, bool readOnly = true)
    {
        _logger = logger;
        _ownsHttpClient = httpClient is null;
        _http = httpClient ?? new HttpClient();
        _http.BaseAddress ??= new Uri(BaseUrl);
        _readOnly = readOnly;

        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        }
        if (!_http.DefaultRequestHeaders.Contains("X-Bunq-Client-Request-Id"))
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Bunq-Client-Request-Id", ClientRequestId);
        }
    }

    private async Task<string> ReadAndLogResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("HTTP {Method} {Url} returned {StatusCode}: {Content}",
                response.RequestMessage?.Method,
                response.RequestMessage?.RequestUri,
                (int)response.StatusCode,
                content);
        }

        return content;
    }

    /// <summary>
    /// Registers the app installation and authenticates, storing the token and user id.
    /// </summary>
    public async Task<SessionRegistrationResult> RegisterSessionAsync(
        string appInstallationUuid,
        string clientPublicKeyPem,
        string deviceDescription = "Android",
        CancellationToken cancellationToken = default)
    {
        SetAppId(appInstallationUuid);

        var request = new SessionRegistrationRequest
        {
            AppInstallationUuid = appInstallationUuid,
            ClientPublicKey = clientPublicKeyPem,
            DeviceDescription = deviceDescription
        };

        using var response = await _http.PostAsJsonAsync("/v1/session-registry-installation", request, JsonOptions, cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<SessionRegistrationItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        var token = envelope.Response.Select(i => i.Token).FirstOrDefault(t => t is not null)
            ?? throw new InvalidOperationException("No token in response.");
        var user = envelope.Response.Select(i => i.UserPerson).FirstOrDefault(u => u is not null)
            ?? throw new InvalidOperationException("No user in response.");

        AuthToken = token.Value;
        UserId = user.Id;

        SetAuthHeader(AuthToken);

        return new SessionRegistrationResult(
            token,
            user,
            envelope.Response.Select(i => i.EncryptionKey).FirstOrDefault(e => e is not null),
            envelope.Response.Select(i => i.TokenDevice).FirstOrDefault(t => t is not null),
            envelope.Response.Select(i => i.ServerPublicKey).FirstOrDefault(k => k is not null),
            envelope.Response.Select(i => i.DeviceBrowser).FirstOrDefault(d => d is not null),
            envelope.Response.Select(i => i.Id).FirstOrDefault(i => i is not null)?.Id);
    }

    /// <summary>
    /// Fetches tricounts accessible to the authenticated user.
    /// </summary>
    public async Task<List<Registry>> GetRegistriesAsync(
        long userId,
        string? publicIdentifierToken = null,
        long? registryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrEmpty(publicIdentifierToken))
        {
            query.Add($"public_identifier_token={Uri.EscapeDataString(publicIdentifierToken)}");
        }
        if (registryId.HasValue)
        {
            query.Add($"registry_id={registryId.Value}");
        }

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;

        using var response = await _http.GetAsync($"/v1/user/{userId}/registry{queryString}", cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<RegistryItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        return [.. envelope.Response
            .Select(i => i.Registry)
            .Where(r => r is not null)
            .Select(r => r!)];
    }

    /// <summary>
    /// Syncs tricounts to the account, allowing join via sharing token without membership.
    /// </summary>
    public async Task<List<Registry>> SynchronizeRegistriesAsync(
        long userId,
        IEnumerable<string>? activeTokens = null,
        IEnumerable<string>? archivedTokens = null,
        IEnumerable<string>? deletedTokens = null,
        CancellationToken cancellationToken = default)
    {
        var request = new RegistrySynchronizationRequest
        {
            AllRegistryActive = [.. (activeTokens ?? []).Select(t => new PublicIdentifierTokenRef { PublicIdentifierToken = t })],
            AllRegistryArchived = [.. (archivedTokens ?? []).Select(t => new PublicIdentifierTokenRef { PublicIdentifierToken = t })],
            AllRegistryDeleted = [.. (deletedTokens ?? []).Select(t => new PublicIdentifierTokenRef { PublicIdentifierToken = t })]
        };

        using var response = await _http.PostAsJsonAsync($"/v1/user/{userId}/registry-synchronization", request, JsonOptions, cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<RegistrySynchronizationWrapper>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        return [.. envelope.Response
            .SelectMany(i => i.RegistrySynchronization?.AllRegistryActive)
            .Where(r => r is not null)];
    }

    /// <summary>
    /// Creates a new tricount.
    /// </summary>
    public async Task<long> CreateRegistryAsync(
        long userId,
        string currency,
        string title,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot create registry.");
        }

        var request = new CreateRegistryRequest
        {
            Currency = currency,
            Title = title,
            Description = description
        };

        using var response = await _http.PostAsJsonAsync($"/v1/user/{userId}/registry", request, JsonOptions, cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<IdItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        var id = envelope.Response.Select(i => i.Id).FirstOrDefault(i => i is not null)
            ?? throw new InvalidOperationException("No id in response.");

        return id.Id;
    }

    /// <summary>
    /// Deletes a tricount permanently.
    /// </summary>
    public async Task<long> DeleteRegistryAsync(long userId, long registryId, CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot delete registry.");
        }

        using var response = await _http.DeleteAsync($"/v1/user/{userId}/registry/{registryId}", cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<IdItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        var id = envelope.Response.Select(i => i.Id).FirstOrDefault(i => i is not null)
            ?? throw new InvalidOperationException("No id in response.");

        return id.Id;
    }

    /// <summary>
    /// Updates tricount metadata (title/emoji/category/status/membership_uuid_active) and/or memberships.
    /// </summary>
    public async Task<long> UpdateRegistryAsync(
        long userId,
        long registryId,
        UpdateRegistryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot update registry.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/v1/user/{userId}/registry/{registryId}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<IdItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        var id = envelope.Response.Select(i => i.Id).FirstOrDefault(i => i is not null)
            ?? throw new InvalidOperationException("No id in response.");

        return id.Id;
    }

    /// <summary>
    /// Lists all memberships of a tricount.
    /// </summary>
    public async Task<List<RegistryMembershipNonUser>> GetRegistryMembershipsAsync(
        long userId,
        long registryId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"/v1/user/{userId}/registry/{registryId}/registry-membership", cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<RegistryMembershipWrapper>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        return [.. envelope.Response
            .Select(i => i.RegistryMembershipNonUser)
            .Where(m => m is not null)
            .Select(m => m!)];
    }

    /// <summary>
    /// Updates membership settings. Note: the server does not persist member name changes here, use <see cref="UpdateRegistryAsync"/> instead.
    /// </summary>
    public async Task<long> UpdateRegistryMembershipAsync(
        long userId,
        long registryId,
        long membershipId,
        UpdateMembershipRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot update registry membership.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/v1/user/{userId}/registry/{registryId}/registry-membership/{membershipId}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();
        return ReadId(content);
    }

    /// <summary>
    /// Creates a new transaction (expense, income or reimbursement).
    /// </summary>
    public async Task<long> CreateRegistryEntryAsync(
        long userId,
        long registryId,
        RegistryEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot create registry entry.");
        }

        using var response = await _http.PostAsJsonAsync($"/v1/user/{userId}/registry/{registryId}/registry-entry", request, JsonOptions, cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();
        return ReadId(content);
    }

    /// <summary>
    /// Edits an existing transaction. All fields are required (not a partial update).
    /// </summary>
    public async Task<long> UpdateRegistryEntryAsync(
        long userId,
        long registryId,
        long entryId,
        RegistryEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot update registry entry.");
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/v1/user/{userId}/registry/{registryId}/registry-entry/{entryId}")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();
        return ReadId(content);
    }

    /// <summary>
    /// Deletes a transaction.
    /// </summary>
    public async Task DeleteRegistryEntryAsync(long userId, long registryId, long entryId, CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot delete registry entry.");
        }

        using var response = await _http.DeleteAsync($"/v1/user/{userId}/registry/{registryId}/registry-entry/{entryId}", cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets the authenticated user profile.
    /// </summary>
    public async Task<UserPerson> GetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"/v1/user/{userId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<ResponseEnvelope<SessionRegistrationItem>>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from server.");

        return envelope.Response.Select(i => i.UserPerson).FirstOrDefault(u => u is not null)
            ?? throw new InvalidOperationException("No user in response.");
    }

    /// <summary>
    /// Lists all gallery attachments of a tricount.
    /// </summary>
    public async Task<List<RegistryGalleryAttachment>> GetGalleryAttachmentsAsync(
        long userId,
        long registryId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"/v1/user/{userId}/registry/{registryId}/gallery-attachment", cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<RegistryGalleryAttachmentItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        return [.. envelope.Response
            .Select(i => i.RegistryGalleryAttachment)
            .Where(a => a is not null)
            .Select(a => a!)];
    }

    /// <summary>
    /// Uploads an image to the tricount gallery and returns its uuid.
    /// </summary>
    public async Task<string> UploadGalleryAttachmentAsync(
        long userId,
        long registryId,
        string uuid,
        byte[] content,
        string contentType = "image/jpeg",
        string description = "",
        CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot upload gallery attachment.");
        }

        using var httpRequest = CreateAttachmentRequest($"/v1/user/{userId}/registry/{registryId}/gallery-attachment/{uuid}", content, contentType, description);

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var responseContent = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<UuidItem>>(responseContent, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        var result = envelope.Response.Select(i => i.Uuid).FirstOrDefault(u => u is not null)
            ?? throw new InvalidOperationException("No uuid in response.");

        return result.Uuid;
    }

    /// <summary>
    /// Deletes a gallery attachment.
    /// </summary>
    public async Task<long> DeleteGalleryAttachmentAsync(long userId, long registryId, string uuid, CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot delete gallery attachment.");
        }

        using var response = await _http.DeleteAsync($"/v1/user/{userId}/registry/{registryId}/gallery-attachment/{uuid}", cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();
        return ReadId(content);
    }

    /// <summary>
    /// Uploads an attachment (e.g. receipt photo) for transactions and returns its id.
    /// </summary>
    public async Task<long> UploadAttachmentAsync(
        long userId,
        long registryId,
        byte[] content,
        string contentType = "image/jpeg",
        string description = "",
        CancellationToken cancellationToken = default)
    {
        if (_readOnly)
        {
            throw new InvalidOperationException("API is in read-only mode, cannot upload attachment.");
        }

        using var httpRequest = CreateAttachmentRequest($"/v1/user/{userId}/registry/{registryId}/attachment", content, contentType, description);

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var responseContent = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();
        return ReadId(responseContent);
    }

    /// <summary>
    /// Gets exchange rates from a source currency to all supported currencies.
    /// </summary>
    public async Task<List<ExchangeRate>> GetExchangeRatesAsync(long userId, string currency, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"/v1/user/{userId}/exchange-rate?currency={Uri.EscapeDataString(currency)}", cancellationToken);
        var content = await ReadAndLogResponse(response);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<ExchangeRateItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        return [.. envelope.Response
            .Select(i => i.ExchangeRate)
            .Where(r => r is not null)
            .Select(r => r!)];
    }

    private static HttpRequestMessage CreateAttachmentRequest(string url, byte[] content, string contentType, string description)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(content)
        };
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        httpRequest.Headers.TryAddWithoutValidation("X-Bunq-Attachment-Description", description);

        return httpRequest;
    }

    private static long ReadId(string content)
    {
        var envelope = JsonSerializer.Deserialize<ResponseEnvelope<IdItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from server.");

        var id = envelope.Response.Select(i => i.Id).FirstOrDefault(i => i is not null)
            ?? throw new InvalidOperationException("No id in response.");

        return id.Id;
    }

    /// <summary>
    /// Restores a previously registered session without contacting the server.
    /// </summary>
    public void RestoreSession(string appInstallationUuid, string authToken, long userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appInstallationUuid);
        ArgumentException.ThrowIfNullOrWhiteSpace(authToken);

        SetAppId(appInstallationUuid);
        AuthToken = authToken;
        UserId = userId;
        SetAuthHeader(authToken);
    }

    private void SetAppId(string appId)
    {
        _http.DefaultRequestHeaders.Remove("app-id");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("app-id", appId);
    }

    private void SetAuthHeader(string token)
    {
        _http.DefaultRequestHeaders.Remove("X-Bunq-Client-Authentication");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Bunq-Client-Authentication", token);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }
}

public sealed record SessionRegistrationResult(
    SessionToken Token,
    UserPerson UserPerson,
    EncryptionKey? EncryptionKey,
    DeviceToken? DeviceToken,
    ServerPublicKey? ServerPublicKey,
    DeviceBrowser? DeviceBrowser,
    long? SessionId);

#region Common envelope

public sealed class ResponseEnvelope<T>
{
    [JsonPropertyName("Response")]
    public List<T> Response { get; set; } = new();
}

public sealed class IdItem
{
    [JsonPropertyName("Id")]
    public IdValue? Id { get; set; }
}

public sealed class IdValue
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

#endregion

#region Session registration

// Session response models live in SessionModels.cs

#endregion

#region Registry (tricount)

public sealed class RegistryItem
{
    [JsonPropertyName("Registry")]
    public Registry? Registry { get; set; }
}

public sealed class RegistrySynchronizationWrapper
{
    [JsonPropertyName("RegistrySynchronization")]
    public RegistrySynchronization RegistrySynchronization { get; set; }
}

public sealed class RegistrySynchronization
{
    [JsonPropertyName("all_registry_active")]
    public Registry[] AllRegistryActive { get; set; } = [];

    [JsonPropertyName("all_registry_archived")]
    public Registry[] AllRegistryArchived { get; set; } = [];
}

[DebuggerDisplay("{Title ?? Description}")]
public sealed class Registry
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("public_identifier_token")]
    public string? PublicIdentifierToken { get; set; }

    [JsonPropertyName("memberships")]
    public List<RegistryMembershipWrapper>? Memberships { get; set; }

    [JsonPropertyName("all_registry_entry")]
    public List<RegistryEntryItem>? AllRegistryEntry { get; set; }

    [JsonPropertyName("membership_uuid_active")]
    public string? MembershipUuidActive { get; set; }

    public override string ToString() => $"{Title ?? Description} ({Currency}) [{Status}]";
}

[DebuggerDisplay("{Alias?.Name ?? Uuid}")]
public sealed class Membership
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("auto_add_card_transaction")]
    public string? AutoAddCardTransaction { get; set; }

    [JsonPropertyName("setting")]
    public JsonElement? Setting { get; set; }

    [JsonPropertyName("alias")]
    public MembershipAlias? Alias { get; set; }

    public override string ToString() => Alias?.ToString() ?? Uuid;
}

[DebuggerDisplay("{Name}")]
public sealed class MembershipAlias
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "UUID";

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    override public string ToString() => $"{Name}";
}

#endregion

#region Registry synchronization

public sealed class RegistrySynchronizationRequest
{
    [JsonPropertyName("all_registry_active")]
    public List<PublicIdentifierTokenRef> AllRegistryActive { get; set; } = new();

    [JsonPropertyName("all_registry_archived")]
    public List<PublicIdentifierTokenRef> AllRegistryArchived { get; set; } = new();

    [JsonPropertyName("all_registry_deleted")]
    public List<PublicIdentifierTokenRef> AllRegistryDeleted { get; set; } = new();
}

public sealed class PublicIdentifierTokenRef
{
    [JsonPropertyName("public_identifier_token")]
    public string PublicIdentifierToken { get; set; } = string.Empty;
}

#endregion

#region Create registry

public sealed class CreateRegistryRequest
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

#endregion

#region Update registry

public sealed class UpdateRegistryRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("emoji")]
    public string? Emoji { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("membership_uuid_active")]
    public string? MembershipUuidActive { get; set; }

    [JsonPropertyName("memberships")]
    public List<Membership>? Memberships { get; set; }

    [JsonPropertyName("deleted_membership_ids")]
    public List<long>? DeletedMembershipIds { get; set; }
}

#endregion