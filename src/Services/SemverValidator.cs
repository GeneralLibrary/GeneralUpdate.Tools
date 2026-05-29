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

    public static bool IsValid(string version) => SemverRegex().IsMatch(version);
}
