using PSVCalc.Core.Models;

namespace PSVCalc.Core.Interfaces;

public interface IStandardProfileProvider
{
    IReadOnlyList<StandardProfile> GetAllProfiles();
    StandardProfile GetActiveProfile();
    StandardProfile GetByProfileId(string profileId);
}

