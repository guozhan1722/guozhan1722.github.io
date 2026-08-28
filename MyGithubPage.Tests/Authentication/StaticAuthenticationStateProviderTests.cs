using MyGithubPage.Authentication;

namespace MyGithubPage.Tests.Authentication;

public class StaticAuthenticationStateProviderTests
{
    [Fact]
    public async Task MatchingCredentials_AuthenticateAndStoreFingerprint()
    {
        var settings = new LoginSettings("wccac", "123456");
        var storage = new FakeSessionStorage();
        var provider = CreateProvider(settings, storage);

        Assert.True(await provider.LoginAsync("wccac", "123456"));
        Assert.True((await provider.GetAuthenticationStateAsync())
            .User.Identity!.IsAuthenticated);
        Assert.Equal(settings.CreateFingerprint(), storage.Value);
    }

    [Fact]
    public async Task WrongCredentials_FailWithoutStorage()
    {
        var storage = new FakeSessionStorage();
        var provider = CreateProvider(
            new LoginSettings("wccac", "123456"), storage);

        Assert.False(await provider.LoginAsync("wccac", "wrong"));

        Assert.Null(storage.Value);
    }

    [Fact]
    public async Task LoginAsync_PropagatesConfigurationFailure()
    {
        var provider = new StaticAuthenticationStateProvider(
            new ThrowingSettingsService(), new FakeSessionStorage());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.LoginAsync("wccac", "123456"));
    }

    [Fact]
    public async Task LoginAsync_PropagatesSessionStorageFailure()
    {
        var storage = new FakeSessionStorage {
            SetException = new InvalidOperationException("storage unavailable")
        };
        var provider = CreateProvider(
            new LoginSettings("wccac", "123456"), storage);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.LoginAsync("wccac", "123456"));
    }

    [Fact]
    public async Task ChangedFingerprint_InvalidatesStoredSession()
    {
        var storage = new FakeSessionStorage {
            Value = new LoginSettings("wccac", "old").CreateFingerprint()
        };
        var provider = CreateProvider(
            new LoginSettings("wccac", "new"), storage);

        Assert.False((await provider.GetAuthenticationStateAsync())
            .User.Identity!.IsAuthenticated);
        Assert.Null(storage.Value);
    }

    [Fact]
    public async Task Logout_ClearsAuthenticationAndStorage()
    {
        var settings = new LoginSettings("wccac", "123456");
        var storage = new FakeSessionStorage {
            Value = settings.CreateFingerprint()
        };
        var provider = CreateProvider(settings, storage);

        _ = await provider.GetAuthenticationStateAsync();
        await provider.LogoutAsync();

        Assert.Null(storage.Value);
        Assert.False((await provider.GetAuthenticationStateAsync())
            .User.Identity!.IsAuthenticated);
    }

    private static StaticAuthenticationStateProvider CreateProvider(
        LoginSettings settings, FakeSessionStorage storage) =>
        new(new FakeSettingsService(settings), storage);

    private sealed class FakeSettingsService(LoginSettings value)
        : ILoginSettingsService
    {
        public Task<LoginSettings> GetAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(value);
    }

    private sealed class ThrowingSettingsService : ILoginSettingsService
    {
        public Task<LoginSettings> GetAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromException<LoginSettings>(
                new InvalidOperationException("settings unavailable"));
    }

    private sealed class FakeSessionStorage : ISessionStorage
    {
        public string? Value { get; set; }
        public Exception? SetException { get; set; }

        public ValueTask<string?> GetAsync(string key) => ValueTask.FromResult(Value);

        public ValueTask SetAsync(string key, string value)
        {
            if (SetException is not null)
            {
                throw SetException;
            }

            Value = value;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key)
        {
            Value = null;
            return ValueTask.CompletedTask;
        }
    }
}
