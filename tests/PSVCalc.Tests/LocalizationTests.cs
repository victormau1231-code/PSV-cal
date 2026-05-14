using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class LocalizationTests
{
    [Fact]
    public void LocalizationKeys_ShouldBeSymmetric()
    {
        IReadOnlyList<string> missingInEn = LocalizationCatalog.MissingKeysInEnglish();
        IReadOnlyList<string> missingInZh = LocalizationCatalog.MissingKeysInChinese();

        Assert.Empty(missingInEn);
        Assert.Empty(missingInZh);
    }
}

