using System.Text.Json;
using Microsoft.JSInterop;

namespace tric.web.Services;

/// <summary>
/// Metadata about a cached attachment blob, used to enforce the storage budget.
/// </summary>
public sealed record BlobIndexEntry(string Key, int Size, DateTime Stored);

/// <summary>
/// Cache in the browser IndexedDB. Stores api responses as JSON and attachments as data urls
/// within a size budget.
/// </summary>
public sealed class CacheService(IndexedDbService db, ILogger<CacheService> logger)
{
    private const string BlobPrefix = "blob.";
    private const string BlobIndexKey = "blobindex";

    /// <summary>Maximum size of a single stored attachment (characters of the data url).</summary>
    public const int MaxBlobSize = 8_000_000;

    /// <summary>Total budget for all stored attachments (characters).</summary>
    public const int MaxTotalBlobSize = 100_000_000;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await db.GetAsync(key, cancellationToken);
        if (json is null)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Discarding unreadable cache entry {Key}.", key);
            await db.RemoveAsync(key, cancellationToken);
            return default;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SetAsync(key, JsonSerializer.Serialize(value), cancellationToken);
            return true;
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Could not write cache entry {Key} (quota exceeded?).", key);
            return false;
        }
    }

    /// <summary>
    /// Returns the cached attachment as data url or null.
    /// </summary>
    public Task<string?> GetBlobAsync(string id, CancellationToken cancellationToken = default)
        => db.GetAsync(BlobPrefix + id, cancellationToken);

    public Task<bool> HasBlobAsync(string id, CancellationToken cancellationToken = default)
        => db.ContainsAsync(BlobPrefix + id, cancellationToken);

    /// <summary>
    /// Stores an attachment as data url. Evicts the oldest attachments when the budget is exceeded.
    /// Returns false if the attachment is too large or could not be stored.
    /// </summary>
    public async Task<bool> SetBlobAsync(string id, string contentType, byte[] content, CancellationToken cancellationToken = default)
    {
        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(content)}";
        if (dataUrl.Length > MaxBlobSize)
        {
            return false;
        }

        var index = await GetAsync<List<BlobIndexEntry>>(BlobIndexKey, cancellationToken) ?? [];
        index.RemoveAll(e => e.Key == id);

        var budget = await GetBudgetAsync(cancellationToken);
        var total = index.Sum(e => (long)e.Size);
        foreach (var oldest in index.OrderBy(e => e.Stored).ToList())
        {
            if (total + dataUrl.Length <= budget)
            {
                break;
            }

            await db.RemoveAsync(BlobPrefix + oldest.Key, cancellationToken);
            index.Remove(oldest);
            total -= oldest.Size;
        }

        try
        {
            await db.SetAsync(BlobPrefix + id, dataUrl, cancellationToken);
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "Could not store attachment {Id} (quota exceeded?).", id);
            return false;
        }

        index.Add(new BlobIndexEntry(id, dataUrl.Length, DateTime.UtcNow));
        await SetAsync(BlobIndexKey, index, cancellationToken);
        return true;
    }

    /// <summary>
    /// Removes all cached data. The session is stored elsewhere and is not affected.
    /// </summary>
    public Task ClearAsync(CancellationToken cancellationToken = default)
        => db.ClearAsync(cancellationToken);

    // Never plan to use more than half of what the browser grants this origin.
    private async Task<long> GetBudgetAsync(CancellationToken cancellationToken)
    {
        var (_, quota) = await db.EstimateAsync(cancellationToken);
        return quota > 0 ? Math.Min(MaxTotalBlobSize, quota / 2) : MaxTotalBlobSize;
    }
}
