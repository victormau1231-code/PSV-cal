namespace PSVCalc.Core.Models;

public sealed class WettedAreaResult
{
    public double EffectiveHeightM { get; init; }

    public double GradeHeightCapM { get; init; }

    public double HeadDepthM { get; init; }

    public double ShellWettedAreaM2 { get; init; }

    public double HeadWettedAreaM2 { get; init; }

    public bool WasLimitedByGradeHeight { get; init; }

    public double TotalWettedAreaM2 => ShellWettedAreaM2 + HeadWettedAreaM2;
}
