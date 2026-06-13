using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using GeneralUpdate.Tools.ViewModels;
using GeneralUpdate.Tools.Views;

namespace GeneralUpdate.Tools.Services;

public static class DialogHelper
{
    public static async Task ShowInfoAsync(string title, string message)
    {
        var owner = (Application.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(20, 16) };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        panel.Children.Add(new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 16, 0, 0),
            MinWidth = 80
        });
        var tcs = new TaskCompletionSource();
        ((Button)panel.Children[1]).Click += (_, _) =>
        {
            dialog.Close();
            tcs.TrySetResult();
        };

        dialog.Content = panel;
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();
        await tcs.Task;
    }

    /// <summary>
    /// Show a result window that runs an operation and streams logs in real time.
    /// The window appears immediately; logs stream via <see cref="IProgress{String}"/>.
    /// Note: the operation is awaited on the UI thread so it should yield appropriately
    /// (e.g. await I/O, Process, or file operations); CPU-bound work should be offloaded
    /// via <c>Task.Run</c> inside the operation delegate.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="operation">Work to perform. Call <c>progress.Report(line)</c> to stream log lines.</param>
    /// <param name="outputDirectory">Optional output directory for the "Open Output" button.</param>
    public static async Task ShowResultWindowAsync(
        string title,
        Func<IProgress<string>, Task> operation,
        string? outputDirectory = null)
    {
        var owner = (Application.Current?.ApplicationLifetime as
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return;

        var vm = new BuildResultWindowViewModel
        {
            WindowTitle = title,
            OutputDirectory = outputDirectory,
        };

        var window = new BuildResultWindow(vm);

        // Kick off the background operation (don't await yet — show window first)
        var task = vm.RunAsync(operation);

        // Show the window — this blocks until the user clicks "退出"
        await window.ShowDialog(owner);

        // Ensure the background task completed (it may have finished before the user closed)
        await task;
    }
}
