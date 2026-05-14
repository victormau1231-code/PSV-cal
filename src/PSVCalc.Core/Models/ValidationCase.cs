namespace PSVCalc.Core.Models;

public sealed class ValidationCase
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public required CalculationInput Input { get; set; }

    public double ExpectedRequiredAreaMm2 { get; set; }
    public string ExpectedOrificeLetter { get; set; } = string.Empty;
    public double AllowedAreaDeviationPercent { get; set; } = 2.0;
}

