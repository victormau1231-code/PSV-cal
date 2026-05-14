using PSVCalc.Core.Enums;

namespace PSVCalc.Core.Models;

public sealed class WettedAreaInput
{
    public VesselOrientation Orientation { get; init; } = VesselOrientation.Horizontal;

    public WettedAreaHeadType HeadType { get; init; } = WettedAreaHeadType.EllipsoidalTwoToOne;

    public double DiameterM { get; init; }

    public double StraightLengthM { get; init; }

    public double LiquidLevelM { get; init; }

    public double BottomElevationFromGradeM { get; init; }

    public double GradeHeightLimitM { get; init; } = 7.6;
}
