using MyGithubPage.Authentication;

namespace MyGithubPage.Tests.Authentication;

public class ReturnUrlTests
{
    [Theory]
    [InlineData(null, "home")]
    [InlineData("", "home")]
    [InlineData("https://evil.example", "home")]
    [InlineData("//evil.example", "home")]
    [InlineData("login", "home")]
    [InlineData("LOGIN", "home")]
    [InlineData("LOGIN?returnUrl=videos", "home")]
    [InlineData("Login#section", "home")]
    [InlineData("login/", "home")]
    [InlineData("login/?returnUrl=videos", "home")]
    [InlineData("./login", "home")]
    [InlineData("home/../login", "home")]
    [InlineData("home/%2E%2E/login", "home")]
    [InlineData("?returnUrl=videos", "home")]
    [InlineData("#login", "home")]
    [InlineData("../videos", "home")]
    [InlineData("videos/%ZZ", "home")]
    [InlineData("videos", "videos")]
    [InlineData("videos?item=2", "videos?item=2")]
    [InlineData("videos/./featured", "videos/featured")]
    [InlineData("videos/archive/../featured?item=2#details",
        "videos/featured?item=2#details")]
    public void Normalize_AllowsOnlyLocalContentRoutes(
        string? candidate, string expected)
    {
        Assert.Equal(expected, ReturnUrl.Normalize(candidate));
    }
}
