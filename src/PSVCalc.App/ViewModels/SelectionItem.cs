namespace PSVCalc.App.ViewModels;

public sealed class SelectionItem<T>
{
    public required T Value { get; init; }
    public required string Label { get; init; }
}

