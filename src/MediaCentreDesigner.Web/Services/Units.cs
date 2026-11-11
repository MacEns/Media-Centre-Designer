using System.Globalization;

namespace MediaCentreDesigner.Services;

public static class Units
{
    /// <summary>Formats inches as a fraction rounded to the nearest 1/16", e.g. 23 1/2".</summary>
    public static string Fmt(double inches)
    {
        var sixteenths = (long)Math.Round(inches * 16, MidpointRounding.AwayFromZero);
        var whole = sixteenths / 16;
        var rem = (int)(sixteenths % 16);
        if (rem == 0) return $"{whole}\"";
        var g = Gcd(rem, 16);
        var frac = $"{rem / g}/{16 / g}";
        return whole == 0 ? $"{frac}\"" : $"{whole} {frac}\"";
    }

    public static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
