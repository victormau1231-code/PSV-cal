using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public sealed class OrificeSelector : IOrificeSelector
{
    private const double MaximumRecommendedUtilization = 0.90;
    private static readonly IReadOnlyList<OrificeDefinition> ApiOrifices = new List<OrificeDefinition>
    {
        Create("D", 0.110, 1, 2),
        Create("E", 0.196, 1, 2),
        Create("F", 0.307, 1, 2),
        Create("G", 0.503, 2, 3),
        Create("H", 0.785, 2, 3),
        Create("J", 1.287, 2, 3),
        Create("K", 1.838, 3, 4),
        Create("L", 2.853, 3, 4),
        Create("M", 3.600, 3, 4),
        Create("N", 4.340, 4, 6),
        Create("P", 6.380, 4, 6),
        Create("Q", 11.050, 6, 8),
        Create("R", 16.000, 6, 8),
        Create("T", 26.000, 8, 10)
    };

    public OrificeRecommendation Recommend(double requiredAreaMm2)
    {
        if (requiredAreaMm2 <= 0)
        {
            throw new ArgumentException("Required area must be greater than zero.", nameof(requiredAreaMm2));
        }

        double minimumRecommendedAreaMm2 = requiredAreaMm2 / MaximumRecommendedUtilization;
        int directIdx = ApiOrifices.ToList().FindIndex(o => o.AreaMm2 >= requiredAreaMm2);
        int idx = ApiOrifices.ToList().FindIndex(o => o.AreaMm2 >= minimumRecommendedAreaMm2);
        bool exceeded = idx < 0;
        if (idx < 0)
        {
            idx = ApiOrifices.Count - 1;
        }

        var selected = ApiOrifices[idx];
        bool wasUpsizedForMargin = directIdx >= 0 && idx > directIdx;
        var candidates = new List<OrificeDefinition>();
        if (idx > 0)
        {
            candidates.Add(ApiOrifices[idx - 1]);
        }

        candidates.Add(selected);

        if (idx < ApiOrifices.Count - 1)
        {
            candidates.Add(ApiOrifices[idx + 1]);
        }

        return new OrificeRecommendation
        {
            RequiredAreaMm2 = requiredAreaMm2,
            MinimumRecommendedAreaMm2 = minimumRecommendedAreaMm2,
            MaximumRecommendedUtilizationPercent = MaximumRecommendedUtilization * 100.0,
            Selected = selected,
            CandidateNeighbors = candidates,
            IsCapacityExceededByLargestOrifice = exceeded,
            WasUpsizedForMargin = wasUpsizedForMargin,
            DirectAreaQualifiedLetter = directIdx >= 0 ? ApiOrifices[directIdx].Letter : null
        };
    }

    public IReadOnlyList<OrificeDefinition> GetAll() => ApiOrifices;

    private static OrificeDefinition Create(string letter, double areaIn2, int inletNominalInch, int outletNominalInch)
    {
        return new OrificeDefinition
        {
            Letter = letter,
            AreaIn2 = areaIn2,
            AreaMm2 = areaIn2 * 645.16,
            InletNominalInch = inletNominalInch,
            OutletNominalInch = outletNominalInch
        };
    }
}
