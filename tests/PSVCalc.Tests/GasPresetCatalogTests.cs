using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class GasPresetCatalogTests
{
    [Fact]
    public void Catalog_ShouldIncludeExpandedCommonGasSet()
    {
        var presets = GasPresetCatalog.GetAll();

        Assert.True(presets.Count >= 19);
        Assert.Contains(presets, x => x.Id == "air");
        Assert.Contains(presets, x => x.Id == "oxygen");
        Assert.Contains(presets, x => x.Id == "propylene");
        Assert.Contains(presets, x => x.Id == "acetylene");
        Assert.Contains(presets, x => x.Id == "carbon_monoxide");
        Assert.Contains(presets, x => x.Id == "sulfur_dioxide");
        Assert.Contains(presets, x => x.Id == "chlorine");
    }
}
