namespace Tech_Store.SecurityTests.Infrastructure;

internal static class SecurityTestRequirements
{
    public static string RequireEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        Skip.If(string.IsNullOrWhiteSpace(value), $"Missing environment variable '{name}'. Configure it before running this security test.");
        return value!;
    }
}
