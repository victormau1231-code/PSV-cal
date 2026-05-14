using PSVCalc.Core.Models;

namespace PSVCalc.Core.Interfaces;

public interface IValidationCaseRunner
{
    ValidationRunSummary Run(ValidationCaseSet caseSet);
}

