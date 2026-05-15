using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class TrimMaterialRecommendationTests
{
    [Fact]
    public void Recommend_ShouldUseHardFacedTrim_ForTwoPhaseOrAbrasiveService()
    {
        var input = BuildBaseInput();
        input.FluidType = FluidType.TwoPhaseEquilibrium;
        input.MaterialServiceCondition = MaterialServiceCondition.CleanNonCorrosive;

        TrimMaterialRecommendation recommendation = ApiTrimMaterialRecommender.Recommend(input);

        Assert.Contains("Stellite", recommendation.SeatMaterial, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Stellite", recommendation.DiscMaterial, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("API 526", recommendation.Basis, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(recommendation.ReviewNotes, note => note.Contains("two-phase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Recommend_ShouldFlagNaceReview_ForSourService()
    {
        var input = BuildBaseInput();
        input.MaterialServiceCondition = MaterialServiceCondition.SourNace;

        TrimMaterialRecommendation recommendation = ApiTrimMaterialRecommender.Recommend(input);

        Assert.Contains("NACE", recommendation.SeatMaterial, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NACE", recommendation.DiscMaterial, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(recommendation.ReviewNotes, note => note.Contains("H2S", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_ShouldAttachTrimMaterialRecommendation()
    {
        var calculator = new SafetyValveCalculator(new OrificeSelector());
        var input = BuildBaseInput();
        input.MaterialServiceCondition = MaterialServiceCondition.SteamHighTemperature;
        input.FluidType = FluidType.Steam;
        input.TemperatureC = 320.0;

        CalculationResult result = calculator.Calculate(input);

        Assert.NotNull(result.TrimMaterialRecommendation);
        Assert.Contains("hard-faced", result.TrimMaterialRecommendation.SeatMaterial, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(result.TrimMaterialRecommendation.ReviewNotes, note => note.Contains("vendor", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_ShouldNotPromoteCleanTrimReviewToWarning()
    {
        var calculator = new SafetyValveCalculator(new OrificeSelector());
        var input = BuildBaseInput();

        CalculationResult result = calculator.Calculate(input);

        Assert.DoesNotContain(result.Warnings, warning => warning.Contains("Trim material review", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_ShouldPromoteSourTrimReviewToWarning()
    {
        var calculator = new SafetyValveCalculator(new OrificeSelector());
        var input = BuildBaseInput();
        input.MaterialServiceCondition = MaterialServiceCondition.SourNace;

        CalculationResult result = calculator.Calculate(input);

        Assert.Contains(result.Warnings, warning => warning.Contains("H2S", StringComparison.OrdinalIgnoreCase));
    }

    private static CalculationInput BuildBaseInput()
    {
        return new CalculationInput
        {
            CaseName = "Trim-Material",
            StandardBasis = CalculationStandardBasis.Api520521Asme,
            FluidType = FluidType.Gas,
            ReliefScenario = ReliefScenario.Overpressure,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.2,
            TemperatureC = 45,
            ReliefLoadKgPerHour = 1100,
            UseGasPreset = false,
            GasPresetId = "custom",
            MolecularWeight = 28.0,
            IsentropicExponentK = 1.35,
            CompressibilityFactorZ = 0.99,
            TwoPhaseInletSpecificVolumeM3PerKg = 0.02,
            TwoPhaseSpecificVolumeAtNinetyPercentPressureM3PerKg = 0.024
        };
    }
}
