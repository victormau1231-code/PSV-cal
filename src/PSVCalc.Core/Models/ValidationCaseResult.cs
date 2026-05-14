namespace PSVCalc.Core.Models;

public sealed class ValidationCaseResult
{
    public required string CaseId { get; init; }
    public required string Description { get; init; }

    public bool Passed { get; init; }
    public double ExpectedRequiredAreaMm2 { get; init; }
    public double ActualRequiredAreaMm2 { get; init; }
    public double AreaDeviationPercent { get; init; }
    public required string ExpectedOrificeLetter { get; init; }
    public required string ActualOrificeLetter { get; init; }

    public string? ErrorMessage { get; init; }
}

