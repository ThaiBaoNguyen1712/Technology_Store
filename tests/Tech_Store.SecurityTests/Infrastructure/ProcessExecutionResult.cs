namespace Tech_Store.SecurityTests.Infrastructure;

internal sealed record ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError);
