namespace PSVCalc.Core.Models;

public sealed class OrificeRecommendation
{
    public double RequiredAreaMm2 { get; init; }
    public double MinimumRecommendedAreaMm2 { get; init; }
    public double MaximumRecommendedUtilizationPercent { get; init; } = 90.0;
    public required OrificeDefinition Selected { get; init; }
    public required IReadOnlyList<OrificeDefinition> CandidateNeighbors { get; init; }
    public bool IsCapacityExceededByLargestOrifice { get; init; }
    public bool WasUpsizedForMargin { get; init; }
    public string? DirectAreaQualifiedLetter { get; init; }
    public double SelectedUtilizationPercent => Selected.AreaMm2 <= 0
        ? 0.0
        : RequiredAreaMm2 / Selected.AreaMm2 * 100.0;
    public string SizeShorthand => $"{Selected.InletNominalInch}{Selected.Letter}{Selected.OutletNominalInch}";
    public string ConnectionDisplay => $"{Selected.InletNominalInch}\" x {Selected.OutletNominalInch}\"";
}
