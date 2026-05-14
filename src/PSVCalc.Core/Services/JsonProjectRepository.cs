using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using PSVCalc.Core.Enums;
using PSVCalc.Core.Interfaces;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Services;

public sealed class JsonProjectRepository : IProjectRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly StoragePaths _paths;
    private readonly object _historySync = new();

    public JsonProjectRepository(StoragePaths paths)
    {
        _paths = paths;
        _paths.EnsureDirectories();
    }

    public string ProjectsDirectory => _paths.ProjectsDirectory;
    public string HistoryDirectory => _paths.HistoryDirectory;
    public string ExportsDirectory => _paths.ExportsDirectory;

    public string SaveProject(ProjectRecord record, string? preferredFileName = null)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        record.SavedAt = DateTimeOffset.Now;
        if (string.IsNullOrWhiteSpace(record.ProjectId))
        {
            record.ProjectId = Guid.NewGuid().ToString("N");
        }

        string safeBase = BuildSafeFileName(preferredFileName ?? record.CaseName);
        string fullPath = Path.Combine(_paths.ProjectsDirectory, $"{safeBase}.json");
        File.WriteAllText(fullPath, JsonSerializer.Serialize(record, JsonOptions));

        if (record.Result is not null)
        {
            AddHistory(new HistoryEntry
            {
                Timestamp = record.SavedAt,
                ProjectId = record.ProjectId,
                CaseName = record.CaseName,
                FluidType = record.Input.FluidType,
                RequiredAreaMm2 = record.Result.RequiredAreaMm2,
                SelectedOrifice = BuildHistorySelection(record),
                ProjectFile = fullPath
            });
        }

        return fullPath;
    }

    private static string BuildHistorySelection(ProjectRecord record)
    {
        if (record.Input.StandardBasis == CalculationStandardBasis.HgT20570_2)
        {
            double throatDiameterMm = Math.Sqrt(4.0 * record.Result!.RequiredAreaMm2 / Math.PI);
            return string.Format(CultureInfo.InvariantCulture, "{0:F3} mm", throatDiameterMm);
        }

        return record.Result!.OrificeRecommendation.Selected.Letter;
    }

    public ProjectRecord LoadProject(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Project file was not found.", filePath);
        }

        string json = File.ReadAllText(filePath);
        var record = JsonSerializer.Deserialize<ProjectRecord>(json, JsonOptions);
        if (record is null)
        {
            throw new InvalidOperationException("Failed to parse project file.");
        }

        return record;
    }

    public void AddHistory(HistoryEntry entry)
    {
        lock (_historySync)
        {
            List<HistoryEntry> all = LoadHistoryInternal();
            all.Insert(0, entry);
            if (all.Count > 200)
            {
                all = all.Take(200).ToList();
            }

            File.WriteAllText(_paths.HistoryIndexPath, JsonSerializer.Serialize(all, JsonOptions));
        }
    }

    public IReadOnlyList<HistoryEntry> LoadHistory(int take = 50)
    {
        lock (_historySync)
        {
            return LoadHistoryInternal().Take(Math.Max(1, take)).ToList();
        }
    }

    private List<HistoryEntry> LoadHistoryInternal()
    {
        if (!File.Exists(_paths.HistoryIndexPath))
        {
            return [];
        }

        string json = File.ReadAllText(_paths.HistoryIndexPath);
        var parsed = JsonSerializer.Deserialize<List<HistoryEntry>>(json, JsonOptions);
        return parsed ?? [];
    }

    private static string BuildSafeFileName(string? raw)
    {
        string candidate = string.IsNullOrWhiteSpace(raw) ? $"case-{DateTime.Now:yyyyMMdd-HHmmss}" : raw;
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            candidate = candidate.Replace(c, '_');
        }

        candidate = candidate.Trim();
        if (candidate.Length == 0)
        {
            candidate = $"case-{DateTime.Now:yyyyMMdd-HHmmss}";
        }

        return candidate;
    }
}
