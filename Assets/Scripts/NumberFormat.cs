using System;
using System.Globalization;

public static class NumberFormat
{
    static readonly string[] Suffixes =
    {
        "", " mil", " mi", " bi", " tri", " quad", " quint", " sext", " sept", " oct", " non", " dec",
    };

    static readonly string[] SuffixesEn =
    {
        "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc",
    };

    // Two decimals below 1000 (fragments of a bone), the usual abbreviation above.
    public static string Fraction(double v)
    {
        if (double.IsNaN(v) || v < 0) v = 0;
        if (v >= 1000) return Format(v);
        string n = (Math.Floor(v * 100) / 100).ToString("0.00", CultureInfo.InvariantCulture);
        return Loc.IsEnglish ? n : n.Replace('.', ',');
    }

    // Formats using the pt-BR convention (dot for thousands, comma for decimals), or the English one.
    // WebGL builds ship with limited culture data, so this is done by hand.
    public static string Format(double v, bool decimals = false)
    {
        if (double.IsNaN(v) || v < 0) v = 0;

        bool en = Loc.IsEnglish;
        char dec = en ? '.' : ',';

        if (v < 1000)
        {
            if (decimals && v < 100)
                return (Math.Floor(v * 10) / 10).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', dec);
            return Math.Floor(v).ToString("0", CultureInfo.InvariantCulture);
        }

        if (v < 1e6)
        {
            string grouped = Math.Floor(v).ToString("N0", CultureInfo.InvariantCulture);
            return en ? grouped : grouped.Replace(',', '.');
        }

        int idx = 0;
        double scaled = v;
        while (scaled >= 1000)
        {
            scaled /= 1000;
            idx++;
        }

        var suffixes = en ? SuffixesEn : Suffixes;
        if (idx >= suffixes.Length)
            return v.ToString("0.00e+0", CultureInfo.InvariantCulture);

        string number = (Math.Floor(scaled * 100) / 100).ToString("0.00", CultureInfo.InvariantCulture).Replace('.', dec);
        return en ? number + " " + suffixes[idx] : number + suffixes[idx];
    }
}
