// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Media;
using HueControl.Services;

namespace HueControl.Tests;

public class ColorMathTests
{
    [Fact]
    public void RgbToXy_Red_LandsInRedRegion()
    {
        (double x, double y) = ColorMath.RgbToXy(Color.FromRgb(255, 0, 0));
        Assert.InRange(x, 0.60, 0.75);
        Assert.InRange(y, 0.28, 0.36);
    }

    [Fact]
    public void RgbToXy_Blue_LandsInBlueRegion()
    {
        (double x, double y) = ColorMath.RgbToXy(Color.FromRgb(0, 0, 255));
        Assert.InRange(x, 0.10, 0.20);
        Assert.InRange(y, 0.02, 0.10);
    }

    [Fact]
    public void RgbToXy_Black_ReturnsOrigin()
    {
        (double x, double y) = ColorMath.RgbToXy(Color.FromRgb(0, 0, 0));
        Assert.Equal(0.0, x);
        Assert.Equal(0.0, y);
    }

    [Fact]
    public void XyToRgb_ProducesValidColor()
    {
        Color c = ColorMath.XyToRgb(0.4, 0.4, 200);
        // Bytes are always valid; assert it is not fully black for a lit point.
        Assert.True(c.R + c.G + c.B > 0);
    }

    [Fact]
    public void ColorTemperature_HighMired_IsWarmerThanLowMired()
    {
        Color warm = ColorMath.ColorTemperatureToRgb(500); // ~2000K
        Color cool = ColorMath.ColorTemperatureToRgb(153); // ~6500K

        // Warm light is red-dominant and carries much less blue than cool light.
        Assert.True(warm.R >= warm.B);
        Assert.True(warm.B < cool.B);
    }
}
