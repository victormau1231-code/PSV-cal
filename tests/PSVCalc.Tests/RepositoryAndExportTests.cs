using PSVCalc.Core;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;
using System.Globalization;

namespace PSVCalc.Tests;

public sealed class RepositoryAndExportTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly StoragePaths _paths;
    private readonly JsonProjectRepository _repository;
    private readonly ExcelHtmlReportExporter _exporter;

    public RepositoryAndExportTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"psvcalc-tests-{Guid.NewGuid():N}");
        _paths = new StoragePaths(_tempRoot);
        _repository = new JsonProjectRepository(_paths);
        _exporter = new ExcelHtmlReportExporter(_paths);
    }

    [Fact]
    public void ProjectRoundTrip_ShouldPersistAndLoad()
    {
        ProjectRecord record = BuildSampleRecord();
        string savedPath = _repository.SaveProject(record, "roundtrip");
        ProjectRecord loaded = _repository.LoadProject(savedPath);

        Assert.Equal(record.CaseName, loaded.CaseName);
        Assert.Equal(record.Input.FluidType, loaded.Input.FluidType);
        Assert.NotNull(loaded.Result);
        Assert.Equal(record.Result!.OrificeRecommendation.Selected.Letter, loaded.Result!.OrificeRecommendation.Selected.Letter);
    }

    [Fact]
    public void HistoryIndex_ShouldStoreRecentEntries()
    {
        _repository.AddHistory(new HistoryEntry
        {
            Timestamp = DateTimeOffset.Now,
            ProjectId = "abc",
            CaseName = "HistoryCase",
            FluidType = FluidType.Gas,
            RequiredAreaMm2 = 321.0,
            SelectedOrifice = "G",
            ProjectFile = "dummy.json"
        });

        IReadOnlyList<HistoryEntry> history = _repository.LoadHistory(10);

        Assert.NotEmpty(history);
        Assert.Equal("HistoryCase", history[0].CaseName);
    }

    [Fact]
    public void SaveProject_ShouldStoreThroatDiameterInHistory_ForHgT()
    {
        ProjectRecord record = BuildSampleRecord();
        _repository.SaveProject(record, "hgt-history");

        HistoryEntry entry = Assert.Single(_repository.LoadHistory(10));
        double throatDiameterMm = Math.Sqrt(4.0 * record.Result!.RequiredAreaMm2 / Math.PI);
        string expected = string.Format(CultureInfo.InvariantCulture, "{0:F3} mm", throatDiameterMm);

        Assert.Equal(expected, entry.SelectedOrifice);
    }

    [Fact]
    public void ExcelExport_ShouldCreateXlsWithExpectedContent()
    {
        ProjectRecord record = BuildSampleRecord();
        string path = _exporter.Export(record, UiLanguage.EnUs, "export-check");
        string content = File.ReadAllText(path);

        Assert.True(File.Exists(path));
        Assert.Contains("Safety Valve Calculation Report", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(record.CaseName, content, StringComparison.Ordinal);
        Assert.Contains("Required Area", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Calculation Standard", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Atmospheric Pressure", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("0.098", content, StringComparison.Ordinal);
        Assert.Contains("Required Throat Diameter", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Selected Orifice", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Copyright \u00A9 2026 VictorMa", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcelExport_ShouldIncludeCapacityUsedPercent_ForApiReports()
    {
        ProjectRecord record = BuildApiSampleRecord();
        string path = _exporter.Export(record, UiLanguage.EnUs, "export-api-capacity");
        string content = File.ReadAllText(path);

        Assert.True(File.Exists(path));
        Assert.Contains("Capacity Used [%]", content, StringComparison.Ordinal);
    }

    private static ProjectRecord BuildSampleRecord()
    {
        var calc = new SafetyValveCalculator(new OrificeSelector());
        var input = new CalculationInput
        {
            CaseName = "RepoCase",
            StandardBasis = CalculationStandardBasis.HgT20570_2,
            FluidType = FluidType.Gas,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            AtmosphericPressure = 0.098,
            RelievingPressure = 1.2,
            BackPressure = 0.2,
            TemperatureC = 45,
            ReliefLoadKgPerHour = 1100,
            UseGasPreset = false,
            GasPresetId = "custom",
            MolecularWeight = 28.0,
            IsentropicExponentK = 1.35,
            CompressibilityFactorZ = 0.99
        };

        CalculationResult result = calc.Calculate(input);
        return new ProjectRecord
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            CaseName = input.CaseName,
            SavedAt = DateTimeOffset.Now,
            SoftwareVersion = "test",
            StandardVersion = CalculationStandardCatalog.GetDisplayName(input.StandardBasis),
            Language = UiLanguage.EnUs,
            Input = input,
            Result = result
        };
    }

    private static ProjectRecord BuildApiSampleRecord()
    {
        var calc = new SafetyValveCalculator(new OrificeSelector());
        var input = new CalculationInput
        {
            CaseName = "ApiRepoCase",
            StandardBasis = CalculationStandardBasis.Api520521Asme,
            FluidType = FluidType.Gas,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            AtmosphericPressure = 0.101325,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.101325,
            TemperatureC = 45,
            ReliefLoadKgPerHour = 1100,
            UseGasPreset = false,
            GasPresetId = "custom",
            MolecularWeight = 28.0,
            IsentropicExponentK = 1.35,
            CompressibilityFactorZ = 0.99
        };

        CalculationResult result = calc.Calculate(input);
        return new ProjectRecord
        {
            ProjectId = Guid.NewGuid().ToString("N"),
            CaseName = input.CaseName,
            SavedAt = DateTimeOffset.Now,
            SoftwareVersion = "test",
            StandardVersion = CalculationStandardCatalog.GetDisplayName(input.StandardBasis),
            Language = UiLanguage.EnUs,
            Input = input,
            Result = result
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
