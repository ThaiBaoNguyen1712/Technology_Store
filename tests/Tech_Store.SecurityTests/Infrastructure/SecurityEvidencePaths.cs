namespace Tech_Store.SecurityTests.Infrastructure;

internal sealed class SecurityEvidencePaths
{
    public SecurityEvidencePaths(string testArea)
    {
        RepositoryRoot = RepositoryRootLocator.Find();
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        OutputDirectory = Path.Combine(RepositoryRoot, "artifacts", "security", testArea, timestamp);
        Directory.CreateDirectory(OutputDirectory);
    }

    public string RepositoryRoot { get; }

    public string OutputDirectory { get; }

    public string GetFilePath(string fileName) => Path.Combine(OutputDirectory, fileName);
}
