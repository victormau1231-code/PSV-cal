namespace PSVCalc.Core.Models;

public sealed class StandardCoefficient
{
    public required string Code { get; init; }
    public double Value { get; init; }
    public required string Unit { get; init; }
    public required string Description { get; init; }
    public required string Clause { get; init; }
}

