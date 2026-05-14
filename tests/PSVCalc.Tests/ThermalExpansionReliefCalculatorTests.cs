using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class ThermalExpansionReliefCalculatorTests
{
    private readonly ThermalExpansionReliefCalculator _calculator = new();

    [Fact]
    public void Calculate_ShouldReturnExpectedVolumeAndMassLoads()
    {
        var input = new ThermalExpansionInput
        {
            VolumetricExpansionCoefficientPerC = 0.0012,
            HeatInputKjPerHour = 25000.0,
            LiquidDensityKgPerM3 = 850.0,
            SpecificHeatKjPerKgC = 2.2
        };

        ThermalExpansionResult result = _calculator.Calculate(input);

        double expectedVolumeFlow = 0.0012 * 25000.0 / (850.0 * 2.2);
        double expectedMassLoad = expectedVolumeFlow * 850.0;

        Assert.Equal(expectedVolumeFlow, result.VolumeReliefFlowM3PerHour, 12);
        Assert.Equal(expectedMassLoad, result.MassReliefLoadKgPerHour, 12);
    }

    [Fact]
    public void Calculate_WithInvalidSpecificHeat_ShouldThrow()
    {
        var input = new ThermalExpansionInput
        {
            VolumetricExpansionCoefficientPerC = 0.0012,
            HeatInputKjPerHour = 25000.0,
            LiquidDensityKgPerM3 = 850.0,
            SpecificHeatKjPerKgC = 0.0
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => _calculator.Calculate(input));

        Assert.Contains("Specific heat", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
