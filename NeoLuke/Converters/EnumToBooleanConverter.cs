using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace NeoLuke.Converters;

/// <summary>
/// Converts an enum value to a boolean for radio button binding
/// </summary>
public class EnumToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        var enumValue = value.ToString();
        var paramValue = parameter.ToString();

        return enumValue?.Equals(paramValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter != null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }

        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
