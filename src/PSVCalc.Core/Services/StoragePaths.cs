namespace PSVCalc.Core.Services;

public sealed class StoragePaths
{
    public string RootDirectory { get; }
    public string ProjectsDirectory => Path.Combine(RootDirectory, "Projects");
    public string HistoryDirectory => Path.Combine(RootDirectory, "History");
    public string ExportsDirectory => Path.Combine(RootDirectory, "Exports");
    public string ValidationDirectory => Path.Combine(RootDirectory, "Validation");
    public string HistoryIndexPath => Path.Combine(HistoryDirectory, "history.json");

    public StoragePaths(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("Root directory cannot be empty.", nameof(rootDirectory));
        }

        RootDirectory = rootDirectory;
        EnsureDirectories();
    }

    public static StoragePaths CreateDefault()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return new StoragePaths(Path.Combine(documents, "PSVCalc"));
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(ProjectsDirectory);
        Directory.CreateDirectory(HistoryDirectory);
        Directory.CreateDirectory(ExportsDirectory);
        Directory.CreateDirectory(ValidationDirectory);
    }
}
