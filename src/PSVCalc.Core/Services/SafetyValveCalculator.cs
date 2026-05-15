using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public sealed class SafetyValveCalculator : ISafetyValveCalculator
{
    private const double DefaultUniversalGasConstant = 8.314_462_618;
    private readonly IOrificeSelector _orificeSelector;
    private readonly IStandardProfileProvider? _standardProfileProvider;
    private readonly TwoPhaseOmegaCalculator _twoPhaseOmegaCalculator = new();
    private readonly ThermalExpansionReliefCalculator _thermalExpansionCalculator = new();

    public SafetyValveCalculator(IOrificeSelector orificeSelector)
        : this(orificeSelector, standardProfileProvider: null)
    {
    }

    public SafetyValveCalculator(IOrificeSelector orificeSelector, IStandardProfileProvider? standardProfileProvider)
    {
        _orificeSelector = orificeSelector;
        _standardProfileProvider = standardProfileProvider;
    }

    public CalculationResult Calculate(CalculationInput input)
    {
        Validate(input);

        StandardProfile? standardProfile = ResolveStandardProfile(input);
        bool isHgT = input.StandardBasis == CalculationStandardBasis.HgT20570_2;

        var warnings = new List<string>();
        var audits = new List<ParameterAudit>();
        double atmosphericPressurePa = ResolveAtmosphericPressurePa(input, standardProfile);

        double setPressureInput = input.UseOperatingPressureBasis
            ? input.OperatingPressure
            : input.SetPressure;
        double relievingPressureInput = input.UseOperatingPressureBasis
            ? input.OperatingPressure * (1.0 + input.AllowedOverpressurePercentInput / 100.0)
            : input.RelievingPressure;
        double blowdownPercent = input.BlowdownPercent > 0
            ? input.BlowdownPercent
            : GetDefaultBlowdownPercent(input.FluidType);
        double reseatPressureInput = Math.Max(
            0.0,
            setPressureInput * (1.0 - blowdownPercent / 100.0));

        if (input.UseOperatingPressureBasis)
        {
            audits.Add(new ParameterAudit
            {
                Name = "OperatingPressure",
                Value = input.OperatingPressure,
                Unit = input.PressureUnit.ToString(),
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "AllowedOverpressurePercentInput",
                Value = input.AllowedOverpressurePercentInput,
                Unit = "%",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "SetPressureDerived",
                Value = setPressureInput,
                Unit = input.PressureUnit.ToString(),
                Source = ParameterSource.Formula
            });
            audits.Add(new ParameterAudit
            {
                Name = "RelievingPressureDerived",
                Value = relievingPressureInput,
                Unit = input.PressureUnit.ToString(),
                Source = ParameterSource.Formula
            });
        }

        audits.Add(new ParameterAudit
        {
            Name = "BlowdownPercent",
            Value = blowdownPercent,
            Unit = "%",
            Source = input.BlowdownPercent > 0 ? ParameterSource.Manual : ParameterSource.Preset
        });
        if (input.ValveConfiguration == ValveConfiguration.PilotOperated)
        {
            audits.Add(new ParameterAudit
            {
                Name = "PilotMinimumOperatingDifferentialPercent",
                Value = input.PilotMinimumOperatingDifferentialPercent,
                Unit = "%",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "PilotSenseLineMode",
                Value = (double)input.PilotSenseLineMode,
                Unit = "-",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "PilotVentToAtmosphere",
                Value = input.PilotVentToAtmosphere ? 1.0 : 0.0,
                Unit = "-",
                Source = ParameterSource.Manual
            });
        }
        audits.Add(new ParameterAudit
        {
            Name = "AtmosphericPressure",
            Value = input.AtmosphericPressure,
            Unit = input.PressureUnit.ToString(),
            Source = ParameterSource.Manual
        });
        audits.Add(new ParameterAudit
        {
            Name = "ReseatPressureDerived",
            Value = reseatPressureInput,
            Unit = input.PressureUnit.ToString(),
            Source = ParameterSource.Formula
        });

        double pSetAbsPa = UnitConverter.ToAbsolutePressurePa(
            setPressureInput,
            input.PressureUnit,
            input.PressureInputMode,
            input.AtmosphericPressure);

        double p1AbsPa = UnitConverter.ToAbsolutePressurePa(
            relievingPressureInput,
            input.PressureUnit,
            input.PressureInputMode,
            input.AtmosphericPressure);

        double pReseatAbsPa = UnitConverter.ToAbsolutePressurePa(
            reseatPressureInput,
            input.PressureUnit,
            input.PressureInputMode,
            input.AtmosphericPressure);

        double p2AbsPa = UnitConverter.ToAbsolutePressurePa(
            input.BackPressure,
            input.PressureUnit,
            input.PressureInputMode,
            input.AtmosphericPressure);

        if (p2AbsPa <= 0)
        {
            p2AbsPa = atmosphericPressurePa;
            warnings.Add("Back pressure was adjusted to atmospheric pressure.");
        }

        if (p2AbsPa >= p1AbsPa)
        {
            p2AbsPa = p1AbsPa * 0.9;
            warnings.Add("Back pressure was reduced to 90% of relieving pressure for physical consistency.");
        }

        double overpressurePercent = (p1AbsPa - pSetAbsPa) / pSetAbsPa * 100.0;
        double allowedOverpressurePercent = isHgT
            ? (input.UseOperatingPressureBasis ? input.AllowedOverpressurePercentInput : overpressurePercent)
            : GetAllowedOverpressurePercent(input.ReliefScenario);
        if (!isHgT && overpressurePercent > allowedOverpressurePercent + 1e-9)
        {
            string scenarioEn = GetScenarioLabelEn(input.ReliefScenario);
            string scenarioZh = GetScenarioLabelZh(input.ReliefScenario);
            warnings.Add(
                $"API limit exceeded ({scenarioEn}): overpressure {overpressurePercent:F2}% > allowed {allowedOverpressurePercent:F2}%. | 超过API限制（{scenarioZh}）：超压 {overpressurePercent:F2}% > 允许值 {allowedOverpressurePercent:F2}%。");
        }

        if (isHgT)
        {
            ApplyHgTApplicabilityWarnings(input, standardProfile, pSetAbsPa, warnings);
        }
        else if ((input.FluidType == FluidType.TwoPhaseEquilibrium || input.FluidType == FluidType.TwoPhaseSubcooled) &&
            input.ReliefScenario == ReliefScenario.ThermalExpansion)
        {
            warnings.Add("API 521 thermal-expansion cases are normally sized as liquid service; use the two-phase branch only when flashing at the relieving device is expected. | API 521 热膨胀工况通常按液体泄放处理，只有在预计阀内会发生闪蒸时才应采用两相分支。");
        }

        double temperatureK = input.TemperatureC + 273.15;
        double steamZMin = GetCoefficient(standardProfile, "STEAM_Z_MIN", 0.85);
        double steamZMax = GetCoefficient(standardProfile, "STEAM_Z_MAX", 1.03);

        ResolveCorrectionCoefficients(
            standardProfile,
            input,
            audits,
            warnings,
            out double kd,
            out double kb,
            out double kc);

        double effectiveFactor = kd * kb * kc;

        double valveMassFlux;
        double pressureRatio;
        double criticalRatio;
        bool isCritical;
        double specificGasConstant;
        string equationBranch;
        double twoPhaseOmega = 0.0;
        double twoPhaseSaturationPressureAbsPa = 0.0;
        double twoPhaseSaturationPressureRatio = 0.0;
        double twoPhaseTransitionSaturationPressureRatio = 0.0;
        double twoPhaseInletSpecificVolumeM3PerKg = 0.0;
        double twoPhaseReferenceSpecificVolumeM3PerKg = 0.0;
        double twoPhaseReferenceDensityKgPerM3 = 0.0;
        double sourceMassFluxKgPerM2S = 0.0;

        double compressibleK = double.NaN;
        double compressibleZ = double.NaN;
        double compressibleMolecularWeight = double.NaN;

        if (input.FluidType == FluidType.TwoPhaseEquilibrium)
        {
            audits.Add(new ParameterAudit
            {
                Name = "TwoPhaseInletSpecificVolume",
                Value = input.TwoPhaseInletSpecificVolumeM3PerKg,
                Unit = "m3/kg",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "TwoPhaseSpecificVolumeAt0.9P1",
                Value = input.TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg,
                Unit = "m3/kg",
                Source = ParameterSource.Manual
            });

            TwoPhaseOmegaResult twoPhase = _twoPhaseOmegaCalculator.ComputeGeneral(
                p1AbsPa,
                p2AbsPa,
                input.TwoPhaseInletSpecificVolumeM3PerKg,
                input.TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg);

            valveMassFlux = twoPhase.MassFluxKgPerM2S * effectiveFactor;
            pressureRatio = twoPhase.PressureRatio;
            criticalRatio = twoPhase.CriticalPressureRatio;
            isCritical = twoPhase.IsCritical;
            specificGasConstant = 0.0;
            equationBranch = twoPhase.EquationBranch;
            twoPhaseOmega = twoPhase.Omega;
            twoPhaseSaturationPressureAbsPa = twoPhase.SaturationPressureAbsPa;
            twoPhaseSaturationPressureRatio = twoPhase.SaturationPressureRatio;
            twoPhaseTransitionSaturationPressureRatio = twoPhase.TransitionSaturationPressureRatio;
            twoPhaseInletSpecificVolumeM3PerKg = twoPhase.InletSpecificVolumeM3PerKg;
            twoPhaseReferenceSpecificVolumeM3PerKg = twoPhase.ReferenceSpecificVolumeM3PerKg;
            twoPhaseReferenceDensityKgPerM3 = twoPhase.ReferenceDensityKgPerM3;

            warnings.Add("API 520 Part I Annex C C.2.2 Omega Method used for two-phase equilibrium/flashing sizing. User-supplied flash-property points are required. | 两相平衡/闪蒸工况按 API 520 Part I Annex C C.2.2 Omega 方法计算，需用户提供闪蒸物性点。");
            warnings.Add("API 520 notes that PRVs do not have certified capacities for two-phase flow; review with process and valve vendor data before issue. | API 520 指出安全阀目前没有两相流认证排量，出具结果前应结合工艺和厂家数据复核。");
        }
        else if (input.FluidType == FluidType.TwoPhaseSubcooled)
        {
            audits.Add(new ParameterAudit
            {
                Name = "LiquidDensity",
                Value = input.LiquidDensityKgPerM3,
                Unit = "kg/m3",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "TwoPhaseDensityAt0.9Ps",
                Value = input.TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3,
                Unit = "kg/m3",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "TwoPhaseSaturationPressureAbsolute",
                Value = input.TwoPhaseSaturationPressureAbsolute,
                Unit = input.PressureUnit.ToString(),
                Source = ParameterSource.Manual
            });

            double saturationPressureAbsPa = UnitConverter.ToAbsolutePressurePa(
                input.TwoPhaseSaturationPressureAbsolute,
                input.PressureUnit,
                PressureInputMode.Absolute,
                input.AtmosphericPressure);

            TwoPhaseOmegaResult twoPhase = _twoPhaseOmegaCalculator.ComputeSubcooled(
                p1AbsPa,
                p2AbsPa,
                input.LiquidDensityKgPerM3,
                input.TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3,
                saturationPressureAbsPa);

            valveMassFlux = twoPhase.MassFluxKgPerM2S * effectiveFactor;
            pressureRatio = twoPhase.PressureRatio;
            criticalRatio = twoPhase.CriticalPressureRatio;
            isCritical = twoPhase.IsCritical;
            specificGasConstant = 0.0;
            equationBranch = twoPhase.EquationBranch;
            twoPhaseOmega = twoPhase.Omega;
            twoPhaseSaturationPressureAbsPa = twoPhase.SaturationPressureAbsPa;
            twoPhaseSaturationPressureRatio = twoPhase.SaturationPressureRatio;
            twoPhaseTransitionSaturationPressureRatio = twoPhase.TransitionSaturationPressureRatio;
            twoPhaseInletSpecificVolumeM3PerKg = twoPhase.InletSpecificVolumeM3PerKg;
            twoPhaseReferenceSpecificVolumeM3PerKg = twoPhase.ReferenceSpecificVolumeM3PerKg;
            twoPhaseReferenceDensityKgPerM3 = twoPhase.ReferenceDensityKgPerM3;

            warnings.Add("API 520 Part I Annex C C.2.3 Omega Method used for subcooled-liquid flashing sizing. Saturation pressure must be entered as an absolute pressure in the selected unit. | 亚冷液闪蒸工况按 API 520 Part I Annex C C.2.3 Omega 方法计算，饱和压力 Ps 需按所选单位输入绝压。");
            warnings.Add("API 520 notes that PRVs do not have certified capacities for two-phase flow; review with process and valve vendor data before issue. | API 520 指出安全阀目前没有两相流认证排量，出具结果前应结合工艺和厂家数据复核。");
        }
        else if (input.FluidType == FluidType.Liquid)
        {
            double density = input.LiquidDensityKgPerM3;
            audits.Add(new ParameterAudit
            {
                Name = "LiquidDensity",
                Value = density,
                Unit = "kg/m3",
                Source = ParameterSource.Manual
            });

            double deltaP = p1AbsPa - p2AbsPa;
            if (deltaP <= 0)
            {
                deltaP = Math.Max(1.0, p1AbsPa * 0.05);
                warnings.Add("Liquid branch adjusted to a minimum positive pressure drop for calculation stability.");
            }

            double baseMassFlux = Math.Sqrt(2.0 * density * deltaP);
            valveMassFlux = effectiveFactor * baseMassFlux;
            pressureRatio = p2AbsPa / p1AbsPa;
            criticalRatio = 0.0;
            isCritical = false;
            specificGasConstant = 0.0;
            equationBranch = "LiquidIncompressible";
        }
        else
        {
            ResolveFluidProperties(
                standardProfile,
                input,
                p1AbsPa,
                temperatureK,
                steamZMin,
                steamZMax,
                warnings,
                audits,
                out double k,
                out double z,
                out double molecularWeight);

            compressibleK = k;
            compressibleZ = z;
            compressibleMolecularWeight = molecularWeight;

            criticalRatio = Math.Pow(2.0 / (k + 1.0), k / (k - 1.0));
            pressureRatio = p2AbsPa / p1AbsPa;
            isCritical = pressureRatio <= criticalRatio;

            double universalR = GetCoefficient(standardProfile, "R_UNIVERSAL", DefaultUniversalGasConstant);
            specificGasConstant = universalR / (molecularWeight / 1000.0);

            double baseMassFlux = isCritical
                ? ComputeCriticalMassFlux(p1AbsPa, temperatureK, k, z, specificGasConstant)
                : ComputeSubcriticalMassFlux(p1AbsPa, p2AbsPa, temperatureK, k, z, specificGasConstant);

            valveMassFlux = baseMassFlux * effectiveFactor;
            equationBranch = input.FluidType == FluidType.Steam
                ? (isCritical ? "SteamCritical" : "SteamSubcritical")
                : (isCritical ? "GasCritical" : "GasSubcritical");

            if (!isCritical)
            {
                warnings.Add("Subcritical flow branch used; validate back pressure assumptions.");
            }
        }

        if (valveMassFlux <= 0)
        {
            throw new InvalidOperationException("Computed valve mass flux is not positive; check coefficients and input conditions.");
        }

        double thermalExpansionVolumeFlowM3PerHour = 0.0;
        double thermalExpansionCalculatedLoadKgPerHour = 0.0;
        double heatInputKw = 0.0;
        double tubeBreakDischargeAreaM2 = 0.0;
        double reliefLoadKgPerSecond;

        if (input.ReliefScenario == ReliefScenario.TubeRupture)
        {
            double highSidePressureAbsPa = UnitConverter.ToAbsolutePressurePa(
                input.HighSidePressure,
                input.PressureUnit,
                input.PressureInputMode,
                input.AtmosphericPressure);
            double highSideTemperatureK = input.HighSideTemperatureC + 273.15;

            audits.Add(new ParameterAudit
            {
                Name = "HighSidePressure",
                Value = input.HighSidePressure,
                Unit = input.PressureUnit.ToString(),
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "HighSideTemperature",
                Value = input.HighSideTemperatureC,
                Unit = "C",
                Source = ParameterSource.Manual
            });

            audits.Add(new ParameterAudit
            {
                Name = "TubeInnerDiameter",
                Value = input.TubeInnerDiameterMm,
                Unit = "mm",
                Source = ParameterSource.Manual
            });

            if (isHgT && input.FluidType == FluidType.Liquid)
            {
                double deltaPMpa = Math.Max((highSidePressureAbsPa - p1AbsPa) / 1_000_000.0, 1e-6);
                double tubeRuptureCalculatedLoadKgPerHour = 5.6
                    * input.TubeInnerDiameterMm
                    * input.TubeInnerDiameterMm
                    * Math.Sqrt(input.LiquidDensityKgPerM3 * deltaPMpa);

                double cappedTubeRuptureLoadKgPerHour = tubeRuptureCalculatedLoadKgPerHour;
                if (input.TubeRuptureHighSideNormalFlowKgPerHour > 0)
                {
                    cappedTubeRuptureLoadKgPerHour = Math.Min(
                        tubeRuptureCalculatedLoadKgPerHour,
                        input.TubeRuptureHighSideNormalFlowKgPerHour);
                }
                else
                {
                    warnings.Add("HG/T 20570.2 clause 7.0.8.2 requires comparison with the high-side normal flow. No normal-flow cap was entered, so the formula result is used directly. | HG/T 20570.2 7.0.8.2 要求与高压侧正常流量比较；当前未输入该上限，因此直接采用公式计算值。");
                }

                reliefLoadKgPerSecond = cappedTubeRuptureLoadKgPerHour / 3600.0;
                sourceMassFluxKgPerM2S = 0.0;

                audits.Add(new ParameterAudit
                {
                    Name = "TubeRupturePressureDifference",
                    Value = deltaPMpa,
                    Unit = "MPa",
                    Source = ParameterSource.Formula
                });
                audits.Add(new ParameterAudit
                {
                    Name = "TubeRuptureCalculatedLoad",
                    Value = tubeRuptureCalculatedLoadKgPerHour,
                    Unit = "kg/h",
                    Source = ParameterSource.Formula
                });
                if (input.TubeRuptureHighSideNormalFlowKgPerHour > 0)
                {
                    audits.Add(new ParameterAudit
                    {
                        Name = "TubeRuptureHighSideNormalFlow",
                        Value = input.TubeRuptureHighSideNormalFlowKgPerHour,
                        Unit = "kg/h",
                        Source = ParameterSource.Manual
                    });
                }

                warnings.Add("HG/T 20570.2 clause 7.0.8 used for tube-rupture relief load: W = 5.6 × d² × sqrt(Gl × ΔP), with the applied load limited by the high-side normal flow when provided. | 换热管破裂泄放量按 HG/T 20570.2 7.0.8 计算：W = 5.6 × d² × sqrt(Gl × ΔP)，并在输入高压侧正常流量时取两者较小值。");
            }
            else
            {
                double tubeDiameterM = input.TubeInnerDiameterMm / 1000.0;
                tubeBreakDischargeAreaM2 = 2.0 * Math.PI * tubeDiameterM * tubeDiameterM / 4.0;

                double tubeBreakMassFlux = ComputeTubeBreakMassFlux(
                    input,
                    highSidePressureAbsPa,
                p2AbsPa,
                atmosphericPressurePa,
                highSideTemperatureK,
                standardProfile,
                    steamZMin,
                    steamZMax,
                    compressibleK,
                    compressibleZ,
                    compressibleMolecularWeight,
                    warnings,
                    audits);

                reliefLoadKgPerSecond = tubeBreakMassFlux * tubeBreakDischargeAreaM2;
                sourceMassFluxKgPerM2S = tubeBreakMassFlux;

                audits.Add(new ParameterAudit
                {
                    Name = "TubeBreakDischargeArea",
                    Value = tubeBreakDischargeAreaM2,
                    Unit = "m2",
                    Source = ParameterSource.Formula
                });
                audits.Add(new ParameterAudit
                {
                    Name = "TubeBreakMassFlux",
                    Value = tubeBreakMassFlux,
                    Unit = "kg/m2/s",
                    Source = ParameterSource.Formula
                });

                if (isHgT)
                {
                    warnings.Add("HG/T 20570.2 clause 7.0.8.3 is explicitly applicable to liquid high-side service. The current tube-rupture case is not single-phase liquid, so the existing high-side property method is retained. | HG/T 20570.2 7.0.8.3 明确适用于高压侧为液体的情况；当前换热管破裂工况并非单相液体，因此暂保留现有高压侧物性法。");
                }
                else
                {
                    warnings.Add("Tube rupture load is auto-derived from guillotine-break area (A = 2 × pi × di^2 / 4) using high-pressure-side fluid properties. | 换热管破裂负荷已按断头台完全断裂面积（A = 2 × π × di² / 4）并采用高压侧流体特性自动计算。");
                }
            }
        }
        else if (input.ReliefScenario == ReliefScenario.Fire)
        {
            heatInputKw = ComputeFireHeatInputKw(input);
            reliefLoadKgPerSecond = heatInputKw / input.VaporizationLatentHeatKjPerKg;

            audits.Add(new ParameterAudit
            {
                Name = "FireWettedArea",
                Value = input.FireWettedAreaM2,
                Unit = "m2",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "FireEnvironmentalFactor",
                Value = input.FireEnvironmentalFactorF,
                Unit = "-",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "FireConstant",
                Value = input.FireConstantC,
                Unit = "-",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "VaporizationLatentHeat",
                Value = input.VaporizationLatentHeatKjPerKg,
                Unit = "kJ/kg",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "FireHeatInput",
                Value = heatInputKw,
                Unit = "kW",
                Source = ParameterSource.Formula
            });
            warnings.Add(isHgT
                ? "Fire relief load uses the current wetted-area heat-input form. Under HG/T 20570.2, verify that Aw follows clause 7.0.10 and uses the 7.5 m wetted-height limit. | 火灾泄放量当前沿用润湿面积热输入形式；按 HG/T 20570.2 计算时，请确认 Aw 已符合 7.0.10 条并采用 7.5 m 润湿高度上限。"
                : "Fire case relief load is auto-derived by API 521-style wetted-surface heat input formula (q = C × F × Aw^0.82, W = q × 3600 / lambda). | 火灾工况泄放量已按API 521润湿面积经验式自动计算（q = C × F × Aw^0.82，W = q × 3600 / λ）。");
        }
        else if (input.ReliefScenario == ReliefScenario.ThermalExpansion)
        {
            if (HasThermalExpansionInputs(input))
            {
                ThermalExpansionResult thermalExpansion = _thermalExpansionCalculator.Calculate(
                    new ThermalExpansionInput
                    {
                        VolumetricExpansionCoefficientPerC = input.ThermalExpansionCoefficientPerC,
                        HeatInputKjPerHour = input.ThermalHeatInputKjPerHour,
                        LiquidDensityKgPerM3 = input.LiquidDensityKgPerM3,
                        SpecificHeatKjPerKgC = input.ThermalSpecificHeatKjPerKgC
                    });

                thermalExpansionVolumeFlowM3PerHour = thermalExpansion.VolumeReliefFlowM3PerHour;
                thermalExpansionCalculatedLoadKgPerHour = thermalExpansion.MassReliefLoadKgPerHour;

                audits.Add(new ParameterAudit
                {
                    Name = "ThermalExpansionCoefficient",
                    Value = input.ThermalExpansionCoefficientPerC,
                    Unit = "1/C",
                    Source = ParameterSource.Manual
                });
                audits.Add(new ParameterAudit
                {
                    Name = "ThermalHeatInput",
                    Value = input.ThermalHeatInputKjPerHour,
                    Unit = "kJ/h",
                    Source = ParameterSource.Manual
                });
                audits.Add(new ParameterAudit
                {
                    Name = "ThermalSpecificHeat",
                    Value = input.ThermalSpecificHeatKjPerKgC,
                    Unit = "kJ/(kg.C)",
                    Source = ParameterSource.Manual
                });
                audits.Add(new ParameterAudit
                {
                    Name = "ThermalExpansionVolumeFlow",
                    Value = thermalExpansionVolumeFlowM3PerHour,
                    Unit = "m3/h",
                    Source = ParameterSource.Formula
                });
                audits.Add(new ParameterAudit
                {
                    Name = "ThermalExpansionCalculatedLoad",
                    Value = thermalExpansionCalculatedLoadKgPerHour,
                    Unit = "kg/h",
                    Source = ParameterSource.Formula
                });

                warnings.Add(isHgT
                    ? "Thermal-expansion popup basis uses HG/T 20570.2 clause 7.0.1 / 7.0.6 blocked-liquid relation: V = B × H / (rho × Cp). The actual sizing load still follows the Relief Load field so manual overrides remain possible. | 热膨胀弹窗按 HG/T 20570.2 7.0.1 / 7.0.6 的受阻液体关系式 V = B × H / (ρ × Cp) 计算；实际选型仍采用“泄放量”输入框中的值，便于保留人工修正。"
                    : "Thermal-expansion popup uses the blocked-liquid heat-input relation V = B × H / (rho × Cp) as an engineering estimate. The actual sizing load still follows the Relief Load field so manual overrides remain possible. | 热膨胀弹窗按受阻液体热输入关系式 V = B × H / (ρ × Cp) 进行工程估算；实际选型仍采用“泄放量”输入框中的值，便于保留人工修正。");

                if (input.ReliefLoadKgPerHour > 0 &&
                    Math.Abs(input.ReliefLoadKgPerHour - thermalExpansionCalculatedLoadKgPerHour)
                    > Math.Max(1.0, thermalExpansionCalculatedLoadKgPerHour * 0.01))
                {
                    warnings.Add(
                        $"Thermal-expansion popup result = {thermalExpansionCalculatedLoadKgPerHour:F3} kg/h, while the active relief-load input = {input.ReliefLoadKgPerHour:F3} kg/h. Confirm whether the load was intentionally adjusted. | 热膨胀弹窗计算结果为 {thermalExpansionCalculatedLoadKgPerHour:F3} kg/h，而当前参与计算的泄放量输入为 {input.ReliefLoadKgPerHour:F3} kg/h，请确认是否进行了人工修正。");
                }
            }

            double appliedThermalLoadKgPerHour = input.ReliefLoadKgPerHour > 0
                ? input.ReliefLoadKgPerHour
                : thermalExpansionCalculatedLoadKgPerHour;
            reliefLoadKgPerSecond = appliedThermalLoadKgPerHour / 3600.0;
        }
        else
        {
            reliefLoadKgPerSecond = input.ReliefLoadKgPerHour / 3600.0;
        }

        if (reliefLoadKgPerSecond <= 0)
        {
            throw new InvalidOperationException("Computed relief load is not positive; check scenario settings and required parameters.");
        }

        double requiredAreaM2 = reliefLoadKgPerSecond / valveMassFlux;
        double requiredAreaMm2 = requiredAreaM2 * 1_000_000.0;

        if (isHgT)
        {
            if (input.FluidType == FluidType.Gas || input.FluidType == FluidType.Steam)
            {
                double hgtAreaMm2 = ComputeHgTGasOrSteamAreaMm2(
                    input,
                    reliefLoadKgPerSecond,
                    p1AbsPa,
                    p2AbsPa,
                    temperatureK,
                    compressibleK,
                    compressibleZ,
                    compressibleMolecularWeight,
                    kd,
                    kb,
                    out string hgtBranch,
                    out double hgtSubcriticalKf,
                    out bool usedEstimatedKf);

                requiredAreaMm2 = hgtAreaMm2;
                requiredAreaM2 = requiredAreaMm2 / 1_000_000.0;
                valveMassFlux = reliefLoadKgPerSecond / requiredAreaM2;
                equationBranch = hgtBranch;

                audits.Add(new ParameterAudit
                {
                    Name = "StandardBasis",
                    Value = input.StandardBasis == CalculationStandardBasis.HgT20570_2 ? 1.0 : 0.0,
                    Unit = "-",
                    Source = ParameterSource.Preset
                });

                if (hgtSubcriticalKf > 0)
                {
                    audits.Add(new ParameterAudit
                    {
                        Name = "HgTKf",
                        Value = hgtSubcriticalKf,
                        Unit = "-",
                        Source = usedEstimatedKf ? ParameterSource.Formula : ParameterSource.Preset
                    });
                }

                if (usedEstimatedKf)
                {
                    warnings.Add("HG/T 20570.2 subcritical gas/steam sizing uses Kf from Fig. 16.0.7. This version estimates Kf from the current compressible-flow relationship for continuity. | HG/T 20570.2 亚临界气体/蒸汽计算中的 Kf 本应由图16.0.7确定，当前版本为保持连续性按现有可压缩流关系估算。");
                }
            }
            else if (input.FluidType == FluidType.Liquid)
            {
                warnings.Add("HG/T 20570.2 liquid-area equations are not fully chart-automated in this version; liquid sizing currently falls back to the existing incompressible branch. | 当前版本尚未完整自动化 HG/T 20570.2 液体面积图表法，液体面积暂按现有不可压缩分支回退计算。");
            }
            else
            {
                warnings.Add("HG/T 20570.2 two-phase clauses are advisory in this version; two-phase sizing currently falls back to the existing Omega-based branch for continuity. | 当前版本尚未完整自动化 HG/T 20570.2 两相图算法，两相面积暂按现有 Omega 分支回退计算。");
            }
        }

        OrificeRecommendation orifice = _orificeSelector.Recommend(requiredAreaMm2);
        if (!isHgT && orifice.WasUpsizedForMargin && !string.IsNullOrWhiteSpace(orifice.DirectAreaQualifiedLetter))
        {
            double directQualifiedAreaMm2 = orifice.CandidateNeighbors
                .FirstOrDefault(x => x.Letter == orifice.DirectAreaQualifiedLetter)?.AreaMm2
                ?? _orificeSelector.GetAll().First(x => x.Letter == orifice.DirectAreaQualifiedLetter).AreaMm2;
            double directAllowedAreaMm2 = directQualifiedAreaMm2 * (orifice.MaximumRecommendedUtilizationPercent / 100.0);

            warnings.Add(
                $"API margin rule applied: calculated area {requiredAreaMm2:F2} mm2 exceeds {orifice.MaximumRecommendedUtilizationPercent:F0}% of orifice {orifice.DirectAreaQualifiedLetter} ({directAllowedAreaMm2:F2} mm2 allowed from {directQualifiedAreaMm2:F2} mm2), so the recommendation was increased to {orifice.Selected.Letter}. | 已应用 API 选型裕量规则：计算面积 {requiredAreaMm2:F2} mm2 超过孔口 {orifice.DirectAreaQualifiedLetter} 的 {orifice.MaximumRecommendedUtilizationPercent:F0}% 限值（{directQualifiedAreaMm2:F2} mm2 的允许值为 {directAllowedAreaMm2:F2} mm2），因此推荐结果已升档为 {orifice.Selected.Letter}。");
        }

        if (!isHgT && orifice.IsCapacityExceededByLargestOrifice)
        {
            warnings.Add(
                $"API orifice range exceeded under the {orifice.MaximumRecommendedUtilizationPercent:F0}% margin rule; the largest standard orifice {orifice.Selected.Letter} still provides only {orifice.Selected.AreaMm2 * (orifice.MaximumRecommendedUtilizationPercent / 100.0):F2} mm2 allowable area. Consider parallel valves or custom design. | 按 {orifice.MaximumRecommendedUtilizationPercent:F0}% 选型裕量规则核算时，API 标准最大孔口 {orifice.Selected.Letter} 的允许面积仍仅为 {orifice.Selected.AreaMm2 * (orifice.MaximumRecommendedUtilizationPercent / 100.0):F2} mm2，建议考虑并联阀或定制设计。");
        }

        TrimMaterialRecommendation trimMaterialRecommendation = ApiTrimMaterialRecommender.Recommend(input);
        if (ShouldPromoteTrimReviewToWarning(input))
        {
            foreach (string note in trimMaterialRecommendation.ReviewNotes)
            {
                warnings.Add($"Trim material review: {note}");
            }
        }

        return new CalculationResult
        {
            CalculatedAt = DateTimeOffset.Now,
            StandardVersion = standardProfile?.Name ?? CalculationStandardCatalog.GetDisplayName(input.StandardBasis),
            RequiredAreaMm2 = requiredAreaMm2,
            OrificeRecommendation = orifice,
            TrimMaterialRecommendation = trimMaterialRecommendation,
            Intermediate = new IntermediateValues
            {
                SetPressureValue = setPressureInput,
                RelievingPressureValue = relievingPressureInput,
                ReseatPressureValue = reseatPressureInput,
                BlowdownPercent = blowdownPercent,
                SetPressureAbsPa = pSetAbsPa,
                RelievingPressureAbsPa = p1AbsPa,
                ReseatPressureAbsPa = pReseatAbsPa,
            BackPressureAbsPa = p2AbsPa,
            AtmosphericPressureAbsPa = atmosphericPressurePa,
            TemperatureK = temperatureK,
                PressureRatioP2OverP1 = pressureRatio,
                CriticalPressureRatio = criticalRatio,
                IsCriticalFlow = isCritical,
                OverpressurePercent = overpressurePercent,
                AllowedOverpressurePercent = allowedOverpressurePercent,
                SpecificGasConstant = specificGasConstant,
                EffectiveDischargeFactor = effectiveFactor,
                MassFluxKgPerM2S = valveMassFlux,
                ReliefLoadKgPerHourUsed = reliefLoadKgPerSecond * 3600.0,
                ThermalExpansionVolumeFlowM3PerHour = thermalExpansionVolumeFlowM3PerHour,
                ThermalExpansionCalculatedLoadKgPerHour = thermalExpansionCalculatedLoadKgPerHour,
                HeatInputKw = heatInputKw,
                TubeBreakDischargeAreaM2 = tubeBreakDischargeAreaM2,
                RequiredAreaM2 = requiredAreaM2,
                TwoPhaseOmega = twoPhaseOmega,
                TwoPhaseSaturationPressureAbsPa = twoPhaseSaturationPressureAbsPa,
                TwoPhaseSaturationPressureRatio = twoPhaseSaturationPressureRatio,
                TwoPhaseTransitionSaturationPressureRatio = twoPhaseTransitionSaturationPressureRatio,
                TwoPhaseInletSpecificVolumeM3PerKg = twoPhaseInletSpecificVolumeM3PerKg,
                TwoPhaseReferenceSpecificVolumeM3PerKg = twoPhaseReferenceSpecificVolumeM3PerKg,
                TwoPhaseReferenceDensityKgPerM3 = twoPhaseReferenceDensityKgPerM3,
                SourceMassFluxKgPerM2S = sourceMassFluxKgPerM2S,
                EquationBranch = equationBranch
            },
            ParameterAudits = audits,
            Warnings = warnings
        };
    }

    private double ComputeTubeBreakMassFlux(
        CalculationInput input,
        double highSidePressureAbsPa,
        double downstreamPressureAbsPa,
        double atmosphericPressurePa,
        double highSideTemperatureK,
        StandardProfile? standardProfile,
        double steamZMin,
        double steamZMax,
        double compressibleK,
        double compressibleZ,
        double compressibleMolecularWeight,
        ICollection<string> warnings,
        ICollection<ParameterAudit> audits)
    {
        if (downstreamPressureAbsPa <= 0)
        {
            downstreamPressureAbsPa = atmosphericPressurePa;
        }

        if (downstreamPressureAbsPa >= highSidePressureAbsPa)
        {
            downstreamPressureAbsPa = highSidePressureAbsPa * 0.9;
            warnings.Add("Tube rupture branch reduced downstream pressure to 90% of high-side pressure for physical consistency.");
        }

        if (input.FluidType == FluidType.TwoPhaseEquilibrium)
        {
            audits.Add(new ParameterAudit
            {
                Name = "HighSideTwoPhaseInletSpecificVolume",
                Value = input.HighSideTwoPhaseInletSpecificVolumeM3PerKg,
                Unit = "m3/kg",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "HighSideTwoPhaseSpecificVolumeAt0.9P1",
                Value = input.HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg,
                Unit = "m3/kg",
                Source = ParameterSource.Manual
            });

            return _twoPhaseOmegaCalculator.ComputeGeneral(
                highSidePressureAbsPa,
                downstreamPressureAbsPa,
                input.HighSideTwoPhaseInletSpecificVolumeM3PerKg,
                input.HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg).MassFluxKgPerM2S;
        }

        if (input.FluidType == FluidType.TwoPhaseSubcooled)
        {
            audits.Add(new ParameterAudit
            {
                Name = "HighSideLiquidDensity",
                Value = input.HighSideLiquidDensityKgPerM3,
                Unit = "kg/m3",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "HighSideTwoPhaseDensityAt0.9Ps",
                Value = input.HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3,
                Unit = "kg/m3",
                Source = ParameterSource.Manual
            });
            audits.Add(new ParameterAudit
            {
                Name = "HighSideTwoPhaseSaturationPressureAbsolute",
                Value = input.HighSideTwoPhaseSaturationPressureAbsolute,
                Unit = input.PressureUnit.ToString(),
                Source = ParameterSource.Manual
            });

            double highSideSaturationPressureAbsPa = UnitConverter.ToAbsolutePressurePa(
                input.HighSideTwoPhaseSaturationPressureAbsolute,
                input.PressureUnit,
                PressureInputMode.Absolute,
                input.AtmosphericPressure);

            return _twoPhaseOmegaCalculator.ComputeSubcooled(
                highSidePressureAbsPa,
                downstreamPressureAbsPa,
                input.HighSideLiquidDensityKgPerM3,
                input.HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3,
                highSideSaturationPressureAbsPa).MassFluxKgPerM2S;
        }

        if (input.FluidType == FluidType.Liquid)
        {
            double deltaP = highSidePressureAbsPa - downstreamPressureAbsPa;
            if (deltaP <= 0)
            {
                deltaP = Math.Max(1.0, highSidePressureAbsPa * 0.05);
            }

            return Math.Sqrt(2.0 * input.LiquidDensityKgPerM3 * deltaP);
        }

        double k;
        double z;
        double molecularWeight;

        if (input.FluidType == FluidType.Steam)
        {
            molecularWeight = 18.01528;
            k = EstimateSteamIsentropicExponent(highSideTemperatureK, highSidePressureAbsPa);
            z = EstimateSteamCompressibility(highSideTemperatureK, highSidePressureAbsPa, steamZMin, steamZMax);
        }
        else
        {
            k = compressibleK;
            z = compressibleZ;
            molecularWeight = compressibleMolecularWeight;
        }

        double criticalRatio = Math.Pow(2.0 / (k + 1.0), k / (k - 1.0));
        double ratio = downstreamPressureAbsPa / highSidePressureAbsPa;
        double universalR = GetCoefficient(standardProfile, "R_UNIVERSAL", DefaultUniversalGasConstant);
        double specificGasConstant = universalR / (molecularWeight / 1000.0);

        return ratio <= criticalRatio
            ? ComputeCriticalMassFlux(highSidePressureAbsPa, highSideTemperatureK, k, z, specificGasConstant)
            : ComputeSubcriticalMassFlux(highSidePressureAbsPa, downstreamPressureAbsPa, highSideTemperatureK, k, z, specificGasConstant);
    }

    private static double ComputeFireHeatInputKw(CalculationInput input)
    {
        return input.FireConstantC
               * input.FireEnvironmentalFactorF
               * Math.Pow(input.FireWettedAreaM2, 0.82);
    }

    private void ResolveCorrectionCoefficients(
        StandardProfile? standardProfile,
        CalculationInput input,
        ICollection<ParameterAudit> audits,
        ICollection<string> warnings,
        out double kd,
        out double kb,
        out double kc)
    {
        double kdDefault = input.FluidType switch
        {
            FluidType.Steam => GetCoefficient(standardProfile, "KD_DEFAULT_STEAM", 0.975),
            FluidType.TwoPhaseEquilibrium => GetCoefficient(standardProfile, "KD_DEFAULT_TWO_PHASE", 0.85),
            FluidType.TwoPhaseSubcooled => GetCoefficient(standardProfile, "KD_DEFAULT_TWO_PHASE_SUBCOOLED", 0.65),
            _ => GetCoefficient(standardProfile, "KD_DEFAULT_GAS", 0.975)
        };
        double kbDefault = GetCoefficient(standardProfile, "KB_DEFAULT", 1.0);
        double kcDefault = GetCoefficient(standardProfile, "KC_DEFAULT", 1.0);
        bool isBellowsBalanced = input.ValveConfiguration == ValveConfiguration.BalancedBellows;
        bool isPilotOperated = input.ValveConfiguration == ValveConfiguration.PilotOperated;

        if (isBellowsBalanced)
        {
            if (input.UseCustomBellowsKb)
            {
                warnings.Add("Balanced-bellows valve selected with custom Kb enabled. Confirm the custom back pressure correction against certified vendor data before issue. | 已选择波纹管平衡式安全阀，且启用了自定义 Kb。出具结果前请根据厂家认证资料核对该背压修正系数。");
            }
            else
            {
                kbDefault = 1.0;
                warnings.Add("Balanced-bellows valve selected: Kb defaults to 1.0 in this version. Verify allowable built-up back pressure and bellows vent arrangement against vendor data. | 已选择波纹管平衡式安全阀：当前版本默认 Kb=1.0，请结合厂家资料核对允许背压和波纹管排放方式。");
            }
        }

        if (isPilotOperated)
        {
            kbDefault = 1.0;
            warnings.Add("Pilot-operated valve selected: this version keeps the selected phase-sizing branch and defaults Kb to 1.0. Verify allowable back pressure, minimum operating differential pressure, pilot vent/sense-line arrangement, and service cleanliness against vendor data before issue. | 已选择先导式安全阀：当前版本沿用所选相态分支计算，并默认 Kb=1.0。出具结果前请结合厂家资料复核允许背压、最小启闭压差、导压/放散管布置及介质清洁度。");
            string senseLabelEn = input.PilotSenseLineMode == PilotSenseLineMode.External ? "external sense line" : "internal sense";
            string senseLabelZh = input.PilotSenseLineMode == PilotSenseLineMode.External ? "外置导压" : "内置导压";
            string ventLabelEn = input.PilotVentToAtmosphere ? "atmospheric vent" : "closed vent";
            string ventLabelZh = input.PilotVentToAtmosphere ? "外排放" : "不外排放";
            warnings.Add($"Pilot-operated settings recorded: minimum operating differential {input.PilotMinimumOperatingDifferentialPercent:F2}%, {senseLabelEn}, {ventLabelEn}. | 已记录先导式设置：最小工作压差 {input.PilotMinimumOperatingDifferentialPercent:F2}%，{senseLabelZh}，{ventLabelZh}。");
            if (input.FluidType == FluidType.Liquid ||
                input.FluidType == FluidType.TwoPhaseEquilibrium ||
                input.FluidType == FluidType.TwoPhaseSubcooled)
            {
                warnings.Add("Pilot-operated valve selected for liquid or two-phase service: confirm dynamic stability, blowdown, and certified capacity with the valve vendor before issue. | 先导式安全阀用于液体或两相工况时，请在出具结果前与厂家确认动态稳定性、回座特性及认证排量。");
            }
        }

        kd = input.DischargeCoefficientKd > 0 ? input.DischargeCoefficientKd : kdDefault;
        kb = isBellowsBalanced && !input.UseCustomBellowsKb
            ? 1.0
            : (input.BackPressureCorrectionKb > 0 ? input.BackPressureCorrectionKb : kbDefault);
        kc = input.CombinationCorrectionKc > 0 ? input.CombinationCorrectionKc : kcDefault;

        AddCoefficientAudit(audits, "Kd", kd, input.DischargeCoefficientKd > 0 ? ParameterSource.Manual : ParameterSource.Preset);
        AddCoefficientAudit(
            audits,
            "Kb",
            kb,
            ((isBellowsBalanced && input.UseCustomBellowsKb) || (!isBellowsBalanced && input.BackPressureCorrectionKb > 0))
                ? ParameterSource.Manual
                : ParameterSource.Preset);
        AddCoefficientAudit(audits, "Kc", kc, input.CombinationCorrectionKc > 0 ? ParameterSource.Manual : ParameterSource.Preset);
    }

    private static void AddCoefficientAudit(
        ICollection<ParameterAudit> audits,
        string name,
        double value,
        ParameterSource source)
    {
        audits.Add(new ParameterAudit
        {
            Name = name,
            Value = value,
            Unit = "-",
            Source = source
        });
    }

    private double GetCoefficient(StandardProfile? profile, string code, double fallback)
    {
        if (profile is not null && profile.TryGet(code, out StandardCoefficient? coefficient) && coefficient is not null)
        {
            return coefficient.Value;
        }

        return fallback;
    }

    private StandardProfile? ResolveStandardProfile(CalculationInput input)
    {
        if (_standardProfileProvider is null)
        {
            return null;
        }

        string profileId = CalculationStandardCatalog.GetProfileId(input.StandardBasis);
        try
        {
            return _standardProfileProvider.GetByProfileId(profileId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Calculation standard profile '{profileId}' could not be loaded. Calculation was stopped to avoid falling back to a different standard.",
                ex);
        }
    }

    private void ApplyHgTApplicabilityWarnings(
        CalculationInput input,
        StandardProfile? standardProfile,
        double setPressureAbsPa,
        ICollection<string> warnings)
    {
        double minApplicablePressureMpa = GetCoefficient(standardProfile, "HGT_MIN_APPLICABLE_PRESSURE_MPA", 0.2);
        double maxApplicablePressureMpa = GetCoefficient(standardProfile, "HGT_MAX_APPLICABLE_PRESSURE_MPA", 100.0);
        double setPressureAbsMpa = setPressureAbsPa / 1_000_000.0;

        if (setPressureAbsMpa <= minApplicablePressureMpa || setPressureAbsMpa > maxApplicablePressureMpa)
        {
            warnings.Add(
                $"HG/T 20570.2 applicability warning: the standard is intended for pressure systems above {minApplicablePressureMpa:F1} MPa and not for ultra-high-pressure systems above {maxApplicablePressureMpa:F0} MPa. Current set pressure basis = {setPressureAbsMpa:F4} MPa(abs). | HG/T 20570.2 适用范围提示：本标准适用于压力大于 {minApplicablePressureMpa:F1} MPa 且不属于超过 {maxApplicablePressureMpa:F0} MPa 的超高压系统；当前整定压力基准为 {setPressureAbsMpa:F4} MPa(绝压)。");
        }

        if (input.ReliefScenario == ReliefScenario.Fire)
        {
            double gradeLimitM = GetCoefficient(standardProfile, "HGT_FIRE_GRADE_LIMIT_M", 7.5);
            warnings.Add(
                $"HG/T 20570.2 clause 7.0.10 uses a wetted-height limit of {gradeLimitM:F1} m for external-fire cases; verify the entered Aw and popup calculation basis. | HG/T 20570.2 7.0.10 对火灾工况采用 {gradeLimitM:F1} m 的润湿高度上限，请确认 Aw 输入和弹窗计算基准一致。");
        }

        if (input.ReliefScenario == ReliefScenario.ThermalExpansion)
        {
            warnings.Add("HG/T 20570.2 thermal-expansion cases are generally liquid-service cases; verify that the chosen phase model matches the blocked-in liquid scenario. | HG/T 20570.2 热膨胀工况通常对应液体受阻膨胀，请确认当前相态模型与实际工况一致。");
        }
    }

    private static double ComputeHgTGasOrSteamAreaMm2(
        CalculationInput input,
        double reliefLoadKgPerSecond,
        double p1AbsPa,
        double p2AbsPa,
        double temperatureK,
        double k,
        double z,
        double molecularWeight,
        double c0,
        double kb,
        out string equationBranch,
        out double hgtKf,
        out bool usedEstimatedKf)
    {
        double p1Mpa = p1AbsPa / 1_000_000.0;
        double p2Mpa = p2AbsPa / 1_000_000.0;
        double reliefLoadKgPerHour = reliefLoadKgPerSecond * 3600.0;
        double kc = input.CombinationCorrectionKc > 0 ? input.CombinationCorrectionKc : 1.0;

        double xCoefficient = double.IsFinite(k) && k > 1.0
            ? 520.0 * Math.Sqrt(k * Math.Pow(2.0 / (k + 1.0), (k + 1.0) / (k - 1.0)))
            : 315.0;

        double criticalRatio = double.IsFinite(k) && k > 1.0
            ? Math.Pow(2.0 / (k + 1.0), k / (k - 1.0))
            : 0.55;
        bool isCritical = p2AbsPa / p1AbsPa <= criticalRatio;

        if (isCritical)
        {
            usedEstimatedKf = false;
            hgtKf = 0.0;
            equationBranch = input.FluidType == FluidType.Steam ? "HgTSteamCritical" : "HgTGasCritical";
            return 13.16
                   * reliefLoadKgPerHour
                   / (Math.Max(c0, 1e-9) * Math.Max(xCoefficient, 1e-9) * Math.Max(p1Mpa, 1e-9) * Math.Max(kb, 1e-9) * Math.Max(kc, 1e-9))
                   * Math.Sqrt(Math.Max(z * temperatureK / Math.Max(molecularWeight, 1e-9), 0.0));
        }

        double specificGasConstant = DefaultUniversalGasConstant / (Math.Max(molecularWeight, 1e-9) / 1000.0);
        double exactSubcriticalMassFlux = ComputeSubcriticalMassFlux(
            p1AbsPa,
            p2AbsPa,
            temperatureK,
            k,
            z,
            specificGasConstant);
        double exactAreaMm2 = reliefLoadKgPerSecond / (exactSubcriticalMassFlux * Math.Max(c0, 1e-9) * Math.Max(kb, 1e-9) * Math.Max(kc, 1e-9)) * 1_000_000.0;
        double factor = 1.8e-3
                        * reliefLoadKgPerHour
                        * Math.Sqrt(Math.Max((z * temperatureK) / (Math.Max(molecularWeight, 1e-9) * Math.Max(p1Mpa, 1e-9) * Math.Max(p1Mpa - p2Mpa, 1e-9)), 0.0));
        hgtKf = factor / (Math.Max(c0, 1e-9) * Math.Max(kc, 1e-9) * Math.Max(exactAreaMm2, 1e-9));
        usedEstimatedKf = true;
        equationBranch = input.FluidType == FluidType.Steam ? "HgTSteamSubcritical" : "HgTGasSubcritical";

        return 1.8e-3
               * reliefLoadKgPerHour
               / (Math.Max(c0, 1e-9) * Math.Max(hgtKf, 1e-9) * Math.Max(kc, 1e-9))
               * Math.Sqrt(Math.Max((z * temperatureK) / (Math.Max(molecularWeight, 1e-9) * Math.Max(p1Mpa, 1e-9) * Math.Max(p1Mpa - p2Mpa, 1e-9)), 0.0));
    }

    private static double GetAllowedOverpressurePercent(ReliefScenario scenario)
    {
        return scenario switch
        {
            ReliefScenario.Overpressure => 10.0,
            ReliefScenario.Fire => 21.0,
            ReliefScenario.TubeRupture => 16.0,
            ReliefScenario.ThermalExpansion => 10.0,
            _ => 10.0
        };
    }

    private static double GetDefaultBlowdownPercent(FluidType fluidType)
    {
        return fluidType switch
        {
            FluidType.Steam => 4.0,
            FluidType.Liquid => 10.0,
            FluidType.TwoPhaseEquilibrium => 7.0,
            FluidType.TwoPhaseSubcooled => 10.0,
            _ => 7.0
        };
    }

    private static string GetScenarioLabelEn(ReliefScenario scenario)
    {
        return scenario switch
        {
            ReliefScenario.Overpressure => "Overpressure",
            ReliefScenario.Fire => "Fire",
            ReliefScenario.TubeRupture => "Tube Rupture",
            ReliefScenario.ThermalExpansion => "Thermal Expansion",
            _ => "Overpressure"
        };
    }

    private static string GetScenarioLabelZh(ReliefScenario scenario)
    {
        return scenario switch
        {
            ReliefScenario.Overpressure => "超压",
            ReliefScenario.Fire => "火灾",
            ReliefScenario.TubeRupture => "换热管破裂",
            ReliefScenario.ThermalExpansion => "热膨胀",
            _ => "超压"
        };
    }

    private static void Validate(CalculationInput input)
    {
        double setPressureInput = input.UseOperatingPressureBasis
            ? input.OperatingPressure
            : input.SetPressure;
        double relievingPressureInput = input.UseOperatingPressureBasis
            ? input.OperatingPressure * (1.0 + input.AllowedOverpressurePercentInput / 100.0)
            : input.RelievingPressure;

        if (input.UseOperatingPressureBasis)
        {
            if (input.OperatingPressure <= 0)
            {
                throw new ArgumentException("Operating pressure must be greater than zero.");
            }

            if (input.AllowedOverpressurePercentInput < 0)
            {
                throw new ArgumentException("Allowed overpressure percent cannot be negative.");
            }
        }

        if (input.BlowdownPercent < 0 || input.BlowdownPercent >= 100)
        {
            throw new ArgumentException("Blowdown percent must be between 0 and 100.");
        }

        if (input.ValveConfiguration == ValveConfiguration.PilotOperated &&
            input.PilotMinimumOperatingDifferentialPercent <= 0)
        {
            throw new ArgumentException("Pilot-operated minimum operating differential must be greater than zero.");
        }

        if (setPressureInput <= 0)
        {
            throw new ArgumentException("Set pressure must be greater than zero.");
        }

        if (relievingPressureInput <= 0)
        {
            throw new ArgumentException("Relieving pressure must be greater than zero.");
        }

        if (relievingPressureInput < setPressureInput)
        {
            throw new ArgumentException("Relieving pressure must be greater than or equal to set pressure.");
        }

        if (input.TemperatureC < -273.14)
        {
            throw new ArgumentException("Temperature is below absolute zero.");
        }

        ValidateGasProperties(input);

        bool hasThermalExpansionInputs = HasThermalExpansionInputs(input);

        if (input.ReliefScenario != ReliefScenario.TubeRupture &&
            input.ReliefScenario != ReliefScenario.Fire &&
            !(input.ReliefScenario == ReliefScenario.ThermalExpansion && hasThermalExpansionInputs) &&
            input.ReliefLoadKgPerHour <= 0)
        {
            throw new ArgumentException("Relief load must be greater than zero.");
        }

        if (input.ReliefScenario == ReliefScenario.TubeRupture)
        {
            if (input.TubeInnerDiameterMm <= 0)
            {
                throw new ArgumentException("Tube inner diameter must be greater than zero for tube rupture scenario.");
            }

             if (input.TubeRuptureHighSideNormalFlowKgPerHour < 0)
            {
                throw new ArgumentException("Tube-rupture high-side normal flow cannot be negative.");
            }

            if (input.HighSidePressure <= 0)
            {
                throw new ArgumentException("High-side pressure must be greater than zero for tube rupture scenario.");
            }

            if (input.HighSideTemperatureC < -273.14)
            {
                throw new ArgumentException("High-side temperature is below absolute zero.");
            }
        }

        if (input.ReliefScenario == ReliefScenario.Fire)
        {
            if (input.FireWettedAreaM2 <= 0)
            {
                throw new ArgumentException("Fire wetted area must be greater than zero.");
            }

            if (input.FireEnvironmentalFactorF <= 0)
            {
                throw new ArgumentException("Fire environmental factor must be greater than zero.");
            }

            if (input.FireConstantC <= 0)
            {
                throw new ArgumentException("Fire constant C must be greater than zero.");
            }

            if (input.VaporizationLatentHeatKjPerKg <= 0)
            {
                throw new ArgumentException("Vaporization latent heat must be greater than zero.");
            }
        }

        if (input.ReliefScenario == ReliefScenario.ThermalExpansion && hasThermalExpansionInputs)
        {
            if (input.ThermalExpansionCoefficientPerC <= 0)
            {
                throw new ArgumentException("Thermal expansion coefficient must be greater than zero.");
            }

            if (input.ThermalHeatInputKjPerHour <= 0)
            {
                throw new ArgumentException("Thermal heat input must be greater than zero.");
            }

            if (input.ThermalSpecificHeatKjPerKgC <= 0)
            {
                throw new ArgumentException("Thermal specific heat must be greater than zero.");
            }
        }

        if (input.AtmosphericPressure <= 0)
        {
            throw new ArgumentException("Atmospheric pressure must be greater than zero.");
        }

        if ((input.FluidType == FluidType.Liquid || input.FluidType == FluidType.TwoPhaseSubcooled) &&
            input.LiquidDensityKgPerM3 <= 0)
        {
            throw new ArgumentException("Liquid density must be greater than zero.");
        }

        if (input.FluidType == FluidType.TwoPhaseEquilibrium)
        {
            if (input.TwoPhaseInletSpecificVolumeM3PerKg <= 0)
            {
                throw new ArgumentException("Two-phase inlet specific volume must be greater than zero.");
            }

            if (input.TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg <= 0)
            {
                throw new ArgumentException("Two-phase reference specific volume at 0.9 P1 must be greater than zero.");
            }
        }

        if (input.FluidType == FluidType.TwoPhaseSubcooled)
        {
            if (input.TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 <= 0)
            {
                throw new ArgumentException("Two-phase reference density at 0.9 Ps must be greater than zero.");
            }

            if (input.TwoPhaseSaturationPressureAbsolute <= 0)
            {
                throw new ArgumentException("Two-phase saturation pressure must be greater than zero.");
            }

            double relievingPressureAbsPa = UnitConverter.ToAbsolutePressurePa(
                relievingPressureInput,
                input.PressureUnit,
                input.PressureInputMode,
                input.AtmosphericPressure);
            double saturationPressureAbsPa = UnitConverter.ToAbsolutePressurePa(
                input.TwoPhaseSaturationPressureAbsolute,
                input.PressureUnit,
                PressureInputMode.Absolute,
                input.AtmosphericPressure);

            if (saturationPressureAbsPa > relievingPressureAbsPa + 1e-6)
            {
                throw new ArgumentException("Two-phase saturation pressure cannot exceed the relieving pressure for the subcooled branch.");
            }
        }

        if (input.DischargeCoefficientKd < 0 ||
            input.BackPressureCorrectionKb < 0 ||
            input.CombinationCorrectionKc < 0)
        {
            throw new ArgumentException("Correction coefficients cannot be negative.");
        }

        if (input.ValveConfiguration == ValveConfiguration.BalancedBellows &&
            input.UseCustomBellowsKb &&
            input.BackPressureCorrectionKb <= 0)
        {
            throw new ArgumentException("Custom bellows Kb must be greater than zero.");
        }

        if (input.ReliefScenario == ReliefScenario.TubeRupture)
        {
            if (input.FluidType == FluidType.TwoPhaseEquilibrium)
            {
                if (input.HighSideTwoPhaseInletSpecificVolumeM3PerKg <= 0)
                {
                    throw new ArgumentException("High-side two-phase inlet specific volume must be greater than zero for tube rupture scenario.");
                }

                if (input.HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg <= 0)
                {
                    throw new ArgumentException("High-side two-phase reference specific volume at 0.9 P1 must be greater than zero for tube rupture scenario.");
                }
            }

            if (input.FluidType == FluidType.TwoPhaseSubcooled)
            {
                if (input.HighSideLiquidDensityKgPerM3 <= 0)
                {
                    throw new ArgumentException("High-side liquid density must be greater than zero for tube rupture scenario.");
                }

                if (input.HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 <= 0)
                {
                    throw new ArgumentException("High-side two-phase reference density at 0.9 Ps must be greater than zero for tube rupture scenario.");
                }

                if (input.HighSideTwoPhaseSaturationPressureAbsolute <= 0)
                {
                    throw new ArgumentException("High-side two-phase saturation pressure must be greater than zero for tube rupture scenario.");
                }

                double highSidePressureAbsPa = UnitConverter.ToAbsolutePressurePa(
                    input.HighSidePressure,
                    input.PressureUnit,
                    input.PressureInputMode,
                    input.AtmosphericPressure);
                double highSideSaturationPressureAbsPa = UnitConverter.ToAbsolutePressurePa(
                    input.HighSideTwoPhaseSaturationPressureAbsolute,
                    input.PressureUnit,
                    PressureInputMode.Absolute,
                    input.AtmosphericPressure);

                if (highSideSaturationPressureAbsPa > highSidePressureAbsPa + 1e-6)
                {
                    throw new ArgumentException("High-side two-phase saturation pressure cannot exceed the high-side pressure for tube rupture scenario.");
                }
            }
        }
    }

    private static void ValidateGasProperties(CalculationInput input)
    {
        if (input.FluidType != FluidType.Gas)
        {
            return;
        }

        double molecularWeight = input.MolecularWeight;
        double isentropicExponent = input.IsentropicExponentK;
        double compressibilityFactor = input.CompressibilityFactorZ;

        if (input.UseGasPreset &&
            GasPresetCatalog.TryGetById(input.GasPresetId, out GasPreset? preset) &&
            preset is not null)
        {
            molecularWeight = molecularWeight > 0 ? molecularWeight : preset.MolecularWeight;
            isentropicExponent = isentropicExponent > 0 ? isentropicExponent : preset.IsentropicExponent;
            compressibilityFactor = compressibilityFactor > 0 ? compressibilityFactor : preset.CompressibilityFactor;
        }

        if (molecularWeight <= 0)
        {
            throw new ArgumentException("Molecular weight must be greater than zero for gas service.");
        }

        if (isentropicExponent <= 1.0)
        {
            throw new ArgumentException("Isentropic exponent must be greater than 1.0 for gas service.");
        }

        if (compressibilityFactor <= 0)
        {
            throw new ArgumentException("Compressibility factor must be greater than zero for gas service.");
        }
    }

    private static bool HasThermalExpansionInputs(CalculationInput input)
    {
        return input.ThermalExpansionCoefficientPerC > 0
               && input.ThermalHeatInputKjPerHour > 0
               && input.ThermalSpecificHeatKjPerKgC > 0
               && input.LiquidDensityKgPerM3 > 0;
    }

    private double ResolveAtmosphericPressurePa(CalculationInput input, StandardProfile? standardProfile)
    {
        if (input.AtmosphericPressure > 0)
        {
            return UnitConverter.ToAbsolutePressurePa(
                input.AtmosphericPressure,
                input.PressureUnit,
                PressureInputMode.Absolute,
                input.AtmosphericPressure);
        }

        return GetCoefficient(standardProfile, "ATM_PRESSURE_PA", UnitConverter.AtmosphericPressurePa);
    }

    private static double ComputeCriticalMassFlux(
        double p1AbsPa,
        double temperatureK,
        double k,
        double z,
        double gasConstant)
    {
        return p1AbsPa
               * Math.Sqrt(k / (z * gasConstant * temperatureK))
               * Math.Pow(2.0 / (k + 1.0), (k + 1.0) / (2.0 * (k - 1.0)));
    }

    private static double ComputeSubcriticalMassFlux(
        double p1AbsPa,
        double p2AbsPa,
        double temperatureK,
        double k,
        double z,
        double gasConstant)
    {
        double ratio = p2AbsPa / p1AbsPa;
        double term = (2.0 * k) / (z * gasConstant * temperatureK * (k - 1.0))
                      * (Math.Pow(ratio, 2.0 / k) - Math.Pow(ratio, (k + 1.0) / k));
        if (term <= 0)
        {
            throw new InvalidOperationException("Subcritical term became non-positive; verify pressure ratio and properties.");
        }

        return p1AbsPa * Math.Sqrt(term);
    }

    private static void ResolveFluidProperties(
        StandardProfile? standardProfile,
        CalculationInput input,
        double p1AbsPa,
        double temperatureK,
        double steamZMin,
        double steamZMax,
        ICollection<string> warnings,
        ICollection<ParameterAudit> audits,
        out double k,
        out double z,
        out double molecularWeight)
    {
        if (input.FluidType == FluidType.Steam)
        {
            molecularWeight = 18.01528;
            k = EstimateSteamIsentropicExponent(temperatureK, p1AbsPa);
            z = EstimateSteamCompressibility(temperatureK, p1AbsPa, steamZMin, steamZMax);

            audits.Add(new ParameterAudit
            {
                Name = "MolecularWeight",
                Value = molecularWeight,
                Unit = "kg/kmol",
                Source = ParameterSource.AutoEstimated
            });
            audits.Add(new ParameterAudit
            {
                Name = "IsentropicExponent",
                Value = k,
                Unit = "-",
                Source = ParameterSource.AutoEstimated
            });
            audits.Add(new ParameterAudit
            {
                Name = "CompressibilityFactor",
                Value = z,
                Unit = "-",
                Source = ParameterSource.AutoEstimated
            });
            warnings.Add(input.StandardBasis == CalculationStandardBasis.HgT20570_2
                ? "HG/T 20570.2 steam properties are currently estimated with a simplified engineering correlation. Validate against plant steam-table or design-office data before issue. | 当前 HG/T 20570.2 蒸汽物性仍按简化工程相关式估算，出具结果前请结合蒸汽表或设计院数据复核。"
                : "Steam properties are estimated with a simplified engineering correlation. Validate against plant-standard data.");
            return;
        }

        if (input.UseGasPreset && GasPresetCatalog.TryGetById(input.GasPresetId, out GasPreset? preset) && preset is not null)
        {
            molecularWeight = input.MolecularWeight > 0 ? input.MolecularWeight : preset.MolecularWeight;
            k = input.IsentropicExponentK > 0 ? input.IsentropicExponentK : preset.IsentropicExponent;
            z = input.CompressibilityFactorZ > 0 ? input.CompressibilityFactorZ : preset.CompressibilityFactor;

            audits.Add(new ParameterAudit
            {
                Name = "MolecularWeight",
                Value = molecularWeight,
                Unit = "kg/kmol",
                Source = Math.Abs(molecularWeight - preset.MolecularWeight) < 1e-9
                    ? ParameterSource.Preset
                    : ParameterSource.ManualOverride
            });
            audits.Add(new ParameterAudit
            {
                Name = "IsentropicExponent",
                Value = k,
                Unit = "-",
                Source = Math.Abs(k - preset.IsentropicExponent) < 1e-9
                    ? ParameterSource.Preset
                    : ParameterSource.ManualOverride
            });
            audits.Add(new ParameterAudit
            {
                Name = "CompressibilityFactor",
                Value = z,
                Unit = "-",
                Source = Math.Abs(z - preset.CompressibilityFactor) < 1e-9
                    ? ParameterSource.Preset
                    : ParameterSource.ManualOverride
            });
            return;
        }

        molecularWeight = input.MolecularWeight;
        k = input.IsentropicExponentK;
        z = input.CompressibilityFactorZ;

        audits.Add(new ParameterAudit
        {
            Name = "MolecularWeight",
            Value = molecularWeight,
            Unit = "kg/kmol",
            Source = ParameterSource.Manual
        });
        audits.Add(new ParameterAudit
        {
            Name = "IsentropicExponent",
            Value = k,
            Unit = "-",
            Source = ParameterSource.Manual
        });
        audits.Add(new ParameterAudit
        {
            Name = "CompressibilityFactor",
            Value = z,
            Unit = "-",
            Source = ParameterSource.Manual
        });
    }

    private static double EstimateSteamIsentropicExponent(double temperatureK, double pressurePa)
    {
        double tFactor = (temperatureK - 373.15) / 400.0;
        double pFactor = pressurePa / 10_000_000.0;
        double value = 1.33 - 0.04 * tFactor - 0.02 * pFactor;
        return Math.Clamp(value, 1.08, 1.33);
    }

    private static double EstimateSteamCompressibility(double temperatureK, double pressurePa, double zMin, double zMax)
    {
        double pMpa = pressurePa / 1_000_000.0;
        double tFactor = (temperatureK - 373.15) / 500.0;
        double value = 1.0 - 0.03 * Math.Min(pMpa, 20.0) / 20.0 + 0.01 * tFactor;
        return Math.Clamp(value, zMin, zMax);
    }

    private static bool ShouldPromoteTrimReviewToWarning(CalculationInput input)
    {
        return input.MaterialServiceCondition is MaterialServiceCondition.DirtyAbrasiveTwoPhase
                or MaterialServiceCondition.SourNace
                or MaterialServiceCondition.ChlorideSeaWater
            || input.FluidType is FluidType.TwoPhaseEquilibrium or FluidType.TwoPhaseSubcooled
            || input.ReliefScenario == ReliefScenario.TubeRupture;
    }
}
