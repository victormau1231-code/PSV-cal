namespace PSVCalc.Core.Models;

public sealed class StandardProfile
{
    public required string ProfileId { get; init; }
    public required string Name { get; init; }
    public required string EffectiveDate { get; init; }
    public required string Source { get; init; }
    public required IReadOnlyList<StandardCoefficient> Coefficients { get; init; }

    public bool TryGet(string code, out StandardCoefficient? coefficient)
    {
        coefficient = Coefficients.FirstOrDefault(
            x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        return coefficient is not null;
    }

    public double GetValueOrThrow(string code)
    {
        if (!TryGet(code, out StandardCoefficient? coefficient) || coefficient is null)
        {
            throw new KeyNotFoundException($"Coefficient '{code}' is not defined in profile '{ProfileId}'.");
        }

        return coefficient.Value;
    }
}

