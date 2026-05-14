using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class CalculationTests
{
    private readonly ISafetyValveCalculator _calculator = new SafetyValveCalculator(new OrificeSelector());

    [Fact]
    public void GasCriticalFlow_ShouldUseCriticalBranch()
    {
        var input = new CalculationInput
        {
            CaseName = "Gas-Critical",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.2,
            RelievingPressure = 1.6,
            BackPressure = 0.2,
            TemperatureC = 40,
            ReliefLoadKgPerHour = 2500,
            UseGasPreset = true,
            GasPresetId = "air"
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.True(result.Intermediate.IsCriticalFlow);
        Assert.Equal("GasCritical", result.Intermediate.EquationBranch);
    }

    [Fact]
    public void GasSubcriticalFlow_ShouldUseSubcriticalBranch()
    {
        var input = new CalculationInput
        {
            CaseName = "Gas-Subcritical",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.2,
            RelievingPressure = 1.5,
            BackPressure = 1.2,
            TemperatureC = 30,
            ReliefLoadKgPerHour = 1500,
            UseGasPreset = false,
            MolecularWeight = 28.0,
            IsentropicExponentK = 1.4,
            CompressibilityFactorZ = 1.0
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.False(result.Intermediate.IsCriticalFlow);
        Assert.Equal("GasSubcritical", result.Intermediate.EquationBranch);
    }

    [Fact]
    public void SteamFlow_ShouldUseAutoEstimatedProperties()
    {
        var input = new CalculationInput
        {
            CaseName = "Steam-Auto",
            FluidType = FluidType.Steam,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 2.2,
            RelievingPressure = 2.5,
            BackPressure = 0.25,
            TemperatureC = 300,
            ReliefLoadKgPerHour = 5000
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        ParameterAudit mw = Assert.Single(result.ParameterAudits.Where(x => x.Name == "MolecularWeight"));
        ParameterAudit k = Assert.Single(result.ParameterAudits.Where(x => x.Name == "IsentropicExponent"));
        ParameterAudit z = Assert.Single(result.ParameterAudits.Where(x => x.Name == "CompressibilityFactor"));
        Assert.Equal(ParameterSource.AutoEstimated, mw.Source);
        Assert.Equal(ParameterSource.AutoEstimated, k.Source);
        Assert.Equal(ParameterSource.AutoEstimated, z.Source);
        Assert.Contains(result.Warnings, w => w.Contains("Steam properties", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PresetWithOverride_ShouldMarkManualOverrideSource()
    {
        var input = new CalculationInput
        {
            CaseName = "Preset-Override",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.2,
            BackPressure = 0.2,
            TemperatureC = 25,
            ReliefLoadKgPerHour = 1200,
            UseGasPreset = true,
            GasPresetId = "air",
            MolecularWeight = 30.0,
            IsentropicExponentK = 1.4,
            CompressibilityFactorZ = 1.0
        };

        CalculationResult result = _calculator.Calculate(input);
        ParameterAudit mwAudit = Assert.Single(result.ParameterAudits.Where(x => x.Name == "MolecularWeight"));
        Assert.Equal(ParameterSource.ManualOverride, mwAudit.Source);
    }

    [Theory]
    [InlineData(0.0, 1.4, 1.0, "Molecular weight")]
    [InlineData(28.0, 1.0, 1.0, "Isentropic exponent")]
    [InlineData(28.0, 1.4, 0.0, "Compressibility factor")]
    public void CustomGasWithInvalidProperties_ShouldFail(
        double molecularWeight,
        double isentropicExponent,
        double compressibilityFactor,
        string expectedMessage)
    {
        var input = new CalculationInput
        {
            CaseName = "Custom-Gas-Invalid",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.2,
            BackPressure = 0.2,
            TemperatureC = 25,
            ReliefLoadKgPerHour = 1200,
            UseGasPreset = false,
            MolecularWeight = molecularWeight,
            IsentropicExponentK = isentropicExponent,
            CompressibilityFactorZ = compressibilityFactor
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => _calculator.Calculate(input));
        Assert.Contains(expectedMessage, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LiquidFlow_ShouldUseLiquidBranch()
    {
        var input = new CalculationInput
        {
            CaseName = "Liquid-Case",
            FluidType = FluidType.Liquid,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.1,
            RelievingPressure = 1.3,
            BackPressure = 0.2,
            TemperatureC = 40,
            ReliefLoadKgPerHour = 5000,
            LiquidDensityKgPerM3 = 860
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Equal("LiquidIncompressible", result.Intermediate.EquationBranch);
        Assert.Contains(result.ParameterAudits, x => x.Name == "LiquidDensity" && x.Source == ParameterSource.Manual);
    }

    [Fact]
    public void OverpressureBeyondApiLimit_ShouldEmitBilingualWarning()
    {
        var input = new CalculationInput
        {
            CaseName = "Api-Limit-Warning",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.3,
            BackPressure = 0.2,
            TemperatureC = 25,
            ReliefLoadKgPerHour = 1200,
            UseGasPreset = true,
            GasPresetId = "air"
        };

        CalculationResult result = _calculator.Calculate(input);

        string warning = Assert.Single(result.Warnings.Where(w => w.Contains("API limit exceeded", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains("超过API限制", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void TubeRuptureScenario_ShouldUseGuillotineBreakAreaAndHighSideProperties()
    {
        const double tubeInnerDiameterMm = 19.0;
        var input = new CalculationInput
        {
            CaseName = "Tube-Rupture",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.TubeRupture,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.2,
            BackPressure = 0.2,
            TemperatureC = 30,
            ReliefLoadKgPerHour = 0,
            TubeInnerDiameterMm = tubeInnerDiameterMm,
            UseGasPreset = true,
            GasPresetId = "air"
        };

        CalculationResult result = _calculator.Calculate(input);

        double tubeInnerDiameterM = tubeInnerDiameterMm / 1000.0;
        double expectedDischargeAreaM2 = 2.0 * Math.PI * tubeInnerDiameterM * tubeInnerDiameterM / 4.0;

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Equal(expectedDischargeAreaM2, result.Intermediate.TubeBreakDischargeAreaM2, 12);
        Assert.True(result.Intermediate.ReliefLoadKgPerHourUsed > 0);
        Assert.Contains(
            result.Warnings,
            x => x.Contains("guillotine-break area", StringComparison.OrdinalIgnoreCase) &&
                 x.Contains("高压侧流体特性", StringComparison.Ordinal));
        Assert.Contains(result.ParameterAudits, x => x.Name == "TubeInnerDiameter");
        Assert.Contains(result.ParameterAudits, x => x.Name == "TubeBreakDischargeArea" && x.Source == ParameterSource.Formula);
    }

    [Fact]
    public void TubeRuptureScenario_WithoutTubeInnerDiameter_ShouldFail()
    {
        var input = new CalculationInput
        {
            CaseName = "Tube-Rupture-Invalid",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.TubeRupture,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.2,
            BackPressure = 0.2,
            TemperatureC = 30,
            ReliefLoadKgPerHour = 0,
            TubeInnerDiameterMm = 0.0,
            UseGasPreset = true,
            GasPresetId = "air"
        };

        ArgumentException ex = Assert.Throws<ArgumentException>(() => _calculator.Calculate(input));
        Assert.Contains("Tube inner diameter", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FireScenario_ShouldComputeLoadFromHeatInputFormula()
    {
        var input = new CalculationInput
        {
            CaseName = "Fire-Scenario",
            FluidType = FluidType.Steam,
            ReliefScenario = ReliefScenario.Fire,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.21,
            BackPressure = 0.2,
            TemperatureC = 260,
            ReliefLoadKgPerHour = 1,
            FireWettedAreaM2 = 100.0,
            FireEnvironmentalFactorF = 1.0,
            FireConstantC = 43.2,
            VaporizationLatentHeatKjPerKg = 2257.0
        };

        CalculationResult result = _calculator.Calculate(input);

        double expectedHeatInputKw = 43.2 * Math.Pow(100.0, 0.82);
        double expectedReliefLoadKgPerHour = expectedHeatInputKw * 3600.0 / 2257.0;

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Equal(expectedHeatInputKw, result.Intermediate.HeatInputKw, 10);
        Assert.Equal(expectedReliefLoadKgPerHour, result.Intermediate.ReliefLoadKgPerHourUsed, 8);
        Assert.Contains(result.ParameterAudits, x => x.Name == "FireHeatInput" && x.Source == ParameterSource.Formula);
    }

    [Fact]
    public void ThermalExpansionScenario_ShouldComputeLoadFromPopupInputsWhenManualLoadIsEmpty()
    {
        var input = new CalculationInput
        {
            CaseName = "Thermal-Expansion-Scenario",
            StandardBasis = CalculationStandardBasis.HgT20570_2,
            FluidType = FluidType.Liquid,
            ReliefScenario = ReliefScenario.ThermalExpansion,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.2,
            TemperatureC = 35,
            ReliefLoadKgPerHour = 0.0,
            LiquidDensityKgPerM3 = 860.0,
            ThermalExpansionCoefficientPerC = 0.0011,
            ThermalHeatInputKjPerHour = 18000.0,
            ThermalSpecificHeatKjPerKgC = 2.4
        };

        CalculationResult result = _calculator.Calculate(input);
        double expectedVolumeFlow = 0.0011 * 18000.0 / (860.0 * 2.4);
        double expectedMassLoad = expectedVolumeFlow * 860.0;

        Assert.Equal("LiquidIncompressible", result.Intermediate.EquationBranch);
        Assert.Equal(expectedVolumeFlow, result.Intermediate.ThermalExpansionVolumeFlowM3PerHour, 10);
        Assert.Equal(expectedMassLoad, result.Intermediate.ThermalExpansionCalculatedLoadKgPerHour, 10);
        Assert.Equal(expectedMassLoad, result.Intermediate.ReliefLoadKgPerHourUsed, 10);
        Assert.Contains(result.ParameterAudits, x => x.Name == "ThermalExpansionCalculatedLoad" && x.Source == ParameterSource.Formula);
        Assert.Contains(result.Warnings, x => x.Contains("HG/T 20570.2", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OperatingPressureBasis_ShouldDeriveOverpressureForCalculation()
    {
        var input = new CalculationInput
        {
            CaseName = "Operating-Basis",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            UseOperatingPressureBasis = true,
            OperatingPressure = 1.5,
            AllowedOverpressurePercentInput = 10.0,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.0,
            BackPressure = 0.2,
            TemperatureC = 30,
            ReliefLoadKgPerHour = 1800,
            UseGasPreset = true,
            GasPresetId = "air"
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Equal(10.0, result.Intermediate.OverpressurePercent, 8);
        Assert.Equal(1.5, result.Intermediate.SetPressureValue, 8);
        Assert.Equal(1.65, result.Intermediate.RelievingPressureValue, 8);
        Assert.Equal(1.395, result.Intermediate.ReseatPressureValue, 8);
        Assert.Contains(result.ParameterAudits, x => x.Name == "OperatingPressure");
        Assert.Contains(result.ParameterAudits, x => x.Name == "RelievingPressureDerived" && x.Source == ParameterSource.Formula);
        Assert.Contains(result.ParameterAudits, x => x.Name == "ReseatPressureDerived" && x.Source == ParameterSource.Formula);
    }

    [Fact]
    public void GaugePressureWithCustomAtmosphericPressure_ShouldUseCustomAbsoluteBasis()
    {
        var input = new CalculationInput
        {
            CaseName = "Custom-Atmosphere",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Gauge,
            PressureUnit = PressureUnit.MPa,
            AtmosphericPressure = 0.095,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.0,
            TemperatureC = 30,
            ReliefLoadKgPerHour = 1600,
            UseGasPreset = true,
            GasPresetId = "air"
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.Equal(95_000.0, result.Intermediate.AtmosphericPressureAbsPa, 6);
        Assert.Equal(1_095_000.0, result.Intermediate.SetPressureAbsPa, 6);
        Assert.Equal(1_195_000.0, result.Intermediate.RelievingPressureAbsPa, 6);
        Assert.Equal(95_000.0, result.Intermediate.BackPressureAbsPa, 6);
        Assert.Contains(result.ParameterAudits, x => x.Name == "AtmosphericPressure" && Math.Abs(x.Value - 0.095) < 1e-9);
    }

    [Fact]
    public void BalancedBellows_ShouldDefaultKbToOneAndEmitWarning()
    {
        var input = new CalculationInput
        {
            CaseName = "Bellows-BackPressure",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            ValveConfiguration = ValveConfiguration.BalancedBellows,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.2,
            TemperatureC = 35,
            ReliefLoadKgPerHour = 1500,
            UseGasPreset = true,
            GasPresetId = "air",
            BackPressureCorrectionKb = 0.0
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Contains(result.Warnings, x => x.Contains("Balanced-bellows valve selected", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ParameterAudits, x => x.Name == "Kb" && Math.Abs(x.Value - 1.0) < 1e-9);
    }

    [Fact]
    public void BalancedBellowsWithCustomKb_ShouldHonorManualKb()
    {
        var input = new CalculationInput
        {
            CaseName = "Bellows-Custom-Kb",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            ValveConfiguration = ValveConfiguration.BalancedBellows,
            UseCustomBellowsKb = true,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.2,
            TemperatureC = 35,
            ReliefLoadKgPerHour = 1500,
            UseGasPreset = true,
            GasPresetId = "air",
            BackPressureCorrectionKb = 0.84
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Contains(result.Warnings, x => x.Contains("custom Kb enabled", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ParameterAudits, x => x.Name == "Kb" && Math.Abs(x.Value - 0.84) < 1e-9 && x.Source == ParameterSource.Manual);
    }

    [Fact]
    public void PilotOperated_ShouldDefaultKbToOneAndEmitVendorReviewWarning()
    {
        var input = new CalculationInput
        {
            CaseName = "Pilot-Operated-Gas",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            ValveConfiguration = ValveConfiguration.PilotOperated,
            PilotMinimumOperatingDifferentialPercent = 12.5,
            PilotSenseLineMode = PilotSenseLineMode.External,
            PilotVentToAtmosphere = false,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.2,
            TemperatureC = 32,
            ReliefLoadKgPerHour = 1600,
            UseGasPreset = true,
            GasPresetId = "air",
            BackPressureCorrectionKb = 0.0
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Contains(result.Warnings, x => x.Contains("Pilot-operated valve selected", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, x => x.Contains("external sense line", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ParameterAudits, x => x.Name == "Kb" && Math.Abs(x.Value - 1.0) < 1e-9);
        Assert.Contains(result.ParameterAudits, x => x.Name == "PilotMinimumOperatingDifferentialPercent" && Math.Abs(x.Value - 12.5) < 1e-9);
    }

    [Fact]
    public void TwoPhaseEquilibriumOmega_ShouldUseAnnexCOmegaBranch()
    {
        var input = new CalculationInput
        {
            CaseName = "TwoPhase-Omega",
            FluidType = FluidType.TwoPhaseEquilibrium,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 0.45,
            RelievingPressure = 0.556379,
            BackPressure = 0.2047,
            TemperatureC = 93.3,
            ReliefLoadKgPerHour = 216560.0,
            TwoPhaseInletSpecificVolumeM3PerKg = 0.01945,
            TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = 0.02265,
            DischargeCoefficientKd = 0.85,
            BackPressureCorrectionKb = 1.0,
            CombinationCorrectionKc = 1.0
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Equal("TwoPhaseOmegaCritical", result.Intermediate.EquationBranch);
        Assert.InRange(result.Intermediate.TwoPhaseOmega, 1.480, 1.483);
        Assert.Equal(0.66, result.Intermediate.CriticalPressureRatio, 2);
        Assert.InRange(result.Intermediate.MassFluxKgPerM2S, 2440.0, 2475.0);
    }

    [Fact]
    public void TwoPhaseSubcooledOmega_ShouldUseApiSubcooledBranch()
    {
        var input = new CalculationInput
        {
            CaseName = "TwoPhase-Subcooled",
            FluidType = FluidType.TwoPhaseSubcooled,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.8,
            RelievingPressure = 2.07325,
            BackPressure = 0.1703,
            TemperatureC = 15.6,
            ReliefLoadKgPerHour = 1000.0,
            LiquidDensityKgPerM3 = 511.3,
            TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = 262.7,
            TwoPhaseSaturationPressureAbsolute = 0.741875,
            DischargeCoefficientKd = 0.65,
            BackPressureCorrectionKb = 1.0,
            CombinationCorrectionKc = 1.0
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.RequiredAreaMm2 > 0);
        Assert.Equal("TwoPhaseSubcooledHighCritical", result.Intermediate.EquationBranch);
        Assert.InRange(result.Intermediate.TwoPhaseOmega, 8.51, 8.53);
        Assert.InRange(result.Intermediate.TwoPhaseTransitionSaturationPressureRatio, 0.944, 0.945);
        Assert.Equal(0.3578, result.Intermediate.TwoPhaseSaturationPressureRatio, 3);
        Assert.InRange(result.Intermediate.MassFluxKgPerM2S, 23_970.0, 24_010.0);
    }

    [Fact]
    public void TubeRuptureWithTwoPhaseEquilibrium_ShouldAutoDeriveLoadFromHighSideData()
    {
        const double tubeInnerDiameterMm = 19.0;
        var input = new CalculationInput
        {
            CaseName = "Tube-Rupture-TwoPhase",
            FluidType = FluidType.TwoPhaseEquilibrium,
            ReliefScenario = ReliefScenario.TubeRupture,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 0.45,
            RelievingPressure = 0.556379,
            BackPressure = 0.2047,
            HighSidePressure = 1.5,
            HighSideTemperatureC = 40.0,
            TemperatureC = 93.3,
            ReliefLoadKgPerHour = 0.0,
            TubeInnerDiameterMm = tubeInnerDiameterMm,
            TwoPhaseInletSpecificVolumeM3PerKg = 0.01945,
            TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = 0.02265,
            HighSideTwoPhaseInletSpecificVolumeM3PerKg = 0.008,
            HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = 0.0095,
            DischargeCoefficientKd = 0.85,
            BackPressureCorrectionKb = 1.0,
            CombinationCorrectionKc = 1.0
        };

        CalculationResult result = _calculator.Calculate(input);

        Assert.True(result.Intermediate.ReliefLoadKgPerHourUsed > 0);
        Assert.True(result.Intermediate.SourceMassFluxKgPerM2S > 0);
        Assert.Contains(result.ParameterAudits, x => x.Name == "HighSideTwoPhaseInletSpecificVolume");
        Assert.Contains(result.Warnings, x => x.Contains("API 520 Part I Annex C C.2.2 Omega Method", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HgTGasCriticalFlow_ShouldUseHgTCriticalEquation()
    {
        var calculator = new SafetyValveCalculator(new OrificeSelector(), new JsonStandardProfileProvider());
        var input = new CalculationInput
        {
            CaseName = "HgT-Gas-Critical",
            StandardBasis = CalculationStandardBasis.HgT20570_2,
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.2,
            RelievingPressure = 1.6,
            BackPressure = 0.2,
            TemperatureC = 40,
            ReliefLoadKgPerHour = 2500,
            UseGasPreset = true,
            GasPresetId = "air"
        };

        CalculationResult result = calculator.Calculate(input);

        double temperatureK = input.TemperatureC + 273.15;
        double x = 520.0 * Math.Sqrt(1.4 * Math.Pow(2.0 / 2.4, 6.0));
        double expectedArea = 13.16 * input.ReliefLoadKgPerHour / (0.975 * x * input.RelievingPressure)
                              * Math.Sqrt((temperatureK * 1.0) / 28.97);

        Assert.Equal("HG/T 20570.2-1995", result.StandardVersion);
        Assert.Equal("HgTGasCritical", result.Intermediate.EquationBranch);
        Assert.Equal(expectedArea, result.RequiredAreaMm2, 6);
    }

    [Fact]
    public void HgTTubeRuptureLiquid_ShouldCapCalculatedLoadByHighSideNormalFlow()
    {
        var calculator = new SafetyValveCalculator(new OrificeSelector(), new JsonStandardProfileProvider());
        var input = new CalculationInput
        {
            CaseName = "HgT-Tube-Rupture-Liquid",
            StandardBasis = CalculationStandardBasis.HgT20570_2,
            FluidType = FluidType.Liquid,
            ReliefScenario = ReliefScenario.TubeRupture,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.16,
            BackPressure = 0.2,
            TemperatureC = 35,
            ReliefLoadKgPerHour = 0.0,
            LiquidDensityKgPerM3 = 850.0,
            TubeInnerDiameterMm = 19.0,
            HighSidePressure = 3.0,
            HighSideTemperatureC = 45.0,
            TubeRuptureHighSideNormalFlowKgPerHour = 5000.0
        };

        CalculationResult result = calculator.Calculate(input);

        Assert.Equal("HG/T 20570.2-1995", result.StandardVersion);
        Assert.Equal(5000.0, result.Intermediate.ReliefLoadKgPerHourUsed, 8);
        Assert.Contains(result.Warnings, x => x.Contains("HG/T 20570.2 clause 7.0.8", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ParameterAudits, x => x.Name == "TubeRuptureCalculatedLoad");
        Assert.Contains(result.ParameterAudits, x => x.Name == "TubeRuptureHighSideNormalFlow");
    }

    [Fact]
    public void ApiSelection_ShouldWarnWhenOrificeIsUpsizedForNinetyPercentMargin()
    {
        var calculator = new SafetyValveCalculator(new MarginUpsizeSelector());
        var input = new CalculationInput
        {
            CaseName = "Api-Margin-Upsize",
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.2,
            TemperatureC = 35,
            ReliefLoadKgPerHour = 1500,
            UseGasPreset = true,
            GasPresetId = "air"
        };

        CalculationResult result = calculator.Calculate(input);

        Assert.Contains(result.Warnings, x => x.Contains("API margin rule applied", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Warnings, x => x.Contains("已应用 API 选型裕量规则", StringComparison.Ordinal));
        Assert.Equal("Q", result.OrificeRecommendation.Selected.Letter);
        Assert.True(result.OrificeRecommendation.WasUpsizedForMargin);
    }
}

file sealed class MarginUpsizeSelector : IOrificeSelector
{
    public OrificeRecommendation Recommend(double requiredAreaMm2)
    {
        return new OrificeRecommendation
        {
            RequiredAreaMm2 = requiredAreaMm2,
            MinimumRecommendedAreaMm2 = requiredAreaMm2 / 0.9,
            MaximumRecommendedUtilizationPercent = 90.0,
            Selected = new OrificeDefinition
            {
                Letter = "Q",
                AreaIn2 = 11.05,
                AreaMm2 = 11.05 * 645.16,
                InletNominalInch = 6,
                OutletNominalInch = 8
            },
            CandidateNeighbors =
            [
                new OrificeDefinition
                {
                    Letter = "P",
                    AreaIn2 = 6.38,
                    AreaMm2 = 6.38 * 645.16,
                    InletNominalInch = 4,
                    OutletNominalInch = 6
                },
                new OrificeDefinition
                {
                    Letter = "Q",
                    AreaIn2 = 11.05,
                    AreaMm2 = 11.05 * 645.16,
                    InletNominalInch = 6,
                    OutletNominalInch = 8
                }
            ],
            IsCapacityExceededByLargestOrifice = false,
            WasUpsizedForMargin = true,
            DirectAreaQualifiedLetter = "P"
        };
    }

    public IReadOnlyList<OrificeDefinition> GetAll()
    {
        return
        [
            new OrificeDefinition
            {
                Letter = "P",
                AreaIn2 = 6.38,
                AreaMm2 = 6.38 * 645.16,
                InletNominalInch = 4,
                OutletNominalInch = 6
            },
            new OrificeDefinition
            {
                Letter = "Q",
                AreaIn2 = 11.05,
                AreaMm2 = 11.05 * 645.16,
                InletNominalInch = 6,
                OutletNominalInch = 8
            }
        ];
    }
}
