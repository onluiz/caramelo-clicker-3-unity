using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A flying demon that waits far down the road and fights Caramelo when he gets there. It starts
// `StartDistance` metres away; the road scrolls it toward the dog as he runs, and once it is `FightX`
// pixels in front of him the fight starts: tapping it is an attack from the dog, and it attacks back
// every few seconds. At 0 HP it plays its death animation, fades, and a new demon (at `TargetLevel`)
// starts again from `StartDistance`.
//
// Frames come from Resources/Enemy/demon-{flying,hurt,attack,death}.png, sheets of the Flying Demon
// pack (81x71 frames in a row).
public class Enemy
{
    public const float PixelsPerMeter = 40f;
    public const float StartDistance = 100f; // metres
    const float FightX = 300f;               // where it hovers once the fight starts, px in front of the dog

    const int FrameWidth = 81;
    const int FrameHeight = 71;
    const float Scale = 3f;              // screen pixels per sprite pixel
    const float HoverHeight = 120f;      // centre height above the dog's paws
    const float BobAmplitude = 14f;
    const float BobSpeed = 2.2f;
    const float FlyFps = 8f;
    const float HurtFps = 14f;
    const float AttackFps = 12f;
    const float DeathFps = 10f;
    const float FadeTime = 0.5f;         // after the death animation
    const float RespawnDelay = 1.5f;     // after the fade

    const float AttackInterval = 2.4f;   // seconds between its attacks
    const int AttackHitFrame = 4;        // frame of the attack animation where the blow lands
    const float LungeDistance = 50f;     // how far it darts toward the dog while attacking

    const float ShadowWidth = 170f;
    const float ShadowHeight = 20f;
    const float ShadowAlpha = 0.16f;     // lighter than the dog's: the demon is off the ground

    enum State { Flying, Hurt, Attacking, Dying, Gone }

    public CombatStats Stats { get; private set; }
    public float Distance { get; private set; } = StartDistance; // metres from the dog
    public bool InRange { get; private set; }                    // the fight is on
    public bool Alive => state != State.Dying && state != State.Gone;
    // The dog stands and fights from the moment the demon arrives until it is gone.
    public bool Engaged => InRange && state != State.Gone;
    public int XpReward => 12 + 6 * Stats.Level;
    public int TargetLevel = 1;          // level of the next demon to appear
    public Action Tapped;                // the dog attacked it
    public Action AttackLanded;          // its blow reached the dog

    readonly CombatStats template;
    readonly RectTransform rt, shadowRt, parentRt;
    readonly Image image, shadowImage;
    readonly HealthBar bar;
    readonly Sprite[] flyFrames, hurtFrames, attackFrames, deathFrames;

    State state = State.Flying;
    float time, stateTime, attackTimer;
    bool blowLanded;
    int shownMetres = -1;
    Sprite shown;

    public Enemy(Transform parent, Vector2 anchor, CombatStats template, int level)
    {
        this.template = template;
        TargetLevel = level;
        Stats = CombatStats.Scaled(template, level);
        parentRt = (RectTransform)parent;

        // Drawn first so it sits behind the demon.
        var shadowGo = new GameObject("EnemyShadow", typeof(RectTransform));
        shadowGo.transform.SetParent(parent, false);
        shadowRt = (RectTransform)shadowGo.transform;
        shadowRt.anchorMin = shadowRt.anchorMax = anchor;
        shadowRt.sizeDelta = new Vector2(ShadowWidth, ShadowHeight);
        shadowImage = shadowGo.AddComponent<Image>();
        shadowImage.sprite = ProceduralArt.Blob();
        shadowImage.color = new Color(0.3f, 0.12f, 0f, ShadowAlpha);
        shadowImage.raycastTarget = false;

        var go = new GameObject("Enemy", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(FrameWidth * Scale, FrameHeight * Scale);

        image = go.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false; // only tappable once the fight is on
        var trigger = go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(_ => { if (Alive && InRange) Tapped?.Invoke(); });
        trigger.triggers.Add(entry);

        // Name and HP over the demon's head (a child, so it moves and fades with it).
        bar = new HealthBar(rt, new Vector2(0.5f, 1f), new Vector2(0f, 4f), ProceduralArt.Hex("#E2483D"));

        flyFrames = LoadSheet("Enemy/demon-flying");
        hurtFrames = LoadSheet("Enemy/demon-hurt");
        attackFrames = LoadSheet("Enemy/demon-attack");
        deathFrames = LoadSheet("Enemy/demon-death");

        RefreshLabel();
        Show(flyFrames, 0);
        Place(0f, 0f);
    }

    static Sprite[] LoadSheet(string path)
    {
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogError("Demon sprite sheet not found at Resources/" + path);
            return new Sprite[0];
        }

        tex.filterMode = FilterMode.Point;
        var frames = new Sprite[tex.width / FrameWidth];
        for (int i = 0; i < frames.Length; i++)
        {
            var rect = new Rect(i * FrameWidth, 0f, FrameWidth, FrameHeight);
            frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        return frames;
    }

    // "Demônio Nv.2", plus the distance while it is still on its way.
    public void RefreshLabel()
    {
        string text = Loc.T("Demônio") + " " + Loc.T("Nv.") + Stats.Level;
        if (!InRange) text += "  " + Mathf.CeilToInt(Distance) + " m";
        bar.SetLabel(text);
    }

    // Screen position of the demon's HP bar, for floating texts.
    public Vector2 ScreenPosition => bar.ScreenPosition;

    void Show(Sprite[] frames, int index)
    {
        if (frames.Length == 0) return;
        var sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        if (sprite == shown) return;
        shown = sprite;
        image.sprite = sprite;
    }

    // x is how far in front of the dog it is, on screen.
    float ScreenX => Mathf.Max(FightX, Distance * PixelsPerMeter);

    void Place(float bob, float lunge)
    {
        float x = ScreenX - lunge;
        rt.anchoredPosition = new Vector2(x, DogRig.GroundY + HoverHeight + bob);
        shadowRt.anchoredPosition = new Vector2(x, DogRig.GroundY - 4f);
    }

    // Applies a hit from the dog. A miss does nothing here; the caller shows it.
    public void TakeHit(CombatStats.AttackResult hit)
    {
        if (!Alive || hit.Missed) return;

        Stats.TakeDamage(hit.Damage);
        bar.SetFill(Stats.Hp / Stats.MaxHp);
        stateTime = 0f;
        state = Stats.IsDead ? State.Dying : State.Hurt;
        if (state == State.Dying) image.raycastTarget = false;
    }

    // `travel` is how many pixels the road moved this frame (the dog's stride).
    public void Tick(float dt, float travel)
    {
        time += dt;
        stateTime += dt;

        if (!InRange && state != State.Gone)
        {
            Distance = Mathf.Max(FightX / PixelsPerMeter, Distance - travel / PixelsPerMeter);
            if (Distance * PixelsPerMeter <= FightX)
            {
                InRange = true;
                image.raycastTarget = true;
                attackTimer = 0f;
                RefreshLabel();
            }
            else if (Mathf.CeilToInt(Distance) != shownMetres)
            {
                shownMetres = Mathf.CeilToInt(Distance);
                RefreshLabel();
            }
        }

        float bob = Mathf.Sin(time * BobSpeed) * BobAmplitude;
        float lunge = 0f;
        float alpha = 1f;

        switch (state)
        {
            case State.Flying:
                Show(flyFrames, (int)(time * FlyFps) % Mathf.Max(1, flyFrames.Length));
                if (InRange)
                {
                    attackTimer += dt;
                    if (attackTimer >= AttackInterval) StartAttack();
                }
                break;

            case State.Hurt:
                {
                    int frame = (int)(stateTime * HurtFps);
                    if (frame >= hurtFrames.Length)
                    {
                        state = State.Flying;
                        goto case State.Flying;
                    }
                    Show(hurtFrames, frame);
                    break;
                }

            case State.Attacking:
                {
                    int frame = (int)(stateTime * AttackFps);
                    if (frame >= attackFrames.Length)
                    {
                        state = State.Flying;
                        goto case State.Flying;
                    }
                    Show(attackFrames, frame);
                    // Darts toward the dog and back around the moment the blow lands.
                    lunge = LungeDistance * Mathf.Sin(Mathf.PI * Mathf.Clamp01((float)frame / attackFrames.Length));
                    if (!blowLanded && frame >= AttackHitFrame)
                    {
                        blowLanded = true;
                        AttackLanded?.Invoke();
                    }
                    break;
                }

            case State.Dying:
                {
                    float animLength = deathFrames.Length / DeathFps;
                    Show(deathFrames, (int)(stateTime * DeathFps));
                    bob *= Mathf.Clamp01(1f - stateTime / animLength); // settles as it dies
                    if (stateTime > animLength) alpha = Mathf.Clamp01(1f - (stateTime - animLength) / FadeTime);
                    if (stateTime > animLength + FadeTime)
                    {
                        state = State.Gone;
                        stateTime = 0f;
                    }
                    break;
                }

            case State.Gone:
                alpha = 0f;
                if (stateTime >= RespawnDelay) Respawn();
                break;
        }

        // Off the end of the game area (still far down the road): nothing to draw.
        bool onScreen = ScreenX < parentRt.rect.width * 0.5f + 250f;
        rt.gameObject.SetActive(onScreen);
        shadowRt.gameObject.SetActive(onScreen);

        Place(bob, lunge);
        UpdateShadow(bob, alpha);
        image.color = new Color(1f, 1f, 1f, alpha);
        bar.SetAlpha(alpha);
    }

    void StartAttack()
    {
        state = State.Attacking;
        stateTime = 0f;
        attackTimer = 0f;
        blowLanded = false;
    }

    // Brings a new demon, at TargetLevel with full HP, back to the start of the road.
    public void Respawn()
    {
        Stats = CombatStats.Scaled(template, TargetLevel);
        Distance = StartDistance;
        InRange = false;
        shownMetres = -1;
        bar.SetFill(1f);
        RefreshLabel();
        image.raycastTarget = false;
        state = State.Flying;
        stateTime = 0f;
        attackTimer = 0f;
    }

    // The shadow shrinks and fades as the demon rises.
    void UpdateShadow(float bob, float alpha)
    {
        float k = 1f - 0.5f * (bob + BobAmplitude) / (2f * BobAmplitude);
        shadowRt.localScale = new Vector3(k, k, 1f);
        shadowImage.color = new Color(0.3f, 0.12f, 0f, ShadowAlpha * k * alpha);
    }
}
