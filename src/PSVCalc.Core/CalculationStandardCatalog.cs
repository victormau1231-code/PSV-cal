using PSVCalc.Core.Enums;

namespace PSVCalc.Core;

public static class CalculationStandardCatalog
{
    public const string ApiProfileId = "api520-521-asme-2026-04-01";
    public const string HgTProfileId = "hg-t-20570.2-1995";

    public static string GetProfileId(CalculationStandardBasis basis)
    {
        return basis switch
        {
            CalculationStandardBasis.HgT20570_2 => HgTProfileId,
            _ => ApiProfileId
        };
    }

    public static string GetDisplayName(CalculationStandardBasis basis)
    {
        return basis switch
        {
            CalculationStandardBasis.HgT20570_2 => "HG/T 20570.2-1995",
            _ => "API 520/521 + ASME (frozen 2026-04-01)"
        };
    }
}
