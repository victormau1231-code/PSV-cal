using System.Windows;
using System.Windows.Controls;
using PSVCalc.App.ViewModels;
using PSVCalc.App.Views;

namespace PSVCalc.App.Features.Inputs;

public partial class FluidCoefficientsInputTab : UserControl
{
    public FluidCoefficientsInputTab()
    {
        InitializeComponent();
    }

    private void OpenPilotOperatedSettings_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var dialog = new PilotOperatedSettingsWindow(
            vm.Language,
            vm.PilotMinimumOperatingDifferentialPercent,
            vm.PilotSenseLineMode,
            vm.PilotVentToAtmosphere)
        {
            Owner = Window.GetWindow(this)
        };

        bool? accepted = dialog.ShowDialog();
        if (accepted == true)
        {
            vm.PilotMinimumOperatingDifferentialPercent = dialog.SelectedMinimumOperatingDifferentialPercent;
            vm.PilotSenseLineMode = dialog.SelectedSenseLineMode;
            vm.PilotVentToAtmosphere = dialog.SelectedVentToAtmosphere;
            vm.StatusMessage = vm.PilotSettingsSummary;
        }
    }
}
