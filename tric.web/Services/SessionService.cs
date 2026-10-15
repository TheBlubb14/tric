using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using tric.lib;

namespace tric.web.Services;

/// <summary>
/// Persisted installation and session data of this browser.
/// </summary>
public sealed record StoredSession(string AppInstallationUuid, string PublicKeyPem, string? Token, long? UserId);

/// <summary>
/// Creates the key once per browser, persists it encrypted in the local storage and keeps the <see cref="Api"/> authenticated.
/// Must be used after the first render because it depends on the browser local storage.
/// </summary>
public sealed class SessionService(Api api, ProtectedLocalStorage localStorage, ILogger<SessionService> logger)
{
    private const string StorageKey = "tric.session";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private StoredSession? _session;
    private bool _initialized;

    public Api Api => api;

    /// <summary>
    /// Id of the authenticated user. Only valid after <see cref="EnsureSessionAsync"/>.
    /// </summary>
    public long UserId => api.UserId ?? throw new InvalidOperationException("Session is not initialized.");

    /// <summary>
    /// Loads or creates the stored installation data and makes sure the api is authenticated.
    /// </summary>
    public async Task EnsureSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            _session = await LoadAsync();

            if (_session is null || string.IsNullOrWhiteSpace(_session.PublicKeyPem) || string.IsNullOrWhiteSpace(_session.AppInstallationUuid))
            {
                _session = new StoredSession(Guid.NewGuid().ToString(), KeyService.CreatePublicKeyPem(), null, null);
                await localStorage.SetAsync(StorageKey, _session);
                logger.LogInformation("Created new installation key.");
            }

            if (!string.IsNullOrWhiteSpace(_session.Token) && _session.UserId is long userId)
            {
                api.RestoreSession(_session.AppInstallationUuid, _session.Token, userId);
            }
            else
            {
                await RegisterAsync(cancellationToken);
            }

            _initialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Runs an api call and transparently registers a new session once if the token was rejected.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Api, long, Task<T>> call, CancellationToken cancellationToken = default)
    {
        await EnsureSessionAsync(cancellationToken);

        try
        {
            return await call(api, UserId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Session rejected, registering again.");
            await _lock.WaitAsync(cancellationToken);
            try
            {
                await RegisterAsync(cancellationToken);
            }
            finally
            {
                _lock.Release();
            }

            return await call(api, UserId);
        }
    }

    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        var session = _session ?? throw new InvalidOperationException("No installation data.");

        SessionRegistrationResult result;
        try
        {
            result = await api.RegisterSessionAsync(session.AppInstallationUuid, session.PublicKeyPem, "Web", cancellationToken);
        }
        catch (HttpRequestException)
        {
            // The server requires a unique key per installation, so retry once with a fresh one.
            session = new StoredSession(Guid.NewGuid().ToString(), KeyService.CreatePublicKeyPem(), null, null);
            result = await api.RegisterSessionAsync(session.AppInstallationUuid, session.PublicKeyPem, "Web", cancellationToken);
        }

        _session = session with { Token = result.Token.Value, UserId = result.UserPerson.Id };
        await localStorage.SetAsync(StorageKey, _session);
    }

    private async Task<StoredSession?> LoadAsync()
    {
        try
        {
            var result = await localStorage.GetAsync<StoredSession>(StorageKey);
            return result.Success ? result.Value : null;
        }
        catch (CryptographicException ex)
        {
            // The data protection keys changed (e.g. server reset), so the stored session cannot be read anymore.
            logger.LogWarning(ex, "Stored session could not be decrypted, creating a new one.");
            await localStorage.DeleteAsync(StorageKey);
            return null;
        }
    }
}
