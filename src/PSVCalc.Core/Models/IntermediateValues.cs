namespace PSVCalc.Core.Models;

public sealed class IntermediateValues
{
    public double SetPressureValue { get; init; }
    public double RelievingPressureValue { get; init; }
    public double ReseatPressureValue { get; init; }
    public double BlowdownPercent { get; init; }

    public double SetPressureAbsPa { get; init; }
    public double RelievingPressureAbsPa { get; init; }
    public double ReseatPressureAbsPa { get; init; }
    public double BackPressureAbsPa { get; init; }
    public double AtmosphericPressureAbsPa { get; init; }
    public double TemperatureK { get; init; }

    public double PressureRatioP2OverP1 { get; init; }
    public double CriticalPressureRatio { get; init; }
    public bool IsCriticalFlow { get; init; }

    public double OverpressurePercent { get; init; }
    public double AllowedOverpressurePercent { get; init; }

    public double SpecificGasConstant { get; init; }
    public double EffectiveDischargeFactor { get; init; }
    public double MassFluxKgPerM2S { get; init; }
    public double ReliefLoadKgPerHourUsed { get; init; }
    public double ThermalExpansionVolumeFlowM3PerHour { get; init; }
    public double ThermalExpansionCalculatedLoadKgPerHour { get; init; }
    public double HeatInputKw { get; init; }
    public double TubeBreakDischargeAreaM2 { get; init; }
    public double RequiredAreaM2 { get; init; }
    public double TwoPhaseOmega { get; init; }
    public double TwoPhaseSaturationPressureAbsPa { get; init; }
    public double TwoPhaseSaturationPressureRatio { get; init; }
    public double TwoPhaseTransitionSaturationPressureRatio { get; init; }
    public double TwoPhaseInletSpecificVolumeM3PerKg { get; init; }
    public double TwoPhaseReferenceSpecificVolumeM3PerKg { get; init; }
    public double TwoPhaseReferenceDensityKgPerM3 { get; init; }
    public double SourceMassFluxKgPerM2S { get; init; }

    public required string EquationBranch { get; init; }
}
