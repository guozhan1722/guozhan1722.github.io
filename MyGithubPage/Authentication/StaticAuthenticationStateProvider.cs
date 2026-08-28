using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MyGithubPage.Authentication;

public sealed class StaticAuthenticationStateProvider(
    ILoginSettingsService loginSettingsService,
    ISessionStorage sessionStorage) : AuthenticationStateProvider
{
    private const string SessionKey = "my-github-page.login";
    private const string AuthenticationType = "StaticLogin";

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var storedFingerprint = await sessionStorage.GetAsync(SessionKey);
            if (storedFingerprint is null)
            {
                return AnonymousState();
            }

            var settings = await loginSettingsService.GetAsync();
            if (!string.Equals(storedFingerprint, settings.CreateFingerprint(),
                    StringComparison.Ordinal))
            {
                await sessionStorage.RemoveAsync(SessionKey);
                return AnonymousState();
            }

            return AuthenticatedState(settings.Username);
        }
        catch
        {
            return AnonymousState();
        }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var settings = await loginSettingsService.GetAsync();
        if (!string.Equals(username, settings.Username,
                StringComparison.Ordinal) ||
            !string.Equals(password, settings.Password,
                StringComparison.Ordinal))
        {
            return false;
        }

        await sessionStorage.SetAsync(SessionKey, settings.CreateFingerprint());
        NotifyAuthenticationStateChanged(
            Task.FromResult(AuthenticatedState(settings.Username)));
        return true;
    }

    public async Task LogoutAsync()
    {
        try
        {
            await sessionStorage.RemoveAsync(SessionKey);
        }
        catch
        {
        }

        NotifyAuthenticationStateChanged(Task.FromResult(AnonymousState()));
    }

    private static AuthenticationState AuthenticatedState(string username) =>
        new(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, username)], AuthenticationType)));

    private static AuthenticationState AnonymousState() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
