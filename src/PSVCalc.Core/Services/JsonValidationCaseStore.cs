using System.Text.Json;
using System.Text.Json.Serialization;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public sealed class JsonValidationCaseStore : IValidationCaseStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ValidationCaseSet LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Validation case file was not found.", path);
        }

        string content = File.ReadAllText(path);
        ValidationCaseSet? set = JsonSerializer.Deserialize<ValidationCaseSet>(content, JsonOptions);
        if (set is null)
        {
            throw new InvalidOperationException("Failed to deserialize validation case set.");
        }

        return set;
    }

    public void SaveToFile(string path, ValidationCaseSet caseSet)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string content = JsonSerializer.Serialize(caseSet, JsonOptions);
        File.WriteAllText(path, content);
    }

    public string EnsureTemplate(string destinationDirectory, string fileName = "onsite-cases.template.json")
    {
        Directory.CreateDirectory(destinationDirectory);
        string fullPath = Path.Combine(destinationDirectory, fileName);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        string templateContent = EmbeddedResourceReader.ReadJsonResource("Data.validation", "onsite-cases.template.json");
        File.WriteAllText(fullPath, templateContent);
        return fullPath;
    }
}

