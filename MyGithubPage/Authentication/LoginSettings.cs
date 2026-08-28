using System.Security.Cryptography;
using System.Text;

namespace MyGithubPage.Authentication;

public sealed record LoginSettings(string Username, string Password)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException(
                "Login settings must include a username and password.");
        }
    }

    public string CreateFingerprint()
    {
        var bytes = Encoding.UTF8.GetBytes($"{Username}\n{Password}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
