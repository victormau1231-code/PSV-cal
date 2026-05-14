using PSVCalc.Core.Models;

namespace PSVCalc.Core.Interfaces;

public interface IProjectRepository
{
    string ProjectsDirectory { get; }
    string HistoryDirectory { get; }
    string ExportsDirectory { get; }

    string SaveProject(ProjectRecord record, string? preferredFileName = null);
    ProjectRecord LoadProject(string filePath);

    void AddHistory(HistoryEntry entry);
    IReadOnlyList<HistoryEntry> LoadHistory(int take = 50);
}

