namespace PSVCalc.Core.Models;

public sealed class GasPreset
{
    public required string Id { get; init; }
    public required string NameZh { get; init; }
    public required string NameEn { get; init; }
    public double MolecularWeight { get; init; }
    public double IsentropicExponent { get; init; }
    public double CompressibilityFactor { get; init; }
}

