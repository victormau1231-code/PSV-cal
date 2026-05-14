using PSVCalc.Core;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;
using PSVCalc.Core.Reporting;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class ReportDataBuilderTests
{
    [Fact]
    public void Build_ShouldExposeCapacityUsed_ForApiReport()
    {
        var builder = new SafetyValveReportDataBuilder();
        SafetyValveReportDocument document = builder.Build(BuildRecord(CalculationStandardBasis.Api520521Asme), UiLanguage.EnUs);

        Assert.Contains(document.ResultRows, row => row.Label == "Capacity Used [%]");
        Assert.Contains(document.SummaryCards.SelectMany(card => card.Details), row => row.Label == "Capacity Used [%]");
    }

    [Fact]
    public void Build_ShouldExposeThroatDiameter_ForHgTReport()
    {
        var builder = new SafetyValveReportDataBuilder();
        SafetyValveReportDocument document = builder.Build(BuildRecord(CalculationStandardBasis.HgT20570_2), UiLanguage.EnUs);

        Assert.Contains(document.ResultRows, row => row.Label == "Required Throat Diameter");
        Assert.DoesNotContain(document.ResultRows, row => row.Label == "Recommended Orifice");
    }

    private static ProjectRecord BuildRecord(CalculationStandardBasis standardBasis)
    {
        var input = new CalculationInput
        {
            CaseName = "ReportBuilderCase",
            StandardBasis = standardBasis,
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

        var calculator = new SafetyValveCalculator(new OrificeSelector());
        CalculationResult result = calculator.Calculate(input);
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
}
