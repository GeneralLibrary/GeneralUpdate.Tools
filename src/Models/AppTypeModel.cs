namespace GeneralUpdate.Tool.Avalonia.Models;

public class AppTypeModel
{
    public string DisplayName { get; set; }

    public int Value { get; set; }

    public override string ToString() => DisplayName;
}