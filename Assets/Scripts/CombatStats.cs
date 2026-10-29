using System;
using UnityEngine;

public enum Stat { Strength, Vitality, Intelligence, Dexterity, Luck, Agility }

// A fighter's attributes, level, HP and everything derived from them. Derived values are plain formulas,
// so anything that has stats, the dog today and enemies later, can attack or be attacked. Serializable
// so the dog's progress can be saved with JsonUtility.
//
//   Força        -> attack damage
//   Vitalidade   -> max HP and defense
//   Inteligência -> critical damage
//   Destreza     -> accuracy (against the target's agility)
//   Sorte        -> critical chance
//   Agilidade    -> evasion (against the attacker's dexterity)
//   Nível        -> adds a little to HP and attack
[Serializable]
public class CombatStats
{
    public const int PointsPerLevel = 3;

    public int Level = 1;
    public int Strength, Vitality, Intelligence, Dexterity, Luck, Agility;
    public int Xp, StatPoints;
    public float Hp;

    public CombatStats(int level, int strength, int vitality, int intelligence, int dexterity, int luck, int agility)
    {
        Level = level;
        Strength = strength;
        Vitality = vitality;
        Intelligence = intelligence;
        Dexterity = dexterity;
        Luck = luck;
        Agility = agility;
        Hp = MaxHp;
    }

    // An enemy template at a given level: Strength and Vitality grow by 1 per level, the rest by 1 every 2 levels.
    public static CombatStats Scaled(CombatStats template, int level)
    {
        int up = Mathf.Max(0, level - 1);
        return new CombatStats(level, template.Strength + up, template.Vitality + up, template.Intelligence + up / 2,
            template.Dexterity + up / 2, template.Luck + up / 2, template.Agility + up / 2);
    }

    public float MaxHp => 20f + Vitality * 8f + Level * 4f;
    public float Attack => 4f + Strength * 2f + Level;
    public float Defense => Vitality * 0.5f;
    public float CritChance => Mathf.Clamp(Luck * 0.015f, 0f, 0.6f);
    public float CritMultiplier => 1.5f + Intelligence * 0.05f;
    public bool IsDead => Hp <= 0f;
    public int XpToNext => 20 + 20 * Level;

    public int Get(Stat stat)
    {
        switch (stat)
        {
            case Stat.Strength: return Strength;
            case Stat.Vitality: return Vitality;
            case Stat.Intelligence: return Intelligence;
            case Stat.Dexterity: return Dexterity;
            case Stat.Luck: return Luck;
            default: return Agility;
        }
    }

    // Spends one stat point on `stat`. More vitality also heals what it adds to the max HP.
    public bool Spend(Stat stat)
    {
        if (StatPoints <= 0) return false;
        StatPoints--;
        switch (stat)
        {
            case Stat.Strength: Strength++; break;
            case Stat.Vitality: Vitality++; Hp += 8f; break;
            case Stat.Intelligence: Intelligence++; break;
            case Stat.Dexterity: Dexterity++; break;
            case Stat.Luck: Luck++; break;
            default: Agility++; break;
        }
        return true;
    }

    // Adds experience and returns how many levels it gained. Levelling up gives stat points and heals fully.
    public int AddXp(int amount)
    {
        Xp += amount;
        int gained = 0;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            StatPoints += PointsPerLevel;
            gained++;
        }
        if (gained > 0) Hp = MaxHp;
        return gained;
    }

    // Keeps HP sane after loading a save made with different formulas.
    public void ClampHp()
    {
        if (Hp <= 0f || Hp > MaxHp) Hp = MaxHp;
    }

    // Chance that this fighter's attack lands on `target`: 90% when dexterity equals agility,
    // 2 points per point of difference, never below 10% and never above 100%.
    public float HitChance(CombatStats target)
    {
        return Mathf.Clamp(0.9f + 0.02f * (Dexterity - target.Agility), 0.1f, 1f);
    }

    public struct AttackResult
    {
        public bool Missed, Critical;
        public int Damage;
    }

    public AttackResult Attacks(CombatStats target)
    {
        var r = new AttackResult();
        if (UnityEngine.Random.value >= HitChance(target))
        {
            r.Missed = true;
            return r;
        }

        float dmg = Attack * UnityEngine.Random.Range(0.9f, 1.1f);
        r.Critical = UnityEngine.Random.value < CritChance;
        if (r.Critical) dmg *= CritMultiplier;
        r.Damage = Mathf.Max(1, Mathf.RoundToInt(dmg - target.Defense));
        return r;
    }

    public void TakeDamage(int amount)
    {
        Hp = Mathf.Max(0f, Hp - amount);
    }
}
