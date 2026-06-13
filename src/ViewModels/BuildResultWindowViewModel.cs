using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AvaloniaEdit.Document;

namespace GeneralUpdate.Tools.ViewModels;

public class BuildResultWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> Log { get; } = new();
    public TextDocument Document { get; } = new();
    public string WindowTitle { get; set; } = "Build Result";
    public string? OutputDirectory { get; set; }

    private bool _isRunning = true;
    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); OnPropertyChanged(nameof(CanOpenOutput)); }
    }

    private bool _success;
    public bool Success
    {
        get => _success;
        set { _success = value; OnPropertyChanged(nameof(Success)); }
    }

    public bool CanOpenOutput => !IsRunning && !string.IsNullOrWhiteSpace(OutputDirectory) && Directory.Exists(OutputDirectory);

    public void AppendLog(string line)
    {
        Log.Add(line);
        OnPropertyChanged(nameof(Log));
    }

    public void OpenOutput()
    {
        if (!CanOpenOutput) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = OutputDirectory, UseShellExecute = true });
        }
        catch { }
    }

    /// <summary>
    /// Run an operation in the background, streaming log lines to the UI.
    /// </summary>
    public async Task RunAsync(Func<IProgress<string>, Task> operation)
    {
        IsRunning = true;
        var progress = new Progress<string>(line => AppendLog(line));
        try
        {
            await operation(progress);
            Success = true;
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
            Success = false;
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
