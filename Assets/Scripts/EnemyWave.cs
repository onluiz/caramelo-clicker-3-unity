using System;
using UnityEngine;

// The demons Caramelo runs into. Each wave is a random group (one, a pair, a trio...) that starts
// `StartDistance` metres down the road; the road scrolls it toward the dog as he runs, and once its front
// is `FightX` pixels in front of him the fight starts. Later waves allow bigger groups. When every demon
// of the group is dead the next wave starts from the beginning of the road.
public class EnemyWave
{
    public const float PixelsPerMeter = 40f;
    public const float StartDistance = 100f; // metres
    const float FightX = 300f;               // where the front of the wave hovers once the fight starts, px in front of the dog

    // Where each demon of a group sits relative to the front one (px behind it, px of hover height).
    static readonly float[] SlotX = { 0f, 100f, 200f, 50f, 150f };
    static readonly float[] SlotHover = { 110f, 200f, 110f, 290f, 380f };

    public int Number { get; private set; } = 1;
    public int TargetLevel = 1;              // level of the demons of the next wave
    public float Distance { get; private set; } = StartDistance; // metres from the dog to the front of the wave
    public bool InRange { get; private set; }
    // The dog stands and fights from the moment the front of the wave arrives until the last demon is gone.
    public bool Engaged => InRange && !AllFinished;
    public int Size { get; private set; }
    public Enemy[] Units => units;

    public Action<Enemy> Tapped, AttackLanded;

    readonly CombatStats template;
    readonly Enemy[] units;
    int shownMetres = -1;

    bool AllFinished
    {
        get
        {
            foreach (var u in units) if (!u.Finished) return false;
            return true;
        }
    }

    public EnemyWave(Transform parent, Vector2 anchor, CombatStats template, int level)
    {
        this.template = template;
        TargetLevel = level;
        units = new Enemy[SlotX.Length];
        for (int i = 0; i < units.Length; i++)
        {
            units[i] = new Enemy(parent, anchor);
            units[i].Tapped = e => Tapped?.Invoke(e);
            units[i].AttackLanded = e => AttackLanded?.Invoke(e);
        }
        Spawn();
    }

    // What a demon of the next wave looks like, for the attributes screen.
    public CombatStats Preview => CombatStats.Scaled(template, TargetLevel);

    // Largest group the wave number allows: 1 at first, then pairs, trios and so on up to 5.
    public static int MaxGroup(int wave)
    {
        if (wave < 3) return 1;
        if (wave < 6) return 2;
        if (wave < 10) return 3;
        if (wave < 15) return 4;
        return 5;
    }

    // The group is random but biased to the biggest size allowed: wave 1-2 are single, 3-5 single or a pair,
    // 6-9 a pair or a trio, and so on.
    static int PickGroupSize(int wave)
    {
        int max = MaxGroup(wave);
        return UnityEngine.Random.Range(Mathf.Max(1, max - 1), max + 1);
    }

    void Spawn()
    {
        Size = PickGroupSize(Number);
        Distance = StartDistance;
        InRange = false;
        shownMetres = -1;

        // Random seating: the demons behind the front one take shuffled slots, so the same group does not
        // look the same twice. The front one always sits in slot 0, so the group starts at the same line.
        var slots = new[] { 1, 2, 3, 4 };
        for (int i = slots.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int t = slots[i]; slots[i] = slots[j]; slots[j] = t;
        }

        for (int i = 0; i < units.Length; i++)
        {
            if (i >= Size) { units[i].Deactivate(); continue; }

            int slot = i == 0 ? 0 : slots[i - 1];
            // Now and then a member is one level above the rest.
            int level = TargetLevel + (i > 0 && UnityEngine.Random.value < 0.3f ? 1 : 0);
            units[i].Activate(CombatStats.Scaled(template, level),
                SlotX[slot] + UnityEngine.Random.Range(-12f, 12f), SlotHover[slot]);
        }
    }

    // The same wave again from the start of the road (after the dog was beaten).
    public void Restart() { Spawn(); }

    public void Reset()
    {
        Number = 1;
        Spawn();
    }

    // `travel` is how many pixels the road moved this frame (the dog's stride).
    public void Tick(float dt, float travel)
    {
        if (!InRange)
        {
            Distance = Mathf.Max(FightX / PixelsPerMeter, Distance - travel / PixelsPerMeter);
            if (Distance * PixelsPerMeter <= FightX)
            {
                InRange = true;
                foreach (var u in units) if (u.InUse) u.Engage();
            }
            else if (Mathf.CeilToInt(Distance) != shownMetres)
            {
                shownMetres = Mathf.CeilToInt(Distance);
                if (units[0].InUse) units[0].SetMeters(shownMetres + " m");
            }
        }

        float frontX = Mathf.Max(FightX, Distance * PixelsPerMeter);
        foreach (var u in units) u.Tick(dt, frontX);

        // The last demon is gone: on to the next wave.
        if (InRange && AllFinished)
        {
            Number++;
            Spawn();
        }
    }

    public void RefreshLabels()
    {
        foreach (var u in units) if (u.InUse) u.RefreshLabel();
    }
}
