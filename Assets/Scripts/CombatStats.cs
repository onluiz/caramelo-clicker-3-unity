using UnityEngine;

// A fighter's base attributes and everything derived from them. Derived values are plain formulas
// (no state), so anything that has stats, the dog today and enemies later, can attack or be attacked.
//
//   Força        -> attack damage
//   Vitalidade   -> max HP and defense
//   Inteligência -> critical damage
//   Destreza     -> accuracy (against the target's agility)
//   Sorte        -> critical chance
//   Agilidade    -> evasion (against the attacker's dexterity)
//   Nível        -> adds a little to HP and attack
public class CombatStats
{
    public int Level = 1;
    public int Strength, Vitality, Intelligence, Dexterity, Luck, Agility;

    public CombatStats(int level, int strength, int vitality, int intelligence, int dexterity, int luck, int agility)
    {
        Level = level;
        Strength = strength;
        Vitality = vitality;
        Intelligence = intelligence;
        Dexterity = dexterity;
        Luck = luck;
        Agility = agility;
    }

    public float MaxHp => 20f + Vitality * 8f + Level * 4f;
    public float Attack => 4f + Strength * 2f + Level;
    public float Defense => Vitality * 0.5f;
    public float CritChance => Mathf.Clamp(Luck * 0.015f, 0f, 0.6f);
    public float CritMultiplier => 1.5f + Intelligence * 0.05f;

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
        if (Random.value >= HitChance(target))
        {
            r.Missed = true;
            return r;
        }

        float dmg = Attack * Random.Range(0.9f, 1.1f);
        r.Critical = Random.value < CritChance;
        if (r.Critical) dmg *= CritMultiplier;
        r.Damage = Mathf.Max(1, Mathf.RoundToInt(dmg - target.Defense));
        return r;
    }
}
