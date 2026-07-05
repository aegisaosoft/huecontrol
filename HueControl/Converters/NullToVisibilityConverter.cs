// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HueControl.Converters;

/// <summary>Returns Visible when the bound value is non-null, otherwise Collapsed.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
