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
        if (normalized.StartsWith("//", StringComparison.Ordinal) ||
            normalized.Contains('\\') ||
            !HasValidPercentEncoding(normalized))
        {
            return Home;
        }

        if (normalized[0] == '/')
        {
            normalized = normalized[1..];
        }

        if (string.IsNullOrEmpty(normalized) ||
            Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            return Home;
        }

        var suffixIndex = normalized.IndexOfAny(['?', '#']);
        var path = suffixIndex < 0 ? normalized : normalized[..suffixIndex];
        var suffix = suffixIndex < 0 ? string.Empty : normalized[suffixIndex..];
        if (string.IsNullOrEmpty(path))
        {
            return Home;
        }

        var segments = new List<(string Raw, string Decoded)>();
        foreach (var rawSegment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string decodedSegment;
            try
            {
                decodedSegment = Uri.UnescapeDataString(rawSegment);
            }
            catch (UriFormatException)
            {
                return Home;
            }

            if (decodedSegment.Contains('/') ||
                decodedSegment.Contains('\\') ||
                decodedSegment.Any(char.IsControl))
            {
                return Home;
            }

            if (decodedSegment == ".")
            {
                continue;
            }

            if (decodedSegment == "..")
            {
                if (segments.Count == 0)
                {
                    return Home;
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add((rawSegment, decodedSegment));
        }

        if (segments.Count == 0 ||
            string.Equals(segments[0].Decoded, "login", StringComparison.OrdinalIgnoreCase))
        {
            return Home;
        }

        return string.Join('/', segments.Select(segment => segment.Raw)) + suffix;
    }

    private static bool HasValidPercentEncoding(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
            {
                continue;
            }

            if (index + 2 >= value.Length ||
                !Uri.IsHexDigit(value[index + 1]) ||
                !Uri.IsHexDigit(value[index + 2]))
            {
                return false;
            }

            index += 2;
        }

        return true;
    }
}
