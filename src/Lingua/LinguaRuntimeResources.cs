using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Irihi.Lingua;

/// <summary>
/// Stores and resolves localized resources by culture, walking the culture
/// hierarchy (zh-CN → zh → invariant) until a match is found.
/// </summary>
public sealed class LinguaRuntimeResources
{
    private readonly Dictionary<string, Dictionary<string, string>> _resources = new();
    private readonly string _invariantCultureName;

    public LinguaRuntimeResources(string invariantCultureName = "")
    {
        _invariantCultureName = invariantCultureName;
    }

    /// <summary>Register a full set of resource strings for a specific culture.</summary>
    public void Add(CultureInfo culture, IReadOnlyDictionary<string, string> resources)
    {
        var key = culture.Name;
        if (!_resources.TryGetValue(key, out var dict))
            _resources[key] = dict = new Dictionary<string, string>();
        foreach (var kvp in resources)
            dict[kvp.Key] = kvp.Value;
    }

    /// <summary>
    /// Resolve resources for the given culture, walking up the hierarchy
    /// (culture → parent → parent … → invariant) and merging all keys.
    /// </summary>
    public IReadOnlyDictionary<string, string> Resolve(CultureInfo culture)
    {
        var result = new Dictionary<string, string>();

        // Start from invariant (base), then overlay specific cultures
        var chain = WalkCultureChain(culture);
        for (var i = chain.Count - 1; i >= 0; i--)
        {
            if (_resources.TryGetValue(chain[i], out var dict))
            {
                foreach (var kvp in dict)
                    result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private static List<string> WalkCultureChain(CultureInfo culture)
    {
        var chain = new List<string>();
        var current = culture;
        while (true)
        {
            chain.Add(current.Name);
            if (current == CultureInfo.InvariantCulture || current.Parent == current)
                break;
            current = current.Parent;
        }
        return chain;
    }
}
