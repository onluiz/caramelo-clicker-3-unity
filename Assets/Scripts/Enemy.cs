using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One flying demon of a wave (see EnemyWave, which spawns, places and counts them). While the fight is
// on, tapping it is an attack from the dog and it attacks back every few seconds. At 0 HP it plays its
// death animation and fades out for good; the wave decides what comes next.
//
// Frames come from Resources/Enemy/demon-{flying,hurt,attack,death}.png, sheets of the Flying Demon
// pack (81x71 frames in a row).
public class Enemy
{
    const int FrameWidth = 81;
    const int FrameHeight = 71;
    const float Scale = 3f;              // screen pixels per sprite pixel
    const float BobAmplitude = 14f;
    const float BobSpeed = 2.2f;
    const float FlyFps = 8f;
    const float HurtFps = 14f;
    const float AttackFps = 12f;
    const float DeathFps = 10f;
    const float FadeTime = 0.5f;         // after the death animation

    const float AttackInterval = 3f;     // seconds between its attacks
    const int AttackHitFrame = 4;        // frame of the attack animation where the blow lands
    const float LungeDistance = 50f;     // how far it darts toward the dog while attacking

    const float ShadowWidth = 170f;
    const float ShadowHeight = 20f;
    const float ShadowAlpha = 0.16f;     // lighter than the dog's: the demon is off the ground

    enum State { Flying, Hurt, Attacking, Dying, Gone }

    public CombatStats Stats { get; private set; }
    public bool InUse { get; private set; }                       // part of the current wave
    public bool Alive => InUse && state != State.Dying && state != State.Gone;
    public bool Finished => !InUse || state == State.Gone;        // dead and faded out (or never spawned)
    public int XpReward => 12 + 6 * Stats.Level;
    public Action<Enemy> Tapped;         // the dog attacked it
    public Action<Enemy> AttackLanded;   // its blow reached the dog

    readonly RectTransform rt, shadowRt, parentRt;
    readonly Image image, shadowImage;
    readonly HealthBar bar;
    readonly Sprite[] flyFrames, hurtFrames, attackFrames, deathFrames;

    State state = State.Gone;
    float time, stateTime, attackTimer;
    float xOffset, hover;
    bool blowLanded, inRange;
    string meters = "";
    Sprite shown;

    public Enemy(Transform parent, Vector2 anchor)
    {
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
        entry.callback.AddListener(_ => { if (Alive && inRange) Tapped?.Invoke(this); });
        trigger.triggers.Add(entry);

        // Name and HP over the demon's head (a child, so it moves and fades with it).
        bar = new HealthBar(rt, new Vector2(0.5f, 1f), new Vector2(0f, 4f), ProceduralArt.Hex("#E2483D"));

        flyFrames = LoadSheet("Enemy/demon-flying");
        hurtFrames = LoadSheet("Enemy/demon-hurt");
        attackFrames = LoadSheet("Enemy/demon-attack");
        deathFrames = LoadSheet("Enemy/demon-death");

        SetShown(false);
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

    // Puts a fresh demon with `stats` in the wave: `xOffset` px behind the front of the wave, hovering
    // `hover` px above the dog's paws.
    public void Activate(CombatStats stats, float xOffset, float hover)
    {
        Stats = stats;
        this.xOffset = xOffset;
        this.hover = hover;
        InUse = true;
        inRange = false;
        meters = "";
        state = State.Flying;
        stateTime = 0f;
        time = UnityEngine.Random.Range(0f, 6f); // so a group does not bob in lockstep
        attackTimer = 0f;
        image.raycastTarget = false;
        bar.SetFill(1f);
        RefreshLabel();
    }

    public void Deactivate()
    {
        InUse = false;
        state = State.Gone;
        SetShown(false);
    }

    // The fight starts: its first blow comes some time within the first half of an interval.
    public void Engage()
    {
        inRange = true;
        image.raycastTarget = true;
        attackTimer = UnityEngine.Random.Range(0f, AttackInterval * 0.5f);
        meters = "";
        RefreshLabel();
    }

    // The distance shown after the name, while the wave is still on its way ("" to hide it).
    public void SetMeters(string text)
    {
        if (text == meters) return;
        meters = text;
        RefreshLabel();
    }

    public void RefreshLabel()
    {
        if (Stats == null) return;
        bar.SetLabel(Loc.T("Demônio") + " " + Loc.T("Nv.") + Stats.Level + (meters.Length > 0 ? "  " + meters : ""));
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

    void SetShown(bool on)
    {
        rt.gameObject.SetActive(on);
        shadowRt.gameObject.SetActive(on);
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

    // `frontX` is where the front of the wave is, in px in front of the dog.
    public void Tick(float dt, float frontX)
    {
        if (!InUse || state == State.Gone) return;

        time += dt;
        stateTime += dt;

        float bob = Mathf.Sin(time * BobSpeed) * BobAmplitude;
        float lunge = 0f;
        float alpha = 1f;

        switch (state)
        {
            case State.Flying:
                Show(flyFrames, (int)(time * FlyFps) % Mathf.Max(1, flyFrames.Length));
                if (inRange)
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
                        AttackLanded?.Invoke(this);
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
                        SetShown(false);
                        return;
                    }
                    break;
                }
        }

        float x = frontX + xOffset - lunge;
        // Far down the road, past the end of the game area: nothing to draw.
        SetShown(x < parentRt.rect.width * 0.5f + 250f);

        rt.anchoredPosition = new Vector2(x, DogRig.GroundY + hover + bob);
        shadowRt.anchoredPosition = new Vector2(x, DogRig.GroundY - 4f);
        float k = 1f - 0.5f * (bob + BobAmplitude) / (2f * BobAmplitude); // the shadow shrinks as the demon rises
        shadowRt.localScale = new Vector3(k, k, 1f);
        shadowImage.color = new Color(0.3f, 0.12f, 0f, ShadowAlpha * k * alpha);
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
}
