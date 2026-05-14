namespace PSVCalc.Core.Models;

public sealed class OrificeDefinition
{
    public required string Letter { get; init; }
    public double AreaIn2 { get; init; }
    public double AreaMm2 { get; init; }
    public int InletNominalInch { get; init; }
    public int OutletNominalInch { get; init; }
}
