using System.Text.Json;
using tric.lib;

namespace tric.web.Services;

/// <summary>
/// Cache-first access to the tricount data. Every refresh overwrites the cached responses.
/// </summary>
public sealed class DataService(
    SessionService session,
    CacheService cache,
    IHttpClientFactory httpClientFactory,
    ILogger<DataService> logger)
{
    private const string RegistriesKey = "registries";

    /// <summary>
    /// Raised after cached data changed so that views can re-read it.
    /// </summary>
    public event Action? Changed;

    private static string AttachmentsKey(long registryId) => $"attachments.{registryId}";

    #region Registries

    public async Task<List<Registry>> GetCachedRegistriesAsync(CancellationToken cancellationToken = default)
        => await cache.GetAsync<List<Registry>>(RegistriesKey, cancellationToken) ?? [];

    /// <summary>
    /// Reloads all tricounts (including memberships and entries) and replaces the cache.
    /// </summary>
    public async Task<List<Registry>> RefreshRegistriesAsync(CancellationToken cancellationToken = default)
    {
        var registries = await session.ExecuteAsync((api, userId) => api.GetRegistriesAsync(userId, cancellationToken: cancellationToken), cancellationToken);
        await cache.SetAsync(RegistriesKey, registries, cancellationToken);
        Changed?.Invoke();
        return registries;
    }

    /// <summary>
    /// Reloads a single tricount and merges it into the cache.
    /// </summary>
    public async Task<Registry?> RefreshRegistryAsync(long registryId, CancellationToken cancellationToken = default)
    {
        var loaded = await session.ExecuteAsync((api, userId) => api.GetRegistriesAsync(userId, registryId: registryId, cancellationToken: cancellationToken), cancellationToken);
        var registry = loaded.FirstOrDefault(r => r.Id == registryId);

        var cached = await GetCachedRegistriesAsync(cancellationToken);
        cached.RemoveAll(r => r.Id == registryId);
        if (registry is not null)
        {
            cached.Add(registry);
        }

        await cache.SetAsync(RegistriesKey, cached, cancellationToken);
        Changed?.Invoke();
        return registry;
    }

    /// <summary>
    /// Refreshes everything: registries, gallery attachments and the attachment files.
    /// </summary>
    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        var registries = await RefreshRegistriesAsync(cancellationToken);
        foreach (var registry in registries)
        {
            await RefreshAttachmentsAsync(registry, cancellationToken);
        }
    }

    /// <summary>
    /// Joins a tricount by its sharing token (or link) and refreshes the data.
    /// </summary>
    public async Task JoinRegistryAsync(string tokenOrLink, CancellationToken cancellationToken = default)
    {
        var token = ExtractToken(tokenOrLink);
        await session.ExecuteAsync((api, userId) => api.SynchronizeRegistriesAsync(userId, activeTokens: [token], cancellationToken: cancellationToken), cancellationToken);
        await RefreshRegistriesAsync(cancellationToken);
    }

    public async Task<long> CreateRegistryAsync(string title, string currency, string? description, CancellationToken cancellationToken = default)
    {
        var id = await session.ExecuteAsync((api, userId) => api.CreateRegistryAsync(userId, currency, title, description, cancellationToken), cancellationToken);
        await RefreshRegistriesAsync(cancellationToken);
        return id;
    }

    public async Task DeleteRegistryAsync(long registryId, CancellationToken cancellationToken = default)
    {
        await session.ExecuteAsync((api, userId) => api.DeleteRegistryAsync(userId, registryId, cancellationToken), cancellationToken);
        await RefreshRegistriesAsync(cancellationToken);
    }

    public async Task UpdateRegistryAsync(long registryId, UpdateRegistryRequest request, CancellationToken cancellationToken = default)
    {
        await session.ExecuteAsync((api, userId) => api.UpdateRegistryAsync(userId, registryId, request, cancellationToken), cancellationToken);
        await RefreshRegistryAsync(registryId, cancellationToken);
    }

    #endregion

    #region Entries

    public async Task SaveEntryAsync(long registryId, long? entryId, RegistryEntryRequest request, CancellationToken cancellationToken = default)
    {
        await session.ExecuteAsync(async (api, userId) =>
            entryId is long id
                ? await api.UpdateRegistryEntryAsync(userId, registryId, id, request, cancellationToken)
                : await api.CreateRegistryEntryAsync(userId, registryId, request, cancellationToken), cancellationToken);
        await RefreshRegistryAsync(registryId, cancellationToken);
    }

    public async Task DeleteEntryAsync(long registryId, long entryId, CancellationToken cancellationToken = default)
    {
        await session.ExecuteAsync(async (api, userId) =>
        {
            await api.DeleteRegistryEntryAsync(userId, registryId, entryId, cancellationToken);
            return true;
        }, cancellationToken);
        await RefreshRegistryAsync(registryId, cancellationToken);
    }

    #endregion

    #region Attachments

    public async Task<List<RegistryGalleryAttachment>> GetCachedAttachmentsAsync(long registryId, CancellationToken cancellationToken = default)
        => await cache.GetAsync<List<RegistryGalleryAttachment>>(AttachmentsKey(registryId), cancellationToken) ?? [];

    /// <summary>
    /// Reloads the gallery attachments of a tricount and downloads files that are not cached yet.
    /// Also caches receipt attachments referenced by entries.
    /// </summary>
    public async Task RefreshAttachmentsAsync(Registry registry, CancellationToken cancellationToken = default)
    {
        var attachments = await session.ExecuteAsync((api, userId) => api.GetGalleryAttachmentsAsync(userId, registry.Id, cancellationToken), cancellationToken);
        await cache.SetAsync(AttachmentsKey(registry.Id), attachments, cancellationToken);

        foreach (var info in attachments.Select(a => a.Attachment).Where(a => a?.Uuid is not null))
        {
            await DownloadAsync(info!.Uuid!, info.ContentType, SelectUrl(info.Urls), cancellationToken);
        }

        foreach (var attachment in (registry.AllRegistryEntry ?? []).SelectMany(e => e.RegistryEntry?.Attachment ?? []))
        {
            var (id, url) = ReadAttachmentReference(attachment);
            if (id is not null && url is not null)
            {
                await DownloadAsync(id, null, url, cancellationToken);
            }
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// Returns the cached attachment as data url, or null if it is not cached.
    /// </summary>
    public Task<string?> GetAttachmentDataUrlAsync(string id, CancellationToken cancellationToken = default)
        => cache.GetBlobAsync(id, cancellationToken);

    /// <summary>
    /// Uploads a receipt for use in an entry and returns its attachment id.
    /// </summary>
    public Task<long> UploadReceiptAsync(long registryId, byte[] content, string contentType, CancellationToken cancellationToken = default)
        => session.ExecuteAsync((api, userId) => api.UploadAttachmentAsync(userId, registryId, content, contentType, cancellationToken: cancellationToken), cancellationToken);

    /// <summary>
    /// Uploads an image into the gallery of a tricount.
    /// </summary>
    public async Task UploadGalleryImageAsync(Registry registry, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        var uuid = Guid.NewGuid().ToString();
        await session.ExecuteAsync((api, userId) => api.UploadGalleryAttachmentAsync(userId, registry.Id, uuid, content, contentType, cancellationToken: cancellationToken), cancellationToken);
        await RefreshAttachmentsAsync(registry, cancellationToken);
    }

    public async Task DeleteGalleryImageAsync(Registry registry, string uuid, CancellationToken cancellationToken = default)
    {
        await session.ExecuteAsync((api, userId) => api.DeleteGalleryAttachmentAsync(userId, registry.Id, uuid, cancellationToken), cancellationToken);
        await RefreshAttachmentsAsync(registry, cancellationToken);
    }

    private async Task DownloadAsync(string id, string? contentType, string? url, CancellationToken cancellationToken)
    {
        if (url is null || await cache.HasBlobAsync(id, cancellationToken))
        {
            return;
        }

        try
        {
            using var http = httpClientFactory.CreateClient();
            using var response = await http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var type = response.Content.Headers.ContentType?.MediaType ?? contentType ?? "image/jpeg";
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (!await cache.SetBlobAsync(id, type, bytes, cancellationToken))
            {
                logger.LogInformation("Attachment {Id} not cached (size {Size} bytes).", id, bytes.Length);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not download attachment {Id}.", id);
        }
    }

    private static string? SelectUrl(List<AttachmentUrl>? urls)
        => urls?.FirstOrDefault(u => string.Equals(u.Type, "ORIGINAL", StringComparison.OrdinalIgnoreCase))?.Url
           ?? urls?.FirstOrDefault()?.Url;

    // The shape of attachments embedded in entries is not modeled, so look for an id and a url generically.
    private static (string? Id, string? Url) ReadAttachmentReference(JsonElement element)
    {
        string? id = null;
        string? url = null;
        Visit(element);
        return (id, url);

        void Visit(JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in e.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            var value = property.Value.GetString();
                            if (id is null && property.Name is "uuid" or "attachment_public_uuid")
                            {
                                id = value;
                            }
                            else if (url is null && property.Name == "url" && value?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                url = value;
                            }
                        }
                        else
                        {
                            Visit(property.Value);
                        }
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in e.EnumerateArray())
                    {
                        Visit(item);
                    }

                    break;
            }
        }
    }

    #endregion

    /// <summary>
    /// Extracts the sharing token from a tricount link like https://tricount.com/tABC123xyz.
    /// </summary>
    public static string ExtractToken(string tokenOrLink)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenOrLink);

        var value = tokenOrLink.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            value = uri.Segments.LastOrDefault()?.Trim('/') ?? value;
        }

        return value;
    }
}
