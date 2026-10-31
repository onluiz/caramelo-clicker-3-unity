using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The town on the horizon behind the dog: the first time a building is bought, its prop (Resources/Props) pops up
// in a fixed slot of a looping world that scrolls with the dog's stride. Props are derived from the owned counts, so
// they come back with a loaded save and go away on a rebirth. When a new one would be off screen the world pans to it.
public class TownProps
{
    class Def
    {
        public string Building, File;
        public string Slot;                               // building whose slot to share (default: its own), e.g. goods on another building's stall
        public int Cols = 1, Frames = 1, FrameW, FrameH; // sheet layout, row-major from the top; FrameW 0 = single image
        public float Height;                              // drawn height in canvas px
        public float Air;                                 // hovering height above the ground
        public float Lift;                                // height above the ground for things resting on a counter
        public float Offset;                              // sideways shift inside the building's slot
        public float Fps = 10f;
        public bool Patrol, Bob;
    }

    // In shop order: the slots are laid out in this order, so the town fills up as the game progresses.
    static readonly Def[] Defs =
    {
        new Def { Building = "vira-lata", File = "trash-can", Height = 40f, Offset = 90f },
        new Def { Building = "vira-lata", File = "tiny_dog", Height = 50f, Offset = -70f },
        new Def { Building = "venda-petiscos", File = "dog-stall", Height = 120f },
        new Def { Building = "venda-petiscos", File = "stall-treats", Height = 21f, Offset = -26f, Lift = 39f }, // on the stall counter, left
        new Def { Building = "cachorro-entregador", File = "cachorro-entregador", Cols = 5, Frames = 17, FrameW = 68, FrameH = 60, Height = 78f, Patrol = true },
        new Def { Building = "racao-caramelo", Slot = "venda-petiscos", File = "stall-feed", Height = 31f, Offset = 26f, Lift = 39f }, // same stall, right
        new Def { Building = "petshop", File = "petshop", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 200f },
        new Def { Building = "iluminacao-caramelo", File = "street-lamp-on", Height = 190f },
        new Def { Building = "drone-entregas", File = "red-drone", Cols = 5, Frames = 17, FrameW = 357, FrameH = 282, Height = 100f, Air = 190f, Bob = true },
        new Def { Building = "faro-minerador", File = "mina-petiscos", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 200f },
        new Def { Building = "fabrica-de-petiscos", File = "fabrica-petiscos", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 210f },
        new Def { Building = "fabrica-de-racao", File = "fabrica-racao", Cols = 3, Frames = 8, FrameW = 256, FrameH = 256, Height = 210f },
        new Def { Building = "estatua-caramelo", File = "dog_statue", Height = 115f },
        new Def { Building = "templo-caramelo", File = "templo-caramelo", Height = 185f },
        new Def { Building = "banco-canino", File = "banco-canino", Height = 170f },
        new Def { Building = "planta-lunar", File = "lunar-flower", Cols = 3, Frames = 9, FrameW = 246, FrameH = 256, Height = 180f },
        new Def { Building = "fabrica-de-pudim-lunar", File = "fabrica-pudim-lunar", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 210f },
    };

    const float SizeScale = 3.7f;     // all prop heights are multiplied by this, so buildings tower over the dog as he runs past
    const float SlotSpacing = 340f;   // world px between building slots
    const float Margin = 220f;        // how far past the edges a prop is before it wraps around
    const float PopSeconds = 0.45f;
    const float PanPxPerSecond = 1500f;
    const float PatrolSpeed = 40f, PatrolRange = 110f;

    class Prop
    {
        public Def def;
        public int building;
        public float slotX;
        public RectTransform rt;
        public Image img;
        public Sprite[] frames;
        public float pop;        // 0..1 growing in, -1 while waiting for the pan to finish
        public float anim, bob, patrolX;
        public int dir = 1, frame = -1;
    }

    static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

    readonly RectTransform layer;
    readonly int[] buildingOf = new int[Defs.Length];
    readonly float[] slotOf = new float[Defs.Length];
    readonly Prop[] props = new Prop[Defs.Length];
    readonly int slotCount;
    float scroll, panLeft;
    bool started;

    // Extra world speed (px/s) while panning to a new building; the ground and sky follow it.
    public float PanSpeed { get; private set; }

    // groundY is the top of the ground strip, in the parent's space (distance from its 40% height line).
    public TownProps(Transform parent, float groundY)
    {
        var go = new GameObject("Town", typeof(RectTransform));
        layer = go.GetComponent<RectTransform>();
        layer.SetParent(parent, false);
        layer.anchorMin = new Vector2(0f, 0.4f);
        layer.anchorMax = new Vector2(1f, 0.4f);
        layer.pivot = new Vector2(0f, 0f);
        layer.anchoredPosition = new Vector2(0f, groundY - 64f);
        layer.sizeDelta = Vector2.zero;

        var slots = new Dictionary<string, int>();
        for (int i = 0; i < Defs.Length; i++)
        {
            string key = Defs[i].Building;
            buildingOf[i] = System.Array.FindIndex(GameDefs.Buildings, b => b.Key == key);
            if (buildingOf[i] < 0) Debug.LogError("TownProps: unknown building " + key);
            string slot = Defs[i].Slot ?? key;
            if (!slots.ContainsKey(slot)) slots[slot] = slots.Count;
            slotOf[i] = slots[slot] * SlotSpacing;
        }
        slotCount = slots.Count;
    }

    public void Tick(float dt, float stride, int[] owned)
    {
        float panelW = layer.rect.width;
        if (panelW <= 1f) return; // layout not ready yet
        float worldW = Mathf.Max(slotCount * SlotSpacing, panelW + 2f * Margin + 80f);

        bool initial = !started;
        started = true;
        Prop newest = null;
        for (int i = 0; i < Defs.Length; i++)
        {
            bool has = buildingOf[i] >= 0 && owned[buildingOf[i]] > 0;
            if (has && props[i] == null)
            {
                props[i] = Spawn(Defs[i], buildingOf[i], slotOf[i], initial);
                if (props[i] != null) newest = props[i];
            }
            else if (!has && props[i] != null)
            {
                Object.Destroy(props[i].rt.gameObject);
                props[i] = null;
            }
        }

        if (newest != null)
        {
            // Later defs are drawn in front, so goods end up on top of the stall whichever was bought first.
            foreach (var p in props)
                if (p != null) p.rt.SetAsLastSibling();
            if (!initial) RequestPan(newest, panelW, worldW);
        }

        float step = Mathf.Min(panLeft, PanPxPerSecond * dt);
        panLeft -= step;
        PanSpeed = dt > 0f ? step / dt : 0f;
        scroll += stride * dt + step;

        foreach (var p in props)
            if (p != null) Place(p, dt, worldW);
    }

    Prop Spawn(Def def, int building, float slotX, bool instant)
    {
        var frames = Frames(def);
        if (frames == null) return null;

        var go = new GameObject(def.File, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(layer, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.sprite = frames[0];
        float aspect = frames[0].rect.width / frames[0].rect.height;
        float h = def.Height * SizeScale;
        rt.sizeDelta = new Vector2(h * aspect, h);

        return new Prop
        {
            def = def, building = building, slotX = slotX + def.Offset * SizeScale, rt = rt, img = img, frames = frames,
            pop = instant ? 1f : -1f,
            anim = Random.value * frames.Length,
            bob = Random.value * 6f,
            patrolX = Random.Range(-PatrolRange, PatrolRange),
            dir = Random.value < 0.5f ? 1 : -1,
        };
    }

    // The world only moves one way, so the pan is how far to scroll for the new prop to land in view, left or right of the dog.
    void RequestPan(Prop p, float panelW, float worldW)
    {
        float sx = ScreenX(p.slotX, scroll + panLeft, worldW);
        bool visible = sx > 0.08f * panelW && sx < 0.92f * panelW && Mathf.Abs(sx - 0.5f * panelW) > 150f;
        if (visible) return;
        float toLeft = Mathf.Repeat(sx - 0.22f * panelW, worldW);
        float toRight = Mathf.Repeat(sx - 0.78f * panelW, worldW);
        panLeft += Mathf.Min(toLeft, toRight);
    }

    static float ScreenX(float slotX, float scrollPos, float worldW)
    {
        return Mathf.Repeat(slotX - scrollPos + Margin, worldW) - Margin;
    }

    void Place(Prop p, float dt, float worldW)
    {
        if (p.pop < 0f)
        {
            if (panLeft <= 0.5f) p.pop = 0f;
        }
        else if (p.pop < 1f)
        {
            p.pop = Mathf.Min(1f, p.pop + dt / PopSeconds);
        }

        float t = Mathf.Max(p.pop, 0f) - 1f;
        float grow = p.pop >= 1f ? 1f : (p.pop < 0f ? 0f : 1f + 2.70158f * t * t * t + 1.70158f * t * t); // ease-out-back

        float x = ScreenX(p.slotX, scroll, worldW);
        if (p.def.Patrol)
        {
            p.patrolX += p.dir * PatrolSpeed * dt;
            if (p.patrolX > PatrolRange) p.dir = -1;
            else if (p.patrolX < -PatrolRange) p.dir = 1;
            x += p.patrolX;
        }

        float y = (p.def.Air + p.def.Lift) * SizeScale;
        if (p.def.Bob)
        {
            p.bob += dt;
            y += Mathf.Sin(p.bob * 2.2f) * 9f;
        }

        p.rt.anchoredPosition = new Vector2(x, y);
        p.rt.localScale = new Vector3(grow * (p.def.Patrol && p.dir < 0 ? -1f : 1f), grow, 1f);

        if (p.frames.Length > 1)
        {
            p.anim += dt * p.def.Fps;
            int f = (int)p.anim % p.frames.Length;
            if (f != p.frame)
            {
                p.frame = f;
                p.img.sprite = p.frames[f];
            }
        }
    }

    // Cuts the sheet into sprites (cached by file). Single images give one sprite.
    static Sprite[] Frames(Def def)
    {
        if (Cache.TryGetValue(def.File, out var cached) && cached != null && cached.Length > 0 && cached[0] != null) return cached;

        var tex = Resources.Load<Texture2D>("Props/" + def.File);
        if (tex == null)
        {
            Debug.LogError("Prop not found at Resources/Props/" + def.File);
            return null;
        }

        tex.filterMode = FilterMode.Point;
        var frames = new Sprite[def.Frames];
        for (int i = 0; i < frames.Length; i++)
        {
            Rect rect;
            if (def.FrameW <= 0)
            {
                rect = new Rect(0, 0, tex.width, tex.height);
            }
            else
            {
                int col = i % def.Cols, row = i / def.Cols;
                rect = new Rect(col * def.FrameW, tex.height - (row + 1) * def.FrameH, def.FrameW, def.FrameH);
            }

            frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
        }

        Cache[def.File] = frames;
        return frames;
    }
}
