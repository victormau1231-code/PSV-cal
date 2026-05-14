using System.IO;
using System.Reflection;
using PSVCalc.App.ViewModels;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.App.Tests;

public sealed class MainViewModelPersistenceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"psvcalc-app-tests-{Guid.NewGuid():N}");

    [Fact]
    public void BuildProjectRecord_ShouldRecalculateStaleInputs()
    {
        var repository = new FakeProjectRepository(_tempRoot);
        var viewModel = new MainViewModel(
            new SafetyValveCalculator(new OrificeSelector()),
            repository,
            new FakeExcelReportExporter(_tempRoot),
            new FakeValidationCaseStore(),
            new FakeValidationCaseRunner(),
            new StoragePaths(_tempRoot));

        viewModel.PressureInputMode = PressureInputMode.Absolute;
        viewModel.PressureUnit = PressureUnit.MPa;
        viewModel.SetPressure = 1.0;
        viewModel.RelievingPressure = 1.1;
        viewModel.BackPressure = 0.2;
        viewModel.UseGasPreset = false;
        viewModel.MolecularWeight = 28.0;
        viewModel.IsentropicExponentK = 1.4;
        viewModel.CompressibilityFactorZ = 1.0;
        viewModel.ReliefLoadKgPerHour = 1000.0;

        viewModel.CalculateCommand.Execute(null);
        double firstArea = viewModel.RequiredAreaMm2;

        viewModel.ReliefLoadKgPerHour = 2000.0;

        ProjectRecord record = InvokeBuildProjectRecord(viewModel);

        Assert.False(viewModel.IsCalculationStale);
        Assert.Equal(2000.0, record.Input.ReliefLoadKgPerHour);
        Assert.NotNull(record.Result);
        Assert.True(record.Result!.RequiredAreaMm2 > firstArea * 1.5);
    }

    private static ProjectRecord InvokeBuildProjectRecord(MainViewModel viewModel)
    {
        MethodInfo method = typeof(MainViewModel).GetMethod(
            "BuildProjectRecord",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildProjectRecord was not found.");

        return (ProjectRecord)(method.Invoke(viewModel, null)
            ?? throw new InvalidOperationException("BuildProjectRecord returned null."));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        private readonly List<HistoryEntry> _history = [];

        public FakeProjectRepository(string root)
        {
            ProjectsDirectory = Path.Combine(root, "Projects");
            HistoryDirectory = Path.Combine(root, "History");
            ExportsDirectory = Path.Combine(root, "Exports");
            Directory.CreateDirectory(ProjectsDirectory);
            Directory.CreateDirectory(HistoryDirectory);
            Directory.CreateDirectory(ExportsDirectory);
        }

        public string ProjectsDirectory { get; }

        public string HistoryDirectory { get; }

        public string ExportsDirectory { get; }

        public string SaveProject(ProjectRecord record, string? preferredFileName = null)
        {
            string path = Path.Combine(ProjectsDirectory, $"{preferredFileName ?? record.CaseName}.json");
            File.WriteAllText(path, record.CaseName);
            return path;
        }

        public ProjectRecord LoadProject(string filePath) => throw new NotSupportedException();

        public void AddHistory(HistoryEntry entry) => _history.Insert(0, entry);

        public IReadOnlyList<HistoryEntry> LoadHistory(int take = 50) => _history.Take(take).ToArray();
    }

    private sealed class FakeExcelReportExporter : IExcelReportExporter
    {
        private readonly string _root;

        public FakeExcelReportExporter(string root)
        {
            _root = root;
        }

        public string Export(ProjectRecord record, UiLanguage language, string? preferredFileName = null)
        {
            string path = Path.Combine(_root, $"{preferredFileName ?? record.CaseName}.xls");
            File.WriteAllText(path, record.CaseName);
            return path;
        }
    }

    private sealed class FakeValidationCaseStore : IValidationCaseStore
    {
        public ValidationCaseSet LoadFromFile(string path) => throw new NotSupportedException();

        public void SaveToFile(string path, ValidationCaseSet caseSet) => throw new NotSupportedException();

        public string EnsureTemplate(string destinationDirectory, string fileName = "onsite-cases.template.json") =>
            Path.Combine(destinationDirectory, fileName);
    }

    private sealed class FakeValidationCaseRunner : IValidationCaseRunner
    {
        public ValidationRunSummary Run(ValidationCaseSet caseSet) => new()
        {
            RunAt = DateTimeOffset.Now,
            SetName = caseSet.Name,
            StandardVersion = "test",
            Results = []
        };
    }
}
