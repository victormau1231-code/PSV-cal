using PSVCalc.Core.Enums;

namespace PSVCalc.Core.Models;

public sealed class ParameterAudit
{
    public required string Name { get; init; }
    public double Value { get; init; }
    public required string Unit { get; init; }
    public ParameterSource Source { get; init; }
}

