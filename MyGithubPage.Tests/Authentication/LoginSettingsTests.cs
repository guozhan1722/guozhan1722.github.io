using MyGithubPage.Authentication;

namespace MyGithubPage.Tests.Authentication;

public class LoginSettingsTests
{
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
}
