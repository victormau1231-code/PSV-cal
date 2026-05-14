using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public static class GasPresetCatalog
{
    private static readonly IReadOnlyList<GasPreset> Presets =
    [
        new()
        {
            Id = "air",
            NameZh = "\u7a7a\u6c14",
            NameEn = "Air",
            MolecularWeight = 28.97,
            IsentropicExponent = 1.40,
            CompressibilityFactor = 1.00
        },
        new()
        {
            Id = "nitrogen",
            NameZh = "\u6c2e\u6c14",
            NameEn = "Nitrogen",
            MolecularWeight = 28.01,
            IsentropicExponent = 1.40,
            CompressibilityFactor = 1.00
        },
        new()
        {
            Id = "oxygen",
            NameZh = "\u6c27\u6c14",
            NameEn = "Oxygen",
            MolecularWeight = 32.00,
            IsentropicExponent = 1.40,
            CompressibilityFactor = 1.00
        },
        new()
        {
            Id = "hydrogen",
            NameZh = "\u6c22\u6c14",
            NameEn = "Hydrogen",
            MolecularWeight = 2.016,
            IsentropicExponent = 1.41,
            CompressibilityFactor = 1.00
        },
        new()
        {
            Id = "helium",
            NameZh = "\u6c26\u6c14",
            NameEn = "Helium",
            MolecularWeight = 4.003,
            IsentropicExponent = 1.66,
            CompressibilityFactor = 1.00
        },
        new()
        {
            Id = "argon",
            NameZh = "\u6c29\u6c14",
            NameEn = "Argon",
            MolecularWeight = 39.95,
            IsentropicExponent = 1.67,
            CompressibilityFactor = 1.00
        },
        new()
        {
            Id = "methane",
            NameZh = "\u7532\u70f7",
            NameEn = "Methane",
            MolecularWeight = 16.04,
            IsentropicExponent = 1.31,
            CompressibilityFactor = 0.98
        },
        new()
        {
            Id = "ethane",
            NameZh = "\u4e59\u70f7",
            NameEn = "Ethane",
            MolecularWeight = 30.07,
            IsentropicExponent = 1.19,
            CompressibilityFactor = 0.98
        },
        new()
        {
            Id = "ethylene",
            NameZh = "\u4e59\u70ef",
            NameEn = "Ethylene",
            MolecularWeight = 28.05,
            IsentropicExponent = 1.23,
            CompressibilityFactor = 0.98
        },
        new()
        {
            Id = "propane",
            NameZh = "\u4e19\u70f7",
            NameEn = "Propane",
            MolecularWeight = 44.10,
            IsentropicExponent = 1.13,
            CompressibilityFactor = 0.95
        },
        new()
        {
            Id = "propylene",
            NameZh = "\u4e19\u70ef",
            NameEn = "Propylene",
            MolecularWeight = 42.08,
            IsentropicExponent = 1.15,
            CompressibilityFactor = 0.96
        },
        new()
        {
            Id = "butane",
            NameZh = "\u4e01\u70f7",
            NameEn = "Butane",
            MolecularWeight = 58.12,
            IsentropicExponent = 1.10,
            CompressibilityFactor = 0.93
        },
        new()
        {
            Id = "acetylene",
            NameZh = "\u4e59\u7094",
            NameEn = "Acetylene",
            MolecularWeight = 26.04,
            IsentropicExponent = 1.25,
            CompressibilityFactor = 0.98
        },
        new()
        {
            Id = "carbon_monoxide",
            NameZh = "\u4e00\u6c27\u5316\u78b3",
            NameEn = "Carbon Monoxide",
            MolecularWeight = 28.01,
            IsentropicExponent = 1.40,
            CompressibilityFactor = 1.00
        },
        new()
        {
            Id = "carbon_dioxide",
            NameZh = "\u4e8c\u6c27\u5316\u78b3",
            NameEn = "Carbon Dioxide",
            MolecularWeight = 44.01,
            IsentropicExponent = 1.30,
            CompressibilityFactor = 0.99
        },
        new()
        {
            Id = "ammonia",
            NameZh = "\u6c28\u6c14",
            NameEn = "Ammonia",
            MolecularWeight = 17.03,
            IsentropicExponent = 1.31,
            CompressibilityFactor = 0.99
        },
        new()
        {
            Id = "hydrogen_sulfide",
            NameZh = "\u786b\u5316\u6c22",
            NameEn = "Hydrogen Sulfide",
            MolecularWeight = 34.08,
            IsentropicExponent = 1.30,
            CompressibilityFactor = 0.98
        },
        new()
        {
            Id = "sulfur_dioxide",
            NameZh = "\u4e8c\u6c27\u5316\u786b",
            NameEn = "Sulfur Dioxide",
            MolecularWeight = 64.07,
            IsentropicExponent = 1.26,
            CompressibilityFactor = 0.97
        },
        new()
        {
            Id = "chlorine",
            NameZh = "\u6c2f\u6c14",
            NameEn = "Chlorine",
            MolecularWeight = 70.91,
            IsentropicExponent = 1.33,
            CompressibilityFactor = 0.97
        }
    ];

    public static IReadOnlyList<GasPreset> GetAll() => Presets;

    public static bool TryGetById(string? id, out GasPreset? preset)
    {
        preset = Presets.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        return preset is not null;
    }
}
