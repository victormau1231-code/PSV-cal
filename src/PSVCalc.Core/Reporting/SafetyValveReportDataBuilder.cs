using System.Globalization;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Core.Reporting;

public sealed class SafetyValveReportDataBuilder
{
    public SafetyValveReportDocument Build(ProjectRecord record, UiLanguage language)
    {
        if (record.Result is null)
        {
            throw new InvalidOperationException("Calculation result is required before exporting.");
        }

        CalculationInput input = record.Input;
        CalculationResult result = record.Result;
        IReadOnlyDictionary<string, string> dict = LocalizationCatalog.GetDictionary(language);

        List<ReportRow> inputRows = BuildInputRows(input, result, language, dict);
        List<ReportRow> resultRows = BuildResultRows(input, result, dict);
        List<ReportRow> expertRows = BuildExpertRows(input, result);
        List<ReportRow> auditRows = result.ParameterAudits
            .Select(audit => new ReportRow($"{audit.Name} ({audit.Unit})", $"{F(audit.Value)} [{audit.Source}]"))
            .ToList();

        return new SafetyValveReportDocument
        {
            AppTitle = dict["app_title"],
            ReportTitle = dict["report_title"],
            CaseName = record.CaseName,
            GeneratedAtLabel = dict["generated_at"],
            GeneratedAtValue = record.SavedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            SoftwareVersionLabel = dict["software_version"],
            SoftwareVersionValue = record.SoftwareVersion,
            StandardVersionLabel = dict["standard_version"],
            StandardVersionValue = record.StandardVersion,
            SummaryCards = BuildSummaryCards(input, result, dict),
            InputsTitle = dict["inputs"],
            ResultsTitle = dict["results"],
            WarningsTitle = dict["warnings"],
            ExpertTitle = dict["expert_title"],
            AuditTitle = $"{dict["parameter"]} / {dict["source"]}",
            NoneText = dict["none"],
            InputRows = inputRows,
            ResultRows = resultRows,
            Warnings = result.Warnings,
            ExpertRows = expertRows,
            AuditRows = auditRows
        };
    }

    private static List<ReportCard> BuildSummaryCards(
        CalculationInput input,
        CalculationResult result,
        IReadOnlyDictionary<string, string> dict)
    {
        var cards = new List<ReportCard>
        {
            new(
                dict["required_area"],
                $"{F(result.RequiredAreaMm2)} mm2",
                [new ReportRow("cm2 / in2", $"{F(result.RequiredAreaCm2)} / {F(result.RequiredAreaIn2)}")])
        };

        if (input.StandardBasis == CalculationStandardBasis.HgT20570_2)
        {
            string throatDisplay = $"{F(Math.Sqrt(4.0 * result.RequiredAreaMm2 / Math.PI))} mm";
            cards.Add(new ReportCard(
                dict["required_throat_diameter"],
                throatDisplay,
                [new ReportRow(dict["selection_result"], dict["standard_hgt"])]));
            cards.Add(new ReportCard(
                dict["flow_branch"],
                result.Intermediate.EquationBranch,
                [new ReportRow(dict["standard_basis"], LocalizeStandardBasis(input.StandardBasis, dict))]));
        }
        else
        {
            cards.Add(new ReportCard(
                dict["selected_orifice"],
                $"{result.OrificeRecommendation.Selected.Letter} ({F(result.OrificeRecommendation.Selected.AreaMm2)} mm2)",
                [new ReportRow(dict["size_shorthand"], result.OrificeRecommendation.SizeShorthand)]));
            cards.Add(new ReportCard(
                dict["inlet_outlet_size"],
                result.OrificeRecommendation.ConnectionDisplay,
                [new ReportRow(dict["capacity_used_percent"], F(result.OrificeRecommendation.SelectedUtilizationPercent))]));
        }

        return cards;
    }

    private static List<ReportRow> BuildInputRows(
        CalculationInput input,
        CalculationResult result,
        UiLanguage language,
        IReadOnlyDictionary<string, string> dict)
    {
        var rows = new List<ReportRow>
        {
            new(dict["standard_basis"], LocalizeStandardBasis(input.StandardBasis, dict)),
            new(dict["valve_configuration"], LocalizeValveConfiguration(input.ValveConfiguration, dict)),
            new(dict["fluid_type"], LocalizeFluidType(input.FluidType, dict)),
            new(dict["scenario"], LocalizeScenario(input.ReliefScenario, dict)),
            new(dict["pressure_mode"], LocalizePressureMode(input.PressureInputMode, dict)),
            new(dict["pressure_unit"], input.PressureUnit.ToString()),
            new(dict["atmospheric_pressure"], F(input.AtmosphericPressure)),
            new(dict["use_operating_pressure_basis"], input.UseOperatingPressureBasis ? dict["snapshot_basis_auto"] : dict["snapshot_basis_manual"]),
            new(dict["set_pressure"], F(result.Intermediate.SetPressureValue)),
            new(dict["relieving_pressure"], F(result.Intermediate.RelievingPressureValue)),
            new(dict["reseat_pressure"], F(result.Intermediate.ReseatPressureValue)),
            new(dict["blowdown_percent"], F(result.Intermediate.BlowdownPercent)),
            new(dict["back_pressure"], F(input.BackPressure)),
            new(dict["temperature_c"], F(input.TemperatureC)),
            new(dict["relief_load"], F(result.Intermediate.ReliefLoadKgPerHourUsed))
        };

        if (input.UseOperatingPressureBasis)
        {
            rows.Add(new ReportRow(dict["operating_pressure"], F(input.OperatingPressure)));
            rows.Add(new ReportRow(dict["allowed_overpressure_percent_input"], F(input.AllowedOverpressurePercentInput)));
        }

        if (input.ValveConfiguration == ValveConfiguration.PilotOperated)
        {
            rows.Add(new ReportRow(dict["pilot_min_operating_differential_percent"], F(input.PilotMinimumOperatingDifferentialPercent)));
            rows.Add(new ReportRow(dict["pilot_sense_line_mode"], LocalizePilotSenseLineMode(input.PilotSenseLineMode, dict)));
            rows.Add(new ReportRow(dict["pilot_vent_to_atmosphere"], input.PilotVentToAtmosphere ? dict["pilot_vent_external"] : dict["pilot_vent_closed"]));
        }

        AddScenarioRows(rows, input, dict);
        AddFluidRows(rows, input, language, dict);

        rows.Add(new ReportRow(dict["kd"], F(input.DischargeCoefficientKd)));
        rows.Add(new ReportRow(dict["kb"], F(input.BackPressureCorrectionKb)));
        if (input.ValveConfiguration == ValveConfiguration.BalancedBellows)
        {
            rows.Add(new ReportRow(dict["use_custom_bellows_kb"], input.UseCustomBellowsKb ? dict["yes"] : dict["no"]));
        }

        rows.Add(new ReportRow(dict["kc"], F(input.CombinationCorrectionKc)));
        rows.Add(new ReportRow(dict["notes"], input.Notes));
        return rows;
    }

    private static void AddScenarioRows(
        List<ReportRow> rows,
        CalculationInput input,
        IReadOnlyDictionary<string, string> dict)
    {
        if (input.ReliefScenario == ReliefScenario.TubeRupture)
        {
            rows.Add(new ReportRow(dict["tube_inner_diameter"], F(input.TubeInnerDiameterMm)));
            rows.Add(new ReportRow(dict["high_side_pressure"], F(input.HighSidePressure)));
            rows.Add(new ReportRow(dict["high_side_temperature"], F(input.HighSideTemperatureC)));
            if (input.TubeRuptureHighSideNormalFlowKgPerHour > 0)
            {
                rows.Add(new ReportRow(dict["tube_rupture_high_side_normal_flow"], F(input.TubeRuptureHighSideNormalFlowKgPerHour)));
            }
        }

        if (input.ReliefScenario == ReliefScenario.Fire)
        {
            rows.Add(new ReportRow(dict["fire_wetted_area"], F(input.FireWettedAreaM2)));
            rows.Add(new ReportRow(dict["fire_environment_factor"], F(input.FireEnvironmentalFactorF)));
            rows.Add(new ReportRow(dict["fire_constant"], F(input.FireConstantC)));
            rows.Add(new ReportRow(dict["fire_latent_heat"], F(input.VaporizationLatentHeatKjPerKg)));
        }

        if (input.ReliefScenario == ReliefScenario.ThermalExpansion)
        {
            if (input.ThermalExpansionCoefficientPerC > 0)
            {
                rows.Add(new ReportRow(dict["thermal_expansion_coefficient"], F(input.ThermalExpansionCoefficientPerC)));
            }

            if (input.ThermalHeatInputKjPerHour > 0)
            {
                rows.Add(new ReportRow(dict["thermal_expansion_heat_input"], F(input.ThermalHeatInputKjPerHour)));
            }

            if (input.ThermalSpecificHeatKjPerKgC > 0)
            {
                rows.Add(new ReportRow(dict["thermal_expansion_specific_heat"], F(input.ThermalSpecificHeatKjPerKgC)));
            }
        }
    }

    private static void AddFluidRows(
        List<ReportRow> rows,
        CalculationInput input,
        UiLanguage language,
        IReadOnlyDictionary<string, string> dict)
    {
        if (input.FluidType == FluidType.TwoPhaseEquilibrium)
        {
            rows.Add(new ReportRow(dict["two_phase_inlet_specific_volume"], F(input.TwoPhaseInletSpecificVolumeM3PerKg)));
            rows.Add(new ReportRow(dict["two_phase_reference_specific_volume"], F(input.TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg)));
            if (input.ReliefScenario == ReliefScenario.TubeRupture)
            {
                rows.Add(new ReportRow(dict["high_side_two_phase_inlet_specific_volume"], F(input.HighSideTwoPhaseInletSpecificVolumeM3PerKg)));
                rows.Add(new ReportRow(dict["high_side_two_phase_reference_specific_volume"], F(input.HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg)));
            }
        }
        else if (input.FluidType == FluidType.TwoPhaseSubcooled)
        {
            rows.Add(new ReportRow(dict["liquid_density"], F(input.LiquidDensityKgPerM3)));
            rows.Add(new ReportRow(dict["two_phase_reference_density"], F(input.TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3)));
            rows.Add(new ReportRow(dict["two_phase_saturation_pressure_absolute"], F(input.TwoPhaseSaturationPressureAbsolute)));
            if (input.ReliefScenario == ReliefScenario.TubeRupture)
            {
                rows.Add(new ReportRow(dict["high_side_liquid_density"], F(input.HighSideLiquidDensityKgPerM3)));
                rows.Add(new ReportRow(dict["high_side_two_phase_reference_density"], F(input.HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3)));
                rows.Add(new ReportRow(dict["high_side_two_phase_saturation_pressure_absolute"], F(input.HighSideTwoPhaseSaturationPressureAbsolute)));
            }
        }
        else if (input.FluidType == FluidType.Liquid)
        {
            rows.Add(new ReportRow(dict["liquid_density"], F(input.LiquidDensityKgPerM3)));
        }
        else
        {
            rows.Add(new ReportRow(dict["gas_preset"], LocalizeGasPreset(input.GasPresetId, language, dict)));
            rows.Add(new ReportRow(dict["molecular_weight"], F(input.MolecularWeight)));
            rows.Add(new ReportRow(dict["isentropic_k"], F(input.IsentropicExponentK)));
            rows.Add(new ReportRow(dict["compressibility_z"], F(input.CompressibilityFactorZ)));
        }
    }

    private static List<ReportRow> BuildResultRows(
        CalculationInput input,
        CalculationResult result,
        IReadOnlyDictionary<string, string> dict)
    {
        var rows = new List<ReportRow>
        {
            new(dict["required_area"], $"{F(result.RequiredAreaMm2)} mm2"),
            new(dict["set_pressure"], $"{F(result.Intermediate.SetPressureValue)} {input.PressureUnit}"),
            new(dict["relieving_pressure"], $"{F(result.Intermediate.RelievingPressureValue)} {input.PressureUnit}"),
            new(dict["reseat_pressure"], $"{F(result.Intermediate.ReseatPressureValue)} {input.PressureUnit}")
        };

        if (input.StandardBasis == CalculationStandardBasis.HgT20570_2)
        {
            rows.Add(new ReportRow(dict["required_throat_diameter"], $"{F(Math.Sqrt(4.0 * result.RequiredAreaMm2 / Math.PI))} mm"));
            rows.Add(new ReportRow(dict["flow_branch"], result.Intermediate.EquationBranch));
        }
        else
        {
            rows.Add(new ReportRow(dict["selected_orifice"], $"{result.OrificeRecommendation.Selected.Letter} ({F(result.OrificeRecommendation.Selected.AreaMm2)} mm2)"));
            rows.Add(new ReportRow(dict["inlet_outlet_size"], result.OrificeRecommendation.ConnectionDisplay));
            rows.Add(new ReportRow(dict["size_shorthand"], result.OrificeRecommendation.SizeShorthand));
            rows.Add(new ReportRow(dict["capacity_used_percent"], F(result.OrificeRecommendation.SelectedUtilizationPercent)));
            rows.Add(new ReportRow(dict["flow_branch"], result.Intermediate.EquationBranch));
            rows.Add(new ReportRow(
                dict["orifice_candidates"],
                string.Join(", ", result.OrificeRecommendation.CandidateNeighbors.Select(x => $"{x.Letter} ({F(x.AreaMm2)} mm2)"))));
        }

        return rows;
    }

    private static List<ReportRow> BuildExpertRows(CalculationInput input, CalculationResult result)
    {
        var rows = new List<ReportRow>
        {
            new("Set pressure", F(result.Intermediate.SetPressureValue)),
            new("Relieving pressure", F(result.Intermediate.RelievingPressureValue)),
            new("Reseat pressure", F(result.Intermediate.ReseatPressureValue)),
            new("Blowdown (%)", F(result.Intermediate.BlowdownPercent)),
            new("Pset abs (Pa)", F(result.Intermediate.SetPressureAbsPa)),
            new("P1 abs (Pa)", F(result.Intermediate.RelievingPressureAbsPa)),
            new("Preseat abs (Pa)", F(result.Intermediate.ReseatPressureAbsPa)),
            new("P2 abs (Pa)", F(result.Intermediate.BackPressureAbsPa)),
            new("T (K)", F(result.Intermediate.TemperatureK)),
            new("P2/P1", F(result.Intermediate.PressureRatioP2OverP1)),
            new("Critical ratio", F(result.Intermediate.CriticalPressureRatio)),
            new("Overpressure (%)", F(result.Intermediate.OverpressurePercent)),
            new("Allowed overpressure (%)", F(result.Intermediate.AllowedOverpressurePercent)),
            new("Relief load used (kg/h)", F(result.Intermediate.ReliefLoadKgPerHourUsed)),
            new("Mass flux (kg/m2/s)", F(result.Intermediate.MassFluxKgPerM2S)),
            new("Effective factor", F(result.Intermediate.EffectiveDischargeFactor))
        };

        if (result.Intermediate.HeatInputKw > 0)
        {
            rows.Add(new ReportRow("Fire heat input (kW)", F(result.Intermediate.HeatInputKw)));
        }

        if (result.Intermediate.TubeBreakDischargeAreaM2 > 0)
        {
            rows.Add(new ReportRow("Tube break discharge area (m2)", F(result.Intermediate.TubeBreakDischargeAreaM2)));
        }

        if (result.Intermediate.ThermalExpansionVolumeFlowM3PerHour > 0)
        {
            rows.Add(new ReportRow("Thermal expansion volume flow (m3/h)", F(result.Intermediate.ThermalExpansionVolumeFlowM3PerHour)));
        }

        if (result.Intermediate.ThermalExpansionCalculatedLoadKgPerHour > 0)
        {
            rows.Add(new ReportRow("Thermal expansion calculated load (kg/h)", F(result.Intermediate.ThermalExpansionCalculatedLoadKgPerHour)));
        }

        if (result.Intermediate.TwoPhaseOmega > 0)
        {
            rows.Add(new ReportRow("Two-phase omega", F(result.Intermediate.TwoPhaseOmega)));
        }

        if (result.Intermediate.TwoPhaseSaturationPressureAbsPa > 0)
        {
            rows.Add(new ReportRow("Two-phase Ps abs (Pa)", F(result.Intermediate.TwoPhaseSaturationPressureAbsPa)));
        }

        if (result.Intermediate.TwoPhaseSaturationPressureRatio > 0)
        {
            rows.Add(new ReportRow("Two-phase Ps/P1", F(result.Intermediate.TwoPhaseSaturationPressureRatio)));
        }

        if (result.Intermediate.TwoPhaseTransitionSaturationPressureRatio > 0)
        {
            rows.Add(new ReportRow("Two-phase nst", F(result.Intermediate.TwoPhaseTransitionSaturationPressureRatio)));
        }

        if (result.Intermediate.SourceMassFluxKgPerM2S > 0)
        {
            rows.Add(new ReportRow("Tube rupture source mass flux (kg/m2/s)", F(result.Intermediate.SourceMassFluxKgPerM2S)));
        }

        if (input.StandardBasis != CalculationStandardBasis.HgT20570_2)
        {
            rows.Add(new ReportRow("Capacity Used [%]", F(result.OrificeRecommendation.SelectedUtilizationPercent)));
        }

        return rows;
    }

    private static string F(double value) => value.ToString("G10", CultureInfo.InvariantCulture);

    private static string LocalizeFluidType(FluidType fluidType, IReadOnlyDictionary<string, string> dict)
    {
        return fluidType switch
        {
            FluidType.Gas => dict["gas"],
            FluidType.Steam => dict["steam"],
            FluidType.Liquid => dict["liquid"],
            FluidType.TwoPhaseEquilibrium => dict["two_phase_equilibrium"],
            FluidType.TwoPhaseSubcooled => dict["two_phase_subcooled"],
            _ => fluidType.ToString()
        };
    }

    private static string LocalizeScenario(ReliefScenario scenario, IReadOnlyDictionary<string, string> dict)
    {
        return scenario switch
        {
            ReliefScenario.Overpressure => dict["scenario_overpressure"],
            ReliefScenario.Fire => dict["scenario_fire"],
            ReliefScenario.TubeRupture => dict["scenario_tube_rupture"],
            ReliefScenario.ThermalExpansion => dict["scenario_thermal_expansion"],
            _ => scenario.ToString()
        };
    }

    private static string LocalizePressureMode(PressureInputMode pressureInputMode, IReadOnlyDictionary<string, string> dict)
    {
        return pressureInputMode switch
        {
            PressureInputMode.Absolute => dict["absolute"],
            PressureInputMode.Gauge => dict["gauge"],
            _ => pressureInputMode.ToString()
        };
    }

    private static string LocalizeStandardBasis(CalculationStandardBasis basis, IReadOnlyDictionary<string, string> dict)
    {
        return basis switch
        {
            CalculationStandardBasis.HgT20570_2 => dict["standard_hgt"],
            _ => dict["standard_api"]
        };
    }

    private static string LocalizeValveConfiguration(ValveConfiguration valveConfiguration, IReadOnlyDictionary<string, string> dict)
    {
        return valveConfiguration switch
        {
            ValveConfiguration.BalancedBellows => dict["valve_bellows_balanced"],
            ValveConfiguration.PilotOperated => dict["valve_pilot_operated"],
            _ => dict["valve_conventional_spring"]
        };
    }

    private static string LocalizePilotSenseLineMode(PilotSenseLineMode mode, IReadOnlyDictionary<string, string> dict)
    {
        return mode switch
        {
            PilotSenseLineMode.External => dict["pilot_sense_external"],
            _ => dict["pilot_sense_internal"]
        };
    }

    private static string LocalizeGasPreset(string? presetId, UiLanguage language, IReadOnlyDictionary<string, string> dict)
    {
        if (string.IsNullOrWhiteSpace(presetId) || string.Equals(presetId, "custom", StringComparison.OrdinalIgnoreCase))
        {
            return dict["custom_gas"];
        }

        if (GasPresetCatalog.TryGetById(presetId, out GasPreset? preset) && preset is not null)
        {
            return language == UiLanguage.ZhCn ? preset.NameZh : preset.NameEn;
        }

        return presetId;
    }
}
