// Copyright (c) Aegis AO Soft LLC and Alexander Orlov. All rights reserved.

using System.Windows.Media;

namespace HueControl.Services;

/// <summary>Conversions between sRGB and the CIE xy colour space used by Hue lights.</summary>
public static class ColorMath
{
    /// <summary>Converts an sRGB colour to a Hue xy chromaticity coordinate.</summary>
    public static (double X, double Y) RgbToXy(Color color)
    {
        double r = GammaCorrect(color.R / 255.0);
        double g = GammaCorrect(color.G / 255.0);
        double b = GammaCorrect(color.B / 255.0);

        // Wide-gamut RGB D65 conversion matrix used by the Hue reference implementation.
        double bigX = r * 0.664511 + g * 0.154324 + b * 0.162028;
        double bigY = r * 0.283881 + g * 0.668433 + b * 0.047685;
        double bigZ = r * 0.000088 + g * 0.072310 + b * 0.986039;

        double sum = bigX + bigY + bigZ;
        if (sum <= 0.0)
            return (0.0, 0.0);

        return (bigX / sum, bigY / sum);
    }

    /// <summary>Approximate xy + brightness back to an sRGB colour for UI swatches.</summary>
    public static Color XyToRgb(double x, double y, int brightness)
    {
        if (y <= 0.0)
            y = 0.0001;

        double bigY = Math.Clamp(brightness / 254.0, 0.0, 1.0);
        double bigX = (bigY / y) * x;
        double bigZ = (bigY / y) * (1.0 - x - y);

        double r = bigX * 1.656492 - bigY * 0.354851 - bigZ * 0.255038;
        double g = -bigX * 0.707196 + bigY * 1.655397 + bigZ * 0.036152;
        double b = bigX * 0.051713 - bigY * 0.121364 + bigZ * 1.011530;

        r = GammaExpand(r);
        g = GammaExpand(g);
        b = GammaExpand(b);

        double max = Math.Max(r, Math.Max(g, b));
        if (max > 1.0)
        {
            r /= max;
            g /= max;
            b /= max;
        }

        return Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));
    }

    /// <summary>Approximates a mired colour temperature as an sRGB colour for UI swatches.</summary>
    public static Color ColorTemperatureToRgb(int mired)
    {
        double kelvin = Math.Clamp(1_000_000.0 / Math.Max(mired, 1), 2000, 6500);
        double temp = kelvin / 100.0;

        double r, g, b;
        if (temp <= 66)
        {
            r = 255;
            g = 99.4708025861 * Math.Log(temp) - 161.1195681661;
        }
        else
        {
            r = 329.698727446 * Math.Pow(temp - 60, -0.1332047592);
            g = 288.1221695283 * Math.Pow(temp - 60, -0.0755148492);
        }

        if (temp >= 66)
            b = 255;
        else if (temp <= 19)
            b = 0;
        else
            b = 138.5177312231 * Math.Log(temp - 10) - 305.0447927307;

        return Color.FromRgb(ToByte(r / 255.0), ToByte(g / 255.0), ToByte(b / 255.0));
    }

    private static double GammaCorrect(double value)
        => value > 0.04045 ? Math.Pow((value + 0.055) / 1.055, 2.4) : value / 12.92;

    private static double GammaExpand(double value)
        => value <= 0.0031308 ? 12.92 * value : 1.055 * Math.Pow(value, 1.0 / 2.4) - 0.055;

    private static byte ToByte(double value) => (byte)Math.Clamp(value * 255.0, 0, 255);
}
