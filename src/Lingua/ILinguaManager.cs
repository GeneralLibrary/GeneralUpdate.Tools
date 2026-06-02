using System;
using System.Collections.Generic;
using System.Globalization;

namespace Irihi.Lingua;

/// <summary>
/// Interface implemented by all Lingua language managers.
/// Provides culture switching and reactive observable access to localized strings.
/// </summary>
public interface ILinguaManager
{
    /// <summary>Switch the active culture and push new values to all subscribers.</summary>
    void UpdateCulture(CultureInfo culture);

    /// <summary>Get an observable that emits when the given key's translation changes.</summary>
    IObservable<string?> GetObservable(string key);

    /// <summary>Register resource strings for a specific culture at runtime.</summary>
    void AddResources(CultureInfo culture, IReadOnlyDictionary<string, string> resources);
}
