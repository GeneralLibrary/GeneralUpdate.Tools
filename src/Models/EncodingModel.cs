using System.Text;

namespace GeneralUpdate.Tool.Avalonia.Models;

public class EncodingModel
{
    public string DisplayName { get; set; }

    public Encoding Value { get; set; }

    public int Type { get; set; }

    public override string ToString() => DisplayName;
}