using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace GeneralUpdate.Tool.Avalonia;

public class ClipboardUtility
{
    private static IClipboard? _clipboard = null;

    public static async Task SetText(string content) 
    {
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Text, content);
        await _clipboard?.SetDataObjectAsync(dataObject);
    }

    public static void CreateClipboard(Visual visual)
    {
        _clipboard = TopLevel.GetTopLevel(visual)?.Clipboard;
    }
}