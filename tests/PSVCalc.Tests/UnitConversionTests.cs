using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class UnitConversionTests
{
    [Fact]
    public void GaugePressure_ShouldConvertToAbsolutePa()
    {
        double absPa = UnitConverter.ToAbsolutePressurePa(
            pressureValue: 1.0,
            unit: PressureUnit.MPa,
            mode: PressureInputMode.Gauge);

        Assert.InRange(absPa, 1_101_324.0, 1_101_326.0);
    }

    [Fact]
    public void PressureUnitRoundTrip_ShouldStayConsistent()
    {
        double originalMpa = 2.35;
        double asBar = UnitConverter.PressureFromMPa(originalMpa, PressureUnit.bar);
        double backToMpa = UnitConverter.PressureToMPa(asBar, PressureUnit.bar);

        Assert.InRange(backToMpa, 2.349999, 2.350001);
    }

    [Fact]
    public void DefaultGaugeBackPressure_ShouldRepresentAtmosphericDischarge()
    {
        var input = new CalculationInput();

        Assert.Equal(PressureInputMode.Gauge, input.PressureInputMode);
        Assert.Equal(0.0, input.BackPressure);

        double backPressureAbsPa = UnitConverter.ToAbsolutePressurePa(
            input.BackPressure,
            input.PressureUnit,
            input.PressureInputMode,
            input.AtmosphericPressure);

        Assert.InRange(backPressureAbsPa, 101_324.0, 101_326.0);
    }
}
