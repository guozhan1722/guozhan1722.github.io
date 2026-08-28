namespace MyGithubPage.Authentication;

public interface ISessionStorage
{
    ValueTask<string?> GetAsync(string key);
    ValueTask SetAsync(string key, string value);
    ValueTask RemoveAsync(string key);
}
