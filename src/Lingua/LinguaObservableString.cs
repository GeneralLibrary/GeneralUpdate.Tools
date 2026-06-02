using System;
using System.Collections.Generic;

namespace Irihi.Lingua;

/// <summary>
/// A behaviour-subject style observable string that replays the latest value
/// to new subscribers and pushes updates when the culture changes.
/// </summary>
public sealed class LinguaObservableString : IObservable<string?>
{
    private readonly List<IObserver<string?>> _observers = new();
    private string? _currentValue;

    public string Key { get; }
    public string? CurrentValue => _currentValue;

    public LinguaObservableString(string key, string? initialValue)
    {
        Key = key;
        _currentValue = initialValue;
    }

    /// <summary>Push a new value to all active subscribers.</summary>
    public void OnNext(string? value)
    {
        _currentValue = value;
        // Snapshot to avoid mutation during iteration
        var snapshot = _observers.ToArray();
        foreach (var observer in snapshot)
            observer.OnNext(value);
    }

    public IDisposable Subscribe(IObserver<string?> observer)
    {
        _observers.Add(observer);
        // Replay current value to new subscriber (behaviour-subject semantics)
        observer.OnNext(_currentValue);
        return new Unsubscriber(_observers, observer);
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<IObserver<string?>> _observers;
        private readonly IObserver<string?> _observer;

        public Unsubscriber(List<IObserver<string?>> observers, IObserver<string?> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose() => _observers.Remove(_observer);
    }
}
