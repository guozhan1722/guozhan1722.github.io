using System.Net;
using System.Text;
using System.Text.Json;
using MyGithubPage.Authentication;

namespace MyGithubPage.Tests.Authentication;

public class LoginSettingsServiceTests
{
    [Fact]
    public async Task GetAsync_RequestsSettingsWithCacheBustingQuery()
    {
        Uri? requestedUri = null;
        using var client = CreateClient(request => {
            requestedUri = request.RequestUri;
            return JsonResponse("""
                {"username":"wccac","password":"123456"}
                """);
        });

        var settings = await new LoginSettingsService(client).GetAsync();

        Assert.Equal(new LoginSettings("wccac", "123456"), settings);
        Assert.NotNull(requestedUri);
        Assert.Equal("/login-settings.json", requestedUri.AbsolutePath);
        Assert.Matches("^\\?v=[0-9]+$", requestedUri.Query);
    }

    [Fact]
    public async Task GetAsync_NullPayloadIsRejected()
    {
        using var client = CreateClient(_ => JsonResponse("null"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new LoginSettingsService(client).GetAsync());

        Assert.Equal("Login settings could not be loaded.", exception.Message);
    }

    [Theory]
    [InlineData("{\"username\":\"\",\"password\":\"123456\"}")]
    [InlineData("{\"username\":\"wccac\",\"password\":null}")]
    public async Task GetAsync_InvalidSettingsPropagateValidationFailure(string payload)
    {
        using var client = CreateClient(_ => JsonResponse(payload));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new LoginSettingsService(client).GetAsync());

        Assert.Equal(
            "Login settings must include a username and password.",
            exception.Message);
    }

    [Fact]
    public async Task GetAsync_RequestFailureIsPropagated()
    {
        var expected = new HttpRequestException("settings unavailable");
        using var client = CreateClient(_ => throw expected);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => new LoginSettingsService(client).GetAsync());

        Assert.Same(expected, exception);
    }

    [Fact]
    public async Task GetAsync_DeserializationFailureIsPropagated()
    {
        using var client = CreateClient(_ => JsonResponse("{"));

        await Assert.ThrowsAsync<JsonException>(
            () => new LoginSettingsService(client).GetAsync());
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new StubHttpMessageHandler(respond)) {
            BaseAddress = new Uri("https://example.test/")
        };

    private static HttpResponseMessage JsonResponse(string payload) => new(HttpStatusCode.OK) {
        Content = new StringContent(payload, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
}
