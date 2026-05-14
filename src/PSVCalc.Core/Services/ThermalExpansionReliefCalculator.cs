using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public sealed class ThermalExpansionReliefCalculator
{
    public ThermalExpansionResult Calculate(ThermalExpansionInput input)
    {
        if (input.VolumetricExpansionCoefficientPerC <= 0)
        {
            throw new ArgumentException("Thermal expansion coefficient must be greater than zero.", nameof(input));
        }

        if (input.HeatInputKjPerHour <= 0)
        {
            throw new ArgumentException("Thermal heat input must be greater than zero.", nameof(input));
        }

        if (input.LiquidDensityKgPerM3 <= 0)
        {
            throw new ArgumentException("Liquid density must be greater than zero.", nameof(input));
        }

        if (input.SpecificHeatKjPerKgC <= 0)
        {
            throw new ArgumentException("Specific heat must be greater than zero.", nameof(input));
        }

        double volumeReliefFlowM3PerHour =
            input.VolumetricExpansionCoefficientPerC
            * input.HeatInputKjPerHour
            / (input.LiquidDensityKgPerM3 * input.SpecificHeatKjPerKgC);

        double massReliefLoadKgPerHour = volumeReliefFlowM3PerHour * input.LiquidDensityKgPerM3;

        return new ThermalExpansionResult
        {
            VolumeReliefFlowM3PerHour = volumeReliefFlowM3PerHour,
            MassReliefLoadKgPerHour = massReliefLoadKgPerHour
        };
    }
}
