using System.Windows;
using System.Windows.Controls;
using PSVCalc.App.ViewModels;
using PSVCalc.App.Views;

namespace PSVCalc.App.Features.Inputs;

public partial class ScenarioDetailsInputTab : UserControl
{
    public ScenarioDetailsInputTab()
    {
        InitializeComponent();
    }

    private void OpenWettedAreaCalculator_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var dialog = new WettedAreaCalculatorWindow(vm.Language, vm.FireWettedAreaM2, vm.CurrentWettedAreaGradeLimitM)
        {
            Owner = Window.GetWindow(this)
        };

        bool? accepted = dialog.ShowDialog();
        if (accepted == true)
        {
            vm.FireWettedAreaM2 = dialog.SelectedAreaM2;
            vm.StatusMessage = $"{vm.Loc["fire_wetted_area"]}: {dialog.SelectedAreaM2:F3} m2";
        }
    }

    private void OpenThermalExpansionCalculator_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var dialog = new ThermalExpansionCalculatorWindow(
            vm.Language,
            vm.StandardBasis,
            vm.ThermalExpansionCoefficientPerC,
            vm.ThermalHeatInputKjPerHour,
            vm.LiquidDensityKgPerM3,
            vm.ThermalSpecificHeatKjPerKgC,
            vm.ReliefLoadKgPerHour)
        {
            Owner = Window.GetWindow(this)
        };

        bool? accepted = dialog.ShowDialog();
        if (accepted == true)
        {
            vm.ThermalExpansionCoefficientPerC = dialog.SelectedExpansionCoefficientPerC;
            vm.ThermalHeatInputKjPerHour = dialog.SelectedHeatInputKjPerHour;
            vm.LiquidDensityKgPerM3 = dialog.SelectedLiquidDensityKgPerM3;
            vm.ThermalSpecificHeatKjPerKgC = dialog.SelectedSpecificHeatKjPerKgC;
            vm.ReliefLoadKgPerHour = dialog.SelectedMassReliefLoadKgPerHour;
            vm.StatusMessage = $"{vm.Loc["relief_load"]}: {dialog.SelectedMassReliefLoadKgPerHour:F3} kg/h";
        }
    }
}
