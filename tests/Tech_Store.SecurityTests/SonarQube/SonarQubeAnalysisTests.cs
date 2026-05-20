using Tech_Store.SecurityTests.Infrastructure;
using Xunit.Abstractions;

namespace Tech_Store.SecurityTests.SonarQube;

public sealed class SonarQubeAnalysisTests
{
    private readonly ITestOutputHelper _output;

    public SonarQubeAnalysisTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [SkippableFact]
    public async Task Run_sonarqube_analysis_and_capture_evidence()
    {
        var sonarHostUrl = SecurityTestRequirements.RequireEnvironmentVariable("SONAR_HOST_URL");
        var sonarToken = SecurityTestRequirements.RequireEnvironmentVariable("SONAR_TOKEN");
        var repositoryRoot = RepositoryRootLocator.Find();
        var evidence = new SecurityEvidencePaths("sonarqube");
        var logPath = evidence.GetFilePath("console.log");
        var scriptPath = Path.Combine(repositoryRoot, "tests", "security", "run-sonarqube-analysis.ps1");

        var result = await ProcessEvidenceRunner.RunAsync(
            fileName: "powershell",
            arguments:
            [
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                scriptPath,
                "-SonarHostUrl",
                sonarHostUrl,
                "-SonarToken",
                sonarToken,
                "-EvidenceRoot",
                evidence.OutputDirectory
            ],
            workingDirectory: repositoryRoot,
            evidenceLogPath: logPath,
            environmentVariables: new Dictionary<string, string?>
            {
                ["SONAR_PROJECT_KEY"] = Environment.GetEnvironmentVariable("SONAR_PROJECT_KEY"),
                ["SONAR_PROJECT_NAME"] = Environment.GetEnvironmentVariable("SONAR_PROJECT_NAME"),
                ["SONAR_SCANNER_VERSION"] = Environment.GetEnvironmentVariable("SONAR_SCANNER_VERSION")
            });

        _output.WriteLine($"Evidence directory: {evidence.OutputDirectory}");
        _output.WriteLine(result.StandardOutput);
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            _output.WriteLine(result.StandardError);
        }

        Assert.True(
            result.ExitCode == 0,
            $"SonarQube analysis failed with exit code {result.ExitCode}. Review evidence at {logPath}.");
    }
}
