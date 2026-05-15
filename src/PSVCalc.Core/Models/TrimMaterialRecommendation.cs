namespace PSVCalc.Core.Models;

public sealed class TrimMaterialRecommendation
{
    public required string SeatMaterial { get; init; }
    public required string DiscMaterial { get; init; }
    public required string ServiceBasis { get; init; }
    public required string Basis { get; init; }
    public required IReadOnlyList<string> ReviewNotes { get; init; }
}
