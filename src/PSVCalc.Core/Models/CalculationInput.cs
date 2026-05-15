using PSVCalc.Core.Enums;

namespace PSVCalc.Core.Models;

public sealed class CalculationInput
{
    public string CaseName { get; set; } = "New Case";
    public CalculationStandardBasis StandardBasis { get; set; } = CalculationStandardBasis.Api520521Asme;
    public FluidType FluidType { get; set; } = FluidType.Gas;
    public MaterialServiceCondition MaterialServiceCondition { get; set; } = MaterialServiceCondition.CleanNonCorrosive;
    public ReliefScenario ReliefScenario { get; set; } = ReliefScenario.Overpressure;
    public ValveConfiguration ValveConfiguration { get; set; } = ValveConfiguration.ConventionalSpring;

    public PressureInputMode PressureInputMode { get; set; } = PressureInputMode.Gauge;
    public PressureUnit PressureUnit { get; set; } = PressureUnit.MPa;
    public double AtmosphericPressure { get; set; } = 0.101325;
    public bool UseOperatingPressureBasis { get; set; } = false;
    public double OperatingPressure { get; set; } = 1.0;
    public double AllowedOverpressurePercentInput { get; set; } = 10.0;
    public double BlowdownPercent { get; set; } = 7.0;
    public double PilotMinimumOperatingDifferentialPercent { get; set; } = 10.0;
    public PilotSenseLineMode PilotSenseLineMode { get; set; } = PilotSenseLineMode.Internal;
    public bool PilotVentToAtmosphere { get; set; } = true;
    public double SetPressure { get; set; } = 1.0;
    public double RelievingPressure { get; set; } = 1.0;
    public double BackPressure { get; set; } = 0.0;
    public double HighSidePressure { get; set; } = 1.5;
    public double HighSideTemperatureC { get; set; } = 40.0;

    public double TemperatureC { get; set; } = 38.0;
    public double ReliefLoadKgPerHour { get; set; } = 1000.0;
    public double ThermalExpansionCoefficientPerC { get; set; } = 0.0;
    public double ThermalHeatInputKjPerHour { get; set; } = 0.0;
    public double ThermalSpecificHeatKjPerKgC { get; set; } = 0.0;
    public double FireWettedAreaM2 { get; set; } = 10.0;
    public double FireEnvironmentalFactorF { get; set; } = 1.0;
    public double FireConstantC { get; set; } = 43.2;
    public double VaporizationLatentHeatKjPerKg { get; set; } = 2257.0;

    public double DischargeCoefficientKd { get; set; } = 0.975;
    public double BackPressureCorrectionKb { get; set; } = 1.0;
    public bool UseCustomBellowsKb { get; set; } = false;
    public double CombinationCorrectionKc { get; set; } = 1.0;

    public bool UseGasPreset { get; set; } = true;
    public string? GasPresetId { get; set; } = "air";
    public double MolecularWeight { get; set; } = 28.97;
    public double CompressibilityFactorZ { get; set; } = 1.0;
    public double IsentropicExponentK { get; set; } = 1.4;
    public double LiquidDensityKgPerM3 { get; set; } = 1000.0;
    public double TwoPhaseInletSpecificVolumeM3PerKg { get; set; } = 0.02;
    public double TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg { get; set; } = 0.024;
    public double TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 { get; set; } = 260.0;
    public double TwoPhaseSaturationPressureAbsolute { get; set; } = 0.6;
    public double TubeInnerDiameterMm { get; set; } = 19.0;
    public double TubeRuptureHighSideNormalFlowKgPerHour { get; set; } = 0.0;
    public double HighSideLiquidDensityKgPerM3 { get; set; } = 900.0;
    public double HighSideTwoPhaseInletSpecificVolumeM3PerKg { get; set; } = 0.02;
    public double HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg { get; set; } = 0.024;
    public double HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 { get; set; } = 260.0;
    public double HighSideTwoPhaseSaturationPressureAbsolute { get; set; } = 0.8;

    public string Notes { get; set; } = string.Empty;
}
