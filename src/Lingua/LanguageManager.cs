using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Irihi.Lingua;

/// <summary>
/// Base class for Lingua language managers. Concrete managers extend this
/// to expose strongly-typed <see cref="IObservable{T}"/> properties for each resource key.
///
/// Usage:
/// <code>
/// public sealed class AppLanguageManager : LanguageManager
/// {
///     public static new AppLanguageManager Instance { get; } = new();
///     public IObservable&lt;string?&gt; App_Title => GetObservable("App.Title");
/// }
/// </code>
/// </summary>
public abstract class LanguageManager : ILinguaManager
{
    private readonly Dictionary<string, LinguaObservableString> _observables = new();
    private readonly LinguaRuntimeResources _resources;
    private CultureInfo _currentCulture = CultureInfo.InvariantCulture;

    protected LanguageManager(string invariantCultureName = "")
    {
        _resources = new LinguaRuntimeResources(invariantCultureName);
    }

    // ── ILinguaManager ───────────────────────────────────────

    /// <inheritdoc />
    public void UpdateCulture(CultureInfo culture)
    {
        _currentCulture = culture;
        var resolved = _resources.Resolve(culture);

        // Push updated values to all observables
        foreach (var kvp in _observables)
        {
            var key = kvp.Key;
            var observable = kvp.Value;
            var newValue = resolved.TryGetValue(key, out var val) ? val : null;
            observable.OnNext(newValue);
        }
    }

    /// <inheritdoc />
    public IObservable<string?> GetObservable(string key)
    {
        if (_observables.TryGetValue(key, out var existing))
            return existing;

        // Look up current value from resolved resources
        var resolved = _resources.Resolve(_currentCulture);
        var initialValue = resolved.TryGetValue(key, out var val) ? val : null;

        var observable = new LinguaObservableString(key, initialValue);
        _observables[key] = observable;
        return observable;
    }

    /// <inheritdoc />
    public void AddResources(CultureInfo culture, IReadOnlyDictionary<string, string> resources)
    {
        _resources.Add(culture, resources);

        // If we're adding resources for the current culture, push updates
        if (culture.Name == _currentCulture.Name)
        {
            foreach (var kvp in resources)
            {
                if (_observables.TryGetValue(kvp.Key, out var obs))
                    obs.OnNext(kvp.Value);
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────

    /// <summary>Current active culture.</summary>
    public CultureInfo CurrentCulture => _currentCulture;

    /// <summary>
    /// Add resource strings for a culture from a flat dictionary.
    /// Convenience overload that accepts a regular <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    public void AddResources(string cultureName, Dictionary<string, string> resources)
    {
        AddResources(new CultureInfo(cultureName), resources);
    }

    /// <summary>
    /// Resolve a single key for the current culture (synchronous, non-observable access).
    /// </summary>
    public string? Resolve(string key)
    {
        var resolved = _resources.Resolve(_currentCulture);
        return resolved.TryGetValue(key, out var val) ? val : null;
    }
}
