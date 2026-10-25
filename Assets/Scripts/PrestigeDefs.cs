using System;
using System.Collections.Generic;

public enum BlessingEffect { Production, Click, Discount, StartPetiscos, GoldenSpeed, Offline }

public class BlessingDef
{
    public string Id;
    public string Name;
    public string Description;
    public double Cost; // in Ossos Celestiais
    public BlessingEffect Effect;
    public double Value;
}

// Prestige ("Ascensão"): resetting a run grants Ossos Celestiais based on everything earned across all runs.
// Every osso ever earned adds a permanent production bonus, and unspent ossos buy permanent blessings.
public static class PrestigeDefs
{
    // Lifetime petiscos needed for the first osso. Ossos grow with the cube root of lifetime earnings,
    // so doubling the osso count asks for 8x more petiscos.
    public const double Threshold = 1e8;
    public const double BonusPerOsso = 0.02;
    public const double BaseOfflineEfficiency = 0.5;
    public const double BaseOfflineSeconds = 8 * 3600;
    public const double BlessedOfflineSeconds = 12 * 3600;

    public static string NameOf(BlessingDef b) { return Loc.BlessingName(b.Id, b.Name); }
    public static string DescOf(BlessingDef b) { return Loc.BlessingDesc(b.Id, b.Description); }

    public static readonly BlessingDef[] Blessings =
    {
        new BlessingDef { Id = "mochila", Name = "Mochila de Petiscos", Description = "Cada vida começa com 10.000 petiscos.", Cost = 1, Effect = BlessingEffect.StartPetiscos, Value = 1e4 },
        new BlessingDef { Id = "faro", Name = "Faro Divino", Description = "Seus cliques rendem o dobro.", Cost = 2, Effect = BlessingEffect.Click, Value = 2 },
        new BlessingDef { Id = "patas", Name = "Patas Celestiais", Description = "Toda a produção aumenta em 25%.", Cost = 4, Effect = BlessingEffect.Production, Value = 1.25 },
        new BlessingDef { Id = "pechincha1", Name = "Pechincha Celestial", Description = "Construções 5% mais baratas.", Cost = 6, Effect = BlessingEffect.Discount, Value = 0.95 },
        new BlessingDef { Id = "osso", Name = "Osso Brilhante", Description = "Ossos dourados aparecem 30% mais rápido.", Cost = 9, Effect = BlessingEffect.GoldenSpeed, Value = 0.7 },
        new BlessingDef { Id = "sono", Name = "Sono Profundo", Description = "Offline o Caramelo rende 80% (em vez de 50%) por até 12h.", Cost = 14, Effect = BlessingEffect.Offline, Value = 0.8 },
        new BlessingDef { Id = "cofre", Name = "Cofre Celestial", Description = "Cada vida começa com 10 milhões de petiscos.", Cost = 25, Effect = BlessingEffect.StartPetiscos, Value = 1e7 },
        new BlessingDef { Id = "bencao", Name = "Bênção do Caramelo", Description = "Toda a produção dobra.", Cost = 40, Effect = BlessingEffect.Production, Value = 2 },
        new BlessingDef { Id = "pechincha2", Name = "Pechincha Divina", Description = "Construções 10% mais baratas.", Cost = 70, Effect = BlessingEffect.Discount, Value = 0.9 },
        new BlessingDef { Id = "lenda", Name = "Lenda Canina", Description = "Toda a produção triplica.", Cost = 120, Effect = BlessingEffect.Production, Value = 3 },
    };

    public static readonly Dictionary<string, BlessingDef> ById = BuildIndex();

    static Dictionary<string, BlessingDef> BuildIndex()
    {
        var map = new Dictionary<string, BlessingDef>();
        foreach (var b in Blessings) map[b.Id] = b;
        return map;
    }

    // Total ossos a player with this much lifetime production has earned (spent or not).
    public static double OssosFor(double lifetimeEarned)
    {
        if (lifetimeEarned < Threshold) return 0;
        return Math.Floor(Math.Pow(lifetimeEarned / Threshold, 1.0 / 3.0) + 1e-9);
    }
}
