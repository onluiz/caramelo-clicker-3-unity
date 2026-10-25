using System;
using System.Globalization;

public static class NumberFormat
{
    static readonly string[] Suffixes =
    {
        "", " mil", " mi", " bi", " tri", " quad", " quint", " sext", " sept", " oct", " non", " dec",
    };

    // Formats using the pt-BR convention (dot for thousands, comma for decimals).
    // WebGL builds ship with limited culture data, so this is done by hand.
    public static string Format(double v, bool decimals = false)
    {
        if (double.IsNaN(v) || v < 0) v = 0;

        if (v < 1000)
        {
            if (decimals && v < 100)
                return (Math.Floor(v * 10) / 10).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
            return Math.Floor(v).ToString("0", CultureInfo.InvariantCulture);
        }

        if (v < 1e6)
            return Math.Floor(v).ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.');

        int idx = 0;
        double scaled = v;
        while (scaled >= 1000)
        {
            scaled /= 1000;
            idx++;
        }

        if (idx >= Suffixes.Length)
            return v.ToString("0.00e+0", CultureInfo.InvariantCulture);

        return (Math.Floor(scaled * 100) / 100).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',') + Suffixes[idx];
    }
}
