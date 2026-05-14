namespace PSVCalc.Core.Models;

public sealed class CalculationResult
{
    public DateTimeOffset CalculatedAt { get; init; }
    public required string StandardVersion { get; init; }

    public double RequiredAreaMm2 { get; init; }
    public double RequiredAreaCm2 => RequiredAreaMm2 / 100.0;
    public double RequiredAreaIn2 => RequiredAreaMm2 / 645.16;

    public required OrificeRecommendation OrificeRecommendation { get; init; }
    public required IntermediateValues Intermediate { get; init; }
    public required IReadOnlyList<ParameterAudit> ParameterAudits { get; init; }
    public required IReadOnlyList<string> Warnings { get; init; }
}

