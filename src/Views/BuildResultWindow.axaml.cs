using System;
using System.Collections.Specialized;
using AvaloniaEdit;
using GeneralUpdate.Tools.ViewModels;
using Ursa.Controls;

namespace GeneralUpdate.Tools.Views;

public partial class BuildResultWindow : UrsaWindow
{
    private readonly BuildResultWindowViewModel _vm;

    public BuildResultWindow(BuildResultWindowViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        // Subscribe to log changes to auto-scroll the editor
        vm.Log.CollectionChanged += OnLogChanged;
        LogEditor.Document = vm.Document;
    }

    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;

        // Append each new line to the AvaloniaEdit document
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                var line = item?.ToString() ?? "";
                _vm.Document.Insert(_vm.Document.TextLength, line + Environment.NewLine);
            }
        }

        // Auto-scroll to end
        LogEditor.ScrollToEnd();
    }

    private void OnOpenOutputClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _vm.OpenOutput();
    }

    private void OnExitClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
