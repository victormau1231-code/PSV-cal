using System.Globalization;
using System.Windows;
using System.Windows.Media;
using PSVCalc.App.ViewModels;
using PSVCalc.Core.Enums;

namespace PSVCalc.App.Views;

public partial class PilotOperatedSettingsWindow : Window
{
    public PilotOperatedSettingsWindow(
        UiLanguage language,
        double minimumOperatingDifferentialPercent,
        PilotSenseLineMode senseLineMode,
        bool ventToAtmosphere)
    {
        Loc = new LocalizationProvider { Language = language };
        SenseLineModeOptions =
        [
            new SelectionItem<PilotSenseLineMode> { Value = PilotSenseLineMode.Internal, Label = Loc["pilot_sense_internal"] },
            new SelectionItem<PilotSenseLineMode> { Value = PilotSenseLineMode.External, Label = Loc["pilot_sense_external"] }
        ];

        InitializeComponent();
        DataContext = this;

        MinimumDifferentialTextBox.Text = minimumOperatingDifferentialPercent.ToString("0.##", CultureInfo.CurrentCulture);
        SenseLineModeComboBox.SelectedValue = senseLineMode;
        VentToAtmosphereCheckBox.IsChecked = ventToAtmosphere;
        StatusTextBlock.Text = Loc["pilot_settings_ready"];
    }

    public LocalizationProvider Loc { get; }

    public IReadOnlyList<SelectionItem<PilotSenseLineMode>> SenseLineModeOptions { get; }

    public string VentCheckBoxLabel => Loc["pilot_vent_external"];

    public double SelectedMinimumOperatingDifferentialPercent { get; private set; }

    public PilotSenseLineMode SelectedSenseLineMode { get; private set; }

    public bool SelectedVentToAtmosphere { get; private set; }

    private void ApplyResult_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildOutput(out string errorMessage))
        {
            StatusTextBlock.Text = errorMessage;
            StatusTextBlock.Foreground = (Brush)(TryFindResource("BrushWarningText") ?? Brushes.IndianRed);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool TryBuildOutput(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!TryParsePositive(MinimumDifferentialTextBox.Text, out double minimumDifferentialPercent))
        {
            errorMessage = $"{Loc["pilot_min_operating_differential_percent"]}: {Loc["validation_positive_number"]}";
            return false;
        }

        if (SenseLineModeComboBox.SelectedValue is not PilotSenseLineMode senseLineMode)
        {
            errorMessage = $"{Loc["pilot_sense_line_mode"]}: {Loc["validation_required"]}";
            return false;
        }

        SelectedMinimumOperatingDifferentialPercent = minimumDifferentialPercent;
        SelectedSenseLineMode = senseLineMode;
        SelectedVentToAtmosphere = VentToAtmosphereCheckBox.IsChecked == true;
        return true;
    }

    private static bool TryParsePositive(string? text, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value) && value > 0)
        {
            return true;
        }

        return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) && value > 0;
    }
}
