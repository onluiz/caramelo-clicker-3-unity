using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// Builds and drives the whole uGUI hierarchy. Layout is anchor-based against a 1600x900 (landscape) or 1080x1920 (portrait) reference.
public class GameUI
{
    const float GroundTop = 215f;         // the ground strip's top edge, this far below the dog's anchor
    const float LeftWidth = 0.58f;        // landscape: share of the width used by the game area
    const float GameScale = 0.85f;        // the whole game area (HUD, ground, dog) is drawn a bit smaller than the panel
    const float GameScaleMin = 0.6f;      // ...and shrinks further on big screens, but never below this
    const float ShrinkFromShortSide = 1080f; // shortest screen side (px) where the shrinking starts
    const float ShrinkToShortSide = 2160f;   // shortest screen side (px) where it reaches GameScaleMin
    const bool ShowDevTab = false;        // the Dev tab stays hidden for now (DevMode still gates it outside production)
    const float PortraitShopHeight = 0.45f; // portrait: share of the height used by the shop
    const float RowHeight = 146f;
    const int FloatPoolSize = 24;
    const int FallingPoolSize = 28;
    static readonly int[] BuyModes = { 1, 10, 100, -1 };
    static readonly string[] BuyLabels = { "x1", "x10", "x100", "Máx" };

    static readonly Color Ink = ProceduralArt.Hex("#4A2108");
    static readonly Color PanelDark = ProceduralArt.Hex("#3B2314");
    static readonly Color RowOn = ProceduralArt.Hex("#FFF1DA");
    static readonly Color RowOff = ProceduralArt.Hex("#CDB99C");
    static readonly Color Good = ProceduralArt.Hex("#2E7D32");
    static readonly Color Bad = ProceduralArt.Hex("#B3261E");
    static readonly Color InfoBg = ProceduralArt.Hex("#3A2F5C");
    static readonly Color InfoText = ProceduralArt.Hex("#F3ECFF");
    static readonly Color AscendReady = ProceduralArt.Hex("#FFB627");
    static readonly Color CardBg = ProceduralArt.Hex("#3B2314");
    static readonly Color CardBorder = ProceduralArt.Hex("#F6A33B");
    static readonly Color CardText = ProceduralArt.Hex("#FFF1D6");
    static readonly Color TabOn = ProceduralArt.Hex("#F6A33B");
    static readonly Color TabOff = ProceduralArt.Hex("#6B452B");

    class RowView
    {
        public GameObject go;
        public Button button;
        public Image bg, icon;
        public TextMeshProUGUI name, desc, cost, count;
    }

    class FloatingText
    {
        public RectTransform rt;
        public TextMeshProUGUI text;
        public float life, maxLife;
        public bool active;
    }

    class FallingBone
    {
        public RectTransform rt;
        public float speed, spin;
        public bool active;
    }

    readonly GameState s;
    readonly GameAudio sfx;

    RectTransform canvasRect, bgLayer, floatLayer, overlayLayer;
    SkyParallax sky;
    Image nightShade, headLight, headGlow;
    TextMeshProUGUI forceNightText;
    Sprite[] headLightFrames;
    const float NightShadeMax = 0.58f;
    const float HeadGlowY = -150f; // the glow is centred on his body, so he is lit too
    const float HeadLightX = 25f, HeadLightY = -30f; // from the dog anchor: right above his head
    RectTransform hudCoin, hudFrag, hudGem, hudGear, hudSit, hudCps, hudCpsShadow, hudBuff, shopBackdrop, safeRoot, tapArea, leftPanel, rightPanel, tabBuildingsRt, tabUpgradesRt, tabCeuRt, tabDevRt, buyBarRt, scrollRt;
    CanvasScaler scaler;
    bool portrait;
    Vector2Int lastScreen;
    Rect lastSafe;
    DogRig rig;
    MiniDogs minis;
    TownProps town;
    double worldX; // how far the dog has run, in canvas px; the world is fixed and the camera follows him
    RectTransform groundBand;

    class GroundMark
    {
        public RectTransform rt;
        public double x;
    }

    readonly List<GroundMark> groundMarks = new List<GroundMark>();
    double nextMarkX;
    bool groundStarted;

    // Asphalt under the industrial district: a dark strip (plus a light curb) per lap copy, positioned in world space.
    static readonly Color AsphaltColor = new Color(0.27f, 0.28f, 0.31f, 1f);
    static readonly Color AsphaltMark = new Color(0.17f, 0.18f, 0.2f, 0.55f);
    static readonly Color AsphaltCurb = new Color(0.62f, 0.63f, 0.66f, 1f);
    static readonly Color LaneColor = new Color(1f, 0.8f, 0.15f, 0.95f);
    const double LaneStep = 150.0;   // distance between dashes' starts
    const float LaneDash = 80f;
    readonly List<GroundMark> laneMarks = new List<GroundMark>();
    double nextLaneX;
    bool laneStarted;
    const float AsphaltMargin = 200f; // the asphalt extends this far past the first and last building
    readonly RectTransform[] asphalt = new RectTransform[2];
    readonly RectTransform[] asphaltCurb = new RectTransform[2];

    // Residential district: a lawn with a pale pavement strip along the top, where the dog walks.
    static readonly Color LawnColor = new Color(0.45f, 0.68f, 0.3f, 1f);
    static readonly Color LawnMark = new Color(0.25f, 0.46f, 0.17f, 0.55f);
    static readonly Color PavementColor = new Color(0.82f, 0.78f, 0.69f, 1f);
    static readonly Color PavementEdge = new Color(0.58f, 0.54f, 0.46f, 1f);
    const float PavementHeight = 62f;
    readonly RectTransform[] lawn = new RectTransform[2];
    readonly RectTransform[] pavement = new RectTransform[2];
    readonly RectTransform[] pavementEdge = new RectTransform[2];

    static bool InZone(double x, double lap, double l, double r)
    {
        double m = x - System.Math.Floor((x - l) / lap) * lap; // l <= m < l + lap
        return m < r;
    }

    bool OnAsphalt(double x) => InZone(x, town.LapPx, town.IndustrialL - AsphaltMargin, town.IndustrialR + AsphaltMargin);
    bool OnLawn(double x) => InZone(x, town.LapPx, town.CityL - AsphaltMargin, town.CityR + AsphaltMargin);
    float clock;

    TextMeshProUGUI counterText, counterShadow, cpsText, buffText, toastText, statsText;
    Image statsPill;
    TextMeshProUGUI tabBuildingsText, tabUpgradesText, tabCeuText, tabDevText, emptyText, muteText, resetText, sitText, awakeText;
    readonly ScreenAwake awake = new ScreenAwake();
    Image tabBuildingsBg, tabUpgradesBg, tabCeuBg, tabDevBg, resetBg;
    GameObject devContent, ceuContent;
    bool showDev, showCeu;
    readonly bool devEnabled = ShowDevTab && DevMode.Enabled;
    RowView ascendRow, ossosRow;
    Image buffChip;
    RectTransform settingsRoot;
    TextMeshProUGUI buyLabelText, settingsTitleText, closeText, langText;
    TextMeshProUGUI ossosText, fragmentsText, cpsShadow;
    readonly List<RowView> blessingRows = new List<RowView>();
    float ascendConfirmTimer;
    readonly List<Image> buyBg = new List<Image>();
    readonly List<TextMeshProUGUI> buyText = new List<TextMeshProUGUI>();
    GameObject buyBar;
    ScrollRect scroll;
    GameObject buildingsContent, upgradesContent;
    readonly List<RowView> buildingRows = new List<RowView>();
    readonly List<RowView> upgradeRows = new List<RowView>();
    bool showUpgrades;
    int buyModeIndex;

    readonly FloatingText[] floats = new FloatingText[FloatPoolSize];
    readonly FallingBone[] falling = new FallingBone[FallingPoolSize];
    float fallingAcc;

    RectTransform goldenRect;
    Image goldenImage;
    bool goldenActive;
    float goldenTimer, goldenLife;

    float toastTimer, resetConfirmTimer, uiTimer, counterTimer;

    public GameUI(GameState state, GameAudio audio)
    {
        s = state;
        sfx = audio;

        BuildCanvas();
        BuildBackground();
        BuildLeftPanel();
        BuildRightPanel();
        BuildOverlay();
        BuildSettings();
        ApplyLayout();
        ApplyLanguage();

        goldenTimer = Random.Range(25f, 45f) * (float)s.GoldenInterval;
        RefreshAll();
        if (!string.IsNullOrEmpty(s.OfflineMessage)) ShowToast(s.OfflineMessage, 8f);
    }

    // ---------------------------------------------------------------- building blocks

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Anchors(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Fills the given region but draws its content at `scale`: the panel is laid out larger by 1/scale
    // around the region's centre and then scaled down, so the background still covers the whole region.
    static void AnchorsScaled(RectTransform rt, float x0, float y0, float x1, float y1, float scale)
    {
        float cx = (x0 + x1) / 2f, cy = (y0 + y1) / 2f;
        float hx = (x1 - x0) / 2f / scale, hy = (y1 - y0) / 2f / scale;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(cx - hx, cy - hy);
        rt.anchorMax = new Vector2(cx + hx, cy + hy);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = new Vector3(scale, scale, 1f);
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Box(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
    }

    static Image NewImage(Transform parent, string name, Sprite sprite, Color color, bool sliced = false, bool raycast = false)
    {
        var rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        if (sliced) img.type = Image.Type.Sliced;
        return img;
    }

    static TextMeshProUGUI NewText(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        // The default font has no ellipsis glyph, so truncate instead of letting TMP warn every rebuild.
        t.overflowMode = TextOverflowModes.Truncate;
        return t;
    }

    static Button NewButton(Transform parent, string name, string label, float fontSize, Color bgColor, Color textColor,
        out Image bg, out TextMeshProUGUI text)
    {
        bg = NewImage(parent, name, ProceduralArt.RoundedRect(), bgColor, true, true);
        var btn = bg.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        StyleButton(btn);
        text = NewText(bg.transform, "Label", label, fontSize, textColor, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(text.rectTransform);
        return btn;
    }

    static void StyleButton(Button btn)
    {
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.05f;
        btn.colors = colors;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
    }

    // ---------------------------------------------------------------- construction

    void BuildCanvas()
    {
        var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = (RectTransform)go.transform;
    }

    void BuildBackground()
    {
        var bg = NewImage(canvasRect, "Background",
            ProceduralArt.Gradient(ProceduralArt.Hex("#FFE2B0"), ProceduralArt.Hex("#F59E4B")), Color.white);
        Stretch(bg.rectTransform);

        // Behind the falling bones; ApplyLayout sizes it to the game area.
        sky = new SkyParallax(canvasRect);

        bgLayer = NewRect("FallingBones", canvasRect);
        Stretch(bgLayer);
        for (int i = 0; i < falling.Length; i++)
        {
            var img = NewImage(bgLayer, "Bone", IconLoader.Load("petisco"), new Color(1f, 1f, 1f, 0.55f));
            img.gameObject.SetActive(false);
            falling[i] = new FallingBone { rt = img.rectTransform };
        }

        // The shop's colour reaches the screen edges, behind the notch / rounded corners outside the safe area.
        shopBackdrop = NewImage(canvasRect, "ShopBackdrop", null, PanelDark, false, false).rectTransform;

        // Game and shop panels live inside the device's safe area (notches, rounded corners).
        safeRoot = NewRect("SafeArea", canvasRect);
        Stretch(safeRoot);

        // The whole game area is a tap target. It sits under every other element, so anything
        // that blocks raycasts (buttons, the shop) is not a click on the dog. The HUD counters let taps through.
        var tap = NewImage(safeRoot, "TapArea", null, new Color(0f, 0f, 0f, 0f), false, true);
        tapArea = tap.rectTransform;
        var trigger = tap.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(data => OnDogDown((PointerEventData)data));
        trigger.triggers.Add(entry);
    }

    void BuildLeftPanel()
    {
        var left = NewRect("LeftPanel", safeRoot);
        leftPanel = left;

        // HUD (top row): petiscos pill, ossos pill and the settings button; petiscos per second below.
        var coinPill = NewImage(left, "CoinPill", ProceduralArt.RoundedRect(), CardBg, true);
        // Three pills of equal width: petiscos, fragments of the next celestial bone, celestial bones.
        Box(coinPill.rectTransform, new Vector2(0.05f, 1f), new Vector2(0.30f, 1f), new Vector2(0f, -120f), new Vector2(0f, -28f));
        hudCoin = coinPill.rectTransform;
        counterText = PillContent(coinPill, IconLoader.Load("petisco"));
        // No dark pill behind the petiscos: the value gets a drop shadow like the income text.
        coinPill.color = new Color(0f, 0f, 0f, 0f);
        counterShadow = NewText(coinPill.transform, "ValueShadow", "0", 56, ProceduralArt.Hex("#5A2E12"), TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        Box(counterShadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(92f, 0f), new Vector2(-20f, -8f));
        counterShadow.enableAutoSizing = true;
        counterShadow.fontSizeMin = counterText.fontSizeMin;
        counterShadow.fontSizeMax = counterText.fontSizeMax;
        counterShadow.transform.SetSiblingIndex(counterText.transform.GetSiblingIndex());

        var fragPill = NewImage(left, "FragmentsPill", ProceduralArt.RoundedRect(), CardBg, true);
        Box(fragPill.rectTransform, new Vector2(0.32f, 1f), new Vector2(0.49f, 1f), new Vector2(0f, -120f), new Vector2(0f, -28f));
        hudFrag = fragPill.rectTransform;
        fragmentsText = PillContent(fragPill, IconLoader.Load("fragmento-celestial"), 54f);

        var gemPill = NewImage(left, "OssosPill", ProceduralArt.RoundedRect(), CardBg, true);
        Box(gemPill.rectTransform, new Vector2(0.51f, 1f), new Vector2(0.68f, 1f), new Vector2(0f, -120f), new Vector2(0f, -28f));
        hudGem = gemPill.rectTransform;
        ossosText = PillContent(gemPill, IconLoader.Load("osso-celestial"));

        var gear = NewButton(left, "SettingsButton", "", 28, CardBg, Color.white, out _, out _);
        Place((RectTransform)gear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(92f, 92f));
        var gearIcon = NewImage(gear.transform, "Gear", ProceduralArt.Gear(), Color.white);
        Box(gearIcon.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 14f), new Vector2(-14f, -14f));
        hudGear = (RectTransform)gear.transform;
        gear.onClick.AddListener(() => SetSettingsOpen(true));

        // Sit / run toggle under the gear: the dog sits until the button is pressed again.
        var sitBtn = NewButton(left, "SitButton", "Sentar", 32, CardBg, Color.white, out _, out sitText);
        Place((RectTransform)sitBtn.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -134f), new Vector2(190f, 76f));
        hudSit = (RectTransform)sitBtn.transform;
        sitBtn.onClick.AddListener(() =>
        {
            rig.Sitting = !rig.Sitting;
            RefreshSitLabel();
        });

        // Where a game would show the stage: the income, as "n/s".
        // The dark copy behind is its drop shadow (the font's outline shader variant is not available).
        cpsShadow = NewText(left, "CpsShadow", "0/s", 84, ProceduralArt.Hex("#5A2E12"), TextAlignmentOptions.Center, FontStyles.Bold);
        Box(cpsShadow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, -256f), new Vector2(4f, -146f));
        cpsText = NewText(left, "Cps", "0/s", 84, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Box(cpsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -250f), new Vector2(0f, -140f));
        hudCps = cpsText.rectTransform;
        hudCpsShadow = cpsShadow.rectTransform;
        foreach (var t in new[] { cpsText, cpsShadow })
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = 28f;
            t.fontSizeMax = 56f; // the petiscos total now has the big size
        }

        buffChip = NewImage(left, "BuffChip", ProceduralArt.RoundedRect(), Bad, true);
        Box(buffChip.rectTransform, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, -316f), new Vector2(0f, -260f));
        hudBuff = buffChip.rectTransform;
        buffText = NewText(buffChip.transform, "Buff", "", 32, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Box(buffText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
        buffText.enableAutoSizing = true;
        buffText.fontSizeMin = 18f;
        buffText.fontSizeMax = 32f;
        buffChip.gameObject.SetActive(false);

        int hudCount = left.childCount; // everything built so far is HUD, and goes back on top of the night shade
        BuildGround(left);
        town = new TownProps(left, -GroundTop);
        town.OnCanClicked = OnTrashCanClicked;

        rig = new DogRig(left, new Vector2(0.5f, 0.4f));
        minis = new MiniDogs(left, rig);

        // Night shade over the ground, town and dogs; lit lamps and their glow go above it, then the HUD.
        nightShade = NewImage(left, "NightShade", null, new Color(0.02f, 0.04f, 0.16f, 0f), false);
        Stretch(nightShade.rectTransform);
        town.RaiseAboveShade(); // buildings are only slightly tinted at night, not shaded as much as the ground
        town.AttachLitLayer(left);

        // Caramelo's own light, above his head: switches on with the dark.
        headLightFrames = TownProps.Sheet("light", 4, 12, 48, 48);
        headGlow = NewImage(left, "HeadGlow", TownProps.GlowSprite(), new Color(1f, 0.82f, 0.45f, 0f), false);
        {
            var grt = headGlow.rectTransform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.4f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.sizeDelta = new Vector2(900f, 900f);
            grt.anchoredPosition = new Vector2(0f, HeadGlowY);
        }

        minis.Root.SetAsLastSibling(); // the mini dogs too, and Caramelo goes in front of them
        rig.Root.SetAsLastSibling(); // Caramelo is drawn above the night shade, so he stays as bright as by day

        if (headLightFrames != null)
        {
            headLight = NewImage(left, "HeadLight", headLightFrames[0], new Color(1f, 0.95f, 0.7f, 0f), false);
            var hrt = headLight.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.4f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(48f * 5f, 48f * 5f);
            hrt.anchoredPosition = new Vector2(HeadLightX, HeadLightY);
        }
        for (int i = 0; i < hudCount; i++) left.GetChild(0).SetAsLastSibling();

        toastText = NewText(left, "Toast", "", 32, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Anchors(toastText.rectTransform, 0.02f, 0.075f, 0.98f, 0.155f);
        toastText.textWrappingMode = TextWrappingModes.Normal;

        statsPill = NewImage(left, "StatsPill", ProceduralArt.RoundedRect(), new Color(CardBg.r, CardBg.g, CardBg.b, 0.9f), true);
        Place(statsPill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(620f, 56f));
        statsText = NewText(statsPill.transform, "Stats", "", 30, CardText, TextAlignmentOptions.Center, FontStyles.Bold);
        Box(statsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
    }

    // A dark rounded pill with an icon inside its left end and an auto-sized value.
    static TextMeshProUGUI PillContent(Image pill, Sprite icon, float iconSize = 68f)
    {
        var img = NewImage(pill.transform, "Icon", icon, Color.white);
        img.preserveAspect = true;
        Place(img.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f + (68f - iconSize) / 2f, 0f), new Vector2(iconSize, iconSize));
        var t = NewText(pill.transform, "Value", "0", 56, Color.white, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        Box(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(90f, 4f), new Vector2(-22f, -4f));
        t.enableAutoSizing = true;
        t.fontSizeMin = 24f;
        t.fontSizeMax = 56f;
        return t;
    }

    // Settings popup: sound, restart and close. Built last so it draws above everything.
    void BuildSettings()
    {
        settingsRoot = NewRect("Settings", canvasRect);
        Stretch(settingsRoot);

        var dim = NewImage(settingsRoot, "Dim", null, new Color(0f, 0f, 0f, 0.6f), false, true);
        Stretch(dim.rectTransform);
        var dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => SetSettingsOpen(false));

        var border = NewImage(settingsRoot, "Panel", ProceduralArt.RoundedRect(), CardBorder, true, true);
        Place(border.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 748f));
        var panel = NewImage(border.transform, "Inner", ProceduralArt.RoundedRect(), CardBg, true);
        Box(panel.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));

        var title = settingsTitleText = NewText(panel.transform, "Title", "Configurações", 54, CardText, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 80f));

        var muteBtn = NewButton(panel.transform, "MuteButton", "", 38, ProceduralArt.Hex("#8A5A2B"), Color.white, out _, out muteText);
        Place((RectTransform)muteBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(640f, 96f));
        muteBtn.onClick.AddListener(() =>
        {
            sfx.SetMuted(!sfx.Muted);
            RefreshButtons();
        });

        var langBtn = NewButton(panel.transform, "LanguageButton", "", 38, ProceduralArt.Hex("#8A5A2B"), Color.white, out _, out langText);
        Place((RectTransform)langBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(640f, 96f));
        langBtn.onClick.AddListener(() =>
        {
            Loc.Set(Loc.IsEnglish ? Lang.Pt : Lang.En);
            ApplyLanguage();
        });

        var awakeBtn = NewButton(panel.transform, "ScreenAwakeButton", "", 38, ProceduralArt.Hex("#8A5A2B"), Color.white, out _, out awakeText);
        Place((RectTransform)awakeBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -344f), new Vector2(640f, 96f));
        awakeBtn.onClick.AddListener(() =>
        {
            awake.Next();
            awakeText.text = awake.Label();
        });
        awakeText.text = awake.Label();

        var resetBtn = NewButton(panel.transform, "ResetButton", "", 38, ProceduralArt.Hex("#8A5A2B"), Color.white, out resetBg, out resetText);
        Place((RectTransform)resetBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -452f), new Vector2(640f, 96f));
        resetBtn.onClick.AddListener(OnResetClicked);

        float closeY = -584f;
        if (DevMode.Enabled)
        {
            // Dev only (Editor / local builds): type an amount of petiscos and add it to the balance.
            border.rectTransform.sizeDelta = new Vector2(780f, 976f);
            closeY = -812f;

            var nightBtn = NewButton(panel.transform, "ForceNightButton", "", 38, ProceduralArt.Hex("#8A5A2B"), Color.white, out _, out forceNightText);
            Place((RectTransform)nightBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -668f), new Vector2(640f, 96f));
            nightBtn.onClick.AddListener(() =>
            {
                sky.ForceNight = !sky.ForceNight;
                forceNightText.text = sky.ForceNight ? "Forçar noite: ligado" : "Forçar noite: desligado";
            });
            forceNightText.text = "Forçar noite: desligado";

            var field = NewImage(panel.transform, "DevAmount", ProceduralArt.RoundedRect(), ProceduralArt.Hex("#2B1A0E"), true, true);
            Place(field.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-114f, -560f), new Vector2(400f, 96f));
            var viewport = NewRect("Viewport", field.transform);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(20f, 6f);
            viewport.offsetMax = new Vector2(-20f, -6f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var inputText = NewText(viewport, "Text", "", 38, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Stretch(inputText.rectTransform);
            var hint = NewText(viewport, "Placeholder", "Petiscos (ex: 1e12)", 32, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            Stretch(hint.rectTransform);
            var input = field.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = inputText;
            input.placeholder = hint;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.targetGraphic = field;

            var addBtn = NewButton(panel.transform, "DevAddButton", "Adicionar", 34, TabOn, Ink, out _, out _);
            Place((RectTransform)addBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(256f, -560f), new Vector2(224f, 96f));
            addBtn.onClick.AddListener(() =>
            {
                string raw = input.text.Trim().Replace(',', '.');
                if (double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double amount)
                    && amount > 0.0 && !double.IsInfinity(amount))
                    DevAdd(amount);
                else sfx.PlayError();
            });
        }

        var closeBtn = NewButton(panel.transform, "CloseButton", "Fechar", 38, TabOn, Ink, out _, out closeText);
        Place((RectTransform)closeBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, closeY), new Vector2(640f, 96f));
        closeBtn.onClick.AddListener(() => SetSettingsOpen(false));

        settingsRoot.gameObject.SetActive(false);
    }

    // Re-applies every text that is not refreshed continuously (labels and row names/descriptions).
    void ApplyLanguage()
    {
        tabBuildingsText.text = Loc.T("Construções");
        buyLabelText.text = Loc.T("Comprar:");
        for (int i = 0; i < BuyLabels.Length; i++) buyText[i].text = Loc.T(BuyLabels[i]);
        emptyText.text = Loc.T("Nenhuma melhoria por enquanto.\nContinue juntando petiscos!");
        settingsTitleText.text = Loc.T("Configurações");
        closeText.text = Loc.T("Fechar");
        langText.text = Loc.T("Idioma: Português");
        awakeText.text = awake.Label();
        RefreshSitLabel();

        for (int i = 0; i < upgradeRows.Count; i++)
        {
            upgradeRows[i].name.text = GameDefs.NameOf(GameDefs.Upgrades[i]);
            upgradeRows[i].desc.text = GameDefs.DescOf(GameDefs.Upgrades[i]);
        }
        for (int i = 0; i < blessingRows.Count; i++)
        {
            blessingRows[i].name.text = PrestigeDefs.NameOf(PrestigeDefs.Blessings[i]);
            blessingRows[i].desc.text = PrestigeDefs.DescOf(PrestigeDefs.Blessings[i]);
        }

        RefreshButtons();
        RefreshAll();
    }

    // The button offers the action the dog is not doing: "Sentar" while he runs, "Correr" while he sits.
    void RefreshSitLabel()
    {
        sitText.text = Loc.T(rig.Sitting ? "Correr" : "Sentar");
    }

    void SetSettingsOpen(bool open)
    {
        resetConfirmTimer = 0f;
        RefreshButtons();
        settingsRoot.gameObject.SetActive(open);
    }

    // Splits the tab row into equal-width buttons (14px outer margin, 8px gaps).
    void LayoutTabs(float yMin, float yMax)
    {
        var tabs = devEnabled
            ? new[] { tabBuildingsRt, tabUpgradesRt, tabCeuRt, tabDevRt }
            : new[] { tabBuildingsRt, tabUpgradesRt, tabCeuRt };
        int n = tabs.Length;
        const float outer = 14f, gap = 8f;
        float shrink = (2f * outer + gap * (n - 1)) / n;
        for (int i = 0; i < n; i++)
        {
            float left = outer + i * (gap - shrink);
            Box(tabs[i], new Vector2((float)i / n, yMin), new Vector2((float)(i + 1) / n, yMax),
                new Vector2(left, 0f), new Vector2(left - shrink, 0f));
        }
    }

    void BuildRightPanel()
    {
        var right = NewImage(safeRoot, "RightPanel", null, PanelDark, false, true);
        var rt = right.rectTransform;
        rightPanel = rt;

        // Tabs
        var tabB = NewButton(rt, "TabBuildings", "Construções", 28, TabOn, Ink, out tabBuildingsBg, out tabBuildingsText);
        tabBuildingsRt = (RectTransform)tabB.transform;
        tabB.onClick.AddListener(() => ShowTab(0));

        var tabU = NewButton(rt, "TabUpgrades", "Melhorias", 28, TabOff, Color.white, out tabUpgradesBg, out tabUpgradesText);
        tabUpgradesRt = (RectTransform)tabU.transform;
        tabU.onClick.AddListener(() => ShowTab(1));

        var tabC = NewButton(rt, "TabCeu", "Renascer", 28, TabOff, Color.white, out tabCeuBg, out tabCeuText);
        tabCeuRt = (RectTransform)tabC.transform;
        tabC.onClick.AddListener(() => ShowTab(2));

        var tabD = NewButton(rt, "TabDev", "Dev", 28, TabOff, Color.white, out tabDevBg, out tabDevText);
        tabDevRt = (RectTransform)tabD.transform;
        tabD.onClick.AddListener(() => ShowTab(3));

        foreach (var t in new[] { tabBuildingsText, tabUpgradesText, tabCeuText, tabDevText })
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = 16f;
            t.fontSizeMax = 28f;
        }

        // Buy-amount selector (buildings tab only)
        buyBar = NewRect("BuyBar", rt).gameObject;
        buyBarRt = (RectTransform)buyBar.transform;
        var buyLabel = buyLabelText = NewText(buyBar.transform, "BuyLabel", "Comprar:", 30, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        Anchors(buyLabel.rectTransform, 0f, 0f, 0.28f, 1f);
        for (int i = 0; i < BuyModes.Length; i++)
        {
            int index = i;
            var btn = NewButton(buyBar.transform, "Buy" + BuyLabels[i], BuyLabels[i], 28, TabOff, Color.white, out var bg, out var txt);
            float x0 = 0.3f + i * 0.175f;
            Box((RectTransform)btn.transform, new Vector2(x0, 0.08f), new Vector2(x0 + 0.165f, 0.92f), Vector2.zero, Vector2.zero);
            btn.onClick.AddListener(() =>
            {
                buyModeIndex = index;
                RefreshBuyBar();
                RefreshShop();
            });
            buyBg.Add(bg);
            buyText.Add(txt);
        }

        // Scrolling list
        scrollRt = NewRect("Scroll", rt);
        scroll = scrollRt.gameObject.AddComponent<ScrollRect>();

        var viewport = NewRect("Viewport", scrollRt);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var catcher = viewport.gameObject.AddComponent<Image>();
        catcher.color = new Color(0f, 0f, 0f, 0f);

        buildingsContent = NewContent("BuildingsContent", viewport);
        upgradesContent = NewContent("UpgradesContent", viewport);
        ceuContent = NewContent("CeuContent", viewport);
        devContent = NewContent("DevContent", viewport);

        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        for (int i = 0; i < GameDefs.Buildings.Length; i++)
        {
            int index = i;
            var row = MakeRow(buildingsContent.transform, "Building" + i, IconLoader.For(GameDefs.Buildings[i].IconFile, GameDefs.Buildings[i].Icon));
            row.button.onClick.AddListener(() => BuyBuilding(index));
            buildingRows.Add(row);
        }

        foreach (var def in GameDefs.Upgrades)
        {
            var captured = def;
            var row = MakeRow(upgradesContent.transform, "Upgrade_" + def.Id, IconLoader.For(def.IconFile, def.Icon));
            row.name.text = def.Name;
            row.desc.text = def.Description;
            row.count.gameObject.SetActive(false);
            row.button.onClick.AddListener(() => BuyUpgrade(captured));
            upgradeRows.Add(row);
        }

        BuildCeuRows();
        if (devEnabled) BuildDevRows();
        else tabDevRt.gameObject.SetActive(false);

        emptyText = NewText(rt, "EmptyUpgrades", "Nenhuma melhoria por enquanto.\nContinue juntando petiscos!", 30,
            Color.white, TextAlignmentOptions.Center);
        emptyText.textWrappingMode = TextWrappingModes.Normal;
        Box(emptyText.rectTransform, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.7f), Vector2.zero, Vector2.zero);

        ShowTab(0);
        RefreshBuyBar();
    }

    // "Renascer" tab: be reborn for Ossos Celestiais, then spend them on permanent blessings.
    void BuildCeuRows()
    {
        var bone = IconLoader.Load("osso-celestial");

        // Info card: not a button, so it gets a dark panel, light text and no press feedback.
        ossosRow = MakeRow(ceuContent.transform, "Ossos", bone);
        ossosRow.count.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(ossosRow.button);
        ossosRow.bg.color = InfoBg;
        ossosRow.bg.raycastTarget = false;
        ossosRow.name.color = InfoText;
        ossosRow.desc.color = InfoText;
        ossosRow.cost.color = InfoText;

        // The ascend action is the important one: own icon, bigger title, pulses when ready.
        ascendRow = MakeRow(ceuContent.transform, "Ascend", IconLoader.For("ceu-ascender", GameDefs.PawIcon));
        ascendRow.count.gameObject.SetActive(false);
        ascendRow.button.onClick.AddListener(OnAscendClicked);
        ascendRow.name.fontSizeMax = 40f;

        foreach (var def in PrestigeDefs.Blessings)
        {
            var captured = def;
            var row = MakeRow(ceuContent.transform, "Blessing_" + def.Id, bone);
            row.name.text = def.Name;
            row.desc.text = def.Description;
            row.count.gameObject.SetActive(false);
            row.button.onClick.AddListener(() => BuyBlessing(captured));
            blessingRows.Add(row);
        }
    }

    void OnAscendClicked()
    {
        if (s.PendingOssos < 1)
        {
            sfx.PlayError();
            ShowToast(Loc.T("Ainda falta produzir mais petiscos para ganhar um Osso Celestial."), 4f);
            return;
        }

        if (ascendConfirmTimer <= 0f)
        {
            ascendConfirmTimer = 6f;
            ShowToast(Loc.T("Renascer reinicia petiscos, construções e melhorias. Toque de novo para confirmar!"), 6f);
            RefreshCeu();
            return;
        }

        ascendConfirmTimer = 0f;
        double gained = s.Ascend();
        s.Save();
        HideGolden();
        sfx.PlayGolden();
        ShowToast(Loc.F(gained == 1 ? "O Caramelo renasceu! +{0} Osso Celestial" : "O Caramelo renasceu! +{0} Ossos Celestiais", NumberFormat.Format(gained)), 6f);
        RefreshAll();
    }

    void BuyBlessing(BlessingDef def)
    {
        if (s.TryBuyBlessing(def)) sfx.PlayBuy();
        else sfx.PlayError();
        RefreshAll();
    }

    // Developer tab: cheats for testing the late game quickly.
    void BuildDevRows()
    {
        AddDevRow("+1 mil petiscos", "Adiciona 1.000 petiscos.", () => DevAdd(1e3));
        AddDevRow("+1 milhão de petiscos", "Adiciona 1.000.000 petiscos.", () => DevAdd(1e6));
        AddDevRow("+1 bilhão de petiscos", "Adiciona 1.000.000.000 petiscos.", () => DevAdd(1e9));
        AddDevRow("+1 trilhão de petiscos", "Adiciona 1e12 petiscos.", () => DevAdd(1e12));
        AddDevRow("+1 quatrilhão de petiscos", "Adiciona 1e15 petiscos.", () => DevAdd(1e15));
        AddDevRow("+1 quintilhão de petiscos", "Adiciona 1e18 petiscos.", () => DevAdd(1e18));
        AddDevRow("+1 sextilhão de petiscos", "Adiciona 1e21 petiscos.", () => DevAdd(1e21));
        AddDevRow("+10 Ossos Celestiais", "Adiciona 10 ossos ao saldo (e ao bônus).", () =>
        {
            s.DevAddOssos(10);
            sfx.PlayBuy();
            RefreshAll();
        });
        AddDevRow("Multiplicar por 10", "Multiplica seus petiscos atuais por 10.", () => DevAdd(Math.Max(1.0, s.Petiscos * 9.0)));
        AddDevRow("Zerar petiscos", "Remove todos os petiscos (mantém construções).", () =>
        {
            s.Petiscos = 0;
            RefreshAll();
        });
    }

    void AddDevRow(string title, string desc, UnityEngine.Events.UnityAction action)
    {
        var row = MakeRow(devContent.transform, "Dev_" + title, ProceduralArt.Icon(GameDefs.PawIcon));
        row.name.text = title;
        row.desc.text = desc;
        row.cost.text = "";
        row.count.gameObject.SetActive(false);
        row.button.onClick.AddListener(action);
    }

    void DevAdd(double amount)
    {
        s.Petiscos += amount;
        s.TotalEarned += amount;
        sfx.PlayBuy();
        RefreshAll();
    }

    GameObject NewContent(string name, RectTransform viewport)
    {
        var rt = NewRect(name, viewport);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 14);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt.gameObject;
    }

    RowView MakeRow(Transform parent, string name, Sprite iconSprite)
    {
        var row = new RowView();
        var bg = NewImage(parent, name, ProceduralArt.RoundedRect(), RowOn, true, true);
        row.go = bg.gameObject;
        row.bg = bg;

        var layout = row.go.AddComponent<LayoutElement>();
        layout.minHeight = RowHeight;
        layout.preferredHeight = RowHeight;

        row.button = row.go.AddComponent<Button>();
        row.button.targetGraphic = bg;
        StyleButton(row.button);

        row.icon = NewImage(row.go.transform, "Icon", iconSprite, Color.white);
        row.icon.preserveAspect = true;
        Place(row.icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(88f, 88f));

        // Text boxes must be at least one line tall: Truncate overflow drops lines that don't fit.
        row.name = NewText(row.go.transform, "Name", "", 32, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        Box(row.name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(120f, -46f), new Vector2(-112f, -6f));
        row.name.enableAutoSizing = true;
        row.name.fontSizeMin = 18f;
        row.name.fontSizeMax = 32f;

        row.desc = NewText(row.go.transform, "Desc", "", 20, ProceduralArt.Hex("#35200D"), TextAlignmentOptions.TopLeft);
        row.desc.textWrappingMode = TextWrappingModes.Normal;
        // Flavor texts vary a lot in length: shrink to fit the box instead of cutting them off.
        row.desc.enableAutoSizing = true;
        row.desc.fontSizeMin = 15f;
        row.desc.fontSizeMax = 22f;
        Box(row.desc.rectTransform, Vector2.zero, Vector2.one, new Vector2(120f, 40f), new Vector2(-112f, -46f));

        row.cost = NewText(row.go.transform, "Cost", "", 28, Good, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        // The cost line can carry an effect suffix: shrink to fit instead of cutting it off.
        row.cost.enableAutoSizing = true;
        row.cost.fontSizeMin = 16f;
        row.cost.fontSizeMax = 28f;
        Box(row.cost.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(120f, 4f), new Vector2(-112f, 40f));

        row.count = NewText(row.go.transform, "Count", "", 62, new Color(Ink.r, Ink.g, Ink.b, 0.4f),
            TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        Place(row.count.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(120f, 80f));
        return row;
    }

    void BuildOverlay()
    {
        floatLayer = NewRect("FloatingTexts", canvasRect);
        Stretch(floatLayer);
        for (int i = 0; i < floats.Length; i++)
        {
            var t = NewText(floatLayer, "Float", "", 52, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            t.rectTransform.sizeDelta = new Vector2(400f, 80f);
            t.gameObject.SetActive(false);
            floats[i] = new FloatingText { rt = t.rectTransform, text = t };
        }

        overlayLayer = NewRect("Overlay", canvasRect);
        Stretch(overlayLayer);

        goldenImage = NewImage(overlayLayer, "GoldenBone", ProceduralArt.GoldenBone(), Color.white, false, true);
        goldenRect = goldenImage.rectTransform;
        goldenRect.sizeDelta = new Vector2(100f, 100f);
        var btn = goldenImage.gameObject.AddComponent<Button>();
        btn.targetGraphic = goldenImage;
        btn.transition = Selectable.Transition.None;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(OnGoldenClicked);
        goldenImage.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- interaction

    void OnDogDown(PointerEventData e)
    {
        double value = s.Click();
        sfx.PlayClick();
        rig.Poke();
        minis.Spawn(e.position, worldX);
        SpawnFloat(e.position, "+" + NumberFormat.Format(value, true), Color.white);
    }

    void BuyBuilding(int index)
    {
        int mode = BuyModes[buyModeIndex];
        int n = mode > 0 ? mode : Mathf.Max(1, s.MaxAffordable(index));
        if (s.TryBuy(index, n)) sfx.PlayBuy();
        else sfx.PlayError();
        RefreshShop();
    }

    void BuyUpgrade(UpgradeDef def)
    {
        if (s.TryBuyUpgrade(def)) sfx.PlayBuy();
        else sfx.PlayError();
        RefreshShop();
    }

    void OnGoldenClicked()
    {
        HideGolden();
        sfx.PlayGolden();
        ShowToast(s.ApplyGoldenBone(), 6f);
        RefreshAll();
    }

    // Running past the daycare: the puppies that were following him and have run a full lap since birth stay there, and he earns a golden-bone style reward.
    void OnCrechePassed()
    {
        int n = minis.DropOff(worldX, town.LapPx);
        if (n <= 0) return;
        sfx.PlayGolden();
        string msg = s.ApplyCreche((double)n / MiniDogs.MaxDogs, out double gain);
        SpawnFloat(RectTransformUtility.WorldToScreenPoint(null, rig.Root.position), "+" + NumberFormat.Format(gain, true), new Color(1f, 0.85f, 0.2f), 2f, 1.8f);
        ShowToast(msg, 6f);
        RefreshAll();
    }

    void OnTrashCanClicked(Vector2 screenPos)
    {
        sfx.PlayGolden();
        string msg = s.ApplyTrashCan(out double gain);
        SpawnFloat(screenPos, "+" + NumberFormat.Format(gain, true), new Color(1f, 0.85f, 0.2f), 2f, 1.8f);
        ShowToast(msg, 6f);
        RefreshAll();
    }

    void OnResetClicked()
    {
        if (resetConfirmTimer <= 0f)
        {
            resetConfirmTimer = 6f;
            RefreshButtons();
            ShowToast(Loc.T("Isso apaga todo o progresso! Clique em \"Confirmar reinício\" para continuar."), 6f);
            return;
        }

        resetConfirmTimer = 0f;
        s.ResetAll();
        s.Save();
        RefreshButtons();
        HideGolden();
        ShowToast(Loc.T("Jogo reiniciado. Boa sorte, Caramelo!"), 4f);
        SetSettingsOpen(false);
        RefreshAll();
    }

    // 0 = buildings, 1 = upgrades, 2 = ceu (prestige), 3 = dev.
    void ShowTab(int tab)
    {
        showUpgrades = tab == 1;
        showCeu = tab == 2;
        showDev = tab == 3;
        buildingsContent.SetActive(tab == 0);
        upgradesContent.SetActive(tab == 1);
        ceuContent.SetActive(tab == 2);
        devContent.SetActive(tab == 3);
        buyBar.SetActive(tab == 0);
        ApplyScrollBox();
        var content = tab == 0 ? buildingsContent : tab == 1 ? upgradesContent : tab == 2 ? ceuContent : devContent;
        scroll.content = (RectTransform)content.transform;
        scroll.verticalNormalizedPosition = 1f;

        tabBuildingsBg.color = tab == 0 ? TabOn : TabOff;
        tabBuildingsText.color = tab == 0 ? Ink : Color.white;
        tabUpgradesBg.color = tab == 1 ? TabOn : TabOff;
        tabUpgradesText.color = tab == 1 ? Ink : Color.white;
        tabCeuBg.color = tab == 2 ? TabOn : TabOff;
        tabCeuText.color = tab == 2 ? Ink : Color.white;
        tabDevBg.color = tab == 3 ? TabOn : TabOff;
        tabDevText.color = tab == 3 ? Ink : Color.white;
        RefreshShop();
    }

    void ShowToast(string message, float seconds)
    {
        toastText.text = message;
        toastTimer = seconds;
    }

    // ---------------------------------------------------------------- refresh

    public void RefreshAll()
    {
        RefreshBuyBar();
        RefreshButtons();
        RefreshShop();
        RefreshCounters();
    }

    void RefreshBuyBar()
    {
        for (int i = 0; i < buyBg.Count; i++)
        {
            bool on = i == buyModeIndex;
            buyBg[i].color = on ? TabOn : TabOff;
            buyText[i].color = on ? Ink : Color.white;
        }
    }

    void RefreshButtons()
    {
        muteText.text = Loc.T(sfx.Muted ? "Som: Mudo" : "Som: Ligado");
        bool confirming = resetConfirmTimer > 0f;
        resetText.text = Loc.T(confirming ? "Confirmar reinício!" : "Reiniciar jogo");
        resetBg.color = confirming ? Bad : ProceduralArt.Hex("#8A5A2B");
    }

    void RefreshCounters()
    {
        double petiscos = Math.Floor(s.Petiscos);
        string coin = NumberFormat.Format(petiscos);
        counterText.text = counterShadow.text = coin;
        LayoutCoin(coin);
        ossosText.text = NumberFormat.Format(s.Ossos);
        fragmentsText.text = NumberFormat.Fraction(s.Fragments);
        cpsText.text = cpsShadow.text = NumberFormat.Format(s.Cps, true) + "/s";
        statsText.text = Loc.F("Cliques: {0}   |   Total: {1}", NumberFormat.Format(s.Clicks), NumberFormat.Format(s.LifetimeEarned));

        if (s.ClickFrenzyActive)
            buffText.text = Loc.F("Pata Furiosa! Cliques x{0} ({1}s)", GameState.ClickFrenzyMultiplier, Mathf.CeilToInt((float)(s.ClickFrenzyUntil - s.Time)));
        else if (s.FrenzyActive)
            buffText.text = Loc.F("Frenesi! Produção x{0} ({1}s)", GameState.FrenzyMultiplier, Mathf.CeilToInt((float)(s.FrenzyUntil - s.Time)));
        else
            buffText.text = "";

        buffChip.color = Bad;
        buffChip.gameObject.SetActive(buffText.text.Length > 0);
    }

    void RefreshShop()
    {
        if (showDev) emptyText.gameObject.SetActive(false);
        else if (showCeu)
        {
            emptyText.gameObject.SetActive(false);
            RefreshCeu();
        }
        else if (showUpgrades) RefreshUpgrades();
        else RefreshBuildings();

        double pending = s.PendingOssos;
        tabCeuText.text = Loc.T("Renascer");

        int available = 0;
        foreach (var def in GameDefs.Upgrades)
            if (s.IsUpgradeAvailable(def)) available++;
        tabUpgradesText.text = Loc.T("Melhorias");
    }

    void RefreshCeu()
    {
        ossosRow.name.text = Loc.F("Ossos Celestiais: {0}", NumberFormat.Format(s.Ossos));
        ossosRow.desc.text = Loc.F("Cada osso já ganho dá +{0}% de produção (hoje: +{1}%). Gaste os ossos em bênçãos permanentes.",
            (PrestigeDefs.BonusPerOsso * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture), NumberFormat.Format(s.PrestigeBonus * 100, true));
        ossosRow.cost.text = s.Ascensions > 0 ? Loc.F("Renascimentos: {0}", s.Ascensions) : "";

        double pending = s.PendingOssos;
        bool ready = pending >= 1;
        bool confirming = ascendConfirmTimer > 0f;
        ascendRow.bg.color = confirming ? Bad : ready ? AscendReady : RowOff;
        ascendRow.name.text = Loc.T(confirming ? "Toque de novo para confirmar!" : "Renascer mais poderoso");
        ascendRow.name.color = confirming ? Color.white : Ink;
        ascendRow.desc.color = confirming ? Color.white : ProceduralArt.Hex("#35200D");
        ascendRow.desc.text = Loc.T("O Caramelo usa os ossos celestiais para renascer mais poderoso. Reinicia petiscos, construções e melhorias comuns. Você mantém ossos, bênçãos e cliques.");
        if (ready)
        {
            ascendRow.cost.color = confirming ? Color.white : Ink;
            ascendRow.cost.text = Loc.F(pending == 1 ? "+{0} Osso Celestial" : "+{0} Ossos Celestiais", NumberFormat.Format(pending))
                + (s.Fragments - pending >= 0.01 ? " · " + Loc.F("Fragmentos: {0}", NumberFormat.Fraction(s.Fragments)) : "");
        }
        else
        {
            double needed = PrestigeDefs.Threshold * Math.Pow(s.OssosEver + 1, 3) - s.LifetimeEarned;
            ascendRow.cost.color = Bad;
            ascendRow.cost.text = Loc.F("Fragmentos: {0} de 1 osso · faltam {1} petiscos", NumberFormat.Fraction(s.Fragments), NumberFormat.Format(Math.Max(1, needed)));
        }

        for (int i = 0; i < blessingRows.Count; i++)
        {
            var def = PrestigeDefs.Blessings[i];
            var row = blessingRows[i];
            bool owned = s.IsBlessed(def);
            bool affordable = s.Ossos >= def.Cost;
            row.bg.color = owned ? RowOff : affordable ? RowOn : RowOff;
            row.cost.color = owned ? Ink : affordable ? Good : Bad;
            row.cost.text = owned ? Loc.T("Adquirida") : Loc.F(def.Cost == 1 ? "{0} osso" : "{0} ossos", NumberFormat.Format(def.Cost));
            row.icon.color = owned ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
        }
    }

    void RefreshBuildings()
    {
        emptyText.gameObject.SetActive(false);
        int mode = BuyModes[buyModeIndex];
        bool mysteryShown = false;
        for (int i = 0; i < buildingRows.Count; i++)
        {
            var row = buildingRows[i];
            var def = GameDefs.Buildings[i];

            if (!s.IsBuildingKnown(i))
            {
                bool show = !mysteryShown;
                mysteryShown = true;
                row.go.SetActive(show);
                if (!show) continue;

                row.name.text = "???";
                row.desc.text = Loc.T("Junte mais petiscos para desbloquear.");
                row.cost.text = "";
                row.count.text = "";
                row.icon.color = new Color(0.2f, 0.1f, 0.05f, 0.75f);
                row.bg.color = RowOff;
                continue;
            }

            row.go.SetActive(true);
            int n = mode > 0 ? mode : Mathf.Max(1, s.MaxAffordable(i));
            double cost = s.BuyCost(i, n);
            bool locked = s.IsBuildingLocked(i);
            bool affordable = !locked && s.Petiscos >= cost;

            row.icon.color = locked ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            row.bg.color = affordable ? RowOn : RowOff;
            row.name.text = GameDefs.NameOf(def);
            row.desc.text = GameDefs.DescOf(def);
            if (locked)
            {
                row.cost.color = Bad;
                row.cost.text = "<size=60%>" + Loc.T("Compre O Início para montar a barraca.") + "</size>";
                row.count.text = "";
                continue;
            }
            row.cost.color = affordable ? Good : Bad;
            row.cost.text = NumberFormat.Format(cost) + (n > 1 ? " (x" + n + ")" : "") +
                "  <size=70%><color=#35200D>" + Loc.F("+{0}/s cada", NumberFormat.Format(s.CpsEach(i), true)) + "</color></size>";
            row.count.text = s.Owned[i] > 0 ? s.Owned[i].ToString() : "";
        }
    }

    void RefreshUpgrades()
    {
        int visible = 0;
        for (int i = 0; i < upgradeRows.Count; i++)
        {
            var def = GameDefs.Upgrades[i];
            var row = upgradeRows[i];
            bool available = s.IsUpgradeAvailable(def);
            row.go.SetActive(available);
            if (!available) continue;

            visible++;
            bool affordable = s.Petiscos >= def.Cost;
            row.bg.color = affordable ? RowOn : RowOff;
            row.cost.color = affordable ? Good : Bad;
            row.cost.text = Loc.F("{0} petiscos", NumberFormat.Format(def.Cost)) +
                (def.Effect != null ? "  <size=70%><color=#35200D>" + GameDefs.EffectOf(def) + "</color></size>" : "");
        }

        emptyText.gameObject.SetActive(visible == 0);
    }

    // ---------------------------------------------------------------- layout

    // Landscape: game on the left, shop on the right. Portrait: game on top, shop below.
    void ApplyLayout()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;

        lastScreen = new Vector2Int(Screen.width, Screen.height);
        lastSafe = Screen.safeArea;
        portrait = Screen.height > Screen.width;

        scaler.referenceResolution = portrait ? new Vector2(1080f, 1920f) : new Vector2(1600f, 900f);

        float shortSide = Mathf.Min(Screen.width, Screen.height);
        float gameScale = Mathf.Lerp(GameScale, GameScaleMin, Mathf.InverseLerp(ShrinkFromShortSide, ShrinkToShortSide, shortSide));

        var safe = Screen.safeArea;
        safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

        // The sky fills the game area out to the screen edges, and ends where the shop starts.
        float splitX = (safe.xMin + LeftWidth * safe.width) / Screen.width;
        float splitY = (safe.yMin + PortraitShopHeight * safe.height) / Screen.height;
        if (portrait)
        {
            Anchors(sky.Root, 0f, splitY, 1f, 1f);
            Anchors(shopBackdrop, 0f, 0f, 1f, splitY);
        }
        else
        {
            Anchors(sky.Root, 0f, 0f, splitX, 1f);
            Anchors(shopBackdrop, splitX, 0f, 1f, 1f);
        }

        if (portrait)
        {
            AnchorsScaled(leftPanel, 0f, PortraitShopHeight, 1f, 1f, gameScale);
            Anchors(tapArea, 0f, PortraitShopHeight, 1f, 1f);
            Anchors(rightPanel, 0f, 0f, 1f, PortraitShopHeight);
            LayoutTabs(0.895f, 0.985f);
            Box(buyBarRt, new Vector2(0f, 0.81f), new Vector2(1f, 0.885f), new Vector2(14f, 0f), new Vector2(-14f, 0f));
        }
        else
        {
            AnchorsScaled(leftPanel, 0f, 0f, LeftWidth, 1f, gameScale);
            Anchors(tapArea, 0f, 0f, LeftWidth, 1f);
            Anchors(rightPanel, LeftWidth, 0f, 1f, 1f);
            LayoutTabs(0.915f, 0.985f);
            Box(buyBarRt, new Vector2(0f, 0.845f), new Vector2(1f, 0.905f), new Vector2(14f, 0f), new Vector2(-14f, 0f));
        }

        ApplyHud();
        lastCoinText = null;
        ApplyScrollBox();
    }

    string lastCoinText;

    // The petiscos icon and value sit side by side as one group, centred in the pill (the pill is centred on screen in portrait).
    void LayoutCoin(string value)
    {
        if (value == lastCoinText) return;
        float width = hudCoin.rect.width;
        if (width <= 0f) return;
        lastCoinText = value;

        const float icon = 102f, gap = 14f, size = 84f; // same size the income text used to have
        var iconRt = (RectTransform)hudCoin.Find("Icon");
        counterText.enableAutoSizing = counterShadow.enableAutoSizing = false;
        counterText.fontSize = size;
        float textW = counterText.GetPreferredValues(value, 10000f, 0f).x;
        float avail = width - icon - gap - 24f;
        float fs = textW > avail ? size * avail / textW : size;
        counterText.fontSize = counterShadow.fontSize = fs;
        textW = Mathf.Min(textW * fs / size, avail);

        // The value is centred like the income text under it, and the icon hangs to its left.
        float left = -textW / 2f - gap - icon;
        float textLeft = -textW / 2f;
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(left, 0f);
        foreach (var t in new[] { counterText, counterShadow })
        {
            var rt = t.rectTransform;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(textW + 4f, 110f);
            rt.anchoredPosition = new Vector2(textLeft + (t == counterShadow ? 5f : 0f), t == counterShadow ? -5f : 0f);
        }
    }

    // HUD positions, the same in both orientations: ossos and fragments stacked on the left, petiscos and the income centred, gear in the corner,
    // and the sit/run button at the bottom right of the game area.
    void ApplyHud()
    {
        var top = new Vector2(0f, 1f);
        // The game panel is wider in landscape, so the side pills and the centre one take different shares of it.
        float side = portrait ? 0.21f : 0.26f, mid0 = portrait ? 0.30f : 0.27f, mid1 = 1f - mid0;

        Box(hudGem, new Vector2(0.02f, 1f), new Vector2(side, 1f), new Vector2(0f, -100f), new Vector2(0f, -20f));
        Box(hudFrag, new Vector2(0.02f, 1f), new Vector2(side, 1f), new Vector2(0f, -188f), new Vector2(0f, -108f));
        Box(hudCoin, new Vector2(mid0, 1f), new Vector2(mid1, 1f), new Vector2(0f, -136f), new Vector2(0f, -26f));
        Place(hudGear, Vector2.one, Vector2.one, new Vector2(-16f, -26f), new Vector2(92f, 92f));
        Place(hudSit, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 28f), new Vector2(190f, 76f));
        Box(hudCpsShadow, top, Vector2.one, new Vector2(3f, -208f), new Vector2(3f, -138f));
        Box(hudCps, top, Vector2.one, new Vector2(0f, -205f), new Vector2(0f, -135f));
        Box(hudBuff, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, -274f), new Vector2(0f, -218f));
    }

    // The list sits under the buy-mode bar on the buildings tab; other tabs have no bar, so it moves up to the tabs.
    void ApplyScrollBox()
    {
        bool bar = buyBar.activeSelf;
        float top = portrait ? (bar ? 0.8f : 0.885f) : (bar ? 0.835f : 0.905f);
        Box(scrollRt, Vector2.zero, new Vector2(1f, top), new Vector2(10f, 10f), new Vector2(-10f, 0f));
    }

    // The game area's rectangle in canvas coordinates (origin at the canvas centre).
    Rect LeftPanelInCanvas()
    {
        var corners = new Vector3[4];
        leftPanel.GetWorldCorners(corners);
        var min = canvasRect.InverseTransformPoint(corners[0]);
        var max = canvasRect.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    // ---------------------------------------------------------------- per-frame

    public void Update(float dt)
    {
        clock += dt;
        awake.Update(dt);

        if (Screen.width != lastScreen.x || Screen.height != lastScreen.y || Screen.safeArea != lastSafe)
            ApplyLayout();

        counterTimer += dt;
        if (counterTimer >= 0.05f)
        {
            counterTimer = 0f;
            RefreshCounters();
        }

        uiTimer += dt;
        if (uiTimer >= 0.15f)
        {
            uiTimer = 0f;
            RefreshShop();
        }

        UpdateDog(dt);
        UpdateToast(dt);
        UpdateFloats(dt);
        UpdateFalling(dt);
        UpdateGolden(dt);

        if (resetConfirmTimer > 0f)
        {
            resetConfirmTimer -= dt;
            if (resetConfirmTimer <= 0f) RefreshButtons();
        }

        if (ascendConfirmTimer > 0f)
        {
            ascendConfirmTimer -= dt;
            if (ascendConfirmTimer <= 0f) RefreshCeu();
        }

        // Gentle pulse on the ascend button while it is available.
        bool pulse = showCeu && s.PendingOssos >= 1;
        float k = pulse ? 1f + 0.025f * Mathf.Sin(clock * 4f) : 1f;
        ascendRow.go.transform.localScale = new Vector3(k, k, 1f);
    }

    void UpdateDog(float dt)
    {
        rig.Tick(dt, s.Cps);
        minis.Tick(dt);
        worldX += rig.StrideSpeed * dt; // the dog is the one who moves; everything else sits at fixed world positions
        town.StallTier = s.TierOf(0);
        town.Tick(dt, worldX, s.Owned, sky.Darkness);
        if (town.PassedCreche) OnCrechePassed();
        nightShade.color = new Color(0.02f, 0.04f, 0.16f, NightShadeMax * sky.Darkness);
        if (headLight != null)
        {
            float on = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.55f, sky.Darkness));
            headLight.color = new Color(1f, 0.95f, 0.7f, TownProps.LightAlpha * on);
            headGlow.color = new Color(1f, 0.82f, 0.45f, 0.26f * on);
            headLight.sprite = headLightFrames[(int)(clock * 10f) % headLightFrames.Length];
        }
        sky.Update(dt, rig.StrideSpeed);
        UpdateGround();
    }

    // Ground under the dog's paws, filling the panel down to its bottom edge (behind the buttons).
    // Its marks sit at fixed world positions: built ahead of the dog and destroyed once he is past them.
    void BuildGround(Transform parent)
    {
        // Opaque, so the clouds behind the game area do not show through the ground.
        var band = NewImage(parent, "Ground", null, new Color(0.93f, 0.62f, 0.3f, 1f));
        groundBand = band.rectTransform;
        groundBand.anchorMin = Vector2.zero;
        groundBand.anchorMax = new Vector2(1f, 0.4f);
        groundBand.offsetMin = Vector2.zero;
        groundBand.offsetMax = new Vector2(0f, -GroundTop);

        var edge = NewImage(groundBand, "Edge", null, new Color(0.72f, 0.4f, 0.16f, 0.6f));
        edge.rectTransform.anchorMin = new Vector2(0f, 1f);
        edge.rectTransform.anchorMax = new Vector2(1f, 1f);
        edge.rectTransform.pivot = new Vector2(0.5f, 1f);
        edge.rectTransform.sizeDelta = new Vector2(0f, 5f);

        // Asphalt strips go under the brown edge and the marks, over the dirt; the curbs go on top of the edge.
        for (int i = 0; i < 2; i++)
        {
            var a = NewImage(groundBand, "Asphalt", null, AsphaltColor);
            asphalt[i] = a.rectTransform;
            asphalt[i].SetSiblingIndex(0);
            var c = NewImage(groundBand, "AsphaltCurb", null, AsphaltCurb);
            asphaltCurb[i] = c.rectTransform;
            foreach (var rt in new[] { asphalt[i], asphaltCurb[i] })
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
            }
            asphaltCurb[i].anchorMin = new Vector2(0f, 1f);
            asphaltCurb[i].pivot = new Vector2(0f, 1f);

            var g = NewImage(groundBand, "Lawn", null, LawnColor);
            lawn[i] = g.rectTransform;
            lawn[i].SetSiblingIndex(0);
            var p = NewImage(groundBand, "Pavement", null, PavementColor);
            pavement[i] = p.rectTransform;
            var pe = NewImage(groundBand, "PavementEdge", null, PavementEdge);
            pavementEdge[i] = pe.rectTransform;
            lawn[i].anchorMin = new Vector2(0f, 0f);
            lawn[i].anchorMax = new Vector2(0f, 1f);
            lawn[i].pivot = new Vector2(0f, 0.5f);
            foreach (var rt in new[] { pavement[i], pavementEdge[i] })
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
            }
        }
    }

    void UpdateAsphalt()
    {
        double lap = town.LapPx;
        double l = town.IndustrialL - AsphaltMargin, r = town.IndustrialR + AsphaltMargin;
        double k = System.Math.Floor((worldX - l) / lap); // the lap copy the dog is in (or just past)
        for (int i = 0; i < 2; i++)
        {
            double start = l + (k + i) * lap - worldX;
            float w = (float)(r - l);
            asphalt[i].anchoredPosition = new Vector2((float)start, 0f);
            asphalt[i].sizeDelta = new Vector2(w, 0f);
            asphaltCurb[i].anchoredPosition = new Vector2((float)start, 0f);
            asphaltCurb[i].sizeDelta = new Vector2(w, 5f);
        }

        l = town.CityL - AsphaltMargin;
        r = town.CityR + AsphaltMargin;
        k = System.Math.Floor((worldX - l) / lap);
        for (int i = 0; i < 2; i++)
        {
            float start = (float)(l + (k + i) * lap - worldX);
            float w = (float)(r - l);
            lawn[i].anchoredPosition = new Vector2(start, 0f);
            lawn[i].sizeDelta = new Vector2(w, 0f);
            pavement[i].anchoredPosition = new Vector2(start, 0f);
            pavement[i].sizeDelta = new Vector2(w, PavementHeight);
            pavementEdge[i].anchoredPosition = new Vector2(start, -PavementHeight);
            pavementEdge[i].sizeDelta = new Vector2(w, 4f);
        }
    }

    void UpdateGround()
    {
        float width = groundBand.rect.width;
        float height = groundBand.rect.height;
        if (width <= 1f || height <= 1f) return;

        // The band's size depends on the screen, so the first marks are laid out on first use.
        if (!groundStarted)
        {
            groundStarted = true;
            nextMarkX = worldX - 60.0;
        }

        // Destroy marks the dog has passed.
        for (int i = groundMarks.Count - 1; i >= 0; i--)
        {
            if (groundMarks[i].x >= worldX - 60.0) continue;
            UnityEngine.Object.Destroy(groundMarks[i].rt.gameObject);
            groundMarks.RemoveAt(i);
        }

        // Build the ground ahead of him.
        while (nextMarkX < worldX + width + 60.0)
        {
            var img = NewImage(groundBand, "Mark", ProceduralArt.RoundedRect(), OnAsphalt(nextMarkX) ? AsphaltMark : OnLawn(nextMarkX) ? LawnMark : new Color(0.72f, 0.4f, 0.16f, 0.45f), true);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // x from the left edge, y down from the top
            rt.sizeDelta = new Vector2(Random.Range(18f, 48f), Random.Range(4f, 6f));
            groundMarks.Add(new GroundMark { rt = rt, x = nextMarkX });
            // Lawn tufts stay below the pavement strip.
            float minY = OnLawn(nextMarkX) ? PavementHeight + 14f : 22f;
            rt.anchoredPosition = new Vector2(0f, -Random.Range(minY, Mathf.Max(minY + 8f, height - 12f)));
            nextMarkX += Random.Range(14f, 38f);
        }

        // Yellow dashed centre line on the asphalt, like a road.
        if (!laneStarted) { laneStarted = true; nextLaneX = System.Math.Floor((worldX - 60.0) / LaneStep) * LaneStep; }
        for (int i = laneMarks.Count - 1; i >= 0; i--)
        {
            if (laneMarks[i].x >= worldX - 120.0) continue;
            UnityEngine.Object.Destroy(laneMarks[i].rt.gameObject);
            laneMarks.RemoveAt(i);
        }
        for (; nextLaneX < worldX + width + 120.0; nextLaneX += LaneStep)
        {
            if (!OnAsphalt(nextLaneX)) continue;
            var img = NewImage(groundBand, "Lane", ProceduralArt.RoundedRect(), LaneColor, true);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(LaneDash, 8f);
            rt.anchoredPosition = new Vector2(0f, -height * 0.5f);
            laneMarks.Add(new GroundMark { rt = rt, x = nextLaneX });
        }
        foreach (var mark in laneMarks)
            mark.rt.anchoredPosition = new Vector2((float)(mark.x - worldX), mark.rt.anchoredPosition.y);

        UpdateAsphalt();
        foreach (var mark in groundMarks)
            mark.rt.anchoredPosition = new Vector2((float)(mark.x - worldX), mark.rt.anchoredPosition.y);
    }

    void UpdateToast(float dt)
    {
        if (toastTimer <= 0f) return;
        toastTimer -= dt;
        if (toastTimer <= 0f)
        {
            toastText.text = "";
            return;
        }
        toastText.alpha = Mathf.Clamp01(toastTimer);
    }

    void SpawnFloat(Vector2 screenPos, string text, Color color, float size = 1f, float life = 1f)
    {
        foreach (var f in floats)
        {
            if (f.active) continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer, screenPos, null, out var local);
            f.active = true;
            f.maxLife = f.life = life;
            f.text.fontSize = 52f * size;
            f.rt.sizeDelta = new Vector2(400f, 80f) * size;
            f.text.text = text;
            f.text.color = color;
            f.rt.anchoredPosition = local + new Vector2(Random.Range(-30f, 30f), 20f);
            f.rt.gameObject.SetActive(true);
            return;
        }
    }

    void UpdateFloats(float dt)
    {
        foreach (var f in floats)
        {
            if (!f.active) continue;
            f.life -= dt;
            if (f.life <= 0f)
            {
                f.active = false;
                f.rt.gameObject.SetActive(false);
                continue;
            }
            f.rt.anchoredPosition += new Vector2(0f, 140f * dt);
            var c = f.text.color;
            c.a = Mathf.Clamp01(f.life / f.maxLife * 1.6f);
            f.text.color = c;
        }
    }

    void UpdateFalling(float dt)
    {
        double cps = s.Cps;
        float h = canvasRect.rect.height;
        float w = canvasRect.rect.width;

        if (cps > 0)
        {
            float rate = Mathf.Min(10f, 0.6f + Mathf.Log(1f + (float)cps, 2f) * 0.7f);
            fallingAcc += rate * dt;
        }

        foreach (var b in falling)
        {
            if (!b.active)
            {
                if (fallingAcc < 1f) continue;
                fallingAcc -= 1f;
                b.active = true;
                b.speed = Random.Range(220f, 380f);
                b.spin = Random.Range(-60f, 60f);
                float size = Random.Range(40f, 80f);
                b.rt.sizeDelta = new Vector2(size, size);
                b.rt.anchorMin = b.rt.anchorMax = new Vector2(0f, 1f);
                var area = LeftPanelInCanvas();
                b.rt.anchoredPosition = new Vector2(Random.Range(area.xMin, area.xMax) + w * 0.5f, 60f);
                b.rt.gameObject.SetActive(true);
                continue;
            }

            b.rt.anchoredPosition += new Vector2(0f, -b.speed * dt);
            b.rt.Rotate(0f, 0f, b.spin * dt);
            if (b.rt.anchoredPosition.y < -h - 60f)
            {
                b.active = false;
                b.rt.gameObject.SetActive(false);
            }
        }

        if (fallingAcc > 3f) fallingAcc = 3f;
    }

    void UpdateGolden(float dt)
    {
        if (!goldenActive)
        {
            goldenTimer -= dt;
            if (goldenTimer <= 0f) SpawnGolden();
            return;
        }

        goldenLife -= dt;
        if (goldenLife <= 0f)
        {
            HideGolden();
            return;
        }

        float pulse = 1f + 0.1f * Mathf.Sin(clock * 6f);
        goldenRect.localScale = new Vector3(pulse, pulse, 1f);
        goldenRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(clock * 3f) * 12f);
        goldenImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(goldenLife / 1.5f));
    }

    void SpawnGolden()
    {
        var area = LeftPanelInCanvas();
        float x = area.width > 280f ? Random.Range(area.xMin + 80f, area.xMax - 80f) : area.center.x;
        float y = area.height > 320f ? Random.Range(area.yMin + 90f, area.yMax - 90f) : area.center.y;
        goldenRect.anchorMin = goldenRect.anchorMax = new Vector2(0.5f, 0.5f);
        goldenRect.anchoredPosition = new Vector2(x, y);
        goldenImage.color = Color.white;
        goldenImage.gameObject.SetActive(true);
        goldenActive = true;
        goldenLife = 13f;
    }

    void HideGolden()
    {
        goldenActive = false;
        goldenImage.gameObject.SetActive(false);
        goldenTimer = Random.Range(60f, 150f) * (float)s.GoldenInterval;
    }
}
