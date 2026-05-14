using System.Globalization;
using System.Windows;
using System.Windows.Media;
using PSVCalc.App.ViewModels;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.App.Views;

public partial class WettedAreaCalculatorWindow : Window
{
    private readonly WettedAreaCalculator _calculator = new();
    private readonly double _gradeHeightLimitM;
    private WettedAreaResult? _lastResult;

    public WettedAreaCalculatorWindow(UiLanguage language, double currentAreaM2, double gradeHeightLimitM)
    {
        _gradeHeightLimitM = gradeHeightLimitM;
        Loc = new LocalizationProvider { Language = language };
        OrientationOptions =
        [
            new SelectionItem<VesselOrientation> { Value = VesselOrientation.Horizontal, Label = Loc["orientation_horizontal"] },
            new SelectionItem<VesselOrientation> { Value = VesselOrientation.Vertical, Label = Loc["orientation_vertical"] }
        ];
        HeadTypeOptions =
        [
            new SelectionItem<WettedAreaHeadType> { Value = WettedAreaHeadType.Flat, Label = Loc["head_flat"] },
            new SelectionItem<WettedAreaHeadType> { Value = WettedAreaHeadType.EllipsoidalTwoToOne, Label = Loc["head_ellipsoidal_2to1"] },
            new SelectionItem<WettedAreaHeadType> { Value = WettedAreaHeadType.Hemispherical, Label = Loc["head_hemispherical"] }
        ];

        InitializeComponent();
        DataContext = this;

        OrientationComboBox.SelectedValue = VesselOrientation.Horizontal;
        HeadTypeComboBox.SelectedValue = WettedAreaHeadType.EllipsoidalTwoToOne;
        DiameterTextBox.Text = "2.4";
        StraightLengthTextBox.Text = "8.0";
        LiquidLevelTextBox.Text = "1.9";
        BottomElevationTextBox.Text = "0.8";

        TotalAreaTextBlock.Text = $"{FormatNumber(currentAreaM2)} m2";
        ShellAreaTextBlock.Text = "-";
        HeadAreaTextBlock.Text = "-";
        EffectiveHeightTextBlock.Text = "-";
        GradeCapTextBlock.Text = "-";
    }

    public LocalizationProvider Loc { get; }

    public IReadOnlyList<SelectionItem<VesselOrientation>> OrientationOptions { get; }

    public IReadOnlyList<SelectionItem<WettedAreaHeadType>> HeadTypeOptions { get; }

    public string WettedAreaLimitNote =>
        string.Format(CultureInfo.CurrentCulture, Loc["wetted_area_note_with_limit"], _gradeHeightLimitM);

    public double SelectedAreaM2 { get; private set; }

    private void PreviewCalculation_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildInput(out WettedAreaInput input, out string errorMessage))
        {
            SetStatus(errorMessage, isError: true);
            ApplyButton.IsEnabled = false;
            return;
        }

        try
        {
            WettedAreaResult result = _calculator.Calculate(input);
            _lastResult = result;
            SelectedAreaM2 = result.TotalWettedAreaM2;
            ApplyButton.IsEnabled = true;

            TotalAreaTextBlock.Text = $"{FormatNumber(result.TotalWettedAreaM2)} m2";
            ShellAreaTextBlock.Text = $"{FormatNumber(result.ShellWettedAreaM2)} m2";
            HeadAreaTextBlock.Text = $"{FormatNumber(result.HeadWettedAreaM2)} m2";
            EffectiveHeightTextBlock.Text = $"{FormatNumber(result.EffectiveHeightM)} m";
            GradeCapTextBlock.Text = $"{FormatNumber(result.GradeHeightCapM)} m";

            string status = result.WasLimitedByGradeHeight
                ? Loc["wetted_area_grade_limited"]
                : Loc["wetted_area_ready"];
            SetStatus(status, isError: false);
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

    private bool TryBuildInput(out WettedAreaInput input, out string errorMessage)
    {
        input = new WettedAreaInput();
        errorMessage = string.Empty;

        if (OrientationComboBox.SelectedValue is not VesselOrientation orientation)
        {
            errorMessage = $"{Loc["vessel_orientation"]}: {Loc["validation_required"]}";
            return false;
        }

        if (HeadTypeComboBox.SelectedValue is not WettedAreaHeadType headType)
        {
            errorMessage = $"{Loc["head_type"]}: {Loc["validation_required"]}";
            return false;
        }

        if (!TryParsePositive(DiameterTextBox.Text, Loc["vessel_diameter_m"], out double diameterM, out errorMessage) ||
            !TryParsePositive(StraightLengthTextBox.Text, Loc["straight_shell_length_m"], out double straightLengthM, out errorMessage) ||
            !TryParseNonNegative(LiquidLevelTextBox.Text, Loc["normal_liquid_level_m"], out double liquidLevelM, out errorMessage) ||
            !TryParseNonNegative(BottomElevationTextBox.Text, Loc["bottom_elevation_from_grade_m"], out double bottomElevationM, out errorMessage))
        {
            return false;
        }

        input = new WettedAreaInput
        {
            Orientation = orientation,
            HeadType = headType,
            DiameterM = diameterM,
            StraightLengthM = straightLengthM,
            LiquidLevelM = liquidLevelM,
            BottomElevationFromGradeM = bottomElevationM,
            GradeHeightLimitM = _gradeHeightLimitM
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

    private bool TryParseNonNegative(string? text, string label, out double value, out string errorMessage)
    {
        if (!TryParseDouble(text, out value) || value < 0.0)
        {
            errorMessage = $"{label}: {Loc["validation_non_negative_number"]}";
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
