namespace GeneralUpdate.Tool.Avalonia.Models;

public class PlatformModel
{
    public string DisplayName { get; set; }

    public int Value { get; set; }

    public override string ToString() => DisplayName;
}