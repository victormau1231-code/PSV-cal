using PSVCalc.Core;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;
using PSVCalc.Core.Services;

namespace PSVCalc.Tests;

public sealed class StandardProfileProviderTests
{
    [Fact]
    public void Provider_ShouldLoadEmbeddedProfiles()
    {
        IStandardProfileProvider provider = new JsonStandardProfileProvider();
        IReadOnlyList<StandardProfile> profiles = provider.GetAllProfiles();

        Assert.True(profiles.Count >= 2);
        StandardProfile active = provider.GetActiveProfile();
        Assert.Equal(CalculationStandardCatalog.ApiProfileId, active.ProfileId);
        Assert.True(active.TryGet("KD_DEFAULT_GAS", out StandardCoefficient? coefficient));
        Assert.NotNull(coefficient);
        Assert.True(coefficient!.Value > 0);

        StandardProfile hgt = provider.GetByProfileId(CalculationStandardCatalog.HgTProfileId);
        Assert.Equal("HG/T 20570.2-1995", hgt.Name);
        Assert.True(hgt.TryGet("HGT_FIRE_GRADE_LIMIT_M", out StandardCoefficient? fireLimit));
        Assert.Equal(7.5, fireLimit!.Value, 6);
    }

    [Fact]
    public void Calculator_ShouldFailWhenSelectedStandardProfileIsMissing()
    {
        var calculator = new SafetyValveCalculator(new OrificeSelector(), new MissingHgTProfileProvider());
        var input = new CalculationInput
        {
            CaseName = "Missing-HGT-Profile",
            StandardBasis = CalculationStandardBasis.HgT20570_2,
            FluidType = FluidType.Gas,
            PressureInputMode = PressureInputMode.Absolute,
            PressureUnit = PressureUnit.MPa,
            SetPressure = 1.0,
            RelievingPressure = 1.1,
            BackPressure = 0.2,
            TemperatureC = 40,
            ReliefLoadKgPerHour = 1000,
            UseGasPreset = false,
            MolecularWeight = 28.0,
            IsentropicExponentK = 1.4,
            CompressibilityFactorZ = 1.0
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => calculator.Calculate(input));
        Assert.Contains(CalculationStandardCatalog.HgTProfileId, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class MissingHgTProfileProvider : IStandardProfileProvider
    {
        private readonly JsonStandardProfileProvider _inner = new();

        public IReadOnlyList<StandardProfile> GetAllProfiles() => _inner.GetAllProfiles();

        public StandardProfile GetActiveProfile() => _inner.GetByProfileId(CalculationStandardCatalog.ApiProfileId);

        public StandardProfile GetByProfileId(string profileId)
        {
            if (profileId == CalculationStandardCatalog.HgTProfileId)
            {
                throw new KeyNotFoundException(profileId);
            }

            return _inner.GetByProfileId(profileId);
        }
    }
}
