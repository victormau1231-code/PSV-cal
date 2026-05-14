using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class OrificeSelectorTests
{
    private readonly IOrificeSelector _selector = new OrificeSelector();

    [Fact]
    public void Recommend_ShouldPickSmallestSatisfyingOrifice()
    {
        var recommendation = _selector.Recommend(190.0);
        Assert.Equal("G", recommendation.Selected.Letter);
        Assert.True(recommendation.Selected.AreaMm2 >= recommendation.MinimumRecommendedAreaMm2);
        Assert.Equal(2, recommendation.Selected.InletNominalInch);
        Assert.Equal(3, recommendation.Selected.OutletNominalInch);
        Assert.Equal("2G3", recommendation.SizeShorthand);
        Assert.True(recommendation.WasUpsizedForMargin);
        Assert.Equal("F", recommendation.DirectAreaQualifiedLetter);
        Assert.Contains(recommendation.CandidateNeighbors, x => x.Letter == "F");
        Assert.Contains(recommendation.CandidateNeighbors, x => x.Letter == "H");
    }

    [Fact]
    public void Recommend_WhenTooLarge_ShouldFlagExceeded()
    {
        var recommendation = _selector.Recommend(50_000.0);
        Assert.True(recommendation.IsCapacityExceededByLargestOrifice);
        Assert.Equal("T", recommendation.Selected.Letter);
    }

    [Fact]
    public void Recommend_ShouldKeepDirectOrifice_WhenUtilizationWithinNinetyPercent()
    {
        var recommendation = _selector.Recommend(150.0);

        Assert.Equal("F", recommendation.Selected.Letter);
        Assert.False(recommendation.WasUpsizedForMargin);
        Assert.Equal("F", recommendation.DirectAreaQualifiedLetter);
        Assert.True(recommendation.SelectedUtilizationPercent <= recommendation.MaximumRecommendedUtilizationPercent);
    }
}
