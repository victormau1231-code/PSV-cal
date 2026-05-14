using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class WettedAreaCalculatorTests
{
    private readonly WettedAreaCalculator _calculator = new();

    [Fact]
    public void HorizontalFlatHeadFullLiquid_ShouldMatchClosedFormArea()
    {
        var input = new WettedAreaInput
        {
            Orientation = VesselOrientation.Horizontal,
            HeadType = WettedAreaHeadType.Flat,
            DiameterM = 2.0,
            StraightLengthM = 4.0,
            LiquidLevelM = 2.0,
            BottomElevationFromGradeM = 0.0
        };

        WettedAreaResult result = _calculator.Calculate(input);

        double expectedShellArea = 2.0 * Math.PI * 1.0 * 4.0;
        double expectedHeadArea = 2.0 * Math.PI * 1.0 * 1.0;

        Assert.Equal(expectedShellArea, result.ShellWettedAreaM2, 6);
        Assert.Equal(expectedHeadArea, result.HeadWettedAreaM2, 6);
        Assert.Equal(expectedShellArea + expectedHeadArea, result.TotalWettedAreaM2, 6);
    }

    [Fact]
    public void HorizontalHemisphericalHeadFullLiquid_ShouldMatchCylinderPlusTwoHalfSpheres()
    {
        var input = new WettedAreaInput
        {
            Orientation = VesselOrientation.Horizontal,
            HeadType = WettedAreaHeadType.Hemispherical,
            DiameterM = 2.0,
            StraightLengthM = 4.0,
            LiquidLevelM = 2.0,
            BottomElevationFromGradeM = 0.0
        };

        WettedAreaResult result = _calculator.Calculate(input);

        double expectedShellArea = 2.0 * Math.PI * 1.0 * 4.0;
        double expectedHeadArea = 4.0 * Math.PI * 1.0 * 1.0;

        Assert.Equal(expectedShellArea, result.ShellWettedAreaM2, 4);
        Assert.Equal(expectedHeadArea, result.HeadWettedAreaM2, 3);
    }

    [Fact]
    public void VerticalFlatHeadPartialLiquid_ShouldWetBottomHeadAndShellOnly()
    {
        var input = new WettedAreaInput
        {
            Orientation = VesselOrientation.Vertical,
            HeadType = WettedAreaHeadType.Flat,
            DiameterM = 2.0,
            StraightLengthM = 5.0,
            LiquidLevelM = 3.0,
            BottomElevationFromGradeM = 0.0
        };

        WettedAreaResult result = _calculator.Calculate(input);

        double expectedHeadArea = Math.PI * 1.0 * 1.0;
        double expectedShellArea = 2.0 * Math.PI * 1.0 * 3.0;

        Assert.Equal(expectedHeadArea, result.HeadWettedAreaM2, 6);
        Assert.Equal(expectedShellArea, result.ShellWettedAreaM2, 6);
        Assert.Equal(expectedHeadArea + expectedShellArea, result.TotalWettedAreaM2, 6);
    }

    [Fact]
    public void GradeLimit_ShouldCapEffectiveWettedHeight()
    {
        var input = new WettedAreaInput
        {
            Orientation = VesselOrientation.Horizontal,
            HeadType = WettedAreaHeadType.EllipsoidalTwoToOne,
            DiameterM = 2.0,
            StraightLengthM = 6.0,
            LiquidLevelM = 2.0,
            BottomElevationFromGradeM = 1.5,
            GradeHeightLimitM = 2.0
        };

        WettedAreaResult result = _calculator.Calculate(input);

        Assert.True(result.WasLimitedByGradeHeight);
        Assert.Equal(0.5, result.EffectiveHeightM, 6);
    }
}
