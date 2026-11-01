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
        public int MinStall, MaxStall = 99;               // shown only while the stall has this many "O Início" upgrades
    }

    // In shop order: the slots repeat in this order along the road, so the town fills up as the game progresses.
    static readonly Def[] Defs =
    {
        // The stall grows with the "O Início" upgrades: bare, then stocked and patched (1-2), then repaired with a sign (3+).
        new Def { Building = "o-inicio", File = "dog-stall", Height = 120f, MaxStall = 0 },
        new Def { Building = "o-inicio", File = "stall-bad-full", Height = 115.6f, MinStall = 1, MaxStall = 2 },
        new Def { Building = "o-inicio", File = "stall-good-full", Height = 122.7f, MinStall = 3 },
        new Def { Building = "venda-petiscos", Slot = "o-inicio", File = "stall-treats", Height = 21f, Offset = -26f, Lift = 39f, MaxStall = 0 }, // on the bare stall's counter, left
        new Def { Building = "racao-caramelo", Slot = "o-inicio", File = "stall-feed", Height = 31f, Offset = 26f, Lift = 39f, MaxStall = 0 }, // same stall, right
        new Def { Building = "creche-caramelo", File = "creche-caramelo", Cols = 3, Frames = 8, FrameW = 431, FrameH = 428, Height = 190f }, // right after the stall
        // hidden for now: new Def { Building = "drone-entregas", File = "red-drone", Cols = 5, Frames = 17, FrameW = 357, FrameH = 282, Height = 100f, Air = 190f, Bob = true },
        new Def { Building = "faro-minerador", File = "mina-petiscos", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 200f, Lift = -26f },
        new Def { Building = "fabrica-de-petiscos", File = "fabrica-petiscos", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 210f, Lift = -20f },
        new Def { Building = "fabrica-de-racao", File = "fabrica-racao", Cols = 3, Frames = 8, FrameW = 256, FrameH = 256, Height = 210f, Lift = -22f },
        new Def { Building = "estatua-caramelo", File = "dog_statue", Height = 115f },
        new Def { Building = "sorveteria-caramelo", File = "sorveteria", Height = 170f },
        new Def { Building = "templo-caramelo", File = "templo-caramelo", Height = 185f },
        new Def { Building = "banco-canino", File = "banco-canino", Height = 170f },
        new Def { Building = "pet-shop-caramelo", File = "petshop", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 200f },
        new Def { Building = "veterinaria-caramelo", File = "veterinaria-caramelo", Height = 190f },
        new Def { Building = "loja-pudim-lunar", File = "loja-pudim-lunar", Height = 190f },
        new Def { Building = "base-lunar-caramelo", File = "base-lunar-caramelo", Height = 200f }, // wide: ~1710 px on screen, fits the Ahead margin
        // hidden for now: new Def { Building = "planta-lunar", File = "lunar-flower", Cols = 3, Frames = 9, FrameW = 246, FrameH = 256, Height = 180f },
        new Def { Building = "fabrica-de-pudim-lunar", File = "fabrica-pudim-lunar", Cols = 3, Frames = 9, FrameW = 256, FrameH = 234, Height = 210f, Lift = -22f },
    };

    // The delivery dog is not tied to a slot: once bought he runs along the road like Caramelo, at his own pace.
    const string RunnerBuilding = "cachorro-entregador";
    static readonly Def RunnerDef = new Def { File = "cachorro-entregador", Cols = 5, Frames = 17, FrameW = 68, FrameH = 60, Height = 60f * 3f / SizeScale }; // same pixel scale as Caramelo (3 px per sprite px)
    const float RunnerSpeed = 1000f;      // px/s at his average; Caramelo's top sprint is ~1050, so they trade places
    const float RunnerStride = 44f;       // px covered per animation frame
    const float RunnerSayCooldown = 15f;
    const string RunnerLine = "E aí boss, só fazendo entrega";

    class Runner
    {
        public RectTransform rt, bubble;
        public Image img;
        public CanvasGroup bubbleGroup;
        public Sprite[] frames;
        public double x;
        public float clock, anim, wait, prevRel, sayTimer, cooldown;
        public int enterSide = -1;
        public RectTransform lightRt, glowRt;   // the delivery dog's own light (lit layer)
        public Image lightImg, glowImg;
        public int frame = -1;
    }

    Runner runner;
    int runnerBuilding = -1;

    // The delivery drone (building "drone-entregas"): not tied to a slot either. It cruises around the sky in screen space,
    // hopping between random spots (one of four models), then flies off to the right, is gone for a while and comes back as another model.
    const string DroneBuilding = "drone-entregas";
    const string DroneLine = "Olá Boss, indo fazer entrega! :D";
    const float DroneSpeed = 170f;            // px/s cruising
    const float DroneMinAway = 25f, DroneMaxAway = 50f;
    const float DroneFps = 12f;
    static readonly Def[] DroneVariants =
    {
        new Def { File = "red-drone", Cols = 5, Frames = 17, FrameW = 357, FrameH = 282, Height = 200f },
        new Def { File = "gray-drone", Cols = 17, Frames = 17, FrameW = 192, FrameH = 128, Height = 180f },
        new Def { File = "evolved-drone", Cols = 17, Frames = 17, FrameW = 164, FrameH = 144, Height = 190f },
        new Def { File = "drone-sheet", Cols = 4, Frames = 4, FrameW = 384, FrameH = 512, Height = 230f },
    };

    enum DroneState { Away, Fly, Pause, Leave }

    class DroneFlyer
    {
        public RectTransform rt, bubble;
        public Image img;
        public CanvasGroup bubbleGroup;
        public Sprite[] frames;
        public int variant = -1, frame = -1, legs;
        public DroneState state = DroneState.Away;
        public float timer = 1.5f, t, dur = 1f, anim, bob, sayDelay, sayTimer, angle;
        public bool easeOut, said;
        public Vector2 from, to, pos;
    }

    DroneFlyer drone;
    int droneBuilding = -1;
    const string CanBuildingKey = "vira-lata";
    int canBuilding = -1;

    // An unowned building's slot stays empty with a "VENDA" sign in the middle. Factories form their own district.
    static readonly HashSet<string> Factories = new HashSet<string> { "faro-minerador", "fabrica-de-petiscos", "fabrica-de-racao", "fabrica-de-pudim-lunar" };
    static readonly Def SignDef = new Def { Building = "", File = "sale-sign" };

    const float SizeScale = 3.7f;    // all prop heights are multiplied by this, so buildings tower over the dog as he runs past
    const float PxPerMeter = 100f;
    const float SlotGap = 4f * PxPerMeter;           // empty road between the edges of two neighbouring slots, whatever their sizes
    const float FactoryGap = 100f * PxPerMeter;       // same, between the last town slot and the first factory
    const float StartX = 9f * PxPerMeter;            // world x of the first slot's left edge
    const float MapMeters = 1000f;                   // the road is a loop this long; whatever the layout leaves over is open country
    const float EdgeMargin = 150f;    // a slot is built once its left edge is this far past the screen's right edge, and destroyed once its right edge is this far past the left edge
    const float Behind = 950f;        // bonus cans are destroyed this far past the left edge
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
        public RectTransform lit, glow;  // street lamps: the lit copy and its glow, in the lit layer
        public Image litImg, glowImg;
        public Image[] pieces;           // signs and boards: their parts, tinted at night
        public Color[] pieceColors;
    }

    // One stretch of town: everything built around one slot position in the world.
    class Inst
    {
        public int slot;
        public double x, half;
        public readonly Prop[] byDef = new Prop[Defs.Length];
        public Prop sign, banner;   // banner: welcome board (pseudo slots past slotCount)
    }

    // Bonus trash cans: appear at random spots ahead of the dog, stay in the world and can be tapped for a reward.
    class Can
    {
        public double x;
        public RectTransform rt;
        public float phase, pop;
    }

    const float CanHeight = 75f;
    const float CanDrop = 40f;        // how far below the building baseline the bonus can sits, so it stands on the road
    const int MaxCans = 1;
    readonly List<Can> cans = new List<Can>();
    float canTimer = Random.Range(8f, 20f);
    public System.Action<Vector2> OnCanClicked; // screen position of the can, for the floating reward

    class Boom
    {
        public double x;
        public RectTransform rt;
        public Image img;
        public float t;
    }

    static readonly Def BoomDef = new Def { File = "explosion", Cols = 4, Frames = 13, FrameW = 64, FrameH = 64 };
    const float BoomSize = 420f, BoomFps = 18f;
    readonly List<Boom> booms = new List<Boom>();

    static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

    readonly RectTransform layer;
    readonly int[] buildingOf = new int[Defs.Length];
    readonly int[] defSlot = new int[Defs.Length];
    readonly bool[] slotOwned;
    readonly double[] slotX, slotHalf; // fixed world x of each slot centre within a lap, and half its width   // fixed world x of each slot within a lap
    readonly double lapPx;
    public double LapPx => lapPx;   // length of the looping map, in canvas px
    public double IndustrialL { get; private set; }   // the industrial district, from its welcome board to its last building (world x within a lap)
    public double IndustrialR { get; private set; }
    public double CityL { get; private set; }         // the residential district, same convention
    public double CityR { get; private set; }
    readonly List<Inst> insts = new List<Inst>();
    readonly int slotCount;

    // Welcome boards stand at the start of each district, as pseudo slots numbered after the real ones.
    static readonly string[] BannerText = { "Bem-vindo à\nCidade Caramelo", "Bem-vindo à\nÁrea Industrial Caramelo" };
    const float BannerWidth = 900f;

    // Street lamps (pseudo slots after the banners): one in every gap between two buildings of a district, and one
    // every LampEvery along the open road, never inside a district.
    static readonly Def LampDef = new Def { File = "street-lamp-off", Height = 130f };       // by day
    static readonly Def LampOnDef = new Def { File = "street-lamp-on", Height = 130f };      // lit copy drawn above the night shade
    public const float LightAlpha = 0.55f; // the light animation is kept soft
    const float GlowSize = 1100f, LampHeadFrac = 0.8f;
    RectTransform litLayer;
    static Sprite glowSprite;
    const float LampEvery = 15f * PxPerMeter;
    const int MaxLamps = 200;
    int slotTotal;
    const string LampBuilding = "iluminacao-caramelo"; // the lamps only exist once this is bought
    int lampBuilding = -1;
    bool lampsOn, lampsInit;

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

        runnerBuilding = System.Array.FindIndex(GameDefs.Buildings, b => b.Key == RunnerBuilding);
        droneBuilding = System.Array.FindIndex(GameDefs.Buildings, b => b.Key == DroneBuilding);

        canBuilding = System.Array.FindIndex(GameDefs.Buildings, b => b.Key == CanBuildingKey);

        lampBuilding = System.Array.FindIndex(GameDefs.Buildings, b => b.Key == LampBuilding);

        slotCount = slots.Count;
        slotOwned = new bool[slotCount];

        // Fixed layout: town slots in shop order, then the factories. Slot widths come from the art (the widest
        // prop in the slot), and the gap between neighbouring edges is constant, so spacing never depends on size.
        var names = new string[slotCount];
        foreach (var kv in slots) names[kv.Value] = kv.Key;
        slotX = new double[slotCount + 2 + MaxLamps];
        slotHalf = new double[slotCount + 2 + MaxLamps];
        for (int i = 0; i < Defs.Length; i++)
        {
            var fr = Frames(Defs[i]);
            float h = Defs[i].Height * SizeScale;
            float w = fr != null ? h * fr[0].rect.width / fr[0].rect.height : h;
            slotHalf[defSlot[i]] = System.Math.Max(slotHalf[defSlot[i]], w * 0.5);
        }

        slotHalf[slotCount] = slotHalf[slotCount + 1] = BannerWidth * 0.5;

        // Each district starts with its welcome board, then its slots.
        var order = new List<int> { slotCount };
        for (int sl = 0; sl < slotCount; sl++) if (!Factories.Contains(names[sl])) order.Add(sl);
        order.Add(slotCount + 1);
        for (int sl = 0; sl < slotCount; sl++) if (Factories.Contains(names[sl])) order.Add(sl);

        double edge = StartX;
        for (int k = 0; k < order.Count; k++)
        {
            int idx = order[k];
            if (k > 0) edge += idx == slotCount + 1 ? FactoryGap : SlotGap;
            slotX[idx] = edge + slotHalf[idx];
            edge += slotHalf[idx] * 2;
        }

        lapPx = MapMeters * PxPerMeter;
        slotTotal = slotCount + 2;
        var lf = Frames(LampDef);
        double lampHalf = LampDef.Height * SizeScale * (lf != null ? lf[0].rect.width / lf[0].rect.height : 0.25f) * 0.5;

        // Between neighbouring buildings of the same district.
        for (int k = 1; k < order.Count; k++)
        {
            int a = order[k - 1], b = order[k];
            if (a >= slotCount || b >= slotCount) continue;
            AddLamp((slotX[a] + slotHalf[a] + slotX[b] - slotHalf[b]) * 0.5, lampHalf);
        }

        // Along the open road, skipping both districts (from their welcome board to their last building).
        double cityL = slotX[slotCount] - slotHalf[slotCount], cityR = 0, indL = slotX[slotCount + 1] - slotHalf[slotCount + 1], indR = 0;
        for (int sl = 0; sl < slotCount; sl++)
        {
            if (Factories.Contains(names[sl])) indR = System.Math.Max(indR, slotX[sl] + slotHalf[sl]);
            else cityR = System.Math.Max(cityR, slotX[sl] + slotHalf[sl]);
        }

        CityL = cityL;
        CityR = cityR;
        IndustrialL = indL;
        IndustrialR = indR;

        for (double x = LampEvery; x < lapPx; x += LampEvery)
        {
            if (x > cityL - lampHalf && x < cityR + lampHalf) continue;
            if (x > indL - lampHalf && x < indR + lampHalf) continue;
            AddLamp(x, lampHalf);
        }

        if (edge > lapPx) Debug.LogError("TownProps: layout (" + edge / PxPerMeter + " m) is longer than the map (" + MapMeters + " m)");
    }

    // True for one tick each time the dog runs past the middle of the daycare (once per lap) while it is owned.
    public bool PassedCreche;
    const string CrecheBuilding = "creche-caramelo";
    double crechePrevX = double.NaN;

    void CheckCrechePass(double worldX, int[] owned)
    {
        PassedCreche = false;
        int def = System.Array.FindIndex(Defs, d => d.Building == CrecheBuilding);
        if (def < 0 || !Has(def, owned)) { crechePrevX = double.NaN; return; }
        if (!double.IsNaN(crechePrevX))
        {
            double c = slotX[defSlot[def]];
            PassedCreche = System.Math.Floor((worldX - c) / lapPx) > System.Math.Floor((crechePrevX - c) / lapPx);
        }
        crechePrevX = worldX;
    }

    public int StallTier;   // "O Início" upgrades bought, set by GameUI before Tick

    bool Visible(int def, int[] owned)
    {
        return Has(def, owned) && StallTier >= Defs[def].MinStall && StallTier <= Defs[def].MaxStall;
    }

    bool Has(int def, int[] owned)
    {
        return buildingOf[def] >= 0 && owned[buildingOf[def]] > 0;
    }

    // worldX: how far the dog has run, in canvas px. A thing's screen x is its world x minus worldX.
    public void Tick(float dt, double worldX, int[] owned, float darkness)
    {
        float panelW = layer.rect.width;
        if (panelW <= 1f) return; // layout not ready yet

        CheckCrechePass(worldX, owned);

        nightTint = Color.Lerp(Color.white, NightTint, darkness);
        TickCans(dt, worldX, panelW, canBuilding >= 0 && owned[canBuilding] > 0);

        releaseTimer -= dt;
        if (releaseTimer <= 0f)
        {
            releaseTimer = 3f;
            ReleaseUnused();
        }

        System.Array.Clear(slotOwned, 0, slotCount);
        for (int i = 0; i < Defs.Length; i++)
            if (Has(i, owned)) slotOwned[defSlot[i]] = true;

        bool lamps = lampBuilding >= 0 && owned[lampBuilding] > 0;
        bool lampsJustBought = lamps && !lampsOn && lampsInit;
        lampsOn = lamps;
        lampsInit = true;

        bool dirty = false;

        // Drop what the dog left behind, and keep the stretches still in front of him in line with what is owned
        // (a purchase pops the building up in its slot; a rebirth puts the sign back).
        for (int k = insts.Count - 1; k >= 0; k--)
        {
            var inst = insts[k];
            if (inst.x + inst.half < worldX - EdgeMargin)
            {
                DestroyInst(inst);
                insts.RemoveAt(k);
                continue;
            }

            if (inst.slot >= slotCount + 2 && !lamps)
            {
                DestroyInst(inst);
                insts.RemoveAt(k);
                continue;
            }

            if (inst.slot >= slotCount) continue; // welcome board: nothing to keep in line

            float sx = (float)(inst.x - worldX);
            bool onScreen = sx + inst.half > -100f && sx - inst.half < panelW + 100f;
            for (int i = 0; i < Defs.Length; i++)
            {
                if (defSlot[i] != inst.slot) continue;
                bool has = Visible(i, owned);
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
            }

            bool wantSign = !slotOwned[inst.slot];
            if (wantSign && inst.sign == null)
            {
                inst.sign = SpawnSign(onScreen);
                dirty = true;
            }
            else if (!wantSign && inst.sign != null)
            {
                Object.Destroy(inst.sign.rt.gameObject);
                inst.sign = null;
            }
        }

        // Build every slot that falls inside the window, so the town is there before the dog is.
        double from = worldX - EdgeMargin, limit = worldX + panelW + EdgeMargin;
        long lap0 = (long)System.Math.Floor((from - lapPx) / lapPx);
        long lap1 = (long)System.Math.Floor(limit / lapPx);
        for (long lap = lap0; lap <= lap1; lap++)
        {
            for (int sl = 0; sl < slotTotal; sl++)
            {
                double x = lap * lapPx + slotX[sl];
                if (sl >= slotCount + 2 && !lamps) continue;
                if (x + slotHalf[sl] < from || x - slotHalf[sl] >= limit || HasInst(sl, x)) continue;
                AddInst(sl, x, lampsJustBought && sl >= slotCount + 2, owned);
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
            if (inst.banner != null)
            {
                Place(inst.banner, baseX, dt);
                if (inst.banner.lit != null) UpdateLampLight(inst.banner, darkness);
            }
        }

        TickRunner(dt, worldX, panelW, owned, darkness);
        TickDrone(dt, panelW, owned);
    }

    void TickRunner(float dt, double worldX, float panelW, int[] owned, float darkness)
    {
        bool has = runnerBuilding >= 0 && owned[runnerBuilding] > 0;
        if (!has)
        {
            if (runner != null)
            {
                Object.Destroy(runner.rt.gameObject);
                Object.Destroy(runner.bubble.gameObject);
                if (runner.lightRt != null) Object.Destroy(runner.lightRt.gameObject);
                if (runner.glowRt != null) Object.Destroy(runner.glowRt.gameObject);
                runner = null;
            }

            return;
        }

        float limit = panelW * 0.5f + 450f; // distance from Caramelo at which he is surely off screen
        if (runner == null)
        {
            var frames = Frames(RunnerDef);
            if (frames == null) return;

            var go = new GameObject("delivery-dog", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(litLayer != null ? litLayer : layer, false); // above the night shade: he stays as bright as by day
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = frames[0];
            float h = RunnerDef.Height * SizeScale;
            rt.sizeDelta = new Vector2(h * frames[0].rect.width / frames[0].rect.height, h);

            runner = new Runner { rt = rt, img = img, frames = frames, x = worldX - limit, prevRel = -limit, cooldown = 4f };
            BuildBubble(runner);
            BuildRunnerLight(runner);
        }

        var r = runner;
        r.clock += dt;
        r.cooldown -= dt;

        if (r.wait > 0f)
        {
            r.wait -= dt;
            if (r.wait > 0f) return;
            r.x = worldX + r.enterSide * limit;
            r.prevRel = r.enterSide * limit;
            r.rt.gameObject.SetActive(true);
            SetRunnerLight(r, true);
        }

        // His pace swings slowly between slower and faster than Caramelo's top speed.
        float speed = RunnerSpeed * (0.95f + 0.2f * Mathf.Sin(r.clock * 2f * Mathf.PI / 28f));
        r.x += speed * dt;
        float rel = (float)(r.x - worldX);

        if (Mathf.Abs(rel) > limit)
        {
            // Out of sight: rest a while, then come back from the other side.
            r.wait = Random.Range(6f, 14f);
            r.enterSide = rel > 0f ? -1 : 1;
            r.rt.gameObject.SetActive(false);
            SetRunnerLight(r, false);
            r.sayTimer = 0f;
            r.bubble.gameObject.SetActive(false);
            return;
        }

        // Crossing Caramelo (he stands at the middle of the panel): say hi.
        if (r.prevRel * rel < 0f && r.cooldown <= 0f)
        {
            r.sayTimer = 3.2f;
            r.cooldown = RunnerSayCooldown;
        }

        r.prevRel = rel;

        r.anim += dt * speed / RunnerStride;
        int f = (int)r.anim % r.frames.Length;
        if (f != r.frame)
        {
            r.frame = f;
            r.img.sprite = r.frames[f];
        }

        float screenX = panelW * 0.5f + rel;
        r.rt.anchoredPosition = new Vector2(screenX, 0f);
        r.rt.SetAsLastSibling();
        UpdateRunnerLight(r, screenX, darkness);

        if (r.sayTimer > 0f)
        {
            r.sayTimer -= dt;
            r.bubble.gameObject.SetActive(r.sayTimer > 0f);
            r.bubbleGroup.alpha = Mathf.Clamp01(r.sayTimer / 0.5f);
            r.bubble.anchoredPosition = new Vector2(screenX, r.rt.sizeDelta.y + 10f);
            r.bubble.SetAsLastSibling();
        }
    }

    // The delivery dog carries a light above his head too (drawn in the lit layer, above the night shade).
    void BuildRunnerLight(Runner r)
    {
        if (litLayer == null) return;
        var sheet = Sheet("light", 4, 12, 48, 48);
        if (sheet == null) return;

        var g = NewLightImage(litLayer, "runner-glow", GlowSprite());
        g.rectTransform.sizeDelta = new Vector2(GlowSize, GlowSize);
        g.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var l = NewLightImage(litLayer, "runner-light", sheet[0]);
        l.rectTransform.sizeDelta = new Vector2(48f * 5f, 48f * 5f);
        l.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        r.lightRt = l.rectTransform; r.lightImg = l;
        r.glowRt = g.rectTransform; r.glowImg = g;
    }

    static void SetRunnerLight(Runner r, bool on)
    {
        if (r.lightRt != null) r.lightRt.gameObject.SetActive(on);
        if (r.glowRt != null) r.glowRt.gameObject.SetActive(on);
    }

    void UpdateRunnerLight(Runner r, float screenX, float darkness)
    {
        if (r.lightRt == null) return;
        float on = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.55f, darkness));
        var sheet = Sheet("light", 4, 12, 48, 48);
        if (sheet != null) r.lightImg.sprite = sheet[(int)(r.clock * 10f) % sheet.Length];
        r.lightRt.anchoredPosition = new Vector2(screenX + 20f, 250f);
        r.lightImg.color = new Color(1f, 0.95f, 0.7f, LightAlpha * on);
        r.glowRt.anchoredPosition = new Vector2(screenX, 110f);
        r.glowImg.color = new Color(1f, 0.82f, 0.45f, 0.22f * on);
    }

    void DestroyDrone()
    {
        if (drone == null) return;
        Object.Destroy(drone.rt.gameObject);
        Object.Destroy(drone.bubble.gameObject);
        drone = null;
    }

    void TickDrone(float dt, float panelW, int[] owned)
    {
        if (droneBuilding < 0 || owned[droneBuilding] <= 0 || litLayer == null)
        {
            DestroyDrone();
            return;
        }

        var d = drone;
        if (d == null)
        {
            var go = new GameObject("delivery-drone", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(litLayer, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            go.SetActive(false);
            d = drone = new DroneFlyer { rt = rt, img = img, timer = 1.5f };
            d.bubble = MakeBubble(litLayer, DroneLine, out d.bubbleGroup);
        }

        // The sky band the drone roams in, in the layer's space (its origin sits on the ground line).
        float panelH = ((RectTransform)layer.parent).rect.height;
        float origin = 0.4f * panelH + layer.anchoredPosition.y;
        float minY = 0.42f * panelH - origin, maxY = 0.56f * panelH - origin;
        float w = d.rt.sizeDelta.x;

        d.bob += dt;
        switch (d.state)
        {
            case DroneState.Away:
                d.timer -= dt;
                if (d.timer > 0f) return;
                DroneArrive(d, panelW, minY, maxY);
                return;

            case DroneState.Pause:
                d.timer -= dt;
                d.angle = Mathf.MoveTowards(d.angle, 0f, 14f * dt);
                if (d.timer <= 0f)
                {
                    if (d.legs > 0) DroneHop(d, panelW, minY, maxY, false);
                    else DroneLeave(d, panelW, minY, maxY);
                }

                break;

            default: // Fly, Leave
                d.t += dt / d.dur;
                float k = Mathf.Clamp01(d.t);
                float e = d.state == DroneState.Leave ? k * k : (d.easeOut ? Mathf.Sin(k * Mathf.PI * 0.5f) : k * k * (3f - 2f * k));
                d.pos = Vector2.Lerp(d.from, d.to, e);
                float target = Mathf.Sign(d.to.x - d.from.x) * -7f;
                d.angle = Mathf.MoveTowards(d.angle, target, 30f * dt);
                if (d.t >= 1f)
                {
                    if (d.state == DroneState.Leave)
                    {
                        d.state = DroneState.Away;
                        d.timer = Random.Range(DroneMinAway, DroneMaxAway);
                        d.rt.gameObject.SetActive(false);
                        d.bubble.gameObject.SetActive(false);
                        d.sayTimer = 0f;
                        return;
                    }

                    d.state = DroneState.Pause;
                    d.timer = Random.Range(0f, 0.9f);
                }

                break;
        }

        // Says hello a moment after it shows up.
        if (!d.said)
        {
            d.sayDelay -= dt;
            if (d.sayDelay <= 0f)
            {
                d.said = true;
                d.sayTimer = 3.2f;
            }
        }

        d.anim += dt * DroneFps;
        int f = (int)d.anim % d.frames.Length;
        if (f != d.frame)
        {
            d.frame = f;
            d.img.sprite = d.frames[f];
        }

        var p = d.pos + new Vector2(0f, Mathf.Sin(d.bob * 2.2f) * 9f);
        d.rt.anchoredPosition = p;
        d.rt.localRotation = Quaternion.Euler(0f, 0f, d.angle);
        d.rt.SetAsLastSibling(); // lit lamps are created later in the same layer: keep the drone in front of them

        if (d.sayTimer > 0f)
        {
            d.sayTimer -= dt;
            d.bubble.gameObject.SetActive(d.sayTimer > 0f);
            d.bubbleGroup.alpha = Mathf.Clamp01(d.sayTimer / 0.5f);
            float half = d.bubble.sizeDelta.x * 0.5f;
            d.bubble.anchoredPosition = new Vector2(Mathf.Clamp(p.x, half, Mathf.Max(half, panelW - half)), p.y + d.rt.sizeDelta.y * 0.5f + 10f);
            d.bubble.SetAsLastSibling();
        }
    }

    // A new visit: another model than the last one, entering from the right edge.
    void DroneArrive(DroneFlyer d, float panelW, float minY, float maxY)
    {
        int v;
        do v = Random.Range(0, DroneVariants.Length); while (v == d.variant && DroneVariants.Length > 1);
        var def = DroneVariants[v];
        var frames = Frames(def);
        if (frames == null) { d.timer = 5f; return; }

        d.variant = v;
        d.frames = frames;
        d.frame = -1;
        d.img.sprite = frames[0];
        d.rt.sizeDelta = new Vector2(def.Height * def.FrameW / def.FrameH, def.Height);
        d.pos = new Vector2(panelW + d.rt.sizeDelta.x, Random.Range(minY, maxY));
        d.legs = Random.Range(3, 6);
        d.said = false;
        d.sayDelay = 1.2f;
        d.angle = 0f;
        d.rt.gameObject.SetActive(true);
        DroneHop(d, panelW, minY, maxY, true);
    }

    void DroneHop(DroneFlyer d, float panelW, float minY, float maxY, bool easeOut)
    {
        d.legs--;
        float margin = 56f + d.rt.sizeDelta.x * 0.5f;
        Vector2 target = d.pos;
        for (int i = 0; i < 6; i++)
        {
            target = new Vector2(Random.Range(margin, Mathf.Max(margin, panelW - margin)), Random.Range(minY, maxY));
            if (Vector2.Distance(d.pos, target) >= 300f) break;
        }

        d.from = d.pos;
        d.to = target;
        d.t = 0f;
        d.dur = Mathf.Max(2.5f, Vector2.Distance(d.from, d.to) / DroneSpeed);
        d.easeOut = easeOut;
        d.state = DroneState.Fly;
    }

    void DroneLeave(DroneFlyer d, float panelW, float minY, float maxY)
    {
        d.from = d.pos;
        d.to = new Vector2(panelW + d.rt.sizeDelta.x, Mathf.Clamp(d.pos.y + Random.Range(-60f, 60f), minY, maxY));
        d.t = 0f;
        d.dur = Mathf.Max(2.5f, Vector2.Distance(d.from, d.to) / (DroneSpeed * 1.6f));
        d.state = DroneState.Leave;
    }

    const float BubblePadX = 56f;   // left/right padding between the delivery dog's dialogue text and the box edge

    static void BuildBubble(Runner r)
    {
        r.bubble = MakeBubble(r.rt.parent, RunnerLine, out r.bubbleGroup);
    }

    static RectTransform MakeBubble(Transform parent, string line, out CanvasGroup group)
    {
        var go = new GameObject("delivery-bubble", typeof(RectTransform), typeof(CanvasGroup));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(780f, 130f);

        var body = Piece(rt, "Body", new Vector2(0.5f, 1f), new Vector2(780f, 110f), Vector2.zero, new Color(0.15f, 0.1f, 0.06f));
        var inner = Piece(body.rectTransform, "Inner", new Vector2(0.5f, 0.5f), new Vector2(768f, 98f), Vector2.zero, Color.white);
        var tail = Piece(rt, "Tail", new Vector2(0.5f, 0f), new Vector2(34f, 34f), new Vector2(0f, 4f), Color.white);
        tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        tail.transform.SetAsFirstSibling();

        var tgo = new GameObject("Text", typeof(RectTransform));
        var trt = tgo.GetComponent<RectTransform>();
        trt.SetParent(inner.rectTransform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(BubblePadX, 0f);
        trt.offsetMax = new Vector2(-BubblePadX, 0f);
        var text = tgo.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = Loc.T(line);
        text.fontSize = 50f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(0.15f, 0.1f, 0.06f);
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        // The box hugs the line, with BubblePadX of air on the left and right of the text.
        float width = Mathf.Ceil(text.GetPreferredValues(text.text).x) + 2f * BubblePadX;
        rt.sizeDelta = new Vector2(width + 12f, 130f);
        body.rectTransform.sizeDelta = new Vector2(width + 12f, 110f);
        inner.rectTransform.sizeDelta = new Vector2(width, 98f);

        group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        go.SetActive(false);
        return rt;
    }

    void Explode(double x, float canH)
    {
        var frames = Frames(BoomDef);
        if (frames == null) return;

        var go = new GameObject("explosion", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(layer, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(BoomSize, BoomSize);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.sprite = frames[0];
        rt.SetAsLastSibling();
        booms.Add(new Boom { x = x, rt = rt, img = img, t = 0f });
        boomY = canH * 0.5f - CanDrop;
    }

    float boomY;

    void TickCans(float dt, double worldX, float panelW, bool unlocked)
    {
        if (!unlocked && cans.Count > 0)
        {
            // Bonus cans only exist once "Amigo Vira-lata" is bought (a rebirth takes them away again).
            foreach (var c in cans) Object.Destroy(c.rt.gameObject);
            cans.Clear();
        }

        for (int i = booms.Count - 1; i >= 0; i--)
        {
            var b = booms[i];
            b.t += dt * BoomFps;
            int f = (int)b.t;
            var bf = Frames(BoomDef);
            if (bf == null || f >= bf.Length)
            {
                Object.Destroy(b.rt.gameObject);
                booms.RemoveAt(i);
                continue;
            }

            b.img.sprite = bf[f];
            b.rt.anchoredPosition = new Vector2((float)(b.x - worldX), boomY);
        }

        for (int i = cans.Count - 1; i >= 0; i--)
        {
            var c = cans[i];
            if (c.x < worldX - Behind)
            {
                Object.Destroy(c.rt.gameObject);
                cans.RemoveAt(i);
                continue;
            }

            c.pop = Mathf.Min(1f, c.pop + dt / PopSeconds);
            float t = c.pop - 1f;
            float grow = c.pop >= 1f ? 1f : 1f + 2.70158f * t * t * t + 1.70158f * t * t;
            c.rt.anchoredPosition = new Vector2((float)(c.x - worldX), -CanDrop);
            c.rt.localScale = new Vector3(grow, grow, 1f);
        }

        if (!unlocked)
        {
            canTimer = Mathf.Max(canTimer, 8f); // starts counting once the building is bought
            return;
        }

        canTimer -= dt;
        if (canTimer > 0f) return;
        canTimer = Random.Range(25f, 70f);
        if (cans.Count >= MaxCans) return;

        var frames = Frames(new Def { File = "trash-can" });
        if (frames == null) return;

        var go = new GameObject("bonus-trash-can", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(layer, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        var img = go.AddComponent<Image>();
        img.sprite = frames[0];
        float h = CanHeight * SizeScale;
        rt.sizeDelta = new Vector2(h * frames[0].rect.width / frames[0].rect.height, h);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        var can = new Can { x = worldX + panelW + Random.Range(120f, 1800f), rt = rt, phase = Random.value * 6f };
        btn.onClick.AddListener(() =>
        {
            cans.Remove(can);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(new Vector3(0f, rt.rect.height * 0.5f, 0f)));
            Explode(can.x, rt.sizeDelta.y);
            Object.Destroy(go);
            OnCanClicked?.Invoke(screen);
        });
        cans.Add(can);
        rt.SetAsLastSibling();
    }

    bool HasInst(int slot, double x)
    {
        foreach (var inst in insts)
            if (inst.slot == slot && System.Math.Abs(inst.x - x) < 1.0) return true;
        return false;
    }

    void AddInst(int slot, double x, bool popIn, int[] owned)
    {
        var inst = new Inst { slot = slot, x = x, half = slotHalf[slot] };
        if (slot >= slotCount)
        {
            inst.banner = slot < slotCount + 2 ? SpawnBanner(BannerText[slot - slotCount]) : MakeLampLight(Spawn(LampDef, popIn));
            insts.Add(inst);
            return;
        }

        for (int i = 0; i < Defs.Length; i++)
            if (defSlot[i] == slot && Visible(i, owned)) inst.byDef[i] = Spawn(Defs[i], popIn);
        if (!slotOwned[slot]) inst.sign = SpawnSign(popIn);
        insts.Add(inst);
    }

    float releaseTimer;

    // Frees the sprites and textures of every prop that has no instance alive anymore, so only what is near the
    // dog stays in memory (they are loaded again on demand by Frames).
    // A layer above the night shade (built by GameUI right after it), where lit lamps and their glow are drawn.
    public void AttachLitLayer(Transform parent)
    {
        var go = new GameObject("TownLights", typeof(RectTransform));
        litLayer = go.GetComponent<RectTransform>();
        litLayer.SetParent(parent, false);
        litLayer.anchorMin = layer.anchorMin;
        litLayer.anchorMax = layer.anchorMax;
        litLayer.pivot = layer.pivot;
        litLayer.anchoredPosition = layer.anchoredPosition;
        litLayer.sizeDelta = Vector2.zero;
    }

    Prop MakeLampLight(Prop p)
    {
        if (p == null || litLayer == null) return p;
        var on = Frames(LampOnDef);
        if (on == null) return p;

        var g = NewLightImage(litLayer, "lamp-glow", GlowSprite());
        g.rectTransform.sizeDelta = new Vector2(GlowSize, GlowSize);
        g.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        var l = NewLightImage(litLayer, "lamp-lit", on[0]);
        l.rectTransform.sizeDelta = p.rt.sizeDelta;
        l.rectTransform.pivot = new Vector2(0.5f, 0f);
        p.lit = l.rectTransform; p.litImg = l;
        p.glow = g.rectTransform; p.glowImg = g;
        return p;
    }

    static Image NewLightImage(RectTransform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    // Lamps switch on as it gets dark: the lit copy fades in over the unlit one and a warm glow spreads around the lamp.
    void UpdateLampLight(Prop p, float darkness)
    {
        float on = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.55f, darkness));
        Vector2 pos = p.rt.anchoredPosition;
        p.lit.anchoredPosition = pos;
        p.lit.localScale = p.rt.localScale;
        p.litImg.color = new Color(1f, 1f, 1f, on);
        p.glow.anchoredPosition = pos + new Vector2(0f, p.rt.sizeDelta.y * LampHeadFrac * p.rt.localScale.y);
        p.glow.localScale = p.rt.localScale;
        p.glowImg.color = new Color(1f, 0.82f, 0.45f, 0.6f * on);
    }

    public static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - n * 0.5f) * (x - n * 0.5f) + (y - n * 0.5f) * (y - n * 0.5f)) / (n * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }

        tex.SetPixels32(px);
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return glowSprite;
    }

    // A sprite sheet cut into frames (row-major from the top), cached and kept loaded by ReleaseUnused for "light".
    public static Sprite[] Sheet(string file, int cols, int frames, int frameW, int frameH)
    {
        return Frames(new Def { File = file, Cols = cols, Frames = frames, FrameW = frameW, FrameH = frameH });
    }

    void AddLamp(double x, double half)
    {
        if (slotTotal >= slotX.Length) return;
        slotX[slotTotal] = x;
        slotHalf[slotTotal] = half;
        slotTotal++;
    }

    void ReleaseUnused()
    {
        var used = new HashSet<string> { "trash-can", "explosion", "light" };
        if (runner != null) used.Add(RunnerDef.File);
        if (drone != null && drone.variant >= 0) used.Add(DroneVariants[drone.variant].File);
        foreach (var inst in insts)
            if (inst.banner != null && inst.banner.def == LampDef) { used.Add(LampDef.File); used.Add(LampOnDef.File); }
        foreach (var inst in insts)
            foreach (var p in inst.byDef)
                if (p != null) used.Add(p.def.File);

        var drop = new List<string>();
        foreach (var kv in Cache)
            if (!used.Contains(kv.Key)) drop.Add(kv.Key);

        foreach (var key in drop)
        {
            var frames = Cache[key];
            Cache.Remove(key);
            if (frames == null || frames.Length == 0) continue;
            var tex = frames[0] != null ? frames[0].texture : null;
            foreach (var f in frames)
                if (f != null) Object.Destroy(f);
            if (tex != null) Resources.UnloadAsset(tex);
        }
    }

    static void DestroyInst(Inst inst)
    {
        foreach (var p in inst.byDef)
            if (p != null) Object.Destroy(p.rt.gameObject);
        if (inst.sign != null) Object.Destroy(inst.sign.rt.gameObject);
        if (inst.banner != null)
        {
            Object.Destroy(inst.banner.rt.gameObject);
            if (inst.banner.lit != null) Object.Destroy(inst.banner.lit.gameObject);
            if (inst.banner.glow != null) Object.Destroy(inst.banner.glow.gameObject);
        }
    }

    // A welcome board on two wooden posts, drawn from uGUI pieces.
    Prop SpawnBanner(string message)
    {
        var go = new GameObject("welcome-board", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(layer, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(BannerWidth, 400f);

        var wood = new Color(0.45f, 0.27f, 0.12f);
        var cream = new Color(0.97f, 0.93f, 0.82f);
        Piece(rt, "PostL", new Vector2(0.14f, 0f), new Vector2(40f, 220f), Vector2.zero, wood);
        Piece(rt, "PostR", new Vector2(0.86f, 0f), new Vector2(40f, 220f), Vector2.zero, wood);
        var board = Piece(rt, "Board", new Vector2(0.5f, 1f), new Vector2(BannerWidth, 230f), Vector2.zero, wood);
        var inner = Piece(board.rectTransform, "Inner", new Vector2(0.5f, 0.5f), new Vector2(BannerWidth - 28f, 202f), Vector2.zero, cream);

        var tgo = new GameObject("Text", typeof(RectTransform));
        var trt = tgo.GetComponent<RectTransform>();
        trt.SetParent(inner.rectTransform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var text = tgo.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = Loc.T(message);
        text.fontSize = 64f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(0.45f, 0.27f, 0.12f);
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        return WithPieces(new Prop { def = SignDef, rt = rt, frames = new Sprite[1], pop = 1f });
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
        trt.offsetMin = new Vector2(14f, 0f);   // padding, so longer translations ("FOR SALE") stay inside the board
        trt.offsetMax = new Vector2(-14f, 0f);
        var text = tgo.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = Loc.T("VENDA");
        text.enableAutoSizing = true;
        text.fontSizeMin = 30f;
        text.fontSizeMax = 78f;
        text.fontStyle = TMPro.FontStyles.Bold;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(0.78f, 0.15f, 0.12f);
        text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        post.transform.SetAsFirstSibling();

        return WithPieces(new Prop
        {
            def = SignDef, rt = rt, frames = new Sprite[1],
            pop = popIn ? 0f : 1f,
        });
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
        foreach (var c in cans) c.rt.SetAsLastSibling(); // bonus cans stay in front of every building
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

    static Prop WithPieces(Prop p)
    {
        p.pieces = p.rt.GetComponentsInChildren<Image>();
        p.pieceColors = new Color[p.pieces.Length];
        for (int i = 0; i < p.pieces.Length; i++) p.pieceColors[i] = p.pieces[i].color;
        return p;
    }

    // Buildings stay only a little darker at night than by day.
    static readonly Color NightTint = new Color(0.74f, 0.78f, 0.93f);
    Color nightTint = Color.white;

    public void RaiseAboveShade() { layer.SetAsLastSibling(); }

    void Place(Prop p, float slotScreenX, float dt)
    {
        if (p.img != null) p.img.color = nightTint;
        if (p.pieces != null)
            for (int i = 0; i < p.pieces.Length; i++)
                if (p.pieces[i] != null) p.pieces[i].color = p.pieceColors[i] * nightTint;

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
