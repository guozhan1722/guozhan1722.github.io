namespace MyGithubPage.Tests.Authentication;

public class MainLayoutContractTests
{
    [Fact]
    public void LogoutFailure_IsCaughtAndShownAsAnAlert()
    {
        var source = File.ReadAllText(MainLayoutPath());

        Assert.Contains("catch", source);
        Assert.Contains("role=\"alert\"", source);
        Assert.Contains("Close this tab to finish signing out.", source);
    }

    private static string MainLayoutPath() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "MyGithubPage", "Layout",
        "MainLayout.razor"));
}
