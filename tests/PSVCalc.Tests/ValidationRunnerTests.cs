using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class ValidationRunnerTests
{
    [Fact]
    public void ValidationRunner_ShouldMarkPassAndFailCases()
    {
        IStandardProfileProvider profileProvider = new JsonStandardProfileProvider();
        ISafetyValveCalculator calculator = new SafetyValveCalculator(new OrificeSelector(), profileProvider);
        IValidationCaseRunner runner = new ValidationCaseRunner(calculator);

        var goodInput = new CalculationInput
        {
            CaseName = "GoodCase",
            FluidType = FluidType.Gas,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            RelievingPressure = 1.4,
            BackPressure = 0.3,
            TemperatureC = 40,
            ReliefLoadKgPerHour = 1800,
            UseGasPreset = true,
            GasPresetId = "air",
            DischargeCoefficientKd = 0.975,
            BackPressureCorrectionKb = 1.0,
            CombinationCorrectionKc = 1.0
        };

        CalculationResult baseline = calculator.Calculate(goodInput);

        var set = new ValidationCaseSet
        {
            Name = "RunnerTest",
            Cases =
            [
                new ValidationCase
                {
                    Id = "PASS-1",
                    Description = "should pass",
                    Input = goodInput,
                    ExpectedRequiredAreaMm2 = baseline.RequiredAreaMm2,
                    ExpectedOrificeLetter = baseline.OrificeRecommendation.Selected.Letter,
                    AllowedAreaDeviationPercent = 0.1
                },
                new ValidationCase
                {
                    Id = "FAIL-1",
                    Description = "should fail",
                    Input = goodInput,
                    ExpectedRequiredAreaMm2 = baseline.RequiredAreaMm2 * 0.5,
                    ExpectedOrificeLetter = "D",
                    AllowedAreaDeviationPercent = 0.1
                }
            ]
        };

        ValidationRunSummary summary = runner.Run(set);
        Assert.Equal(2, summary.Total);
        Assert.Equal(1, summary.Passed);
        Assert.Equal(1, summary.Failed);
    }
}

