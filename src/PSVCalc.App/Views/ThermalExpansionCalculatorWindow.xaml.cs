using System.Globalization;
using System.Windows;
using System.Windows.Media;
using PSVCalc.App.ViewModels;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.App.Views;

public partial class ThermalExpansionCalculatorWindow : Window
{
    private readonly ThermalExpansionReliefCalculator _calculator = new();
    private readonly CalculationStandardBasis _standardBasis;
    private ThermalExpansionResult? _lastResult;

    public ThermalExpansionCalculatorWindow(
        UiLanguage language,
        CalculationStandardBasis standardBasis,
        double currentExpansionCoefficientPerC,
        double currentHeatInputKjPerHour,
        double currentLiquidDensityKgPerM3,
        double currentSpecificHeatKjPerKgC,
        double currentReliefLoadKgPerHour)
    {
        _standardBasis = standardBasis;
        Loc = new LocalizationProvider { Language = language };

        InitializeComponent();
        DataContext = this;

        ExpansionCoefficientTextBox.Text = FormatSeed(currentExpansionCoefficientPerC > 0 ? currentExpansionCoefficientPerC : 0.0012);
        HeatInputTextBox.Text = FormatSeed(currentHeatInputKjPerHour > 0 ? currentHeatInputKjPerHour : 25000.0);
        LiquidDensityTextBox.Text = FormatSeed(currentLiquidDensityKgPerM3 > 0 ? currentLiquidDensityKgPerM3 : 850.0);
        SpecificHeatTextBox.Text = FormatSeed(currentSpecificHeatKjPerKgC > 0 ? currentSpecificHeatKjPerKgC : 2.2);

        MassLoadTextBlock.Text = $"{FormatNumber(currentReliefLoadKgPerHour)} kg/h";
        VolumeFlowTextBlock.Text = "-";
        ReliefLoadTextBlock.Text = "-";
    }

    public LocalizationProvider Loc { get; }

    public double SelectedExpansionCoefficientPerC { get; private set; }

    public double SelectedHeatInputKjPerHour { get; private set; }

    public double SelectedLiquidDensityKgPerM3 { get; private set; }

    public double SelectedSpecificHeatKjPerKgC { get; private set; }

    public double SelectedVolumeReliefFlowM3PerHour { get; private set; }

    public double SelectedMassReliefLoadKgPerHour { get; private set; }

    public string ThermalExpansionMethodNote => _standardBasis == CalculationStandardBasis.HgT20570_2
        ? Loc["thermal_expansion_note_hgt"]
        : Loc["thermal_expansion_note_api"];

    private void PreviewCalculation_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildInput(out ThermalExpansionInput input, out string errorMessage))
        {
            SetStatus(errorMessage, isError: true);
            ApplyButton.IsEnabled = false;
            return;
        }

        try
        {
            ThermalExpansionResult result = _calculator.Calculate(input);
            _lastResult = result;

            SelectedExpansionCoefficientPerC = input.VolumetricExpansionCoefficientPerC;
            SelectedHeatInputKjPerHour = input.HeatInputKjPerHour;
            SelectedLiquidDensityKgPerM3 = input.LiquidDensityKgPerM3;
            SelectedSpecificHeatKjPerKgC = input.SpecificHeatKjPerKgC;
            SelectedVolumeReliefFlowM3PerHour = result.VolumeReliefFlowM3PerHour;
            SelectedMassReliefLoadKgPerHour = result.MassReliefLoadKgPerHour;

            ApplyButton.IsEnabled = true;
            VolumeFlowTextBlock.Text = $"{FormatNumber(result.VolumeReliefFlowM3PerHour)} m3/h";
            ReliefLoadTextBlock.Text = $"{FormatNumber(result.MassReliefLoadKgPerHour)} kg/h";
            MassLoadTextBlock.Text = $"{FormatNumber(result.MassReliefLoadKgPerHour)} kg/h";
            SetStatus(Loc["thermal_expansion_ready"], isError: false);
        }
        catch (Exception ex)
        {
            _lastResult = null;
            ApplyButton.IsEnabled = false;
            SetStatus(ex.Message, isError: true);
        }
    }

    private void ApplyResult_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult is null)
        {
            PreviewCalculation_Click(sender, e);
            if (_lastResult is null)
            {
                return;
            }
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool TryBuildInput(out ThermalExpansionInput input, out string errorMessage)
    {
        input = new ThermalExpansionInput();
        errorMessage = string.Empty;

        if (!TryParsePositive(ExpansionCoefficientTextBox.Text, Loc["thermal_expansion_coefficient"], out double coefficient, out errorMessage) ||
            !TryParsePositive(HeatInputTextBox.Text, Loc["thermal_expansion_heat_input"], out double heatInput, out errorMessage) ||
            !TryParsePositive(LiquidDensityTextBox.Text, Loc["liquid_density"], out double density, out errorMessage) ||
            !TryParsePositive(SpecificHeatTextBox.Text, Loc["thermal_expansion_specific_heat"], out double specificHeat, out errorMessage))
        {
            return false;
        }

        input = new ThermalExpansionInput
        {
            VolumetricExpansionCoefficientPerC = coefficient,
            HeatInputKjPerHour = heatInput,
            LiquidDensityKgPerM3 = density,
            SpecificHeatKjPerKgC = specificHeat
        };
        return true;
    }

    private bool TryParsePositive(string? text, string label, out double value, out string errorMessage)
    {
        if (!TryParseDouble(text, out value) || value <= 0.0)
        {
            errorMessage = $"{label}: {Loc["validation_positive_number"]}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryParseDouble(string? text, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatSeed(double value)
    {
        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private string FormatNumber(double value)
    {
        return value.ToString("F3", CultureInfo.CurrentCulture);
    }

    private void SetStatus(string text, bool isError)
    {
        StatusTextBlock.Text = text;
        StatusTextBlock.Foreground = isError
            ? (Brush)(TryFindResource("BrushWarningText") ?? Brushes.IndianRed)
            : (Brush)(TryFindResource("BrushSuccessText") ?? Brushes.ForestGreen);
    }
}
