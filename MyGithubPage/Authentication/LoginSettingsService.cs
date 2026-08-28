using System.Net.Http.Json;

namespace MyGithubPage.Authentication;

public interface ILoginSettingsService
{
    Task<LoginSettings> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class LoginSettingsService(HttpClient httpClient)
    : ILoginSettingsService
{
    public async Task<LoginSettings> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var path = "login-settings.json?v=" +
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var value = await httpClient.GetFromJsonAsync<LoginSettings>(
            path, cancellationToken)
            ?? throw new InvalidOperationException(
                "Login settings could not be loaded.");
        value.Validate();
        return value;
    }
}
