using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using GeneralUpdate.Tools.V12.ViewModels;

namespace GeneralUpdate.Tools.V12;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);
        if (type != null) { var c = (Control)Activator.CreateInstance(type)!; c.DataContext = param; return c; }
        return new TextBlock { Text = "Not Found: " + name };
    }
    public bool Match(object? data) => data is ViewModelBase;
}
