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
}

public class GameState
{
    const string SaveKey = "caramelo_clicker_save_v1";
    const double OfflineEfficiency = 0.5;
    const double MaxOfflineSeconds = 8 * 3600;
    public const double FrenzyMultiplier = 7;
    public const double ClickFrenzyMultiplier = 777;

    public double Petiscos;
    public double TotalEarned;
    public long Clicks;
    public readonly int[] Owned = new int[GameDefs.Buildings.Length];
    public readonly HashSet<string> Upgrades = new HashSet<string>();

    public double Time;
    public double FrenzyUntil;
    public double ClickFrenzyUntil;
    public string OfflineMessage;

    bool dirty = true;
    double baseCps;
    double clickMult = 1;
    double clickPercent;
    readonly double[] buildingMult = new double[GameDefs.Buildings.Length];

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

        clickMult = Math.Pow(2, clickUpgrades);
        clickPercent = percentUpgrades * 0.01;
        double global = Math.Pow(1.25, globalUpgrades);

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
        double g = GameDefs.CostGrowth;
        double first = GameDefs.Buildings[building].BaseCost * Math.Pow(g, Owned[building]);
        return first * (Math.Pow(g, count) - 1) / (g - 1);
    }

    public int MaxAffordable(int building)
    {
        double g = GameDefs.CostGrowth;
        double first = GameDefs.Buildings[building].BaseCost * Math.Pow(g, Owned[building]);
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
            return "Pata Furiosa! Seus cliques valem x" + ClickFrenzyMultiplier + " por 13s!";
        }
        if (roll < 0.45)
        {
            double gain = Math.Min(Petiscos * 0.15, BaseCps * 900) + 13;
            Petiscos += gain;
            TotalEarned += gain;
            return "Sorte! +" + NumberFormat.Format(gain) + " petiscos!";
        }
        FrenzyUntil = Math.Max(FrenzyUntil, Time) + 30;
        return "Frenesi! Produção x" + FrenzyMultiplier + " por 30s!";
    }

    public void ResetAll()
    {
        Petiscos = 0;
        TotalEarned = 0;
        Clicks = 0;
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
        };
        Upgrades.CopyTo(data.upgrades);
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
        dirty = true;

        if (data.savedAtTicks > 0)
        {
            double away = (DateTime.UtcNow - new DateTime(data.savedAtTicks, DateTimeKind.Utc)).TotalSeconds;
            away = Math.Min(away, MaxOfflineSeconds);
            double gain = BaseCps * away * OfflineEfficiency;
            if (away > 60 && gain >= 1)
            {
                Petiscos += gain;
                TotalEarned += gain;
                OfflineMessage = "Bem-vindo de volta! Enquanto você estava fora, o Caramelo juntou " + NumberFormat.Format(gain) + " petiscos.";
            }
        }
    }
}
