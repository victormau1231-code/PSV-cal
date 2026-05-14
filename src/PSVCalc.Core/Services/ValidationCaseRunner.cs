using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public sealed class ValidationCaseRunner : IValidationCaseRunner
{
    private readonly ISafetyValveCalculator _calculator;

    public ValidationCaseRunner(ISafetyValveCalculator calculator)
    {
        _calculator = calculator;
    }

    public ValidationRunSummary Run(ValidationCaseSet caseSet)
    {
        if (caseSet is null)
        {
            throw new ArgumentNullException(nameof(caseSet));
        }

        var results = new List<ValidationCaseResult>();
        foreach (ValidationCase validationCase in caseSet.Cases)
        {
            results.Add(RunSingle(validationCase));
        }

        return new ValidationRunSummary
        {
            RunAt = DateTimeOffset.Now,
            SetName = caseSet.Name,
            StandardVersion = caseSet.StandardVersion,
            Results = results
        };
    }

    private ValidationCaseResult RunSingle(ValidationCase validationCase)
    {
        try
        {
            CalculationResult result = _calculator.Calculate(validationCase.Input);
            double expectedArea = validationCase.ExpectedRequiredAreaMm2;
            double actualArea = result.RequiredAreaMm2;
            double deviationPercent = expectedArea <= 0
                ? 0
                : Math.Abs(actualArea - expectedArea) / expectedArea * 100.0;
            bool areaPassed = expectedArea <= 0 || deviationPercent <= validationCase.AllowedAreaDeviationPercent;
            bool orificePassed = string.IsNullOrWhiteSpace(validationCase.ExpectedOrificeLetter) ||
                                 string.Equals(validationCase.ExpectedOrificeLetter.Trim(),
                                     result.OrificeRecommendation.Selected.Letter,
                                     StringComparison.OrdinalIgnoreCase);

            return new ValidationCaseResult
            {
                CaseId = validationCase.Id,
                Description = validationCase.Description,
                Passed = areaPassed && orificePassed,
                ExpectedRequiredAreaMm2 = expectedArea,
                ActualRequiredAreaMm2 = actualArea,
                AreaDeviationPercent = deviationPercent,
                ExpectedOrificeLetter = validationCase.ExpectedOrificeLetter,
                ActualOrificeLetter = result.OrificeRecommendation.Selected.Letter
            };
        }
        catch (Exception ex)
        {
            return new ValidationCaseResult
            {
                CaseId = validationCase.Id,
                Description = validationCase.Description,
                Passed = false,
                ExpectedRequiredAreaMm2 = validationCase.ExpectedRequiredAreaMm2,
                ActualRequiredAreaMm2 = 0,
                AreaDeviationPercent = 100,
                ExpectedOrificeLetter = validationCase.ExpectedOrificeLetter,
                ActualOrificeLetter = string.Empty,
                ErrorMessage = ex.Message
            };
        }
    }
}

