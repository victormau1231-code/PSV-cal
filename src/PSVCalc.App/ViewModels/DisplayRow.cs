namespace PSVCalc.App.ViewModels;

public sealed class DisplayRow
{
    public required string Parameter { get; init; }
    public required string Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
}

