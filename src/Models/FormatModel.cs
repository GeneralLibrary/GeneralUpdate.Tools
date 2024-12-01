namespace GeneralUpdate.Tool.Avalonia.Models;

public class FormatModel
{
    public string DisplayName { get; set; }

    public int Type { get; set; }

    public string Value { get; set; }

    public override string ToString() => DisplayName;
}