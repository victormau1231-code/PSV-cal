using PSVCalc.Core;
using PSVCalc.Core.Enums;
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

    [Fact]
    public void VersionText_ShouldBeUpdatedTo132()
    {
        Assert.Equal("1.3.2", AppMetadata.SoftwareVersion);
        Assert.Contains("1.3.2", LocalizationCatalog.Get(UiLanguage.ZhCn, "app_title"), StringComparison.Ordinal);
        Assert.Contains("1.3.2", LocalizationCatalog.Get(UiLanguage.EnUs, "app_title"), StringComparison.Ordinal);
    }
}
