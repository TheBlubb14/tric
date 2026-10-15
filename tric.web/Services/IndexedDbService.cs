using System.Text;
using Microsoft.JSInterop;
using tric.lib;

namespace tric.web.Services;

/// <summary>
/// String key/value store on top of the browser IndexedDB. Large values are split into chunks so that
/// no single SignalR message exceeds the hub limit. The meta entry (the plain key) is written last,
/// so an interrupted write never leaves a readable but incomplete value.
/// </summary>
public sealed class IndexedDbService(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>Maximum characters per stored piece.</summary>
    public const int ChunkSize = 256 * 1024;

    private IJSObjectReference? _module;

    private async ValueTask<IJSObjectReference> ModuleAsync(CancellationToken cancellationToken)
        => _module ??= await js.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./idb.js");

    private static string ChunkKey(string key, int index) => $"{key}#{index}";

    private async Task<int?> GetChunkCountAsync(IJSObjectReference module, string key, CancellationToken cancellationToken)
    {
        var meta = await module.InvokeAsync<string?>("get", cancellationToken, key);
        return int.TryParse(meta, out var count) ? count : null;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken);
        if (await GetChunkCountAsync(module, key, cancellationToken) is not int count)
        {
            return null;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var piece = await module.InvokeAsync<string?>("get", cancellationToken, ChunkKey(key, i));
            if (piece is null)
            {
                await RemoveAsync(key, cancellationToken);
                return null;
            }

            builder.Append(piece);
        }

        return builder.ToString();
    }

    public async Task<bool> ContainsAsync(string key, CancellationToken cancellationToken = default)
        => await GetChunkCountAsync(await ModuleAsync(cancellationToken), key, cancellationToken) is not null;

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken);
        var oldCount = await GetChunkCountAsync(module, key, cancellationToken) ?? 0;
        var pieces = Chunker.Split(value, ChunkSize);

        for (var i = 0; i < pieces.Count; i++)
        {
            await module.InvokeVoidAsync("set", cancellationToken, ChunkKey(key, i), pieces[i]);
        }

        await module.InvokeVoidAsync("set", cancellationToken, key, pieces.Count.ToString());

        if (oldCount > pieces.Count)
        {
            await module.InvokeVoidAsync("removeMany", cancellationToken,
                Enumerable.Range(pieces.Count, oldCount - pieces.Count).Select(i => ChunkKey(key, i)).ToArray());
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken);
        var count = await GetChunkCountAsync(module, key, cancellationToken) ?? 0;
        var keys = Enumerable.Range(0, count).Select(i => ChunkKey(key, i)).Append(key).ToArray();
        await module.InvokeVoidAsync("removeMany", cancellationToken, (object)keys);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
        => await (await ModuleAsync(cancellationToken)).InvokeVoidAsync("clear", cancellationToken);

    /// <summary>
    /// Returns used bytes and the quota of the origin, or zeros if the browser does not report them.
    /// </summary>
    public async Task<(long Usage, long Quota)> EstimateAsync(CancellationToken cancellationToken = default)
    {
        var result = await (await ModuleAsync(cancellationToken)).InvokeAsync<long[]>("estimate", cancellationToken);
        return (result[0], result[1]);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone, nothing to release.
        }
    }
}
