using System.Diagnostics;
using System.Text;

namespace Tech_Store.SecurityTests.Infrastructure;

internal static class ProcessEvidenceRunner
{
    public static async Task<ProcessExecutionResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        string evidenceLogPath,
        IDictionary<string, string?>? environmentVariables = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environmentVariables is not null)
        {
            foreach (var entry in environmentVariables)
            {
                if (entry.Value is null)
                {
                    startInfo.Environment.Remove(entry.Key);
                }
                else
                {
                    startInfo.Environment[entry.Key] = entry.Value;
                }
            }
        }

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        await using var writer = new StreamWriter(evidenceLogPath, append: false, Encoding.UTF8);

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                return;
            }

            stdout.AppendLine(args.Data);
            writer.WriteLine($"[stdout] {args.Data}");
            writer.Flush();
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is null)
            {
                return;
            }

            stderr.AppendLine(args.Data);
            writer.WriteLine($"[stderr] {args.Data}");
            writer.Flush();
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process '{fileName}'.");
        }

        await writer.WriteLineAsync($"[meta] command: {fileName} {string.Join(' ', arguments)}");
        await writer.WriteLineAsync($"[meta] cwd: {workingDirectory}");
        await writer.FlushAsync();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        process.WaitForExit();

        return new ProcessExecutionResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
