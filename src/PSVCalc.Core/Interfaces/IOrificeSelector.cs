using PSVCalc.Core.Models;

namespace PSVCalc.Core.Interfaces;

public interface IOrificeSelector
{
    OrificeRecommendation Recommend(double requiredAreaMm2);
    IReadOnlyList<OrificeDefinition> GetAll();
}

