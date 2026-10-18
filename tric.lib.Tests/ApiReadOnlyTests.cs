using System.Net;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;

namespace tric.lib.Tests;

public class ApiReadOnlyTests
{
    private const string ReadOnlyMessage = "read-only";

    private sealed class RecordingHandler(string responseBody = "{\"Response\":[{\"Id\":{\"id\":5}}]}") : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody) });
        }
    }

    private static Api CreateApi(RecordingHandler handler, bool readOnly)
        => new(NullLogger<Api>.Instance, new HttpClient(handler), readOnly);

    // Every public api method that creates, changes or deletes data on the server.
    private static readonly (string Name, Func<Api, Task> Call)[] ModifyingCalls =
    [
        (nameof(Api.CreateRegistryAsync), api => api.CreateRegistryAsync(1, "EUR", "Title")),
        (nameof(Api.DeleteRegistryAsync), api => api.DeleteRegistryAsync(1, 2)),
        (nameof(Api.UpdateRegistryAsync), api => api.UpdateRegistryAsync(1, 2, new UpdateRegistryRequest())),
        (nameof(Api.UpdateRegistryMembershipAsync), api => api.UpdateRegistryMembershipAsync(1, 2, 3, new UpdateMembershipRequest())),
        (nameof(Api.CreateRegistryEntryAsync), api => api.CreateRegistryEntryAsync(1, 2, new RegistryEntryRequest())),
        (nameof(Api.UpdateRegistryEntryAsync), api => api.UpdateRegistryEntryAsync(1, 2, 3, new RegistryEntryRequest())),
        (nameof(Api.DeleteRegistryEntryAsync), api => api.DeleteRegistryEntryAsync(1, 2, 3)),
        (nameof(Api.UploadGalleryAttachmentAsync), api => api.UploadGalleryAttachmentAsync(1, 2, "uuid", [1, 2, 3])),
        (nameof(Api.DeleteGalleryAttachmentAsync), api => api.DeleteGalleryAttachmentAsync(1, 2, "uuid")),
        (nameof(Api.UploadAttachmentAsync), api => api.UploadAttachmentAsync(1, 2, [1, 2, 3]))
    ];

    private static readonly string[] ModifyingPrefixes = ["Create", "Update", "Delete", "Upload"];

    public static IEnumerable<TestCaseData> ModifyingCallCases()
        => ModifyingCalls.Select(c => new TestCaseData(c.Call).SetArgDisplayNames(c.Name));

    [TestCaseSource(nameof(ModifyingCallCases))]
    public async Task WhenApiIsReadOnlyThenModifyingMethodThrows(Func<Api, Task> call)
    {
        var handler = new RecordingHandler();
        using var api = CreateApi(handler, readOnly: true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => call(api));

        Assert.That(ex.Message, Does.Contain(ReadOnlyMessage));
    }

    [TestCaseSource(nameof(ModifyingCallCases))]
    public async Task WhenApiIsReadOnlyThenModifyingMethodSendsNoRequest(Func<Api, Task> call)
    {
        var handler = new RecordingHandler();
        using var api = CreateApi(handler, readOnly: true);

        try
        {
            await call(api);
        }
        catch (InvalidOperationException)
        {
            // Expected, covered by the test above.
        }

        Assert.That(handler.RequestCount, Is.Zero);
    }

    [Test]
    public async Task WhenApiIsCreatedWithoutReadOnlyArgumentThenItIsReadOnly()
    {
        using var api = new Api(NullLogger<Api>.Instance, new HttpClient(new RecordingHandler()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.DeleteRegistryAsync(1, 2));
    }

    [Test]
    public async Task WhenApiIsNotReadOnlyThenModifyingMethodSendsRequest()
    {
        var handler = new RecordingHandler();
        using var api = CreateApi(handler, readOnly: false);

        var id = await api.DeleteRegistryAsync(1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(id, Is.EqualTo(5));
            Assert.That(handler.RequestCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task WhenApiIsReadOnlyThenReadingMethodsStillWork()
    {
        var handler = new RecordingHandler("{\"Response\":[]}");
        using var api = CreateApi(handler, readOnly: true);

        var registries = await api.GetRegistriesAsync(1);

        Assert.Multiple(() =>
        {
            Assert.That(registries, Is.Empty);
            Assert.That(handler.RequestCount, Is.EqualTo(1));
        });
    }

    // Guards against new data modifying methods that are added without a read-only test.
    [Test]
    public void WhenApiGetsNewModifyingMethodThenItIsCoveredByTheTests()
    {
        var covered = ModifyingCalls.Select(c => c.Name).ToHashSet();

        var modifying = typeof(Api)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .Where(name => ModifyingPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
            .Distinct();

        Assert.That(modifying, Is.SubsetOf(covered));
    }
}
