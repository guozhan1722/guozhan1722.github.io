using Microsoft.JSInterop;

namespace MyGithubPage.Authentication;

public sealed class BrowserSessionStorage(IJSRuntime jsRuntime) : ISessionStorage
{
    public ValueTask<string?> GetAsync(string key) =>
        jsRuntime.InvokeAsync<string?>("loginSession.get", key);

    public ValueTask SetAsync(string key, string value) =>
        jsRuntime.InvokeVoidAsync("loginSession.set", key, value);

    public ValueTask RemoveAsync(string key) =>
        jsRuntime.InvokeVoidAsync("loginSession.remove", key);
}
