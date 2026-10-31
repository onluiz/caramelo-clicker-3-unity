using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The town on the horizon: the dog runs through a world with fixed coordinates (worldX is how far he has run) and the
// camera follows him. Town stretches ("slots", in shop order, repeating) are built just past the right edge before he
// gets there and destroyed once he has left them behind. A building bought mid-run is built next, ahead of him, or pops
// up on screen when nothing is around yet. Props come from the owned counts (Resources/Props), so a loaded save rebuilds
// the town and a rebirth clears it.
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

    // In shop order: the slots repeat in this order along the road, so the town fills up as the game progresses.
    static readonly Def[] Defs =
    {
        new Def { Building = "vira-lata", File = "trash-can", Height = 40f, Offset = 90f },
        new Def { Building = "vira-lata", File = "tiny_dog", Height = 50f, Offset = -70f },
        new Def { Building = "o-inicio", File = "dog-stall", Height = 120f },
        new Def { Building = "venda-petiscos", Slot = "o-inicio", File = "stall-treats", Height = 21f, Offset = -26f, Lift = 39f }, // on the stall counter, left
        new Def { Building = "cachorro-entregador", File = "cachorro-entregador", Cols = 5, Frames = 17, FrameW = 68, FrameH = 60, Height = 78f, Patrol = true },
        new Def { Building = "racao-caramelo", Slot = "o-inicio", File = "stall-feed", Height = 31f, Offset = 26f, Lift = 39f }, // same stall, right
        new Def { Building = "petshop", File = "petshop", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 200f },
        new Def { Building = "iluminacao-caramelo", File = "street-lamp-on", Height = 190f },
        new Def { Building = "drone-entregas", File = "red-drone", Cols = 5, Frames = 17, FrameW = 357, FrameH = 282, Height = 100f, Air = 190f, Bob = true },
        new Def { Building = "faro-minerador", File = "mina-petiscos", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 200f },
        new Def { Building = "fabrica-de-petiscos", File = "fabrica-petiscos", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 210f },
        new Def { Building = "fabrica-de-racao", File = "fabrica-racao", Cols = 3, Frames = 8, FrameW = 256, FrameH = 256, Height = 210f },
        new Def { Building = "estatua-caramelo", File = "dog_statue", Height = 115f },
        new Def { Building = "templo-caramelo", File = "templo-caramelo", Height = 185f },
        new Def { Building = "banco-canino", File = "banco-canino", Height = 170f },
        new Def { Building = "veterinaria-caramelo", File = "veterinaria-caramelo", Height = 190f },
        new Def { Building = "base-lunar-caramelo", File = "base-lunar-caramelo", Height = 105f }, // wide: ~900 px on screen, fits the Ahead margin
        new Def { Building = "planta-lunar", File = "lunar-flower", Cols = 3, Frames = 9, FrameW = 246, FrameH = 256, Height = 180f },
        new Def { Building = "fabrica-de-pudim-lunar", File = "fabrica-pudim-lunar", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 210f },
    };

    // A "for sale" sign stands where this building will be, until it is bought.
    const string SaleBuilding = "petshop";
    static readonly Def SignDef = new Def { Building = SaleBuilding, File = "sale-sign" };

    const float SizeScale = 3.7f;    // all prop heights are multiplied by this, so buildings tower over the dog as he runs past
    const float PxPerMeter = 10f;
    const float SlotSpacing = 540f;   // world px between building slots
    const float LapPx = 2000f * PxPerMeter; // the road is a loop of 2000 "meters": the town, then open country, then the town again
    const float Ahead = 450f;         // town is built this far past the right edge (a prop reaches at most ~425 px to either side of its slot)
    const float Behind = 500f;        // and destroyed once its slot is this far past the left edge
    const float PopSeconds = 0.45f;
    const float PatrolSpeed = 40f, PatrolRange = 110f;

    class Prop
    {
        public Def def;
        public RectTransform rt;
        public Image img;
        public Sprite[] frames;
        public float pop;        // 0..1 growing in
        public float anim, bob, patrolX;
        public int dir = 1, frame = -1;
    }

    // One stretch of town: everything built around one slot position in the world.
    class Inst
    {
        public int slot;
        public double x;
        public readonly Prop[] byDef = new Prop[Defs.Length];
        public Prop sign;
    }

    static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

    readonly RectTransform layer;
    readonly int[] buildingOf = new int[Defs.Length];
    readonly int[] defSlot = new int[Defs.Length];
    readonly bool[] slotOwned, slotWasOwned;
    readonly List<Inst> insts = new List<Inst>();
    readonly Queue<int> queue = new Queue<int>(); // slots bought mid-run: built before the regular cycle goes on
    readonly int slotCount;
    readonly int saleSlot;
    int cursor = -1;
    double nextSpawnX;  // where the next slot of the current lap goes
    double lapStart;    // world x of the current lap's first slot
    bool started;

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
            defSlot[i] = slots[slot];
        }

        slotCount = slots.Count;
        saleSlot = slots.TryGetValue(SaleBuilding, out int ss) ? ss : -1;
        slotOwned = new bool[slotCount];
        slotWasOwned = new bool[slotCount];
    }

    bool Has(int def, int[] owned)
    {
        return buildingOf[def] >= 0 && owned[buildingOf[def]] > 0;
    }

    // worldX: how far the dog has run, in canvas px. A thing's screen x is its world x minus worldX.
    public void Tick(float dt, double worldX, int[] owned)
    {
        float panelW = layer.rect.width;
        if (panelW <= 1f) return; // layout not ready yet

        bool initial = !started;
        started = true;
        if (initial) nextSpawnX = lapStart = worldX - Behind;

        System.Array.Clear(slotOwned, 0, slotCount);
        for (int i = 0; i < Defs.Length; i++)
            if (Has(i, owned)) slotOwned[defSlot[i]] = true;

        int ownedCount = 0;
        for (int sl = 0; sl < slotCount; sl++)
        {
            if (slotOwned[sl]) ownedCount++;
            if (slotOwned[sl] && !slotWasOwned[sl] && !initial && !HasInst(sl)) queue.Enqueue(sl);
            slotWasOwned[sl] = slotOwned[sl];
        }

        bool dirty = false;

        // Drop what the dog left behind, and keep the stretches still in front of him in line with what is owned
        // (goods bought later land on a stall that is already built; a rebirth removes everything).
        for (int k = insts.Count - 1; k >= 0; k--)
        {
            var inst = insts[k];
            if (inst.x < worldX - Behind)
            {
                DestroyInst(inst);
                insts.RemoveAt(k);
                continue;
            }

            float sx = (float)(inst.x - worldX);
            bool onScreen = sx > -200f && sx < panelW + 200f;
            bool any = false;
            for (int i = 0; i < Defs.Length; i++)
            {
                if (defSlot[i] != inst.slot) continue;
                bool has = Has(i, owned);
                if (has && inst.byDef[i] == null)
                {
                    inst.byDef[i] = Spawn(Defs[i], onScreen);
                    dirty |= inst.byDef[i] != null;
                }
                else if (!has && inst.byDef[i] != null)
                {
                    Object.Destroy(inst.byDef[i].rt.gameObject);
                    inst.byDef[i] = null;
                }

                any |= inst.byDef[i] != null;
            }

            // The sign stands while the slot is unowned (and the town exists at all); it goes away once the building is bought.
            bool wantSign = inst.slot == saleSlot && !slotOwned[inst.slot] && ownedCount > 0;
            if (wantSign && inst.sign == null)
            {
                inst.sign = SpawnSign(false);
                dirty = true;
            }
            else if (!wantSign && inst.sign != null)
            {
                Object.Destroy(inst.sign.rt.gameObject);
                inst.sign = null;
            }

            if (!any && inst.sign == null) insts.RemoveAt(k);
        }

        // Build ahead of the dog, so the town is there before he is.
        double limit = worldX + panelW + Ahead;
        if (ownedCount == 0)
        {
            queue.Clear();
            cursor = -1;
            nextSpawnX = lapStart = worldX - Behind;
        }
        else
        {
            // Slots bought this run go up right away: on screen with a pop when nothing is around, else just past the right edge.
            while (queue.Count > 0)
            {
                int q = queue.Dequeue();
                if (!slotOwned[q] || HasInst(q)) continue;

                double x = limit;
                bool popIn = false;
                if (insts.Count == 0)
                {
                    x = worldX + 0.78f * panelW;
                    popIn = true;
                    lapStart = x;
                    nextSpawnX = x + SlotSpacing;
                    cursor = q;
                }
                else
                {
                    while (NearInst(x)) x += SlotSpacing;
                }

                AddInst(q, x, popIn, owned);
                dirty = true;
            }

            // The rest of the lap, in shop order; after the last slot comes open country until the next lap starts.
            while (true)
            {
                int slot = PeekSlot(out bool wrapped);
                if (slot < 0) break;

                double x = wrapped ? lapStart + LapPx : nextSpawnX;
                if (x >= limit) break;

                if (wrapped) lapStart = x;
                cursor = slot;
                AddInst(slot, x, false, owned);
                nextSpawnX = x + SlotSpacing;
                dirty = true;
            }
        }

        if (dirty) Reorder();

        foreach (var inst in insts)
        {
            float baseX = (float)(inst.x - worldX);
            for (int i = 0; i < Defs.Length; i++)
                if (inst.byDef[i] != null) Place(inst.byDef[i], baseX, dt);
            if (inst.sign != null) Place(inst.sign, baseX, dt);
        }
    }

    bool HasInst(int slot)
    {
        foreach (var inst in insts)
            if (inst.slot == slot) return true;
        return false;
    }

    bool NearInst(double x)
    {
        foreach (var inst in insts)
            if (System.Math.Abs(inst.x - x) < SlotSpacing) return true;
        return false;
    }

    // The next owned slot after the cursor in shop order (skipping ones already standing); wrapped = it starts a new lap.
    int PeekSlot(out bool wrapped)
    {
        wrapped = false;
        for (int n = 1; n <= slotCount; n++)
        {
            int idx = cursor + n;
            int sl = idx % slotCount;
            if ((slotOwned[sl] || sl == saleSlot) && !HasInst(sl))
            {
                wrapped = idx >= slotCount;
                return sl;
            }
        }

        return -1;
    }

    void AddInst(int slot, double x, bool popIn, int[] owned)
    {
        var inst = new Inst { slot = slot, x = x };
        for (int i = 0; i < Defs.Length; i++)
            if (defSlot[i] == slot && Has(i, owned)) inst.byDef[i] = Spawn(Defs[i], popIn);
        if (slot == saleSlot && !slotOwned[slot]) inst.sign = SpawnSign(popIn);
        insts.Add(inst);
    }

    static void DestroyInst(Inst inst)
    {
        foreach (var p in inst.byDef)
            if (p != null) Object.Destroy(p.rt.gameObject);
        if (inst.sign != null) Object.Destroy(inst.sign.rt.gameObject);
    }

    // A wooden post with a red "VENDA" board, drawn from uGUI pieces instead of a sheet.
    Prop SpawnSign(bool popIn)
    {
        var go = new GameObject("sale-sign", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(layer, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(360f, 330f);

        var wood = new Color(0.45f, 0.27f, 0.12f);
        var post = Piece(rt, "Post", new Vector2(0.5f, 0f), new Vector2(36f, 210f), Vector2.zero, wood);
        var board = Piece(rt, "Board", new Vector2(0.5f, 1f), new Vector2(360f, 150f), Vector2.zero, new Color(0.97f, 0.93f, 0.82f));
        var trim = Piece(board.rectTransform, "Trim", new Vector2(0.5f, 0.5f), new Vector2(336f, 126f), Vector2.zero, new Color(0.78f, 0.15f, 0.12f));
        var inner = Piece(trim.rectTransform, "Inner", new Vector2(0.5f, 0.5f), new Vector2(318f, 108f), Vector2.zero, new Color(0.97f, 0.93f, 0.82f));

        var tgo = new GameObject("Text", typeof(RectTransform));
        var trt = tgo.GetComponent<RectTransform>();
        trt.SetParent(inner.rectTransform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var text = tgo.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = Loc.T("VENDA");
        text.fontSize = 78f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(0.78f, 0.15f, 0.12f);
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        post.transform.SetAsFirstSibling();

        return new Prop
        {
            def = SignDef, rt = rt, frames = new Sprite[1],
            pop = popIn ? 0f : 1f,
        };
    }

    static Image Piece(RectTransform parent, string name, Vector2 anchor, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = anchor;
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.sprite = ProceduralArt.RoundedRect();
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // Later defs are drawn in front, so goods end up on top of the stall whichever was built first.
    void Reorder()
    {
        for (int i = 0; i < Defs.Length; i++)
            foreach (var inst in insts)
                if (inst.byDef[i] != null) inst.byDef[i].rt.SetAsLastSibling();
        foreach (var inst in insts)
            if (inst.sign != null) inst.sign.rt.SetAsLastSibling();
    }

    Prop Spawn(Def def, bool popIn)
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
            def = def, rt = rt, img = img, frames = frames,
            pop = popIn ? 0f : 1f,
            anim = Random.value * frames.Length,
            bob = Random.value * 6f,
            patrolX = Random.Range(-PatrolRange, PatrolRange),
            dir = Random.value < 0.5f ? 1 : -1,
        };
    }

    void Place(Prop p, float slotScreenX, float dt)
    {
        if (p.pop < 1f) p.pop = Mathf.Min(1f, p.pop + dt / PopSeconds);

        float t = p.pop - 1f;
        float grow = p.pop >= 1f ? 1f : 1f + 2.70158f * t * t * t + 1.70158f * t * t; // ease-out-back

        float x = slotScreenX + p.def.Offset * SizeScale;
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
