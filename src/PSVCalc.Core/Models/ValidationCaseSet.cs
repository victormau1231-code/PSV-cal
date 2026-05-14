namespace PSVCalc.Core.Models;

public sealed class ValidationCaseSet
{
    public string Name { get; set; } = "Onsite Validation Cases";
    public string StandardVersion { get; set; } = AppMetadata.StandardVersion;
    public List<ValidationCase> Cases { get; set; } = [];
}

