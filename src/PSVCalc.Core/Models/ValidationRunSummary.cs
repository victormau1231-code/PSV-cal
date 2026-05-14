namespace PSVCalc.Core.Models;

public sealed class ValidationRunSummary
{
    public DateTimeOffset RunAt { get; init; } = DateTimeOffset.Now;
    public required string SetName { get; init; }
    public required string StandardVersion { get; init; }
    public required IReadOnlyList<ValidationCaseResult> Results { get; init; }

    public int Total => Results.Count;
    public int Passed => Results.Count(x => x.Passed);
    public int Failed => Total - Passed;
}

