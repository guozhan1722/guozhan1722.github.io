namespace MyGithubPage.Tests.Authentication;

public class LoginPageContractTests
{
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

    [Fact]
    public void LoginPage_UsesAStandaloneLayout()
    {
        Assert.Contains("@layout LoginLayout", File.ReadAllText(LoginPagePath()));

        var layoutPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "MyGithubPage", "Layout",
            "LoginLayout.razor"));
        var layout = File.ReadAllText(layoutPath);
        Assert.Contains("@inherits LayoutComponentBase", layout);
        Assert.Contains("@Body", layout);
    }

    [Fact]
    public void LoginPage_BindsTypedCredentialsBeforeSubmit()
    {
        var source = File.ReadAllText(LoginPagePath());

        Assert.Equal(2, source.Split("@bind:event=\"oninput\"").Length - 1);
    }

    private static string LoginPagePath() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "MyGithubPage", "Pages",
        "Login.razor"));
}
