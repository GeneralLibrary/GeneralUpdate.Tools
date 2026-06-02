using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace GeneralUpdate.Tools.Converters;

/// <summary>
/// Returns true when the bound int value equals the converter parameter (int).
/// Used for conditional visibility based on enum/combo index.
/// </summary>
public class IntEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intVal && parameter != null && int.TryParse(parameter.ToString(), out var target))
            return intVal == target;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
