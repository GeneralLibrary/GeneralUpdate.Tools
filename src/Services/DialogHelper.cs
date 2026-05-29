using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

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
}
