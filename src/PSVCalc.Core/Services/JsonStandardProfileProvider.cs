using System.Text.Json;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public sealed class JsonStandardProfileProvider : IStandardProfileProvider
{
    private readonly IReadOnlyList<StandardProfile> _profiles;
    private readonly string _activeProfileId;

    public JsonStandardProfileProvider(string activeProfileId = "api520-521-asme-2026-04-01")
    {
        _profiles = LoadProfiles();
        _activeProfileId = activeProfileId;
    }

    public IReadOnlyList<StandardProfile> GetAllProfiles() => _profiles;

    public StandardProfile GetActiveProfile()
    {
        return GetByProfileId(_activeProfileId);
    }

    public StandardProfile GetByProfileId(string profileId)
    {
        StandardProfile? profile = _profiles.FirstOrDefault(
            x => string.Equals(x.ProfileId, profileId, StringComparison.OrdinalIgnoreCase));
        if (profile is null)
        {
            throw new KeyNotFoundException($"Standard profile '{profileId}' was not found.");
        }

        return profile;
    }

    private static IReadOnlyList<StandardProfile> LoadProfiles()
    {
        var files = EmbeddedResourceReader.ReadAllJsonUnder("Data.standards");
        var profiles = new List<StandardProfile>();
        foreach ((string _, string content) in files)
        {
            StandardProfile? profile = JsonSerializer.Deserialize<StandardProfile>(content, JsonOptions);
            if (profile is not null)
            {
                profiles.Add(profile);
            }
        }

        if (profiles.Count == 0)
        {
            throw new InvalidOperationException("No standard profiles were loaded from embedded resources.");
        }

        return profiles;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

