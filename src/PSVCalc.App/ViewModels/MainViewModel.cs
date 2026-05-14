using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PSVCalc.Core;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace PSVCalc.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string CustomGasPresetId = "custom";
    private readonly ISafetyValveCalculator _calculator;
    private readonly IProjectRepository _projectRepository;
    private readonly IExcelReportExporter _excelReportExporter;
    private readonly IValidationCaseStore _validationCaseStore;
    private readonly IValidationCaseRunner _validationCaseRunner;
    private readonly StoragePaths _storagePaths;
    private bool _syncingGasPresetSelection;
    private bool _suppressDirtyTracking = true;
    private PressureUnit _lastPressureUnit = PressureUnit.MPa;
    private CalculationResult? _lastResult;
    private static readonly HashSet<string> TrackedInputProperties =
    [
        nameof(CaseName),
        nameof(StandardBasis),
        nameof(FluidType),
        nameof(PressureInputMode),
        nameof(PressureUnit),
        nameof(AtmosphericPressure),
        nameof(ReliefScenario),
        nameof(ValveConfiguration),
        nameof(SelectedScenarioTabIndex),
        nameof(UseOperatingPressureBasis),
        nameof(OperatingPressure),
        nameof(AllowedOverpressurePercentInput),
        nameof(BlowdownPercent),
        nameof(PilotMinimumOperatingDifferentialPercent),
        nameof(PilotSenseLineMode),
        nameof(PilotVentToAtmosphere),
        nameof(SetPressure),
        nameof(RelievingPressure),
        nameof(BackPressure),
        nameof(HighSidePressure),
        nameof(HighSideTemperatureC),
        nameof(TemperatureC),
        nameof(ReliefLoadKgPerHour),
        nameof(ThermalExpansionCoefficientPerC),
        nameof(ThermalHeatInputKjPerHour),
        nameof(ThermalSpecificHeatKjPerKgC),
        nameof(FireWettedAreaM2),
        nameof(FireEnvironmentalFactorF),
        nameof(FireConstantC),
        nameof(VaporizationLatentHeatKjPerKg),
        nameof(UseGasPreset),
        nameof(SelectedGasPreset),
        nameof(MolecularWeight),
        nameof(IsentropicExponentK),
        nameof(CompressibilityFactorZ),
        nameof(LiquidDensityKgPerM3),
        nameof(TwoPhaseInletSpecificVolumeM3PerKg),
        nameof(TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg),
        nameof(TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3),
        nameof(TwoPhaseSaturationPressureAbsolute),
        nameof(TubeInnerDiameterMm),
        nameof(TubeRuptureHighSideNormalFlowKgPerHour),
        nameof(HighSideLiquidDensityKgPerM3),
        nameof(HighSideTwoPhaseInletSpecificVolumeM3PerKg),
        nameof(HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg),
        nameof(HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3),
        nameof(HighSideTwoPhaseSaturationPressureAbsolute),
        nameof(DischargeCoefficientKd),
        nameof(BackPressureCorrectionKb),
        nameof(UseCustomBellowsKb),
        nameof(CombinationCorrectionKc),
        nameof(Notes)
    ];

    public MainViewModel(
        ISafetyValveCalculator calculator,
        IProjectRepository projectRepository,
        IExcelReportExporter excelReportExporter,
        IValidationCaseStore validationCaseStore,
        IValidationCaseRunner validationCaseRunner,
        StoragePaths storagePaths)
    {
        _calculator = calculator;
        _projectRepository = projectRepository;
        _excelReportExporter = excelReportExporter;
        _validationCaseStore = validationCaseStore;
        _validationCaseRunner = validationCaseRunner;
        _storagePaths = storagePaths;

        Loc = new LocalizationProvider();
        BuildStaticSelections();
        RebuildGasPresetOptions();
        RefreshHistory();

        SelectedGasPreset = GasPresetOptions.FirstOrDefault(x => x.Id == "air") ?? GasPresetOptions.FirstOrDefault();
        ApplySelectedPresetValues();
        RefreshCalculatedPressureDisplays();
        _suppressDirtyTracking = false;
        IsCalculationStale = true;
    }

    public LocalizationProvider Loc { get; }

    public ObservableCollection<SelectionItem<UiLanguage>> LanguageOptions { get; } = [];
    public ObservableCollection<SelectionItem<CalculationStandardBasis>> StandardOptions { get; } = [];
    public ObservableCollection<SelectionItem<FluidType>> FluidOptions { get; } = [];
    public ObservableCollection<SelectionItem<ResultViewMode>> ViewModeOptions { get; } = [];
    public ObservableCollection<SelectionItem<PressureInputMode>> PressureModeOptions { get; } = [];
    public ObservableCollection<SelectionItem<PressureUnit>> PressureUnitOptions { get; } = [];
    public ObservableCollection<SelectionItem<ValveConfiguration>> ValveConfigurationOptions { get; } = [];
    public ObservableCollection<SelectionItem<ReliefScenario>> ScenarioOptions { get; } = [];
    public ObservableCollection<GasPresetOption> GasPresetOptions { get; } = [];

    public ObservableCollection<DisplayRow> ExpertRows { get; } = [];
    public ObservableCollection<DisplayRow> ParameterRows { get; } = [];
    public ObservableCollection<HistoryEntry> HistoryRows { get; } = [];

    [ObservableProperty] private string caseName = "Case-001";
    [ObservableProperty] private CalculationStandardBasis standardBasis = CalculationStandardBasis.Api520521Asme;
    [ObservableProperty] private FluidType fluidType = FluidType.Gas;
    [ObservableProperty] private UiLanguage language = UiLanguage.ZhCn;
    [ObservableProperty] private ResultViewMode resultViewMode = ResultViewMode.Summary;
    [ObservableProperty] private PressureInputMode pressureInputMode = PressureInputMode.Gauge;
    [ObservableProperty] private PressureUnit pressureUnit = PressureUnit.MPa;
    [ObservableProperty] private double atmosphericPressure = 0.101325;
    [ObservableProperty] private ReliefScenario reliefScenario = ReliefScenario.Overpressure;
    [ObservableProperty] private ValveConfiguration valveConfiguration = ValveConfiguration.ConventionalSpring;
    [ObservableProperty] private int selectedScenarioTabIndex;
    [ObservableProperty] private bool useOperatingPressureBasis;

    [ObservableProperty] private double operatingPressure = 1.0;
    [ObservableProperty] private double allowedOverpressurePercentInput = 10.0;
    [ObservableProperty] private double blowdownPercent = 7.0;
    [ObservableProperty] private double pilotMinimumOperatingDifferentialPercent = 10.0;
    [ObservableProperty] private PilotSenseLineMode pilotSenseLineMode = PilotSenseLineMode.Internal;
    [ObservableProperty] private bool pilotVentToAtmosphere = true;
    [ObservableProperty] private double setPressure = 1.0;
    [ObservableProperty] private double relievingPressure = 1.1;
    [ObservableProperty] private double backPressure = 0.0;
    [ObservableProperty] private double highSidePressure = 1.5;
    [ObservableProperty] private double highSideTemperatureC = 40.0;
    [ObservableProperty] private double temperatureC = 38.0;
    [ObservableProperty] private double reliefLoadKgPerHour = 1000.0;
    [ObservableProperty] private double thermalExpansionCoefficientPerC;
    [ObservableProperty] private double thermalHeatInputKjPerHour;
    [ObservableProperty] private double thermalSpecificHeatKjPerKgC;
    [ObservableProperty] private double fireWettedAreaM2 = 10.0;
    [ObservableProperty] private double fireEnvironmentalFactorF = 1.0;
    [ObservableProperty] private double fireConstantC = 43.2;
    [ObservableProperty] private double vaporizationLatentHeatKjPerKg = 2257.0;

    [ObservableProperty] private bool useGasPreset = true;
    [ObservableProperty] private GasPresetOption? selectedGasPreset;
    [ObservableProperty] private double molecularWeight = 28.97;
    [ObservableProperty] private double isentropicExponentK = 1.4;
    [ObservableProperty] private double compressibilityFactorZ = 1.0;
    [ObservableProperty] private double liquidDensityKgPerM3 = 1000.0;
    [ObservableProperty] private double twoPhaseInletSpecificVolumeM3PerKg = 0.02;
    [ObservableProperty] private double twoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = 0.024;
    [ObservableProperty] private double twoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = 260.0;
    [ObservableProperty] private double twoPhaseSaturationPressureAbsolute = 0.6;
    [ObservableProperty] private double tubeInnerDiameterMm = 19.0;
    [ObservableProperty] private double tubeRuptureHighSideNormalFlowKgPerHour;
    [ObservableProperty] private double highSideLiquidDensityKgPerM3 = 900.0;
    [ObservableProperty] private double highSideTwoPhaseInletSpecificVolumeM3PerKg = 0.02;
    [ObservableProperty] private double highSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = 0.024;
    [ObservableProperty] private double highSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = 260.0;
    [ObservableProperty] private double highSideTwoPhaseSaturationPressureAbsolute = 0.8;

    [ObservableProperty] private double dischargeCoefficientKd = 0.975;
    [ObservableProperty] private double backPressureCorrectionKb = 1.0;
    [ObservableProperty] private bool useCustomBellowsKb;
    [ObservableProperty] private double combinationCorrectionKc = 1.0;
    [ObservableProperty] private string notes = string.Empty;

    [ObservableProperty] private double requiredAreaMm2;
    [ObservableProperty] private double requiredAreaCm2;
    [ObservableProperty] private double requiredAreaIn2;
    [ObservableProperty] private string selectedOrifice = "-";
    [ObservableProperty] private string inletOutletSize = "-";
    [ObservableProperty] private string sizeShorthand = "-";
    [ObservableProperty] private string capacityUsedPercentDisplay = "-";
    [ObservableProperty] private string flowBranch = "-";
    [ObservableProperty] private string warningText = "-";
    [ObservableProperty] private string candidateOrifices = "-";
    [ObservableProperty] private string resultSetPressure = "-";
    [ObservableProperty] private string resultRelievingPressure = "-";
    [ObservableProperty] private string resultReseatPressure = "-";
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isCalculationStale = true;

    public bool IsGasFluid => FluidType == FluidType.Gas;
    public bool IsLiquidFluid => FluidType == FluidType.Liquid;
    public bool IsTwoPhaseEquilibriumFluid => FluidType == FluidType.TwoPhaseEquilibrium;
    public bool IsTwoPhaseSubcooledFluid => FluidType == FluidType.TwoPhaseSubcooled;
    public bool IsTwoPhaseFluid => IsTwoPhaseEquilibriumFluid || IsTwoPhaseSubcooledFluid;
    public bool ShowSinglePhaseGasInputs => FluidType == FluidType.Gas;
    public bool ShowSinglePhaseLiquidInputs => FluidType == FluidType.Liquid;
    public bool ShowTwoPhaseEquilibriumInputs => IsTwoPhaseEquilibriumFluid;
    public bool ShowTwoPhaseSubcooledInputs => IsTwoPhaseSubcooledFluid;
    public bool ShowTubeRuptureHighSideSinglePhaseInputs => IsTubeRuptureScenario && !IsTwoPhaseFluid;
    public bool ShowTubeRuptureHighSideTwoPhaseEquilibriumInputs => IsTubeRuptureScenario && IsTwoPhaseEquilibriumFluid;
    public bool ShowTubeRuptureHighSideTwoPhaseSubcooledInputs => IsTubeRuptureScenario && IsTwoPhaseSubcooledFluid;
    public bool IsTubeRuptureScenario => ReliefScenario == ReliefScenario.TubeRupture;
    public bool IsFireScenario => ReliefScenario == ReliefScenario.Fire;
    public bool IsThermalExpansionScenario => ReliefScenario == ReliefScenario.ThermalExpansion;
    public bool IsApiStandard => StandardBasis == CalculationStandardBasis.Api520521Asme;
    public bool IsHgTStandard => StandardBasis == CalculationStandardBasis.HgT20570_2;
    public bool IsManualReliefLoadEnabled => !IsTubeRuptureScenario && !IsFireScenario;
    public bool ShowAutoReliefLoadHint => !IsManualReliefLoadEnabled;
    public bool IsSetAndRelievingManualEnabled => !UseOperatingPressureBasis;
    public bool ShowOperatingPressureInputs => UseOperatingPressureBasis;
    public bool ShowManualSetAndRelievingInputs => !UseOperatingPressureBasis;
    public bool IsExpertView => ResultViewMode == ResultViewMode.Expert;
    public bool ShowScenarioNoExtraInputs => !IsFireScenario && !IsTubeRuptureScenario && !IsThermalExpansionScenario;
    public bool ShowHgTTubeRuptureNormalFlowInput => IsTubeRuptureScenario && IsHgTStandard;
    public bool ShowTubeRuptureApiExplanation => IsTubeRuptureScenario && IsApiStandard;
    public bool ShowTubeRuptureHgTExplanation => IsTubeRuptureScenario && IsHgTStandard;
    public bool ShowThermalExpansionApiExplanation => IsThermalExpansionScenario && IsApiStandard;
    public bool ShowThermalExpansionHgTExplanation => IsThermalExpansionScenario && IsHgTStandard;
    public bool IsBellowsBalanced => ValveConfiguration == ValveConfiguration.BalancedBellows;
    public bool IsPilotOperated => ValveConfiguration == ValveConfiguration.PilotOperated;
    public bool ShowBellowsKbOverrideOption => IsBellowsBalanced;
    public bool IsKbEditable => !IsBellowsBalanced || UseCustomBellowsKb;
    public bool ShowPilotOperatedHint => IsPilotOperated;
    public bool HasThermalExpansionInputs =>
        ThermalExpansionCoefficientPerC > 0
        && ThermalHeatInputKjPerHour > 0
        && ThermalSpecificHeatKjPerKgC > 0
        && LiquidDensityKgPerM3 > 0;
    public double ThermalExpansionCalculatedVolumeFlowM3PerHour => HasThermalExpansionInputs
        ? ThermalExpansionCoefficientPerC * ThermalHeatInputKjPerHour / (LiquidDensityKgPerM3 * ThermalSpecificHeatKjPerKgC)
        : 0.0;
    public double ThermalExpansionCalculatedLoadKgPerHour => ThermalExpansionCalculatedVolumeFlowM3PerHour * LiquidDensityKgPerM3;
    public string ThermalExpansionSummary => HasThermalExpansionInputs
        ? string.Format(
            CultureInfo.CurrentCulture,
            Loc["thermal_expansion_summary"],
            ThermalExpansionCoefficientPerC,
            ThermalHeatInputKjPerHour,
            ThermalSpecificHeatKjPerKgC,
            ThermalExpansionCalculatedVolumeFlowM3PerHour,
            ThermalExpansionCalculatedLoadKgPerHour)
        : Loc["thermal_expansion_summary_empty"];
    public string CurrentSelectionCardTitle => IsHgTStandard
        ? Loc["required_throat_diameter"]
        : Loc["report_section_selection"];
    public string CurrentSelectionCardValue => IsHgTStandard
        ? RequiredThroatDiameterDisplay
        : SelectedOrifice;
    public string CurrentSelectionSecondaryLabel => IsHgTStandard
        ? Loc["required_area"]
        : Loc["size_shorthand"];
    public string CurrentSelectionSecondaryValue => IsHgTStandard
        ? $"{RequiredAreaMm2:F3} mm2"
        : SizeShorthand;
    public string CurrentSelectionTertiaryLabel => IsHgTStandard
        ? Loc["flow_branch"]
        : Loc["inlet_outlet_size"];
    public string CurrentSelectionTertiaryValue => IsHgTStandard
        ? FlowBranch
        : InletOutletSize;
    public string CurrentSelectionQuaternaryLabel => IsHgTStandard
        ? "-"
        : Loc["capacity_used_percent"];
    public string CurrentSelectionQuaternaryValue => IsHgTStandard
        ? "-"
        : CapacityUsedPercentDisplay;
    public string CurrentDetailSelectionLabel => IsHgTStandard
        ? Loc["required_throat_diameter"]
        : Loc["selected_orifice"];
    public string CurrentDetailSelectionValue => IsHgTStandard
        ? RequiredThroatDiameterDisplay
        : SelectedOrifice;
    public bool ShowApiSelectionArtifacts => !IsHgTStandard;
    public bool ShowApiCandidateArtifacts => !IsHgTStandard;
    public double RequiredThroatDiameterMm => RequiredAreaMm2 > 0
        ? Math.Sqrt(4.0 * RequiredAreaMm2 / Math.PI)
        : 0.0;
    public string RequiredThroatDiameterDisplay => RequiredThroatDiameterMm > 0
        ? $"{RequiredThroatDiameterMm:F3} mm"
        : "-";
    public string CurrentPilotSenseLineModeLabel => PilotSenseLineMode switch
    {
        PilotSenseLineMode.External => Loc["pilot_sense_external"],
        _ => Loc["pilot_sense_internal"]
    };
    public string CurrentPilotVentDispositionLabel => PilotVentToAtmosphere
        ? Loc["pilot_vent_external"]
        : Loc["pilot_vent_closed"];
    public string PilotSettingsSummary =>
        $"{Loc["pilot_min_operating_differential_percent"]}: {PilotMinimumOperatingDifferentialPercent:0.##}%  |  {Loc["pilot_sense_line_mode"]}: {CurrentPilotSenseLineModeLabel}  |  {Loc["pilot_vent_to_atmosphere"]}: {CurrentPilotVentDispositionLabel}";
    public double CurrentWettedAreaGradeLimitM => IsHgTStandard ? 7.5 : 7.6;
    public string CurrentFluidLabel => FluidType switch
    {
        FluidType.Gas => Loc["gas"],
        FluidType.Steam => Loc["steam"],
        FluidType.Liquid => Loc["liquid"],
        FluidType.TwoPhaseEquilibrium => Loc["two_phase_equilibrium"],
        FluidType.TwoPhaseSubcooled => Loc["two_phase_subcooled"],
        _ => FluidType.ToString()
    };
    public string CurrentScenarioLabel => ReliefScenario switch
    {
        ReliefScenario.Overpressure => Loc["scenario_overpressure"],
        ReliefScenario.Fire => Loc["scenario_fire"],
        ReliefScenario.TubeRupture => Loc["scenario_tube_rupture"],
        ReliefScenario.ThermalExpansion => Loc["scenario_thermal_expansion"],
        _ => ReliefScenario.ToString()
    };
    public string CurrentPressureBasisLabel => UseOperatingPressureBasis
        ? Loc["snapshot_basis_auto"]
        : Loc["snapshot_basis_manual"];
    public string CurrentPrimaryPressureLabel => WithPressureUnit(UseOperatingPressureBasis
        ? Loc["operating_pressure"]
        : Loc["set_pressure"]);
    public string CurrentPrimaryPressureResultValue => UseOperatingPressureBasis
        ? FormatPressure(OperatingPressure)
        : ResultSetPressure;
    public string CurrentPrimaryPressureHint => UseOperatingPressureBasis
        ? Loc["pressure_card_operating_auto_hint"]
        : Loc["pressure_card_set_manual_hint"];
    public string OperatingPressureLabel => WithPressureUnit(Loc["operating_pressure"]);
    public string CurrentRelievingPressureLabel => WithPressureUnit(UseOperatingPressureBasis
        ? Loc["derived_relieving_pressure"]
        : Loc["relieving_pressure"]);
    public string CurrentRelievingPressureHint => UseOperatingPressureBasis
        ? Loc["pressure_card_relieving_auto_hint"]
        : Loc["pressure_card_relieving_manual_hint"];
    public string CurrentReseatPressureLabel => WithPressureUnit(UseOperatingPressureBasis
        ? Loc["derived_reseat_pressure"]
        : Loc["reseat_pressure"]);
    public string CurrentReseatPressureHint => UseOperatingPressureBasis
        ? Loc["pressure_card_reseat_auto_hint"]
        : Loc["pressure_card_reseat_manual_hint"];
    public string BackPressureLabel => WithPressureUnit(Loc["back_pressure"]);
    public string AtmosphericPressureLabel => WithPressureUnit(Loc["atmospheric_pressure"]);
    public string TwoPhaseSaturationPressureAbsoluteLabel => WithPressureUnit(Loc["two_phase_saturation_pressure_absolute"]);
    public string HighSidePressureLabel => WithPressureUnit(Loc["high_side_pressure"]);
    public string HighSideTwoPhaseSaturationPressureAbsoluteLabel => WithPressureUnit(Loc["high_side_two_phase_saturation_pressure_absolute"]);
    public string CurrentPressureModeLabel => PressureInputMode switch
    {
        PressureInputMode.Gauge => Loc["gauge"],
        PressureInputMode.Absolute => Loc["absolute"],
        _ => PressureInputMode.ToString()
    };
    public string CurrentViewModeLabel => ResultViewMode switch
    {
        ResultViewMode.Summary => Loc["summary"],
        ResultViewMode.Expert => Loc["expert"],
        _ => ResultViewMode.ToString()
    };
    public string CurrentStandardLabel => StandardBasis switch
    {
        CalculationStandardBasis.HgT20570_2 => Loc["standard_hgt"],
        _ => Loc["standard_api"]
    };
    public string CurrentValveConfigurationLabel => ValveConfiguration switch
    {
        ValveConfiguration.BalancedBellows => Loc["valve_bellows_balanced"],
        ValveConfiguration.PilotOperated => Loc["valve_pilot_operated"],
        _ => Loc["valve_conventional_spring"]
    };

    public string CurrentStandardRecordLabel => CalculationStandardCatalog.GetDisplayName(StandardBasis);
    public string CalculationPromptText => IsCalculationStale
        ? Loc["calculate_dirty_hint"]
        : Loc["calculate_ready_hint"];

    partial void OnLanguageChanged(UiLanguage value)
    {
        Loc.Language = value;
        BuildStaticSelections();
        RebuildGasPresetOptions();
        RaiseComputedFlags();
        RaisePilotSettingFlags();
    }

    partial void OnFluidTypeChanged(FluidType value)
    {
        if (IsKnownRecommendedKd(DischargeCoefficientKd))
        {
            DischargeCoefficientKd = GetRecommendedKd(value);
        }

        if (value != FluidType.Gas)
        {
            UseGasPreset = false;
        }
        else if (!UseGasPreset)
        {
            SelectedGasPreset = GasPresetOptions.FirstOrDefault(x => string.Equals(x.Id, CustomGasPresetId, StringComparison.OrdinalIgnoreCase))
                ?? SelectedGasPreset;
        }

        RaiseComputedFlags();
    }

    partial void OnResultViewModeChanged(ResultViewMode value)
    {
        RaiseComputedFlags();
    }

    partial void OnUseOperatingPressureBasisChanged(bool value)
    {
        RefreshCalculatedPressureDisplays();
        RaiseComputedFlags();
    }

    partial void OnOperatingPressureChanged(double value)
    {
        RefreshCalculatedPressureDisplays();
        OnPropertyChanged(nameof(CurrentPrimaryPressureResultValue));
    }

    partial void OnAllowedOverpressurePercentInputChanged(double value) => RefreshCalculatedPressureDisplays();

    partial void OnBlowdownPercentChanged(double value) => RefreshCalculatedPressureDisplays();

    partial void OnThermalExpansionCoefficientPerCChanged(double value) => RaiseComputedFlags();

    partial void OnThermalHeatInputKjPerHourChanged(double value) => RaiseComputedFlags();

    partial void OnThermalSpecificHeatKjPerKgCChanged(double value) => RaiseComputedFlags();

    partial void OnLiquidDensityKgPerM3Changed(double value) => RaiseComputedFlags();

    partial void OnIsCalculationStaleChanged(bool value) => OnPropertyChanged(nameof(CalculationPromptText));

    partial void OnPilotMinimumOperatingDifferentialPercentChanged(double value) => RaisePilotSettingFlags();

    partial void OnPilotSenseLineModeChanged(PilotSenseLineMode value) => RaisePilotSettingFlags();

    partial void OnPilotVentToAtmosphereChanged(bool value) => RaisePilotSettingFlags();

    partial void OnUseCustomBellowsKbChanged(bool value)
    {
        if (IsBellowsBalanced && !value)
        {
            BackPressureCorrectionKb = 1.0;
        }

        RaiseComputedFlags();
    }

    partial void OnSetPressureChanged(double value) => RefreshCalculatedPressureDisplays();

    partial void OnRelievingPressureChanged(double value) => RefreshCalculatedPressureDisplays();

    partial void OnPressureInputModeChanged(PressureInputMode value)
    {
        RefreshCalculatedPressureDisplays();
        RaiseComputedFlags();
    }

    partial void OnPressureUnitChanged(PressureUnit value)
    {
        if (_lastPressureUnit != value)
        {
            if (!_suppressDirtyTracking)
            {
                ConvertPressureInputs(_lastPressureUnit, value);
            }

            _lastPressureUnit = value;
        }

        RefreshCalculatedPressureDisplays();
        RaiseComputedFlags();
    }

    partial void OnStandardBasisChanged(CalculationStandardBasis value)
    {
        RaiseComputedFlags();
    }

    partial void OnResultSetPressureChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentPrimaryPressureResultValue));
    }

    partial void OnResultRelievingPressureChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentRelievingPressureLabel));
        OnPropertyChanged(nameof(CurrentRelievingPressureHint));
    }

    partial void OnResultReseatPressureChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentReseatPressureLabel));
        OnPropertyChanged(nameof(CurrentReseatPressureHint));
    }

    partial void OnRequiredAreaMm2Changed(double value)
    {
        OnPropertyChanged(nameof(RequiredThroatDiameterMm));
        OnPropertyChanged(nameof(RequiredThroatDiameterDisplay));
        OnPropertyChanged(nameof(CurrentSelectionCardValue));
        OnPropertyChanged(nameof(CurrentSelectionSecondaryValue));
        OnPropertyChanged(nameof(CurrentDetailSelectionValue));
    }

    partial void OnSelectedOrificeChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentSelectionCardValue));
        OnPropertyChanged(nameof(CurrentDetailSelectionValue));
    }

    partial void OnInletOutletSizeChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentSelectionTertiaryValue));
    }

    partial void OnSizeShorthandChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentSelectionSecondaryValue));
    }

    partial void OnCapacityUsedPercentDisplayChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentSelectionQuaternaryValue));
    }

    partial void OnFlowBranchChanged(string value)
    {
        OnPropertyChanged(nameof(CurrentSelectionTertiaryValue));
    }

    partial void OnValveConfigurationChanged(ValveConfiguration value)
    {
        if (value != ValveConfiguration.BalancedBellows)
        {
            UseCustomBellowsKb = false;
        }

        if ((value == ValveConfiguration.BalancedBellows && !UseCustomBellowsKb) ||
            value == ValveConfiguration.PilotOperated)
        {
            BackPressureCorrectionKb = 1.0;
        }

        RaiseComputedFlags();
        RaisePilotSettingFlags();
    }

    partial void OnReliefScenarioChanged(ReliefScenario value)
    {
        int tabIndex = ScenarioToTabIndex(value);
        if (SelectedScenarioTabIndex != tabIndex)
        {
            SelectedScenarioTabIndex = tabIndex;
        }
        RaiseComputedFlags();
    }

    partial void OnSelectedScenarioTabIndexChanged(int value)
    {
        ReliefScenario mapped = TabIndexToScenario(value);
        if (ReliefScenario != mapped)
        {
            ReliefScenario = mapped;
            return;
        }

        RaiseComputedFlags();
    }

    partial void OnUseGasPresetChanged(bool value)
    {
        if (_syncingGasPresetSelection || FluidType != FluidType.Gas)
        {
            return;
        }

        _syncingGasPresetSelection = true;
        try
        {
            if (value)
            {
                if (SelectedGasPreset is null || string.Equals(SelectedGasPreset.Id, CustomGasPresetId, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedGasPreset = GasPresetOptions.FirstOrDefault(x => x.Id == "air")
                        ?? GasPresetOptions.FirstOrDefault(x => !string.Equals(x.Id, CustomGasPresetId, StringComparison.OrdinalIgnoreCase))
                        ?? GasPresetOptions.FirstOrDefault();
                }

                ApplySelectedPresetValues();
            }
            else
            {
                SelectedGasPreset = GasPresetOptions.FirstOrDefault(x => string.Equals(x.Id, CustomGasPresetId, StringComparison.OrdinalIgnoreCase))
                    ?? SelectedGasPreset;
            }
        }
        finally
        {
            _syncingGasPresetSelection = false;
        }

        RaiseComputedFlags();
    }

    partial void OnSelectedGasPresetChanged(GasPresetOption? value)
    {
        if (_syncingGasPresetSelection || value is null)
        {
            return;
        }

        _syncingGasPresetSelection = true;
        try
        {
            if (string.Equals(value.Id, CustomGasPresetId, StringComparison.OrdinalIgnoreCase))
            {
                if (UseGasPreset)
                {
                    UseGasPreset = false;
                }
            }
            else
            {
                if (!UseGasPreset && FluidType == FluidType.Gas)
                {
                    UseGasPreset = true;
                }

                ApplySelectedPresetValues();
            }
        }
        finally
        {
            _syncingGasPresetSelection = false;
        }

        RaiseComputedFlags();
    }

    [RelayCommand]
    private void Calculate()
    {
        try
        {
            var result = RecalculateCurrentInput();
            _projectRepository.AddHistory(new HistoryEntry
            {
                Timestamp = DateTimeOffset.Now,
                ProjectId = string.Empty,
                CaseName = CaseName,
                FluidType = FluidType,
                RequiredAreaMm2 = result.RequiredAreaMm2,
                SelectedOrifice = IsHgTStandard
                    ? RequiredThroatDiameterDisplay
                    : result.OrificeRecommendation.Selected.Letter,
                ProjectFile = string.Empty
            });
            RefreshHistory();
            StatusMessage = $"{Loc["results"]}: {result.RequiredAreaMm2:F3} mm2";
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Loc["calc_failed"]}: {ex.Message}";
            MessageBox.Show(ex.Message, Loc["calc_failed"], MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void SaveProject()
    {
        try
        {
            ProjectRecord record = BuildProjectRecord();
            string fileName = $"{CaseName}-{DateTime.Now:yyyyMMdd-HHmmss}";
            string path = _projectRepository.SaveProject(record, fileName);
            StatusMessage = $"{Loc["saved_to"]}: {path}";
            RefreshHistory();
            MessageBox.Show(StatusMessage, Loc["save_project"], MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Loc["validation_failed"]}: {ex.Message}";
            MessageBox.Show(ex.Message, Loc["save_project"], MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void LoadProject()
    {
        try
        {
            var openDialog = new OpenFileDialog
            {
                Title = Loc["load_project"],
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = _projectRepository.ProjectsDirectory
            };

            if (openDialog.ShowDialog() != true)
            {
                return;
            }

            ProjectRecord record = _projectRepository.LoadProject(openDialog.FileName);
            ApplyProjectRecord(record);
            StatusMessage = $"{Loc["project_loaded"]}: {openDialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Loc["validation_failed"]}: {ex.Message}";
            MessageBox.Show(ex.Message, Loc["load_project"], MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        try
        {
            EnsureCurrentResult();

            var saveDialog = new SaveFileDialog
            {
                Title = Loc["export_excel"],
                Filter = "Excel files (*.xls)|*.xls|All files (*.*)|*.*",
                InitialDirectory = _projectRepository.ExportsDirectory,
                FileName = $"{CaseName}-{DateTime.Now:yyyyMMdd-HHmmss}.xls",
                AddExtension = true,
                DefaultExt = ".xls",
                OverwritePrompt = true
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            ProjectRecord record = BuildProjectRecord();
            string generatedPath = _excelReportExporter.Export(
                record,
                Language,
                Path.GetFileNameWithoutExtension(saveDialog.FileName));

            string path = saveDialog.FileName;
            if (!string.Equals(generatedPath, path, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(generatedPath, path, overwrite: true);
                if (generatedPath.StartsWith(_projectRepository.ExportsDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(generatedPath);
                }
            }

            StatusMessage = $"{Loc["exported_to"]}: {path}";
            MessageBox.Show(StatusMessage, Loc["export_excel"], MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Loc["validation_failed"]}: {ex.Message}";
            MessageBox.Show(ex.Message, Loc["export_excel"], MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CaseName = "Case-001";
        StandardBasis = CalculationStandardBasis.Api520521Asme;
        FluidType = FluidType.Gas;
        ReliefScenario = ReliefScenario.Overpressure;
        ValveConfiguration = ValveConfiguration.ConventionalSpring;
        UseOperatingPressureBasis = false;
        PressureInputMode = PressureInputMode.Gauge;
        PressureUnit = PressureUnit.MPa;
        AtmosphericPressure = 0.101325;
        OperatingPressure = 1.0;
        AllowedOverpressurePercentInput = 10.0;
        BlowdownPercent = 7.0;
        PilotMinimumOperatingDifferentialPercent = 10.0;
        PilotSenseLineMode = PilotSenseLineMode.Internal;
        PilotVentToAtmosphere = true;
        SetPressure = 1.0;
        RelievingPressure = 1.1;
        BackPressure = 0.0;
        HighSidePressure = 1.5;
        HighSideTemperatureC = 40.0;
        TemperatureC = 38.0;
        ReliefLoadKgPerHour = 1000.0;
        ThermalExpansionCoefficientPerC = 0.0;
        ThermalHeatInputKjPerHour = 0.0;
        ThermalSpecificHeatKjPerKgC = 0.0;
        FireWettedAreaM2 = 10.0;
        FireEnvironmentalFactorF = 1.0;
        FireConstantC = 43.2;
        VaporizationLatentHeatKjPerKg = 2257.0;
        UseGasPreset = true;
        SelectedGasPreset = GasPresetOptions.FirstOrDefault(x => x.Id == "air") ?? GasPresetOptions.FirstOrDefault();
        ApplySelectedPresetValues();
        LiquidDensityKgPerM3 = 1000.0;
        TwoPhaseInletSpecificVolumeM3PerKg = 0.02;
        TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = 0.024;
        TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = 260.0;
        TwoPhaseSaturationPressureAbsolute = 0.6;
        TubeInnerDiameterMm = 19.0;
        TubeRuptureHighSideNormalFlowKgPerHour = 0.0;
        HighSideLiquidDensityKgPerM3 = 900.0;
        HighSideTwoPhaseInletSpecificVolumeM3PerKg = 0.02;
        HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = 0.024;
        HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = 260.0;
        HighSideTwoPhaseSaturationPressureAbsolute = 0.8;
        DischargeCoefficientKd = 0.975;
        BackPressureCorrectionKb = 1.0;
        UseCustomBellowsKb = false;
        CombinationCorrectionKc = 1.0;
        Notes = string.Empty;

        RequiredAreaMm2 = 0;
        RequiredAreaCm2 = 0;
        RequiredAreaIn2 = 0;
        SelectedOrifice = "-";
        InletOutletSize = "-";
        SizeShorthand = "-";
        CapacityUsedPercentDisplay = "-";
        FlowBranch = "-";
        WarningText = "-";
        CandidateOrifices = "-";
        ResultSetPressure = "-";
        ResultRelievingPressure = "-";
        ResultReseatPressure = "-";
        ExpertRows.Clear();
        ParameterRows.Clear();
        _lastResult = null;
        OnPropertyChanged(nameof(CurrentSelectionQuaternaryLabel));
        StatusMessage = string.Empty;
        RefreshCalculatedPressureDisplays();
        RaisePilotSettingFlags();
        IsCalculationStale = true;
    }

    private void BuildStaticSelections()
    {
        LanguageOptions.Clear();
        LanguageOptions.Add(new SelectionItem<UiLanguage> { Value = UiLanguage.ZhCn, Label = "中文" });
        LanguageOptions.Add(new SelectionItem<UiLanguage> { Value = UiLanguage.EnUs, Label = "English" });

        StandardOptions.Clear();
        StandardOptions.Add(new SelectionItem<CalculationStandardBasis>
        {
            Value = CalculationStandardBasis.Api520521Asme,
            Label = Loc["standard_api"]
        });
        StandardOptions.Add(new SelectionItem<CalculationStandardBasis>
        {
            Value = CalculationStandardBasis.HgT20570_2,
            Label = Loc["standard_hgt"]
        });

        FluidOptions.Clear();
        FluidOptions.Add(new SelectionItem<FluidType> { Value = FluidType.Gas, Label = Loc["gas"] });
        FluidOptions.Add(new SelectionItem<FluidType> { Value = FluidType.Steam, Label = Loc["steam"] });
        FluidOptions.Add(new SelectionItem<FluidType> { Value = FluidType.Liquid, Label = Loc["liquid"] });
        FluidOptions.Add(new SelectionItem<FluidType> { Value = FluidType.TwoPhaseEquilibrium, Label = Loc["two_phase_equilibrium"] });
        FluidOptions.Add(new SelectionItem<FluidType> { Value = FluidType.TwoPhaseSubcooled, Label = Loc["two_phase_subcooled"] });

        ScenarioOptions.Clear();
        ScenarioOptions.Add(new SelectionItem<ReliefScenario> { Value = ReliefScenario.Overpressure, Label = Loc["scenario_overpressure"] });
        ScenarioOptions.Add(new SelectionItem<ReliefScenario> { Value = ReliefScenario.Fire, Label = Loc["scenario_fire"] });
        ScenarioOptions.Add(new SelectionItem<ReliefScenario> { Value = ReliefScenario.TubeRupture, Label = Loc["scenario_tube_rupture"] });
        ScenarioOptions.Add(new SelectionItem<ReliefScenario> { Value = ReliefScenario.ThermalExpansion, Label = Loc["scenario_thermal_expansion"] });

        ViewModeOptions.Clear();
        ViewModeOptions.Add(new SelectionItem<ResultViewMode> { Value = ResultViewMode.Summary, Label = Loc["summary"] });
        ViewModeOptions.Add(new SelectionItem<ResultViewMode> { Value = ResultViewMode.Expert, Label = Loc["expert"] });

        PressureModeOptions.Clear();
        PressureModeOptions.Add(new SelectionItem<PressureInputMode> { Value = PressureInputMode.Gauge, Label = Loc["gauge"] });
        PressureModeOptions.Add(new SelectionItem<PressureInputMode> { Value = PressureInputMode.Absolute, Label = Loc["absolute"] });

        PressureUnitOptions.Clear();
        PressureUnitOptions.Add(new SelectionItem<PressureUnit> { Value = PressureUnit.MPa, Label = "MPa" });
        PressureUnitOptions.Add(new SelectionItem<PressureUnit> { Value = PressureUnit.kPa, Label = "kPa" });
        PressureUnitOptions.Add(new SelectionItem<PressureUnit> { Value = PressureUnit.bar, Label = "bar" });

        ValveConfigurationOptions.Clear();
        ValveConfigurationOptions.Add(new SelectionItem<ValveConfiguration>
        {
            Value = ValveConfiguration.ConventionalSpring,
            Label = Loc["valve_conventional_spring"]
        });
        ValveConfigurationOptions.Add(new SelectionItem<ValveConfiguration>
        {
            Value = ValveConfiguration.BalancedBellows,
            Label = Loc["valve_bellows_balanced"]
        });
        ValveConfigurationOptions.Add(new SelectionItem<ValveConfiguration>
        {
            Value = ValveConfiguration.PilotOperated,
            Label = Loc["valve_pilot_operated"]
        });
    }

    private void RebuildGasPresetOptions()
    {
        string? selectedId = SelectedGasPreset?.Id;
        GasPresetOptions.Clear();

        GasPresetOptions.Add(new GasPresetOption
        {
            Id = CustomGasPresetId,
            Label = Loc["custom_gas"]
        });

        foreach (GasPreset preset in GasPresetCatalog.GetAll())
        {
            GasPresetOptions.Add(new GasPresetOption
            {
                Id = preset.Id,
                Label = Language == UiLanguage.ZhCn ? preset.NameZh : preset.NameEn
            });
        }

        if (selectedId is not null)
        {
            SelectedGasPreset = GasPresetOptions.FirstOrDefault(x => x.Id == selectedId);
        }

        SelectedGasPreset ??= UseGasPreset
            ? GasPresetOptions.FirstOrDefault(x => x.Id == "air") ?? GasPresetOptions.FirstOrDefault()
            : GasPresetOptions.FirstOrDefault(x => x.Id == CustomGasPresetId) ?? GasPresetOptions.FirstOrDefault();
    }

    private void ApplySelectedPresetValues()
    {
        if (!UseGasPreset ||
            SelectedGasPreset is null ||
            string.Equals(SelectedGasPreset.Id, CustomGasPresetId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!GasPresetCatalog.TryGetById(SelectedGasPreset.Id, out GasPreset? preset) || preset is null)
        {
            return;
        }

        MolecularWeight = preset.MolecularWeight;
        IsentropicExponentK = preset.IsentropicExponent;
        CompressibilityFactorZ = preset.CompressibilityFactor;
    }

    private CalculationInput BuildInput()
    {
        return new CalculationInput
        {
            CaseName = string.IsNullOrWhiteSpace(CaseName) ? "New Case" : CaseName.Trim(),
            StandardBasis = StandardBasis,
            FluidType = FluidType,
            ReliefScenario = ReliefScenario,
            ValveConfiguration = ValveConfiguration,
            UseOperatingPressureBasis = UseOperatingPressureBasis,
            PressureInputMode = PressureInputMode,
            PressureUnit = PressureUnit,
            AtmosphericPressure = AtmosphericPressure,
            OperatingPressure = OperatingPressure,
            AllowedOverpressurePercentInput = AllowedOverpressurePercentInput,
            BlowdownPercent = BlowdownPercent,
            PilotMinimumOperatingDifferentialPercent = PilotMinimumOperatingDifferentialPercent,
            PilotSenseLineMode = PilotSenseLineMode,
            PilotVentToAtmosphere = PilotVentToAtmosphere,
            SetPressure = SetPressure,
            RelievingPressure = RelievingPressure,
            BackPressure = BackPressure,
            HighSidePressure = HighSidePressure,
            HighSideTemperatureC = HighSideTemperatureC,
            TemperatureC = TemperatureC,
            ReliefLoadKgPerHour = ReliefLoadKgPerHour,
            ThermalExpansionCoefficientPerC = ThermalExpansionCoefficientPerC,
            ThermalHeatInputKjPerHour = ThermalHeatInputKjPerHour,
            ThermalSpecificHeatKjPerKgC = ThermalSpecificHeatKjPerKgC,
            FireWettedAreaM2 = FireWettedAreaM2,
            FireEnvironmentalFactorF = FireEnvironmentalFactorF,
            FireConstantC = FireConstantC,
            VaporizationLatentHeatKjPerKg = VaporizationLatentHeatKjPerKg,
            DischargeCoefficientKd = DischargeCoefficientKd,
            BackPressureCorrectionKb = BackPressureCorrectionKb,
            UseCustomBellowsKb = UseCustomBellowsKb,
            CombinationCorrectionKc = CombinationCorrectionKc,
            UseGasPreset = UseGasPreset &&
                           SelectedGasPreset is not null &&
                           !string.Equals(SelectedGasPreset.Id, CustomGasPresetId, StringComparison.OrdinalIgnoreCase),
            GasPresetId = SelectedGasPreset?.Id,
            MolecularWeight = MolecularWeight,
            IsentropicExponentK = IsentropicExponentK,
            CompressibilityFactorZ = CompressibilityFactorZ,
            LiquidDensityKgPerM3 = LiquidDensityKgPerM3,
            TwoPhaseInletSpecificVolumeM3PerKg = TwoPhaseInletSpecificVolumeM3PerKg,
            TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg,
            TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3,
            TwoPhaseSaturationPressureAbsolute = TwoPhaseSaturationPressureAbsolute,
            TubeInnerDiameterMm = TubeInnerDiameterMm,
            TubeRuptureHighSideNormalFlowKgPerHour = TubeRuptureHighSideNormalFlowKgPerHour,
            HighSideLiquidDensityKgPerM3 = HighSideLiquidDensityKgPerM3,
            HighSideTwoPhaseInletSpecificVolumeM3PerKg = HighSideTwoPhaseInletSpecificVolumeM3PerKg,
            HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg,
            HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3,
            HighSideTwoPhaseSaturationPressureAbsolute = HighSideTwoPhaseSaturationPressureAbsolute,
            Notes = Notes?.Trim() ?? string.Empty
        };
    }

    private ProjectRecord BuildProjectRecord()
    {
        CalculationResult result = EnsureCurrentResult();

        return new ProjectRecord
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            CaseName = string.IsNullOrWhiteSpace(CaseName) ? "New Case" : CaseName.Trim(),
            SavedAt = DateTimeOffset.Now,
            SoftwareVersion = AppMetadata.SoftwareVersion,
            StandardVersion = CurrentStandardRecordLabel,
            Language = Language,
            Input = BuildInput(),
            Result = result
        };
    }

    private CalculationResult EnsureCurrentResult()
    {
        return _lastResult is not null && !IsCalculationStale
            ? _lastResult
            : RecalculateCurrentInput();
    }

    private CalculationResult RecalculateCurrentInput()
    {
        var input = BuildInput();
        var result = _calculator.Calculate(input);
        _lastResult = result;
        ApplyResult(result);
        IsCalculationStale = false;
        StatusMessage = $"{Loc["results"]}: {result.RequiredAreaMm2:F3} mm2";
        return result;
    }

    private void ApplyProjectRecord(ProjectRecord record)
    {
        _suppressDirtyTracking = true;

        Language = record.Language;
        CaseName = record.CaseName;

        StandardBasis = record.Input.StandardBasis;
        FluidType = record.Input.FluidType;
        ReliefScenario = record.Input.ReliefScenario;
        ValveConfiguration = record.Input.ValveConfiguration;
        UseOperatingPressureBasis = record.Input.UseOperatingPressureBasis;
        PressureInputMode = record.Input.PressureInputMode;
        PressureUnit = record.Input.PressureUnit;
        AtmosphericPressure = record.Input.AtmosphericPressure > 0 ? record.Input.AtmosphericPressure : 0.101325;
        OperatingPressure = record.Input.OperatingPressure;
        AllowedOverpressurePercentInput = record.Input.AllowedOverpressurePercentInput;
        BlowdownPercent = record.Input.BlowdownPercent;
        PilotMinimumOperatingDifferentialPercent = record.Input.PilotMinimumOperatingDifferentialPercent;
        PilotSenseLineMode = record.Input.PilotSenseLineMode;
        PilotVentToAtmosphere = record.Input.PilotVentToAtmosphere;
        SetPressure = record.Input.SetPressure;
        RelievingPressure = record.Input.RelievingPressure;
        BackPressure = record.Input.BackPressure;
        HighSidePressure = record.Input.HighSidePressure;
        HighSideTemperatureC = record.Input.HighSideTemperatureC;
        TemperatureC = record.Input.TemperatureC;
        ReliefLoadKgPerHour = record.Input.ReliefLoadKgPerHour;
        ThermalExpansionCoefficientPerC = record.Input.ThermalExpansionCoefficientPerC;
        ThermalHeatInputKjPerHour = record.Input.ThermalHeatInputKjPerHour;
        ThermalSpecificHeatKjPerKgC = record.Input.ThermalSpecificHeatKjPerKgC;
        FireWettedAreaM2 = record.Input.FireWettedAreaM2;
        FireEnvironmentalFactorF = record.Input.FireEnvironmentalFactorF;
        FireConstantC = record.Input.FireConstantC;
        VaporizationLatentHeatKjPerKg = record.Input.VaporizationLatentHeatKjPerKg;
        DischargeCoefficientKd = record.Input.DischargeCoefficientKd;
        BackPressureCorrectionKb = record.Input.BackPressureCorrectionKb;
        UseCustomBellowsKb = record.Input.UseCustomBellowsKb;
        CombinationCorrectionKc = record.Input.CombinationCorrectionKc;
        UseGasPreset = record.Input.UseGasPreset;
        Notes = record.Input.Notes;
        LiquidDensityKgPerM3 = record.Input.LiquidDensityKgPerM3;
        TwoPhaseInletSpecificVolumeM3PerKg = record.Input.TwoPhaseInletSpecificVolumeM3PerKg;
        TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = record.Input.TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg;
        TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = record.Input.TwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3;
        TwoPhaseSaturationPressureAbsolute = record.Input.TwoPhaseSaturationPressureAbsolute;
        TubeInnerDiameterMm = record.Input.TubeInnerDiameterMm;
        TubeRuptureHighSideNormalFlowKgPerHour = record.Input.TubeRuptureHighSideNormalFlowKgPerHour;
        HighSideLiquidDensityKgPerM3 = record.Input.HighSideLiquidDensityKgPerM3;
        HighSideTwoPhaseInletSpecificVolumeM3PerKg = record.Input.HighSideTwoPhaseInletSpecificVolumeM3PerKg;
        HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = record.Input.HighSideTwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg;
        HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3 = record.Input.HighSideTwoPhaseDensityAtNinetyPercentSaturationPressureKgPerM3;
        HighSideTwoPhaseSaturationPressureAbsolute = record.Input.HighSideTwoPhaseSaturationPressureAbsolute;

        RebuildGasPresetOptions();
        SelectedGasPreset = GasPresetOptions.FirstOrDefault(x =>
            string.Equals(x.Id, record.Input.GasPresetId, StringComparison.OrdinalIgnoreCase))
            ?? (record.Input.UseGasPreset
                ? GasPresetOptions.FirstOrDefault(x => x.Id == "air")
                : GasPresetOptions.FirstOrDefault(x => string.Equals(x.Id, CustomGasPresetId, StringComparison.OrdinalIgnoreCase)))
            ?? GasPresetOptions.FirstOrDefault();
        MolecularWeight = record.Input.MolecularWeight;
        IsentropicExponentK = record.Input.IsentropicExponentK;
        CompressibilityFactorZ = record.Input.CompressibilityFactorZ;

        _lastResult = record.Result;
        if (_lastResult is not null)
        {
            ApplyResult(_lastResult);
        }
        else
        {
            RequiredAreaMm2 = 0;
            RequiredAreaCm2 = 0;
            RequiredAreaIn2 = 0;
            ResultSetPressure = "-";
            ResultRelievingPressure = "-";
            ResultReseatPressure = "-";
            SelectedOrifice = "-";
            InletOutletSize = "-";
            SizeShorthand = "-";
            CapacityUsedPercentDisplay = "-";
            FlowBranch = "-";
            WarningText = "-";
            CandidateOrifices = "-";
            ExpertRows.Clear();
            ParameterRows.Clear();
            OnPropertyChanged(nameof(CurrentSelectionQuaternaryLabel));
        }

        RefreshHistory();
        _suppressDirtyTracking = false;
        IsCalculationStale = record.Result is null;
    }

    private void ApplyResult(CalculationResult result)
    {
        RequiredAreaMm2 = result.RequiredAreaMm2;
        RequiredAreaCm2 = result.RequiredAreaCm2;
        RequiredAreaIn2 = result.RequiredAreaIn2;
        ResultSetPressure = FormatPressure(result.Intermediate.SetPressureValue);
        ResultRelievingPressure = FormatPressure(result.Intermediate.RelievingPressureValue);
        ResultReseatPressure = FormatPressure(result.Intermediate.ReseatPressureValue);
        SelectedOrifice = IsHgTStandard
            ? RequiredThroatDiameterDisplay
            : $"{result.OrificeRecommendation.Selected.Letter} ({result.OrificeRecommendation.Selected.AreaMm2:F2} mm2)";
        InletOutletSize = IsHgTStandard ? "-" : result.OrificeRecommendation.ConnectionDisplay;
        SizeShorthand = IsHgTStandard ? "-" : result.OrificeRecommendation.SizeShorthand;
        CapacityUsedPercentDisplay = IsHgTStandard
            ? "-"
            : result.OrificeRecommendation.SelectedUtilizationPercent.ToString("F2", CultureInfo.CurrentCulture);
        FlowBranch = result.Intermediate.EquationBranch;
        WarningText = result.Warnings.Count == 0 ? Loc["none"] : string.Join(Environment.NewLine, result.Warnings);
        CandidateOrifices = IsHgTStandard
            ? "-"
            : string.Join(
                " | ",
                result.OrificeRecommendation.CandidateNeighbors.Select(x => $"{x.Letter}:{x.AreaMm2:F1}"));

        ExpertRows.Clear();
        AddExpert("Set pressure", result.Intermediate.SetPressureValue, PressureUnit.ToString());
        AddExpert("Relieving pressure", result.Intermediate.RelievingPressureValue, PressureUnit.ToString());
        AddExpert("Reseat pressure", result.Intermediate.ReseatPressureValue, PressureUnit.ToString());
        AddExpert("Blowdown", result.Intermediate.BlowdownPercent, "%");
        AddExpert("P1 abs", result.Intermediate.RelievingPressureAbsPa, "Pa");
        AddExpert("Pset abs", result.Intermediate.SetPressureAbsPa, "Pa");
        AddExpert("Preseat abs", result.Intermediate.ReseatPressureAbsPa, "Pa");
        AddExpert("P2 abs", result.Intermediate.BackPressureAbsPa, "Pa");
        AddExpert("T", result.Intermediate.TemperatureK, "K");
        AddExpert("P2/P1", result.Intermediate.PressureRatioP2OverP1, "-");
        AddExpert("Critical ratio", result.Intermediate.CriticalPressureRatio, "-");
        AddExpert("Overpressure", result.Intermediate.OverpressurePercent, "%");
        AddExpert("Allowed overpressure", result.Intermediate.AllowedOverpressurePercent, "%");
        AddExpert("Relief load used", result.Intermediate.ReliefLoadKgPerHourUsed, "kg/h");
        if (result.Intermediate.ThermalExpansionVolumeFlowM3PerHour > 0)
        {
            AddExpert("Thermal expansion volume flow", result.Intermediate.ThermalExpansionVolumeFlowM3PerHour, "m3/h");
        }
        if (result.Intermediate.ThermalExpansionCalculatedLoadKgPerHour > 0)
        {
            AddExpert("Thermal expansion calculated load", result.Intermediate.ThermalExpansionCalculatedLoadKgPerHour, "kg/h");
        }
        if (result.Intermediate.HeatInputKw > 0)
        {
            AddExpert("Fire heat input", result.Intermediate.HeatInputKw, "kW");
        }
        if (result.Intermediate.TubeBreakDischargeAreaM2 > 0)
        {
            AddExpert("Tube break discharge area", result.Intermediate.TubeBreakDischargeAreaM2, "m2");
        }
        if (result.Intermediate.TwoPhaseOmega > 0)
        {
            AddExpert("Two-phase omega", result.Intermediate.TwoPhaseOmega, "-");
        }
        if (result.Intermediate.TwoPhaseSaturationPressureAbsPa > 0)
        {
            AddExpert("Two-phase Ps abs", result.Intermediate.TwoPhaseSaturationPressureAbsPa, "Pa");
        }
        if (result.Intermediate.TwoPhaseSaturationPressureRatio > 0)
        {
            AddExpert("Two-phase Ps/P1", result.Intermediate.TwoPhaseSaturationPressureRatio, "-");
        }
        if (result.Intermediate.TwoPhaseTransitionSaturationPressureRatio > 0)
        {
            AddExpert("Two-phase nst", result.Intermediate.TwoPhaseTransitionSaturationPressureRatio, "-");
        }
        if (result.Intermediate.TwoPhaseInletSpecificVolumeM3PerKg > 0)
        {
            AddExpert("Two-phase v1", result.Intermediate.TwoPhaseInletSpecificVolumeM3PerKg, "m3/kg");
        }
        if (result.Intermediate.TwoPhaseReferenceSpecificVolumeM3PerKg > 0)
        {
            AddExpert("Two-phase v@0.9P1", result.Intermediate.TwoPhaseReferenceSpecificVolumeM3PerKg, "m3/kg");
        }
        if (result.Intermediate.TwoPhaseReferenceDensityKgPerM3 > 0)
        {
            AddExpert("Two-phase rho@0.9Ps", result.Intermediate.TwoPhaseReferenceDensityKgPerM3, "kg/m3");
        }
        AddExpert("Mass flux", result.Intermediate.MassFluxKgPerM2S, "kg/m2/s");
        if (result.Intermediate.SourceMassFluxKgPerM2S > 0)
        {
            AddExpert("Tube rupture source mass flux", result.Intermediate.SourceMassFluxKgPerM2S, "kg/m2/s");
        }
        AddExpert("Effective factor", result.Intermediate.EffectiveDischargeFactor, "-");
        AddExpert("Required area", result.Intermediate.RequiredAreaM2, "m2");
        if (!IsHgTStandard)
        {
            AddExpert("Capacity Used [%]", result.OrificeRecommendation.SelectedUtilizationPercent, "%");
        }

        ParameterRows.Clear();
        foreach (ParameterAudit audit in result.ParameterAudits)
        {
            ParameterRows.Add(new DisplayRow
            {
                Parameter = audit.Name,
                Value = audit.Value.ToString("G10", CultureInfo.InvariantCulture),
                Unit = audit.Unit,
                Source = audit.Source.ToString()
            });
        }
    }

    private void RefreshHistory()
    {
        HistoryRows.Clear();
        foreach (HistoryEntry item in _projectRepository.LoadHistory(20))
        {
            HistoryRows.Add(item);
        }
    }

    private void AddExpert(string name, double value, string unit)
    {
        ExpertRows.Add(new DisplayRow
        {
            Parameter = name,
            Value = value.ToString("G10", CultureInfo.InvariantCulture),
            Unit = unit
        });
    }

    private static int ScenarioToTabIndex(ReliefScenario scenario)
    {
        return scenario switch
        {
            ReliefScenario.Overpressure => 0,
            ReliefScenario.Fire => 1,
            ReliefScenario.TubeRupture => 2,
            ReliefScenario.ThermalExpansion => 3,
            _ => 0
        };
    }

    private static ReliefScenario TabIndexToScenario(int tabIndex)
    {
        return tabIndex switch
        {
            0 => ReliefScenario.Overpressure,
            1 => ReliefScenario.Fire,
            2 => ReliefScenario.TubeRupture,
            3 => ReliefScenario.ThermalExpansion,
            _ => ReliefScenario.Overpressure
        };
    }

    private static bool IsKnownRecommendedKd(double value)
    {
        return Math.Abs(value - 0.975) < 1e-9
            || Math.Abs(value - 0.85) < 1e-9
            || Math.Abs(value - 0.65) < 1e-9;
    }

    private static double GetRecommendedKd(FluidType fluidType)
    {
        return fluidType switch
        {
            FluidType.TwoPhaseEquilibrium => 0.85,
            FluidType.TwoPhaseSubcooled => 0.65,
            _ => 0.975
        };
    }

    private void RaiseComputedFlags()
    {
        OnPropertyChanged(nameof(IsGasFluid));
        OnPropertyChanged(nameof(IsLiquidFluid));
        OnPropertyChanged(nameof(IsTwoPhaseEquilibriumFluid));
        OnPropertyChanged(nameof(IsTwoPhaseSubcooledFluid));
        OnPropertyChanged(nameof(IsTwoPhaseFluid));
        OnPropertyChanged(nameof(ShowSinglePhaseGasInputs));
        OnPropertyChanged(nameof(ShowSinglePhaseLiquidInputs));
        OnPropertyChanged(nameof(ShowTwoPhaseEquilibriumInputs));
        OnPropertyChanged(nameof(ShowTwoPhaseSubcooledInputs));
        OnPropertyChanged(nameof(ShowTubeRuptureHighSideSinglePhaseInputs));
        OnPropertyChanged(nameof(ShowTubeRuptureHighSideTwoPhaseEquilibriumInputs));
        OnPropertyChanged(nameof(ShowTubeRuptureHighSideTwoPhaseSubcooledInputs));
        OnPropertyChanged(nameof(IsTubeRuptureScenario));
        OnPropertyChanged(nameof(IsFireScenario));
        OnPropertyChanged(nameof(IsThermalExpansionScenario));
        OnPropertyChanged(nameof(IsApiStandard));
        OnPropertyChanged(nameof(IsHgTStandard));
        OnPropertyChanged(nameof(IsManualReliefLoadEnabled));
        OnPropertyChanged(nameof(ShowAutoReliefLoadHint));
        OnPropertyChanged(nameof(IsSetAndRelievingManualEnabled));
        OnPropertyChanged(nameof(ShowOperatingPressureInputs));
        OnPropertyChanged(nameof(ShowManualSetAndRelievingInputs));
        OnPropertyChanged(nameof(IsExpertView));
        OnPropertyChanged(nameof(ShowScenarioNoExtraInputs));
        OnPropertyChanged(nameof(ShowHgTTubeRuptureNormalFlowInput));
        OnPropertyChanged(nameof(ShowTubeRuptureApiExplanation));
        OnPropertyChanged(nameof(ShowTubeRuptureHgTExplanation));
        OnPropertyChanged(nameof(ShowThermalExpansionApiExplanation));
        OnPropertyChanged(nameof(ShowThermalExpansionHgTExplanation));
        OnPropertyChanged(nameof(IsBellowsBalanced));
        OnPropertyChanged(nameof(IsPilotOperated));
        OnPropertyChanged(nameof(ShowBellowsKbOverrideOption));
        OnPropertyChanged(nameof(IsKbEditable));
        OnPropertyChanged(nameof(ShowPilotOperatedHint));
        OnPropertyChanged(nameof(HasThermalExpansionInputs));
        OnPropertyChanged(nameof(ThermalExpansionCalculatedVolumeFlowM3PerHour));
        OnPropertyChanged(nameof(ThermalExpansionCalculatedLoadKgPerHour));
        OnPropertyChanged(nameof(ThermalExpansionSummary));
        OnPropertyChanged(nameof(CurrentWettedAreaGradeLimitM));
        OnPropertyChanged(nameof(CurrentFluidLabel));
        OnPropertyChanged(nameof(CurrentScenarioLabel));
        OnPropertyChanged(nameof(CurrentPressureBasisLabel));
        OnPropertyChanged(nameof(CurrentPrimaryPressureLabel));
        OnPropertyChanged(nameof(CurrentPrimaryPressureResultValue));
        OnPropertyChanged(nameof(CurrentPrimaryPressureHint));
        OnPropertyChanged(nameof(CurrentRelievingPressureLabel));
        OnPropertyChanged(nameof(CurrentRelievingPressureHint));
        OnPropertyChanged(nameof(CurrentReseatPressureLabel));
        OnPropertyChanged(nameof(CurrentReseatPressureHint));
        OnPropertyChanged(nameof(OperatingPressureLabel));
        OnPropertyChanged(nameof(BackPressureLabel));
        OnPropertyChanged(nameof(AtmosphericPressureLabel));
        OnPropertyChanged(nameof(TwoPhaseSaturationPressureAbsoluteLabel));
        OnPropertyChanged(nameof(HighSidePressureLabel));
        OnPropertyChanged(nameof(HighSideTwoPhaseSaturationPressureAbsoluteLabel));
        OnPropertyChanged(nameof(CurrentPressureModeLabel));
        OnPropertyChanged(nameof(CurrentViewModeLabel));
        OnPropertyChanged(nameof(CurrentStandardLabel));
        OnPropertyChanged(nameof(CurrentValveConfigurationLabel));
        OnPropertyChanged(nameof(CurrentStandardRecordLabel));
        OnPropertyChanged(nameof(CurrentSelectionCardTitle));
        OnPropertyChanged(nameof(CurrentSelectionCardValue));
        OnPropertyChanged(nameof(CurrentSelectionSecondaryLabel));
        OnPropertyChanged(nameof(CurrentSelectionSecondaryValue));
        OnPropertyChanged(nameof(CurrentSelectionTertiaryLabel));
        OnPropertyChanged(nameof(CurrentSelectionTertiaryValue));
        OnPropertyChanged(nameof(CurrentSelectionQuaternaryLabel));
        OnPropertyChanged(nameof(CurrentDetailSelectionLabel));
        OnPropertyChanged(nameof(CurrentDetailSelectionValue));
        OnPropertyChanged(nameof(ShowApiSelectionArtifacts));
        OnPropertyChanged(nameof(ShowApiCandidateArtifacts));
        OnPropertyChanged(nameof(RequiredThroatDiameterMm));
        OnPropertyChanged(nameof(RequiredThroatDiameterDisplay));
    }

    private void RaisePilotSettingFlags()
    {
        OnPropertyChanged(nameof(CurrentPilotSenseLineModeLabel));
        OnPropertyChanged(nameof(CurrentPilotVentDispositionLabel));
        OnPropertyChanged(nameof(PilotSettingsSummary));
        OnPropertyChanged(nameof(CalculationPromptText));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_suppressDirtyTracking ||
            e.PropertyName is not string propertyName ||
            !TrackedInputProperties.Contains(propertyName))
        {
            return;
        }

        IsCalculationStale = true;
    }

    private void RefreshCalculatedPressureDisplays()
    {
        double currentSetPressure = UseOperatingPressureBasis
            ? OperatingPressure
            : SetPressure;
        double currentRelievingPressure = UseOperatingPressureBasis
            ? OperatingPressure * (1.0 + AllowedOverpressurePercentInput / 100.0)
            : RelievingPressure;
        double currentReseatPressure = Math.Max(
            0.0,
            currentSetPressure * (1.0 - Math.Max(0.0, BlowdownPercent) / 100.0));

        ResultSetPressure = FormatPressure(currentSetPressure);
        ResultRelievingPressure = FormatPressure(currentRelievingPressure);
        ResultReseatPressure = FormatPressure(currentReseatPressure);
    }

    private void ConvertPressureInputs(PressureUnit fromUnit, PressureUnit toUnit)
    {
        AtmosphericPressure = ConvertPressureValue(AtmosphericPressure, fromUnit, toUnit);
        OperatingPressure = ConvertPressureValue(OperatingPressure, fromUnit, toUnit);
        SetPressure = ConvertPressureValue(SetPressure, fromUnit, toUnit);
        RelievingPressure = ConvertPressureValue(RelievingPressure, fromUnit, toUnit);
        BackPressure = ConvertPressureValue(BackPressure, fromUnit, toUnit);
        HighSidePressure = ConvertPressureValue(HighSidePressure, fromUnit, toUnit);
        TwoPhaseSaturationPressureAbsolute = ConvertPressureValue(TwoPhaseSaturationPressureAbsolute, fromUnit, toUnit);
        HighSideTwoPhaseSaturationPressureAbsolute = ConvertPressureValue(HighSideTwoPhaseSaturationPressureAbsolute, fromUnit, toUnit);
    }

    private static double ConvertPressureValue(double value, PressureUnit fromUnit, PressureUnit toUnit)
    {
        if (fromUnit == toUnit)
        {
            return value;
        }

        double valueMpa = UnitConverter.PressureToMPa(value, fromUnit);
        return UnitConverter.PressureFromMPa(valueMpa, toUnit);
    }

    private string FormatPressure(double value)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:0.####} {1}", value, PressureUnit);
    }

    private string WithPressureUnit(string label) => $"{label} ({PressureUnit})";
}


