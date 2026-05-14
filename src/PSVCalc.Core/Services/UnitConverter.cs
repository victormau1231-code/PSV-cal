using PSVCalc.Core.Enums;

namespace PSVCalc.Core.Services;

public static class UnitConverter
{
    public const double AtmosphericPressurePa = 101_325.0;

    public static double PressureToMPa(double value, PressureUnit unit)
    {
        return unit switch
        {
            PressureUnit.MPa => value,
            PressureUnit.kPa => value / 1_000.0,
            PressureUnit.bar => value / 10.0,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported pressure unit.")
        };
    }

    public static double PressureFromMPa(double valueMPa, PressureUnit unit)
    {
        return unit switch
        {
            PressureUnit.MPa => valueMPa,
            PressureUnit.kPa => valueMPa * 1_000.0,
            PressureUnit.bar => valueMPa * 10.0,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported pressure unit.")
        };
    }

    public static double ToAbsolutePressurePa(
        double pressureValue,
        PressureUnit unit,
        PressureInputMode mode,
        double atmosphericPressureInSelectedUnit = double.NaN)
    {
        double mpaValue = PressureToMPa(pressureValue, unit);
        double atmosphericPressureMpa = ResolveAtmosphericPressureMpa(unit, atmosphericPressureInSelectedUnit);
        double absMpa = mode == PressureInputMode.Gauge
            ? mpaValue + atmosphericPressureMpa
            : mpaValue;
        return absMpa * 1_000_000.0;
    }

    public static double FromAbsolutePressurePa(
        double absolutePressurePa,
        PressureUnit unit,
        PressureInputMode mode,
        double atmosphericPressureInSelectedUnit = double.NaN)
    {
        double absMpa = absolutePressurePa / 1_000_000.0;
        double atmosphericPressureMpa = ResolveAtmosphericPressureMpa(unit, atmosphericPressureInSelectedUnit);
        double displayMpa = mode == PressureInputMode.Gauge
            ? absMpa - atmosphericPressureMpa
            : absMpa;
        return PressureFromMPa(displayMpa, unit);
    }

    private static double ResolveAtmosphericPressureMpa(PressureUnit unit, double atmosphericPressureInSelectedUnit)
    {
        if (double.IsNaN(atmosphericPressureInSelectedUnit) || atmosphericPressureInSelectedUnit <= 0)
        {
            return AtmosphericPressurePa / 1_000_000.0;
        }

        return PressureToMPa(atmosphericPressureInSelectedUnit, unit);
    }
}
