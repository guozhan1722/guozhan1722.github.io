# Static Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Add a configurable, browser-session login gate to every existing page of the static Blazor WebAssembly site.

**Architecture:** A settings service fetches plain-text credentials from a cache-busted JSON asset and derives a configuration fingerprint. A custom AuthenticationStateProvider compares submitted credentials, persists only that fingerprint through a session-storage abstraction, and exposes Blazor authorization state; authorization routing redirects signed-out visitors to a standalone login page.

**Tech Stack:** .NET 9, Blazor WebAssembly, Microsoft.AspNetCore.Components.Authorization, JavaScript interop, browser sessionStorage, xUnit

**Spec:** docs/superpowers/specs/2026-08-27-static-login-design.md

## Global Constraints

- The app remains a static Blazor WebAssembly site deployable by the existing GitHub Pages workflow.
- Credentials are plain text in MyGithubPage/wwwroot/login-settings.json, initially wccac / 123456.
- This is a casual access barrier, not secure authentication or authorization.
- All existing content routes require authentication; /login remains anonymous.
- Authentication persists only for the browser session and is invalidated after refresh when configured credentials change.
- The password is never written to browser storage.
- Configuration load, validation, or storage failures fail closed.

---

## File Structure

- MyGithubPage/Authentication/LoginSettings.cs: configuration validation and fingerprinting.
- MyGithubPage/Authentication/LoginSettingsService.cs: cache-busted JSON loading.
- MyGithubPage/Authentication/ISessionStorage.cs: testable storage boundary.
- MyGithubPage/Authentication/BrowserSessionStorage.cs: JS-backed session storage.
- MyGithubPage/Authentication/StaticAuthenticationStateProvider.cs: login, logout, restoration, and auth state.
- MyGithubPage/Authentication/ReturnUrl.cs: safe local return paths.
- MyGithubPage/Authentication/RedirectToLogin.razor: protected-route redirect.
- MyGithubPage/Pages/Login.razor and Login.razor.css: anonymous login UI.
- MyGithubPage/wwwroot/login-settings.json: editable credentials.
- MyGithubPage/wwwroot/js/session-storage.js: narrow browser storage wrapper.
- MyGithubPage.Tests: unit and source-contract tests.

### Task 1: Credential Configuration

**Files:**
- Create: MyGithubPage/Authentication/LoginSettings.cs
- Create: MyGithubPage/Authentication/LoginSettingsService.cs
- Create: MyGithubPage/wwwroot/login-settings.json
- Create: MyGithubPage.Tests/MyGithubPage.Tests.csproj
- Create: MyGithubPage.Tests/Authentication/LoginSettingsTests.cs
- Modify: MyGithubPage/MyGithubPage.slnx

**Interfaces:**
- Produces: sealed record LoginSettings(string Username, string Password), Validate(), and CreateFingerprint().
- Produces: ILoginSettingsService.GetAsync(CancellationToken) returning Task<LoginSettings>.

- [ ] **Step 1: Scaffold the test project**

~~~bash
dotnet new xunit -n MyGithubPage.Tests -f net9.0
dotnet add MyGithubPage.Tests/MyGithubPage.Tests.csproj reference MyGithubPage/MyGithubPage.csproj
dotnet sln MyGithubPage/MyGithubPage.slnx add MyGithubPage.Tests/MyGithubPage.Tests.csproj
~~~

- [ ] **Step 2: Write failing settings tests**

~~~csharp
[Theory]
[InlineData("", "123456")]
[InlineData("wccac", "")]
[InlineData(" ", "123456")]
public void Validate_RejectsBlankValues(string username, string password)
{
    Assert.Throws<InvalidOperationException>(
        new LoginSettings(username, password).Validate);
}

[Fact]
public void Fingerprint_IsStable_AndChangesWithCredentials()
{
    var original = new LoginSettings("wccac", "123456");
    Assert.Equal(original.CreateFingerprint(),
        new LoginSettings("wccac", "123456").CreateFingerprint());
    Assert.NotEqual(original.CreateFingerprint(),
        new LoginSettings("wccac", "654321").CreateFingerprint());
    Assert.DoesNotContain("123456", original.CreateFingerprint());
}
~~~

- [ ] **Step 3: Prove the tests fail**

Run: dotnet test MyGithubPage.Tests/MyGithubPage.Tests.csproj --filter LoginSettingsTests

Expected: compile failure because LoginSettings does not exist.

- [ ] **Step 4: Implement settings and loader**

LoginSettings validates nonblank values. CreateFingerprint returns uppercase SHA-256 hex for UTF-8 bytes of Username, a newline, and Password.

~~~csharp
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
~~~

Create the JSON exactly:

~~~json
{
  "username": "wccac",
  "password": "123456"
}
~~~

- [ ] **Step 5: Prove tests and build pass**

~~~bash
dotnet test MyGithubPage.Tests/MyGithubPage.Tests.csproj --filter LoginSettingsTests
dotnet build MyGithubPage/MyGithubPage.slnx
~~~

- [ ] **Step 6: Commit**

~~~bash
git add MyGithubPage/Authentication MyGithubPage/wwwroot/login-settings.json MyGithubPage.Tests MyGithubPage/MyGithubPage.slnx
git commit -m "feat: load configurable static login credentials"
~~~

### Task 2: Browser Session Authentication

**Files:**
- Create: MyGithubPage/Authentication/ISessionStorage.cs
- Create: MyGithubPage/Authentication/BrowserSessionStorage.cs
- Create: MyGithubPage/Authentication/StaticAuthenticationStateProvider.cs
- Create: MyGithubPage/wwwroot/js/session-storage.js
- Create: MyGithubPage.Tests/Authentication/StaticAuthenticationStateProviderTests.cs
- Modify: MyGithubPage/wwwroot/index.html
- Modify: MyGithubPage/Program.cs

**Interfaces:**
- Consumes: ILoginSettingsService and LoginSettings from Task 1.
- Produces: ISessionStorage.GetAsync, SetAsync, RemoveAsync.
- Produces: StaticAuthenticationStateProvider.LoginAsync, LogoutAsync, and GetAuthenticationStateAsync.

- [ ] **Step 1: Write failing provider tests**

Use in-memory fakes for settings and storage and add these four cases:

~~~csharp
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
~~~

FakeSessionStorage holds nullable Value. FakeSettingsService returns its constructor value.

- [ ] **Step 2: Prove provider tests fail**

Run: dotnet test MyGithubPage.Tests/MyGithubPage.Tests.csproj --filter StaticAuthenticationStateProviderTests

Expected: compile failure because the provider and storage interface do not exist.

- [ ] **Step 3: Implement session storage**

~~~csharp
public interface ISessionStorage
{
    ValueTask<string?> GetAsync(string key);
    ValueTask SetAsync(string key, string value);
    ValueTask RemoveAsync(string key);
}
~~~

BrowserSessionStorage calls loginSession.get, loginSession.set, and loginSession.remove using IJSRuntime. The JavaScript file contains:

~~~javascript
window.loginSession = {
    get: key => sessionStorage.getItem(key),
    set: (key, value) => sessionStorage.setItem(key, value),
    remove: key => sessionStorage.removeItem(key)
};
~~~

- [ ] **Step 4: Implement authentication state**

Use session key my-github-page.login. LoginAsync performs ordinal comparisons for username and password, stores only the fingerprint on success, creates a ClaimsIdentity with authentication type StaticLogin and ClaimTypes.Name, and notifies state changes. GetAuthenticationStateAsync restores only a matching fingerprint, removes mismatches, and returns anonymous for all configuration/storage failures. LogoutAsync removes the session key and notifies anonymous state.

- [ ] **Step 5: Register services and JavaScript**

Add session-storage.js before blazor.webassembly.js. In Program.cs register AddAuthorizationCore, LoginSettingsService, BrowserSessionStorage, StaticAuthenticationStateProvider, and map AuthenticationStateProvider to that same scoped instance.

- [ ] **Step 6: Prove tests and build pass**

~~~bash
dotnet test MyGithubPage.Tests/MyGithubPage.Tests.csproj --filter StaticAuthenticationStateProviderTests
dotnet build MyGithubPage/MyGithubPage.slnx
~~~

- [ ] **Step 7: Commit**

~~~bash
git add MyGithubPage/Authentication MyGithubPage/wwwroot/js/session-storage.js MyGithubPage/wwwroot/index.html MyGithubPage/Program.cs MyGithubPage.Tests/Authentication
git commit -m "feat: manage static login browser sessions"
~~~

### Task 3: Protected Routing

**Files:**
- Create: MyGithubPage/Authentication/ReturnUrl.cs
- Create: MyGithubPage/Authentication/RedirectToLogin.razor
- Create: MyGithubPage.Tests/Authentication/ReturnUrlTests.cs
- Modify: MyGithubPage/App.razor
- Modify: MyGithubPage/_Imports.razor

**Interfaces:**
- Consumes: AuthenticationStateProvider registration from Task 2.
- Produces: ReturnUrl.Normalize(string?) returning a safe relative route or home.

- [ ] **Step 1: Write failing return URL tests**

~~~csharp
[Theory]
[InlineData(null, "home")]
[InlineData("", "home")]
[InlineData("https://evil.example", "home")]
[InlineData("//evil.example", "home")]
[InlineData("login", "home")]
[InlineData("videos", "videos")]
[InlineData("videos?item=2", "videos?item=2")]
public void Normalize_AllowsOnlyLocalContentRoutes(
    string? candidate, string expected)
{
    Assert.Equal(expected, ReturnUrl.Normalize(candidate));
}
~~~

- [ ] **Step 2: Prove the tests fail**

Run: dotnet test MyGithubPage.Tests/MyGithubPage.Tests.csproj --filter ReturnUrlTests

Expected: compile failure because ReturnUrl does not exist.

- [ ] **Step 3: Implement ReturnUrl.Normalize**

Trim input and one leading slash. Reject empty values, protocol-relative paths, absolute URIs, backslashes, and login with query or fragment. Return home for rejection and the normalized local route otherwise.

- [ ] **Step 4: Protect routes**

Add authorization namespaces to _Imports.razor. Wrap Router with CascadingAuthenticationState. Replace RouteView with AuthorizeRouteView and render RedirectToLogin in NotAuthorized. RedirectToLogin gets the current base-relative path, normalizes and URL-escapes it, then navigates with replace=true to login?returnUrl=....

- [ ] **Step 5: Prove tests and build pass**

~~~bash
dotnet test MyGithubPage.Tests/MyGithubPage.Tests.csproj --filter ReturnUrlTests
dotnet build MyGithubPage/MyGithubPage.slnx
~~~

- [ ] **Step 6: Commit**

~~~bash
git add MyGithubPage/Authentication MyGithubPage/App.razor MyGithubPage/_Imports.razor MyGithubPage.Tests/Authentication
git commit -m "feat: require login for content routes"
~~~

### Task 4: Login and Logout UI

**Files:**
- Create: MyGithubPage/Pages/Login.razor
- Create: MyGithubPage/Pages/Login.razor.css
- Create: MyGithubPage.Tests/Authentication/LoginPageContractTests.cs
- Modify: MyGithubPage/Layout/MainLayout.razor

**Interfaces:**
- Consumes: StaticAuthenticationStateProvider and ReturnUrl.
- Produces: anonymous /login form and authenticated logout action.

- [ ] **Step 1: Write a failing UI source-contract test**

~~~csharp
[Fact]
public void LoginPage_HasAnonymousRouteAndAccessibleFields()
{
    var source = File.ReadAllText(LoginPagePath());
    Assert.Contains("@page \"/login\"", source);
    Assert.Contains("@attribute [AllowAnonymous]", source);
    Assert.Contains("autocomplete=\"username\"", source);
    Assert.Contains("autocomplete=\"current-password\"", source);
    Assert.Contains("Invalid username or password.", source);
}
~~~

LoginPagePath resolves from AppContext.BaseDirectory up four levels into MyGithubPage/Pages/Login.razor.

- [ ] **Step 2: Prove the contract test fails**

Run: dotnet test MyGithubPage.Tests/MyGithubPage.Tests.csproj --filter LoginPageContractTests

Expected: FileNotFoundException because Login.razor does not exist.

- [ ] **Step 3: Implement login UI**

Login.razor declares /login, AllowAnonymous, and layout null. Inject StaticAuthenticationStateProvider and NavigationManager. Bind returnUrl with SupplyParameterFromQuery. Use an EditForm with visible labels, DataAnnotations validation, username and current-password autocomplete, keyboard submission, role=alert errors, and a disabled button while submitting.

On success navigate with replace=true to ReturnUrl.Normalize(returnUrl). On false, clear the password and display Invalid username or password. On configuration/storage exception, clear the password and display Login is unavailable because the site configuration could not be loaded. Login.razor.css centers a responsive Bootstrap card.

- [ ] **Step 4: Add logout**

In MainLayout.razor replace About with an AuthorizeView containing a Log out button. Its handler awaits LogoutAsync and navigates with replace=true to login.

- [ ] **Step 5: Prove contract and build pass**

~~~bash
dotnet test MyGithubPage.Tests/MyGithubPage.Tests.csproj --filter LoginPageContractTests
dotnet build MyGithubPage/MyGithubPage.slnx
~~~

- [ ] **Step 6: Commit**

~~~bash
git add MyGithubPage/Pages/Login.razor MyGithubPage/Pages/Login.razor.css MyGithubPage/Layout/MainLayout.razor MyGithubPage.Tests/Authentication/LoginPageContractTests.cs
git commit -m "feat: add static login and logout UI"
~~~

### Task 5: Release and Browser Verification

**Files:**
- Modify only files required to fix observed verification defects.

**Interfaces:**
- Consumes: complete flow from Tasks 1-4.
- Produces: release-buildable static site matching the approved spec.

- [ ] **Step 1: Run all automated tests**

Run: dotnet test MyGithubPage/MyGithubPage.slnx --configuration Release

Expected: zero failed tests.

- [ ] **Step 2: Publish exactly as deployment does**

Run: dotnet publish MyGithubPage/MyGithubPage.slnx -c Release -o /tmp/mygithubpage-login-release --nologo

Expected: exit code 0 and /tmp/mygithubpage-login-release/wwwroot/login-settings.json contains wccac / 123456.

- [ ] **Step 3: Perform browser smoke verification**

Serve the published wwwroot and verify: /home redirects to login; a wrong password shows the generic error; wccac / 123456 returns to the requested page; refresh preserves the session; Videos and Weather work; logout blocks content again; changing served JSON to password 654321 and refreshing invalidates the session and accepts only the new password.

- [ ] **Step 4: Inspect public assets**

~~~bash
rg -n '"username": "wccac"|"password": "123456"' /tmp/mygithubpage-login-release/wwwroot/login-settings.json
rg -n '123456' /tmp/mygithubpage-login-release/wwwroot | head
~~~

Expected: credentials are visibly public in the JSON as documented, and session-storage.js contains no password.

- [ ] **Step 5: Commit verification fixes if any**

Stage only files changed to fix verified defects and commit with:

~~~bash
git commit -m "fix: correct static login verification issues"
~~~

Do not create an empty commit when no fixes were necessary.
