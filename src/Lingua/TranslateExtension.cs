using System;
using global::Avalonia;
using global::Avalonia.Data;
using global::Avalonia.Markup.Xaml;

namespace Irihi.Lingua;

/// <summary>
/// Avalonia markup extension that binds a <see cref="LinguaKey"/> to a target property.
/// The binding auto-updates when the manager's culture changes.
///
/// Usage in XAML:
/// <code>
/// &lt;TextBlock Text="{Translate {x:Static local:LanguageManager+Keys.App_Title}}" /&gt;
/// </code>
/// </summary>
public sealed class TranslateExtension : MarkupExtension
{
    public LinguaKey? Key { get; set; }

    public TranslateExtension() { }

    public TranslateExtension(LinguaKey key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Key == null)
            return AvaloniaProperty.UnsetValue;

        // Create an observable source that auto-updates on culture change
        var observable = Key.Manager.GetObservable(Key.Key);
        var source = new LinguaBindingSource(observable);

        return new Binding
        {
            Source = source,
            Path = "Value",
            Mode = BindingMode.OneWay,
        };
    }
}

/// <summary>
/// Lightweight binding source that exposes the latest value of an observable string
/// as a CLR property, raising PropertyChanged on each update.
/// </summary>
internal sealed class LinguaBindingSource : System.ComponentModel.INotifyPropertyChanged
{
    private readonly IDisposable _subscription;
    private string? _value;

    public string? Value
    {
        get => _value;
        private set
        {
            _value = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public LinguaBindingSource(IObservable<string?> observable)
    {
        _subscription = observable.Subscribe(
            new AnonymousObserver<string?>(v => Value = v));
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private sealed class AnonymousObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        public AnonymousObserver(Action<T> onNext) => _onNext = onNext;
        public void OnNext(T value) => _onNext(value);
        public void OnCompleted() { }
        public void OnError(Exception error) { }
    }
}
