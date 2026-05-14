using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PSVCalc.Core.Models;
using PSVCalc.Core.Reporting;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace PSVCalc.App.ViewModels;

public partial class MainViewModel
{
    private readonly ValidationHtmlReportRenderer _validationHtmlReportRenderer = new();

    [RelayCommand]
    private void ExportValidationReport()
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Title = Loc["validation_report"],
                Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*",
                InitialDirectory = _projectRepository.ExportsDirectory,
                FileName = $"validation-report-{DateTime.Now:yyyyMMdd-HHmmss}.html",
                AddExtension = true,
                DefaultExt = ".html",
                OverwritePrompt = true
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            string caseFilePath = _validationCaseStore.EnsureTemplate(_storagePaths.ValidationDirectory);
            ValidationCaseSet caseSet = _validationCaseStore.LoadFromFile(caseFilePath);
            ValidationRunSummary summary = _validationCaseRunner.Run(caseSet);

            string outputPath = saveDialog.FileName;
            _validationHtmlReportRenderer.WriteToFile(outputPath, summary, key => Loc[key]);

            string summaryText = $"{Loc["validation_report"]}: {summary.Passed}/{summary.Total} {Loc["validation_status_passed"]}. File: {outputPath}";
            StatusMessage = summaryText;

            MessageBox.Show(
                summaryText,
                Loc["validation_report"],
                MessageBoxButton.OK,
                summary.Failed == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Loc["validation_report"]} failed: {ex.Message}";
            MessageBox.Show(ex.Message, Loc["validation_report"], MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
