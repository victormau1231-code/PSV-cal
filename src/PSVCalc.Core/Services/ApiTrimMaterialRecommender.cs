using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public static class ApiTrimMaterialRecommender
{
    private const string ApiBasis =
        "API trim screening basis: API 526 material-class selection context, API 527 seat-tightness review context, and API 520/521 service-condition review. Final trim selection shall be confirmed with valve vendor and project corrosion/materials engineering.";

    public static TrimMaterialRecommendation Recommend(CalculationInput input)
    {
        var notes = new List<string>
        {
            "Confirm the final valve trim material with certified vendor data, project material class, corrosion allowance philosophy, and seat-tightness requirement before issue."
        };

        MaterialServiceCondition effectiveCondition = ResolveEffectiveCondition(input, notes);

        string seatMaterial;
        string discMaterial;
        string serviceBasis;

        switch (effectiveCondition)
        {
            case MaterialServiceCondition.SourNace:
                seatMaterial = "NACE-compliant 316 SS / Alloy 625 trim, vendor-qualified for sour service";
                discMaterial = "NACE-compliant 316 SS / Alloy 625 trim, hardness-qualified for sour service";
                serviceBasis = "Sour / H2S service";
                notes.Add("Review H2S partial pressure, chloride content, pH, temperature, hardness limits, and NACE MR0175/ISO 15156 applicability.");
                break;

            case MaterialServiceCondition.ChlorideSeaWater:
                seatMaterial = "Duplex 2205 or Alloy 625 trim, project corrosion-class dependent";
                discMaterial = "Duplex 2205 or Alloy 625 trim, project corrosion-class dependent";
                serviceBasis = "Chloride / seawater service";
                notes.Add("Review chloride stress-corrosion cracking risk against chloride level, temperature, oxygen content, and project metallurgy specification.");
                break;

            case MaterialServiceCondition.DirtyAbrasiveTwoPhase:
                seatMaterial = "Stellite hard-faced 316 SS seat or vendor equivalent";
                discMaterial = "Stellite hard-faced 316 SS disc or vendor equivalent";
                serviceBasis = "Dirty, erosive, flashing, or two-phase service";
                notes.Add("Two-phase, flashing, or particulate service can erode seating surfaces; review hard-facing, blowdown, and certified capacity with the valve vendor.");
                break;

            case MaterialServiceCondition.SteamHighTemperature:
                seatMaterial = "Stellite hard-faced 316 SS seat or vendor high-temperature trim equivalent";
                discMaterial = "Stellite hard-faced 316 SS disc or vendor high-temperature trim equivalent";
                serviceBasis = "Steam / high-temperature service";
                notes.Add("Review relieving temperature, thermal cycling, leakage class, and seat/disc hard-facing limits against vendor data.");
                break;

            default:
                seatMaterial = "316 SS seat";
                discMaterial = "316 SS disc";
                serviceBasis = "Clean, non-corrosive service";
                break;
        }

        return new TrimMaterialRecommendation
        {
            SeatMaterial = seatMaterial,
            DiscMaterial = discMaterial,
            ServiceBasis = serviceBasis,
            Basis = ApiBasis,
            ReviewNotes = notes
        };
    }

    private static MaterialServiceCondition ResolveEffectiveCondition(CalculationInput input, ICollection<string> notes)
    {
        if (input.MaterialServiceCondition is MaterialServiceCondition.SourNace or MaterialServiceCondition.ChlorideSeaWater)
        {
            return input.MaterialServiceCondition;
        }

        if (input.FluidType is FluidType.TwoPhaseEquilibrium or FluidType.TwoPhaseSubcooled ||
            input.ReliefScenario == ReliefScenario.TubeRupture ||
            input.MaterialServiceCondition == MaterialServiceCondition.DirtyAbrasiveTwoPhase)
        {
            if (input.MaterialServiceCondition != MaterialServiceCondition.DirtyAbrasiveTwoPhase)
            {
                notes.Add("Material service condition was elevated by the selected relief regime because two-phase, flashing, or tube-rupture flow is erosive by screening.");
            }

            return MaterialServiceCondition.DirtyAbrasiveTwoPhase;
        }

        if (input.FluidType == FluidType.Steam ||
            input.TemperatureC >= 260.0 ||
            input.ReliefScenario == ReliefScenario.Fire ||
            input.MaterialServiceCondition == MaterialServiceCondition.SteamHighTemperature)
        {
            if (input.MaterialServiceCondition != MaterialServiceCondition.SteamHighTemperature)
            {
                notes.Add("Material service condition was elevated by steam, fire, or high relieving temperature screening.");
            }

            return MaterialServiceCondition.SteamHighTemperature;
        }

        return MaterialServiceCondition.CleanNonCorrosive;
    }
}
