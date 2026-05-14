using PSVCalc.Core.Models;

namespace PSVCalc.Core.Interfaces;

public interface ISafetyValveCalculator
{
    CalculationResult Calculate(CalculationInput input);
}

