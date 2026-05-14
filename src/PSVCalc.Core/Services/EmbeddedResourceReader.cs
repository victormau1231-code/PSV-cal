using System.Reflection;

namespace PSVCalc.Core.Services;

internal static class EmbeddedResourceReader
{
    public static IReadOnlyDictionary<string, string> ReadAllJsonUnder(string folderPrefix)
    {
        Assembly assembly = typeof(EmbeddedResourceReader).Assembly;
        string prefix = $"{assembly.GetName().Name}.{folderPrefix.Trim().Replace('\\', '.').Replace('/', '.')}.";

        var names = assembly
            .GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n)
            .ToList();

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string resourceName in names)
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            string shortName = resourceName[(prefix.Length)..];
            result[shortName] = content;
        }

        return result;
    }

    public static string ReadJsonResource(string folderPrefix, string fileName)
    {
        var all = ReadAllJsonUnder(folderPrefix);
        if (!all.TryGetValue(fileName, out string? content))
        {
            throw new FileNotFoundException($"Embedded resource '{folderPrefix}/{fileName}' was not found.");
        }

        return content;
    }
}

