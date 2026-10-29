using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A flying demon that hovers in front of Caramelo, facing him. Tapping it is an attack from the dog.
// Frames come from Resources/Enemy/demon-{flying,hurt,death}.png, sheets of the Flying Demon pack
// (81x71 frames in a row). It flies, flinches when hit, plays its death animation at 0 HP and comes
// back with full HP a moment later. It does not attack yet.
public class Enemy
{
    const int FrameWidth = 81;
    const int FrameHeight = 71;
    const float Scale = 3f;              // screen pixels per sprite pixel
    const float DistanceFromDog = 300f;  // horizontal offset from the dog's anchor
    const float HoverHeight = 120f;      // centre height above the dog's paws
    const float BobAmplitude = 14f;
    const float BobSpeed = 2.2f;
    const float FlyFps = 8f;
    const float HurtFps = 14f;
    const float DeathFps = 10f;
    const float FadeTime = 0.5f;         // after the death animation
    const float RespawnDelay = 1.5f;     // after the fade

    const float ShadowWidth = 170f;
    const float ShadowHeight = 20f;
    const float ShadowAlpha = 0.16f;     // lighter than the dog's: the demon is off the ground

    const float BarWidth = 150f;
    const float BarHeight = 14f;

    enum State { Flying, Hurt, Dying, Gone }

    public readonly CombatStats Stats;
    public float Hp { get; private set; }
    public bool Alive => state != State.Dying && state != State.Gone;
    public Action Tapped;

    readonly RectTransform rt, shadowRt, barFillRt;
    readonly Image image, shadowImage, barBg, barFill;
    readonly TextMeshProUGUI nameText;
    readonly Sprite[] flyFrames, hurtFrames, deathFrames;

    State state = State.Flying;
    float time, stateTime;
    Sprite shown;

    public Enemy(Transform parent, Vector2 anchor, CombatStats stats)
    {
        Stats = stats;
        Hp = stats.MaxHp;

        // Drawn first so it sits behind the demon.
        var shadowGo = new GameObject("EnemyShadow", typeof(RectTransform));
        shadowGo.transform.SetParent(parent, false);
        shadowRt = (RectTransform)shadowGo.transform;
        shadowRt.anchorMin = shadowRt.anchorMax = anchor;
        shadowRt.anchoredPosition = new Vector2(DistanceFromDog, DogRig.GroundY - 4f);
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
        image.raycastTarget = true; // a tap on the demon is an attack and does not reach the tap area under it
        var trigger = go.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(_ => { if (Alive) Tapped?.Invoke(); });
        trigger.triggers.Add(entry);

        // HP bar and name over the demon's head (children, so they move and fade with it).
        barBg = NewBarImage("HpBack", new Color(0.12f, 0.05f, 0.02f, 0.85f));
        barBg.rectTransform.sizeDelta = new Vector2(BarWidth, BarHeight);
        barBg.rectTransform.anchorMin = barBg.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        barBg.rectTransform.pivot = new Vector2(0.5f, 0f);
        barBg.rectTransform.anchoredPosition = new Vector2(0f, 4f);
        barFill = NewBarImage("HpFill", ProceduralArt.Hex("#E2483D"));
        barFill.transform.SetParent(barBg.transform, false);
        barFillRt = barFill.rectTransform;
        barFillRt.anchorMin = Vector2.zero;
        barFillRt.anchorMax = Vector2.one;
        barFillRt.offsetMin = new Vector2(2f, 2f);
        barFillRt.offsetMax = new Vector2(-2f, -2f);

        var nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(barBg.transform, false);
        nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 26f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.raycastTarget = false;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Truncate;
        var nameRt = nameText.rectTransform;
        nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
        nameRt.pivot = new Vector2(0.5f, 0f);
        nameRt.sizeDelta = new Vector2(280f, 36f);
        nameRt.anchoredPosition = new Vector2(0f, 2f);

        flyFrames = LoadSheet("Enemy/demon-flying");
        hurtFrames = LoadSheet("Enemy/demon-hurt");
        deathFrames = LoadSheet("Enemy/demon-death");

        RefreshLabel();
        Show(flyFrames, 0);
        Place(0f);
    }

    Image NewBarImage(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(rt, false);
        var img = go.AddComponent<Image>();
        img.sprite = ProceduralArt.RoundedRect();
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
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

    void RefreshLabel()
    {
        nameText.text = Loc.T("Demônio") + " " + Loc.T("Nv.") + Stats.Level;
    }

    void Show(Sprite[] frames, int index)
    {
        if (frames.Length == 0) return;
        var sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        if (sprite == shown) return;
        shown = sprite;
        image.sprite = sprite;
    }

    void Place(float bob)
    {
        rt.anchoredPosition = new Vector2(DistanceFromDog, DogRig.GroundY + HoverHeight + bob);
    }

    // Applies a hit from the dog. A miss does nothing here; the caller shows it.
    public void TakeHit(CombatStats.AttackResult hit)
    {
        if (!Alive || hit.Missed) return;

        Hp = Mathf.Max(0f, Hp - hit.Damage);
        barFillRt.anchorMax = new Vector2(Hp / Stats.MaxHp, 1f);
        stateTime = 0f;
        state = Hp <= 0f ? State.Dying : State.Hurt;
        if (state == State.Dying) image.raycastTarget = false;
    }

    // Screen position of the demon's centre, for floating texts (the canvas is screen-space overlay).
    public Vector2 ScreenPosition => RectTransformUtility.WorldToScreenPoint(null, rt.position);

    public void Tick(float dt)
    {
        time += dt;
        stateTime += dt;

        float bob = Mathf.Sin(time * BobSpeed) * BobAmplitude;
        float alpha = 1f;

        switch (state)
        {
            case State.Flying:
                Show(flyFrames, (int)(time * FlyFps) % Mathf.Max(1, flyFrames.Length));
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

        Place(bob);
        UpdateShadow(bob, alpha);
        image.color = new Color(1f, 1f, 1f, alpha);
        barBg.color = new Color(0.12f, 0.05f, 0.02f, 0.85f * alpha);
        barFill.color = new Color(0.89f, 0.28f, 0.24f, alpha);
        nameText.alpha = alpha;
    }

    void Respawn()
    {
        Hp = Stats.MaxHp;
        barFillRt.anchorMax = Vector2.one;
        image.raycastTarget = true;
        state = State.Flying;
        stateTime = 0f;
    }

    // The shadow shrinks and fades as the demon rises.
    void UpdateShadow(float bob, float alpha)
    {
        float k = 1f - 0.5f * (bob + BobAmplitude) / (2f * BobAmplitude);
        shadowRt.localScale = new Vector3(k, k, 1f);
        shadowImage.color = new Color(0.3f, 0.12f, 0f, ShadowAlpha * k * alpha);
    }
}
