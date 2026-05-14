using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public sealed class WettedAreaCalculator
{
    private const int IntegrationSteps = 512;

    public WettedAreaResult Calculate(WettedAreaInput input)
    {
        Validate(input);

        double radius = input.DiameterM / 2.0;
        double headDepth = GetHeadDepth(input.HeadType, radius);
        double totalHeight = input.Orientation == VesselOrientation.Horizontal
            ? input.DiameterM
            : input.StraightLengthM + (2.0 * headDepth);

        double gradeHeightCap = Math.Max(0.0, input.GradeHeightLimitM - input.BottomElevationFromGradeM);
        double uncappedHeight = Math.Min(input.LiquidLevelM, totalHeight);
        double effectiveHeight = Math.Max(0.0, Math.Min(uncappedHeight, gradeHeightCap));
        bool wasLimitedByGrade = effectiveHeight + 1e-9 < uncappedHeight;

        (double shellArea, double headArea) = input.Orientation == VesselOrientation.Horizontal
            ? CalculateHorizontal(input.HeadType, radius, input.StraightLengthM, effectiveHeight)
            : CalculateVertical(input.HeadType, radius, input.StraightLengthM, effectiveHeight);

        return new WettedAreaResult
        {
            EffectiveHeightM = effectiveHeight,
            GradeHeightCapM = gradeHeightCap,
            HeadDepthM = headDepth,
            ShellWettedAreaM2 = shellArea,
            HeadWettedAreaM2 = headArea,
            WasLimitedByGradeHeight = wasLimitedByGrade
        };
    }

    private static (double ShellArea, double HeadArea) CalculateHorizontal(
        WettedAreaHeadType headType,
        double radius,
        double straightLengthM,
        double fillHeightM)
    {
        double shellArea = GetCircularArcLength(radius, fillHeightM) * straightLengthM;
        double headAreaPerSide = GetHorizontalHeadArea(headType, radius, fillHeightM);
        return (shellArea, 2.0 * headAreaPerSide);
    }

    private static (double ShellArea, double HeadArea) CalculateVertical(
        WettedAreaHeadType headType,
        double radius,
        double straightLengthM,
        double fillHeightM)
    {
        if (headType == WettedAreaHeadType.Flat)
        {
            double flatBottomHeadArea = fillHeightM > 0.0 ? Math.PI * radius * radius : 0.0;
            double shellHeight = Math.Min(fillHeightM, straightLengthM);
            double shellArea = shellHeight * 2.0 * Math.PI * radius;
            double flatTopHeadArea = fillHeightM >= straightLengthM ? Math.PI * radius * radius : 0.0;
            return (shellArea, flatBottomHeadArea + flatTopHeadArea);
        }

        double headDepth = GetHeadDepth(headType, radius);
        double bottomHeadFill = Math.Min(fillHeightM, headDepth);
        double shellFill = Math.Min(Math.Max(fillHeightM - headDepth, 0.0), straightLengthM);
        double topHeadFill = Math.Min(Math.Max(fillHeightM - headDepth - straightLengthM, 0.0), headDepth);

        double bottomHeadArea = GetHeadAreaFromPole(headType, radius, bottomHeadFill);
        double shellAreaVertical = shellFill * 2.0 * Math.PI * radius;
        double topHeadAreaVertical = GetHeadAreaFromTangent(headType, radius, topHeadFill);

        return (shellAreaVertical, bottomHeadArea + topHeadAreaVertical);
    }

    private static double GetHorizontalHeadArea(
        WettedAreaHeadType headType,
        double radius,
        double fillHeightM)
    {
        if (headType == WettedAreaHeadType.Flat)
        {
            return GetCircularSegmentArea(radius, fillHeightM);
        }

        double headDepth = GetHeadDepth(headType, radius);
        return IntegrateSimpson(
            theta =>
            {
                double localRadius = radius * Math.Cos(theta);
                if (localRadius <= 1e-10)
                {
                    return 0.0;
                }

                double ds = Math.Sqrt(
                    (radius * radius * Math.Sin(theta) * Math.Sin(theta)) +
                    (headDepth * headDepth * Math.Cos(theta) * Math.Cos(theta)));
                double wettedAngle = GetWettedAngle(localRadius, radius, fillHeightM);
                return wettedAngle * localRadius * ds;
            },
            0.0,
            Math.PI / 2.0,
            IntegrationSteps);
    }

    private static double GetHeadAreaFromPole(
        WettedAreaHeadType headType,
        double radius,
        double depthFromPoleM)
    {
        if (headType == WettedAreaHeadType.Flat)
        {
            return depthFromPoleM > 0.0 ? Math.PI * radius * radius : 0.0;
        }

        double headDepth = GetHeadDepth(headType, radius);
        double clampedDepth = Clamp(depthFromPoleM, 0.0, headDepth);
        if (clampedDepth <= 0.0)
        {
            return 0.0;
        }

        if (clampedDepth >= headDepth)
        {
            return GetFullHeadArea(headType, radius);
        }

        return GetFullHeadArea(headType, radius) - GetHeadAreaFromTangent(headType, radius, headDepth - clampedDepth);
    }

    private static double GetHeadAreaFromTangent(
        WettedAreaHeadType headType,
        double radius,
        double depthFromTangentM)
    {
        if (headType == WettedAreaHeadType.Flat)
        {
            return depthFromTangentM > 0.0 ? Math.PI * radius * radius : 0.0;
        }

        double headDepth = GetHeadDepth(headType, radius);
        double clampedDepth = Clamp(depthFromTangentM, 0.0, headDepth);
        if (clampedDepth <= 0.0)
        {
            return 0.0;
        }

        double thetaMax = Math.Asin(clampedDepth / headDepth);
        return IntegrateSimpson(
            theta =>
            {
                double localRadius = radius * Math.Cos(theta);
                double ds = Math.Sqrt(
                    (radius * radius * Math.Sin(theta) * Math.Sin(theta)) +
                    (headDepth * headDepth * Math.Cos(theta) * Math.Cos(theta)));
                return 2.0 * Math.PI * localRadius * ds;
            },
            0.0,
            thetaMax,
            IntegrationSteps);
    }

    private static double GetFullHeadArea(WettedAreaHeadType headType, double radius)
    {
        if (headType == WettedAreaHeadType.Flat)
        {
            return Math.PI * radius * radius;
        }

        return GetHeadAreaFromTangent(headType, radius, GetHeadDepth(headType, radius));
    }

    private static double GetHeadDepth(WettedAreaHeadType headType, double radius)
    {
        return headType switch
        {
            WettedAreaHeadType.Flat => 0.0,
            WettedAreaHeadType.EllipsoidalTwoToOne => radius / 2.0,
            WettedAreaHeadType.Hemispherical => radius,
            _ => radius / 2.0
        };
    }

    private static double GetWettedAngle(double localRadius, double vesselRadius, double fillHeightM)
    {
        double localFill = fillHeightM - (vesselRadius - localRadius);
        if (localFill <= 0.0)
        {
            return 0.0;
        }

        if (localFill >= 2.0 * localRadius)
        {
            return 2.0 * Math.PI;
        }

        double ratio = Clamp((localRadius - localFill) / localRadius, -1.0, 1.0);
        return 2.0 * Math.Acos(ratio);
    }

    private static double GetCircularArcLength(double radius, double fillHeightM)
    {
        if (fillHeightM <= 0.0)
        {
            return 0.0;
        }

        if (fillHeightM >= 2.0 * radius)
        {
            return 2.0 * Math.PI * radius;
        }

        double ratio = Clamp((radius - fillHeightM) / radius, -1.0, 1.0);
        return radius * 2.0 * Math.Acos(ratio);
    }

    private static double GetCircularSegmentArea(double radius, double fillHeightM)
    {
        if (fillHeightM <= 0.0)
        {
            return 0.0;
        }

        if (fillHeightM >= 2.0 * radius)
        {
            return Math.PI * radius * radius;
        }

        double x = radius - fillHeightM;
        return (radius * radius * Math.Acos(Clamp(x / radius, -1.0, 1.0))) -
               (x * Math.Sqrt(Math.Max(0.0, (radius * radius) - (x * x))));
    }

    private static double IntegrateSimpson(Func<double, double> function, double lowerBound, double upperBound, int steps)
    {
        int slices = steps % 2 == 0 ? steps : steps + 1;
        double step = (upperBound - lowerBound) / slices;
        double sum = function(lowerBound) + function(upperBound);

        for (int i = 1; i < slices; i++)
        {
            double x = lowerBound + (i * step);
            sum += (i % 2 == 0 ? 2.0 : 4.0) * function(x);
        }

        return sum * step / 3.0;
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private static void Validate(WettedAreaInput input)
    {
        if (input.DiameterM <= 0.0)
        {
            throw new ArgumentException("Vessel diameter must be greater than zero.", nameof(input));
        }

        if (input.StraightLengthM <= 0.0)
        {
            throw new ArgumentException("Straight shell length must be greater than zero.", nameof(input));
        }

        if (input.LiquidLevelM < 0.0)
        {
            throw new ArgumentException("Normal liquid level cannot be negative.", nameof(input));
        }

        if (input.BottomElevationFromGradeM < 0.0)
        {
            throw new ArgumentException("Bottom elevation from grade cannot be negative.", nameof(input));
        }

        if (input.GradeHeightLimitM <= 0.0)
        {
            throw new ArgumentException("Grade height limit must be greater than zero.", nameof(input));
        }
    }
}
