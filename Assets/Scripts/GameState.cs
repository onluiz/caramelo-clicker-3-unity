using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public double petiscos;
    public double totalEarned;
    public long clicks;
    public int[] owned;
    public string[] upgrades;
    public long savedAtTicks;
    public double ossos;
    public double ossosEver;
    public double lifetimeBase;
    public int ascensions;
    public string[] blessings;
}

public class GameState
{
    const string SaveKey = "caramelo_clicker_save_v3";
    public const double FrenzyMultiplier = 7;
    public const double ClickFrenzyMultiplier = 777;

    public double Petiscos;
    public double TotalEarned;
    public long Clicks;
    public readonly int[] Owned = new int[GameDefs.Buildings.Length];
    public readonly HashSet<string> Upgrades = new HashSet<string>();

    // Prestige: Ossos is the spendable balance, OssosEver everything ever earned (it drives the bonus).
    public double Ossos;
    public double OssosEver;
    public double LifetimeBase; // petiscos earned in previous runs; TotalEarned is the current run
    public int Ascensions;
    public readonly HashSet<string> Blessings = new HashSet<string>();

    public double Time;
    public double FrenzyUntil;
    public double ClickFrenzyUntil;
    public string OfflineMessage;

    bool dirty = true;
    double baseCps;
    double clickMult = 1;
    double clickPercent;
    double costMult = 1, startPetiscos, goldenInterval = 1;
    double offlineEfficiency = PrestigeDefs.BaseOfflineEfficiency, offlineSeconds = PrestigeDefs.BaseOfflineSeconds;
    readonly double[] buildingMult = new double[GameDefs.Buildings.Length];

    public double LifetimeEarned => LifetimeBase + TotalEarned;
    public double PendingOssos => Math.Max(0, PrestigeDefs.OssosFor(LifetimeEarned) - OssosEver);
    public double Fragments => Math.Max(0, PrestigeDefs.OssosExact(LifetimeEarned) - OssosEver);
    public double PrestigeBonus => OssosEver * PrestigeDefs.BonusPerOsso; // 0.2 = +20%
    public double GoldenInterval { get { Recalc(); return goldenInterval; } }

    public bool FrenzyActive => Time < FrenzyUntil;
    public bool ClickFrenzyActive => Time < ClickFrenzyUntil;

    public double BaseCps { get { Recalc(); return baseCps; } }
    public double Cps => BaseCps * (FrenzyActive ? FrenzyMultiplier : 1);

    public double ClickValue
    {
        get
        {
            Recalc();
            double v = clickMult + baseCps * clickPercent;
            return v * (ClickFrenzyActive ? ClickFrenzyMultiplier : 1);
        }
    }

    public double CpsEach(int building)
    {
        Recalc();
        return GameDefs.Buildings[building].BaseCps * buildingMult[building];
    }

    void Recalc()
    {
        if (!dirty) return;
        dirty = false;

        int clickUpgrades = 0, percentUpgrades = 0, globalUpgrades = 0;
        var tiers = new int[GameDefs.Buildings.Length];
        foreach (var id in Upgrades)
        {
            if (!GameDefs.ById.TryGetValue(id, out var u)) continue;
            switch (u.Kind)
            {
                case UpgradeKind.ClickMultiplier: clickUpgrades++; break;
                case UpgradeKind.ClickPercent: percentUpgrades++; break;
                case UpgradeKind.GlobalMultiplier: globalUpgrades++; break;
                case UpgradeKind.Building: tiers[u.Building]++; break;
            }
        }

        double blessedProduction = 1, blessedClick = 1;
        costMult = 1;
        startPetiscos = 0;
        goldenInterval = 1;
        offlineEfficiency = PrestigeDefs.BaseOfflineEfficiency;
        offlineSeconds = PrestigeDefs.BaseOfflineSeconds;
        foreach (var id in Blessings)
        {
            if (!PrestigeDefs.ById.TryGetValue(id, out var b)) continue;
            switch (b.Effect)
            {
                case BlessingEffect.Production: blessedProduction *= b.Value; break;
                case BlessingEffect.Click: blessedClick *= b.Value; break;
                case BlessingEffect.Discount: costMult *= b.Value; break;
                case BlessingEffect.StartPetiscos: startPetiscos = Math.Max(startPetiscos, b.Value); break;
                case BlessingEffect.GoldenSpeed: goldenInterval *= b.Value; break;
                case BlessingEffect.Offline:
                    offlineEfficiency = Math.Max(offlineEfficiency, b.Value);
                    offlineSeconds = PrestigeDefs.BlessedOfflineSeconds;
                    break;
            }
        }

        clickMult = Math.Pow(2, clickUpgrades) * blessedClick;
        clickPercent = percentUpgrades * 0.01;
        double global = Math.Pow(1.25, globalUpgrades) * (1 + PrestigeBonus) * blessedProduction;

        baseCps = 0;
        for (int i = 0; i < buildingMult.Length; i++)
        {
            buildingMult[i] = Math.Pow(2, tiers[i]) * global;
            baseCps += Owned[i] * GameDefs.Buildings[i].BaseCps * buildingMult[i];
        }
    }

    public void Tick(double dt)
    {
        Time += dt;
        double gain = Cps * dt;
        Petiscos += gain;
        TotalEarned += gain;
    }

    public double Click()
    {
        double v = ClickValue;
        Petiscos += v;
        TotalEarned += v;
        Clicks++;
        return v;
    }

    public double BuyCost(int building, int count)
    {
        Recalc();
        double g = GameDefs.CostGrowth;
        double first = GameDefs.Buildings[building].BaseCost * costMult * Math.Pow(g, Owned[building]);
        return first * (Math.Pow(g, count) - 1) / (g - 1);
    }

    public int MaxAffordable(int building)
    {
        Recalc();
        double g = GameDefs.CostGrowth;
        double first = GameDefs.Buildings[building].BaseCost * costMult * Math.Pow(g, Owned[building]);
        if (Petiscos < first) return 0;
        return (int)Math.Floor(Math.Log(1 + Petiscos * (g - 1) / first) / Math.Log(g));
    }

    public bool TryBuy(int building, int count)
    {
        if (count <= 0) return false;
        double cost = BuyCost(building, count);
        if (Petiscos < cost) return false;
        Petiscos -= cost;
        Owned[building] += count;
        dirty = true;
        return true;
    }

    public bool IsBuildingKnown(int building)
    {
        return Owned[building] > 0 || TotalEarned >= GameDefs.Buildings[building].BaseCost * 0.5;
    }

    public bool IsUpgradeAvailable(UpgradeDef u)
    {
        if (Upgrades.Contains(u.Id)) return false;
        if (u.Kind == UpgradeKind.Building) return Owned[u.Building] >= u.RequiredOwned;
        return TotalEarned >= u.Cost * 0.5;
    }

    public bool TryBuyUpgrade(UpgradeDef u)
    {
        if (!IsUpgradeAvailable(u) || Petiscos < u.Cost) return false;
        Petiscos -= u.Cost;
        Upgrades.Add(u.Id);
        dirty = true;
        return true;
    }

    // Returns the message to show the player.
    public string ApplyGoldenBone()
    {
        double roll = UnityEngine.Random.value;
        if (roll < 0.10)
        {
            ClickFrenzyUntil = Math.Max(ClickFrenzyUntil, Time) + 13;
            return Loc.F("Pata Furiosa! Seus cliques valem x{0} por 13s!", ClickFrenzyMultiplier);
        }
        if (roll < 0.45)
        {
            double gain = Math.Min(Petiscos * 0.15, BaseCps * 900) + 13;
            Petiscos += gain;
            TotalEarned += gain;
            return Loc.F("Sorte! +{0} petiscos!", NumberFormat.Format(gain));
        }
        FrenzyUntil = Math.Max(FrenzyUntil, Time) + 30;
        return Loc.F("Frenesi! Produção x{0} por 30s!", FrenzyMultiplier);
    }

    public bool IsBlessed(BlessingDef b) { return Blessings.Contains(b.Id); }

    public bool TryBuyBlessing(BlessingDef b)
    {
        if (IsBlessed(b) || Ossos < b.Cost) return false;
        Ossos -= b.Cost;
        Blessings.Add(b.Id);
        dirty = true;
        return true;
    }

    // Starts a new run: keeps ossos, blessings, clicks and lifetime totals; everything else resets.
    // Returns the ossos gained (0 means nothing happened).
    public double Ascend()
    {
        double gained = PendingOssos;
        if (gained < 1) return 0;

        Ossos += gained;
        OssosEver += gained;
        LifetimeBase += TotalEarned;
        Ascensions++;

        TotalEarned = 0;
        Array.Clear(Owned, 0, Owned.Length);
        Upgrades.Clear();
        FrenzyUntil = 0;
        ClickFrenzyUntil = 0;
        dirty = true;
        Recalc();
        Petiscos = startPetiscos;
        return gained;
    }

    // Test helper for the Dev tab.
    public void DevAddOssos(double amount)
    {
        Ossos += amount;
        OssosEver += amount;
        dirty = true;
    }

    public void ResetAll()
    {
        Petiscos = 0;
        TotalEarned = 0;
        Clicks = 0;
        Ossos = 0;
        OssosEver = 0;
        LifetimeBase = 0;
        Ascensions = 0;
        Blessings.Clear();
        Array.Clear(Owned, 0, Owned.Length);
        Upgrades.Clear();
        FrenzyUntil = 0;
        ClickFrenzyUntil = 0;
        dirty = true;
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    public void Save()
    {
        var data = new SaveData
        {
            petiscos = Petiscos,
            totalEarned = TotalEarned,
            clicks = Clicks,
            owned = (int[])Owned.Clone(),
            upgrades = new string[Upgrades.Count],
            savedAtTicks = DateTime.UtcNow.Ticks,
            ossos = Ossos,
            ossosEver = OssosEver,
            lifetimeBase = LifetimeBase,
            ascensions = Ascensions,
            blessings = new string[Blessings.Count],
        };
        Upgrades.CopyTo(data.upgrades);
        Blessings.CopyTo(data.blessings);
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(json); }
        catch (Exception) { return; }
        if (data == null) return;

        Petiscos = data.petiscos;
        TotalEarned = data.totalEarned;
        Clicks = data.clicks;
        if (data.owned != null)
            Array.Copy(data.owned, Owned, Math.Min(data.owned.Length, Owned.Length));
        Upgrades.Clear();
        if (data.upgrades != null)
            foreach (var id in data.upgrades)
                if (GameDefs.ById.ContainsKey(id)) Upgrades.Add(id);

        Ossos = data.ossos;
        OssosEver = data.ossosEver;
        LifetimeBase = data.lifetimeBase;
        Ascensions = data.ascensions;
        Blessings.Clear();
        if (data.blessings != null)
            foreach (var id in data.blessings)
                if (PrestigeDefs.ById.ContainsKey(id)) Blessings.Add(id);
        dirty = true;

        if (data.savedAtTicks > 0)
        {
            double away = (DateTime.UtcNow - new DateTime(data.savedAtTicks, DateTimeKind.Utc)).TotalSeconds;
            Recalc();
            away = Math.Min(away, offlineSeconds);
            double gain = BaseCps * away * offlineEfficiency;
            if (away > 60 && gain >= 1)
            {
                Petiscos += gain;
                TotalEarned += gain;
                OfflineMessage = Loc.F("Bem-vindo de volta! Enquanto você estava fora, o Caramelo juntou {0} petiscos.", NumberFormat.Format(gain));
            }
        }
    }
}
