using Tech_Store.SecurityTests.Infrastructure;
using Xunit.Abstractions;

namespace Tech_Store.SecurityTests.Zap;

public sealed class OwaspZapSqlInjectionTests
{
    private readonly ITestOutputHelper _output;

    public OwaspZapSqlInjectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public async Task Run_owasp_zap_sql_injection_scan_and_capture_evidence()
    {
        var targetUrl = SecurityTestRequirements.RequireEnvironmentVariable("ZAP_TARGET_URL");
        var repositoryRoot = RepositoryRootLocator.Find();
        var evidence = new SecurityEvidencePaths("zap");
        var logPath = evidence.GetFilePath("console.log");
        var scriptPath = Path.Combine(repositoryRoot, "tests", "security", "run-zap-sqli-scan.ps1");

        var result = await ProcessEvidenceRunner.RunAsync(
            fileName: "powershell",
            arguments:
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                scriptPath,
                "-TargetUrl",
                targetUrl,
                "-EvidenceRoot",
                evidence.OutputDirectory
            ],
            workingDirectory: repositoryRoot,
            evidenceLogPath: logPath,
            environmentVariables: new Dictionary<string, string?>
            {
                ["ZAP_DOCKER_IMAGE"] = Environment.GetEnvironmentVariable("ZAP_DOCKER_IMAGE"),
                ["ZAP_MAX_MINUTES"] = Environment.GetEnvironmentVariable("ZAP_MAX_MINUTES")
            });

        _output.WriteLine($"Evidence directory: {evidence.OutputDirectory}");
        _output.WriteLine(result.StandardOutput);
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            _output.WriteLine(result.StandardError);
        }

        Assert.True(
            result.ExitCode == 0,
            $"OWASP ZAP SQL injection scan failed with exit code {result.ExitCode}. Review evidence at {logPath}.");
    }
}
