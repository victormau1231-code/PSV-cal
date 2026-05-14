namespace PSVCalc.Core.Services;

internal sealed class TwoPhaseOmegaCalculator
{
    public TwoPhaseOmegaResult ComputeGeneral(
        double p1AbsPa,
        double p2AbsPa,
        double inletSpecificVolumeM3PerKg,
        double referenceSpecificVolumeM3PerKg)
    {
        ValidatePositive(p1AbsPa, nameof(p1AbsPa));
        ValidatePositive(p2AbsPa, nameof(p2AbsPa));
        ValidatePositive(inletSpecificVolumeM3PerKg, nameof(inletSpecificVolumeM3PerKg));
        ValidatePositive(referenceSpecificVolumeM3PerKg, nameof(referenceSpecificVolumeM3PerKg));

        double omega = 9.0 * ((referenceSpecificVolumeM3PerKg / inletSpecificVolumeM3PerKg) - 1.0);
        if (omega <= 0)
        {
            throw new ArgumentException("Two-phase omega must be positive; check the specific-volume inputs.");
        }

        double actualRatio = p2AbsPa / p1AbsPa;
        double criticalRatio = SolveBracketed(
            eta => (eta * eta)
                   + ((omega * omega) - (2.0 * omega)) * Math.Pow(1.0 - eta, 2.0)
                   + (2.0 * omega * omega * Math.Log(eta))
                   + (2.0 * omega * omega * (1.0 - eta)),
            min: 1e-6,
            max: 0.999999,
            fallback: EstimateGeneralCriticalRatio(omega));

        bool isCritical = criticalRatio >= actualRatio;
        double massFlux = isCritical
            ? criticalRatio * Math.Sqrt(p1AbsPa / (inletSpecificVolumeM3PerKg * omega))
            : Math.Sqrt(
                (-2.0 * (omega * Math.Log(actualRatio) + (omega - 1.0) * (1.0 - actualRatio)))
                / (omega * ((1.0 / actualRatio) - 1.0) + 1.0))
              * Math.Sqrt(p1AbsPa / inletSpecificVolumeM3PerKg);

        return new TwoPhaseOmegaResult(
            MassFluxKgPerM2S: massFlux,
            CriticalPressureRatio: criticalRatio,
            PressureRatio: actualRatio,
            IsCritical: isCritical,
            Omega: omega,
            SaturationPressureAbsPa: 0.0,
            SaturationPressureRatio: 0.0,
            TransitionSaturationPressureRatio: 0.0,
            InletSpecificVolumeM3PerKg: inletSpecificVolumeM3PerKg,
            ReferenceSpecificVolumeM3PerKg: referenceSpecificVolumeM3PerKg,
            ReferenceDensityKgPerM3: 0.0,
            EquationBranch: isCritical ? "TwoPhaseOmegaCritical" : "TwoPhaseOmegaSubcritical");
    }

    public TwoPhaseOmegaResult ComputeSubcooled(
        double p1AbsPa,
        double p2AbsPa,
        double inletDensityKgPerM3,
        double densityAtNinetyPercentSaturationPressureKgPerM3,
        double saturationPressureAbsPa)
    {
        ValidatePositive(p1AbsPa, nameof(p1AbsPa));
        ValidatePositive(p2AbsPa, nameof(p2AbsPa));
        ValidatePositive(inletDensityKgPerM3, nameof(inletDensityKgPerM3));
        ValidatePositive(densityAtNinetyPercentSaturationPressureKgPerM3, nameof(densityAtNinetyPercentSaturationPressureKgPerM3));
        ValidatePositive(saturationPressureAbsPa, nameof(saturationPressureAbsPa));

        double omegaS = 9.0 * ((inletDensityKgPerM3 / densityAtNinetyPercentSaturationPressureKgPerM3) - 1.0);
        if (omegaS <= 0)
        {
            throw new ArgumentException("Subcooled two-phase omega must be positive; check the density inputs.");
        }

        double etaS = saturationPressureAbsPa / p1AbsPa;
        if (etaS <= 0.0 || etaS > 1.0 + 1e-9)
        {
            throw new ArgumentException("Saturation pressure must be positive and not exceed the relieving pressure in the subcooled two-phase branch.");
        }

        double etaA = p2AbsPa / p1AbsPa;
        double etaSt = (2.0 * omegaS) / (1.0 + 2.0 * omegaS);
        bool isLowSubcooling = saturationPressureAbsPa >= (etaSt * p1AbsPa);

        double criticalRatio = etaS <= etaSt
            ? etaS
            : SolveBracketed(
                etaC => (((omegaS + etaS - 1.0) / (2.0 * etaS)) * etaC * etaC)
                        - (2.0 * (omegaS - 1.0) * etaC)
                        + (omegaS * etaS * Math.Log(etaC / etaS))
                        + (1.5 * omegaS * etaS)
                        - 1.0,
                min: Math.Max(etaS + 1e-6, 1e-6),
                max: 0.999999,
                fallback: etaS);

        bool isCritical;
        double massFlux;
        string equationBranch;

        if (isLowSubcooling)
        {
            isCritical = criticalRatio >= etaA;
            double eta = isCritical ? criticalRatio : etaA;
            double numerator = (2.0 * (1.0 - etaS))
                               + (2.0 * omegaS * etaS * Math.Log(etaS / eta))
                               + ((omegaS - 1.0) * Math.Pow(etaS - eta, 2.0));
            double denominator = Math.Pow((omegaS * ((etaS / eta) - 1.0)) + 1.0, 2.0);
            massFlux = Math.Sqrt((numerator / denominator) * p1AbsPa * inletDensityKgPerM3);
            equationBranch = isCritical
                ? "TwoPhaseSubcooledLowCritical"
                : "TwoPhaseSubcooledLowSubcritical";
        }
        else
        {
            isCritical = saturationPressureAbsPa >= p2AbsPa;
            double limitingPressure = isCritical ? saturationPressureAbsPa : p2AbsPa;
            massFlux = 1.414 * Math.Sqrt(inletDensityKgPerM3 * (p1AbsPa - limitingPressure));
            equationBranch = isCritical
                ? "TwoPhaseSubcooledHighCritical"
                : "TwoPhaseSubcooledAllLiquid";
        }

        return new TwoPhaseOmegaResult(
            MassFluxKgPerM2S: massFlux,
            CriticalPressureRatio: criticalRatio,
            PressureRatio: etaA,
            IsCritical: isCritical,
            Omega: omegaS,
            SaturationPressureAbsPa: saturationPressureAbsPa,
            SaturationPressureRatio: etaS,
            TransitionSaturationPressureRatio: etaSt,
            InletSpecificVolumeM3PerKg: 1.0 / inletDensityKgPerM3,
            ReferenceSpecificVolumeM3PerKg: 0.0,
            ReferenceDensityKgPerM3: densityAtNinetyPercentSaturationPressureKgPerM3,
            EquationBranch: equationBranch);
    }

    private static double EstimateGeneralCriticalRatio(double omega)
    {
        double baseTerm = 1.0446 - 0.0093431 * Math.Sqrt(omega) - 0.56256 * omega;
        baseTerm = Math.Max(baseTerm, 1e-6);
        double exponent = -0.70355 - (0.014685 * Math.Log(omega));
        return 1.0 / (1.0 + Math.Pow(baseTerm, exponent));
    }

    private static double SolveBracketed(
        Func<double, double> function,
        double min,
        double max,
        double fallback)
    {
        const int segments = 4000;
        double previousX = min;
        double previousY = function(previousX);

        for (int i = 1; i <= segments; i++)
        {
            double x = min + ((max - min) * i / segments);
            double y = function(x);
            if (!double.IsNaN(previousY) && !double.IsNaN(y) && Math.Sign(previousY) != Math.Sign(y))
            {
                return Bisection(function, previousX, x);
            }

            previousX = x;
            previousY = y;
        }

        return fallback;
    }

    private static double Bisection(Func<double, double> function, double lower, double upper)
    {
        double fLower = function(lower);

        for (int i = 0; i < 80; i++)
        {
            double middle = (lower + upper) / 2.0;
            double fMiddle = function(middle);

            if (Math.Abs(fMiddle) < 1e-10)
            {
                return middle;
            }

            if (Math.Sign(fLower) == Math.Sign(fMiddle))
            {
                lower = middle;
                fLower = fMiddle;
            }
            else
            {
                upper = middle;
            }
        }

        return (lower + upper) / 2.0;
    }

    private static void ValidatePositive(double value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"{name} must be greater than zero.");
        }
    }
}

internal sealed record TwoPhaseOmegaResult(
    double MassFluxKgPerM2S,
    double CriticalPressureRatio,
    double PressureRatio,
    bool IsCritical,
    double Omega,
    double SaturationPressureAbsPa,
    double SaturationPressureRatio,
    double TransitionSaturationPressureRatio,
    double InletSpecificVolumeM3PerKg,
    double ReferenceSpecificVolumeM3PerKg,
    double ReferenceDensityKgPerM3,
    string EquationBranch);
