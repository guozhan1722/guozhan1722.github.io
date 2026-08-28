namespace MyGithubPage.Authentication;

public static class ReturnUrl
{
    private const string Home = "home";

    public static string Normalize(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Home;
        }

        var normalized = candidate.Trim();
        if (normalized.StartsWith("//", StringComparison.Ordinal))
        {
            return Home;
        }

        if (normalized[0] == '/')
        {
            normalized = normalized[1..];
        }

        if (string.IsNullOrEmpty(normalized) ||
            normalized.Contains('\\') ||
            Uri.TryCreate(normalized, UriKind.Absolute, out _) ||
            normalized is "login" ||
            normalized.StartsWith("login?", StringComparison.Ordinal) ||
            normalized.StartsWith("login#", StringComparison.Ordinal))
        {
            return Home;
        }

        return normalized;
    }
}
