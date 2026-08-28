using System.Diagnostics;

namespace MyGithubPage.Tests.Deployment;

public class GitHubPagesDeploymentTests
{
    [Fact]
    public async Task PreparationScript_CreatesNoJekyllAndMatchingSpaFallback()
    {
        var output = Path.Combine(Path.GetTempPath(), $"pages-{Guid.NewGuid():N}");
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(
            Path.Combine(output, "index.html"),
            "<!doctype html><title>SPA fixture</title>");

        try
        {
            var startInfo = new ProcessStartInfo {
                FileName = "/bin/sh",
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(RepositoryPath(
                ".github", "scripts", "prepare-pages.sh"));
            startInfo.ArgumentList.Add(output);
            using var process = Process.Start(startInfo)!;
            var standardError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            Assert.True(process.ExitCode == 0, standardError);
            Assert.True(File.Exists(Path.Combine(output, ".nojekyll")));
            Assert.Equal(
                await File.ReadAllBytesAsync(Path.Combine(output, "index.html")),
                await File.ReadAllBytesAsync(Path.Combine(output, "404.html")));
        }
        finally
        {
            Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void Workflow_PreparesPublishedWwwrootBeforeDeployment()
    {
        var workflow = File.ReadAllText(RepositoryPath(
            ".github", "workflows", "main.yml"));

        Assert.Contains(
            "sh .github/scripts/prepare-pages.sh release/wwwroot",
            workflow);
    }

    private static string RepositoryPath(params string[] parts) =>
        Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", .. parts]));
}
