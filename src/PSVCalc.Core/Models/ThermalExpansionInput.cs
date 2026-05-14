namespace PSVCalc.Core.Models;

public sealed class ThermalExpansionInput
{
    public double VolumetricExpansionCoefficientPerC { get; init; }
    public double HeatInputKjPerHour { get; init; }
    public double LiquidDensityKgPerM3 { get; init; }
    public double SpecificHeatKjPerKgC { get; init; }
}
