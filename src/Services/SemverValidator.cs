using System;
using System.Text.RegularExpressions;

namespace GeneralUpdate.Tools.Services;

/// <summary>
///     Shared semver validation per https://semver.org/spec/v2.0.0.html.
/// </summary>
public static partial class SemverValidator
{
    // MAJOR.MINOR.PATCH[-pre][+build]
    [GeneratedRegex(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$",
        RegexOptions.Compiled)]
    private static partial Regex SemverRegex();

    // Captures: 1=MAJOR, 2=MINOR, 3=PATCH, 4=pre-release (including leading '-'), 5=build (including leading '+')
    [GeneratedRegex(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?(\+([0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*))?$",
        RegexOptions.Compiled)]
    private static partial Regex SemverPartsRegex();

    public static bool IsValid(string version) => SemverRegex().IsMatch(version);

    /// <summary>
    ///     Extract the numeric (MAJOR, MINOR, PATCH) core for sorting.
    ///     Returns (0,0,0) if the version cannot be parsed.
    /// </summary>
    public static (int Major, int Minor, int Patch) ParseCore(string version)
    {
        var m = SemverPartsRegex().Match(version);
        if (!m.Success) return (0, 0, 0);
        return (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
    }

    /// <summary>
    ///     Compare two SemVer 2.0 version strings.
    ///     Returns negative if <paramref name="a"/> &lt; <paramref name="b"/>,
    ///     zero if equal, positive if <paramref name="a"/> &gt; <paramref name="b"/>.
    /// </summary>
    public static int Compare(string a, string b)
    {
        var ma = SemverPartsRegex().Match(a);
        var mb = SemverPartsRegex().Match(b);
        if (!ma.Success || !mb.Success)
            throw new ArgumentException("Both versions must be valid SemVer 2.0.");

        // Compare MAJOR.MINOR.PATCH numerically
        for (int i = 1; i <= 3; i++)
        {
            var cmp = int.Parse(ma.Groups[i].Value).CompareTo(int.Parse(mb.Groups[i].Value));
            if (cmp != 0) return cmp;
        }

        // Pre-release comparison
        // A version without pre-release has higher precedence than one with pre-release
        var preA = ma.Groups[5].Value; // Group 5 = pre-release identifiers (without leading '-')
        var preB = mb.Groups[5].Value;
        var hasPreA = !string.IsNullOrEmpty(preA);
        var hasPreB = !string.IsNullOrEmpty(preB);

        if (!hasPreA && !hasPreB) return 0;
        if (!hasPreA && hasPreB) return 1;
        if (hasPreA && !hasPreB) return -1;

        // Both have pre-release — compare identifiers
        var idsA = preA.Split('.');
        var idsB = preB.Split('.');
        var count = Math.Min(idsA.Length, idsB.Length);
        for (int i = 0; i < count; i++)
        {
            var cmp = ComparePreReleaseIdentifier(idsA[i], idsB[i]);
            if (cmp != 0) return cmp;
        }

        // All compared identifiers equal — longer pre-release has higher precedence
        return idsA.Length.CompareTo(idsB.Length);
    }

    private static int ComparePreReleaseIdentifier(string a, string b)
    {
        var numA = int.TryParse(a, out var nA);
        var numB = int.TryParse(b, out var nB);

        if (numA && numB) return nA.CompareTo(nB);       // both numeric
        if (numA && !numB) return -1;                     // numeric < alphanumeric
        if (!numA && numB) return 1;                      // alphanumeric > numeric
        return string.CompareOrdinal(a, b);               // both alphanumeric
    }
}
