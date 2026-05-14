using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class ValidationCaseStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IValidationCaseStore _store = new JsonValidationCaseStore();

    public ValidationCaseStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"psvcalc-validation-{Guid.NewGuid():N}");
    }

    [Fact]
    public void EnsureTemplate_ShouldCreateTemplateJson()
    {
        string path = _store.EnsureTemplate(_tempDir);
        Assert.True(File.Exists(path));
        string content = File.ReadAllText(path);
        Assert.Contains("SITE-001", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveAndLoad_ShouldRoundTripCaseSet()
    {
        var set = new ValidationCaseSet
        {
            Name = "RoundTrip",
            Cases =
            [
                new ValidationCase
                {
                    Id = "R1",
                    Description = "Round trip test",
                    Input = new CalculationInput
                    {
                        CaseName = "R1",
                        FluidType = FluidType.Gas,
                        PressureInputMode = PressureInputMode.Gauge,
                        PressureUnit = PressureUnit.MPa,
                        RelievingPressure = 1.3,
                        BackPressure = 0.2,
                        TemperatureC = 35,
                        ReliefLoadKgPerHour = 1500
                    },
                    ExpectedRequiredAreaMm2 = 123.4,
                    ExpectedOrificeLetter = "G",
                    AllowedAreaDeviationPercent = 3.0
                }
            ]
        };

        string path = Path.Combine(_tempDir, "roundtrip.json");
        _store.SaveToFile(path, set);
        ValidationCaseSet loaded = _store.LoadFromFile(path);

        Assert.Equal(set.Name, loaded.Name);
        Assert.Single(loaded.Cases);
        Assert.Equal("R1", loaded.Cases[0].Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}

