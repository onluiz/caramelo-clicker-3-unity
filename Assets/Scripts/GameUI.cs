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
    const float LeftWidth = 0.58f;        // landscape: share of the width used by the game area
    const float GameScale = 0.85f;        // the whole game area (HUD, ground, dog) is drawn a bit smaller than the panel
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
    RectTransform safeRoot, leftPanel, rightPanel, tabBuildingsRt, tabUpgradesRt, tabCeuRt, tabDevRt, buyBarRt, scrollRt;
    CanvasScaler scaler;
    bool portrait;
    Vector2Int lastScreen;
    Rect lastSafe;
    DogRig rig;
    RectTransform groundBand;
    readonly List<RectTransform> groundMarks = new List<RectTransform>();
    float clock;

    TextMeshProUGUI counterText, cpsText, buffText, toastText, statsText;
    Image statsPill;
    TextMeshProUGUI tabBuildingsText, tabUpgradesText, tabCeuText, tabDevText, emptyText, muteText, resetText;
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

        bgLayer = NewRect("FallingBones", canvasRect);
        Stretch(bgLayer);
        for (int i = 0; i < falling.Length; i++)
        {
            var img = NewImage(bgLayer, "Bone", IconLoader.Load("petisco"), new Color(1f, 1f, 1f, 0.55f));
            img.gameObject.SetActive(false);
            falling[i] = new FallingBone { rt = img.rectTransform };
        }

        // Game and shop panels live inside the device's safe area (notches, rounded corners).
        safeRoot = NewRect("SafeArea", canvasRect);
        Stretch(safeRoot);
    }

    void BuildLeftPanel()
    {
        var left = NewRect("LeftPanel", safeRoot);
        leftPanel = left;

        // HUD (top row): petiscos pill, ossos pill and the settings button; petiscos per second below.
        var coinPill = NewImage(left, "CoinPill", ProceduralArt.RoundedRect(), CardBg, true);
        // Three pills of equal width: petiscos, fragments of the next celestial bone, celestial bones.
        Box(coinPill.rectTransform, new Vector2(0.05f, 1f), new Vector2(0.30f, 1f), new Vector2(0f, -120f), new Vector2(0f, -28f));
        counterText = PillContent(coinPill, IconLoader.Load("petisco"));

        var fragPill = NewImage(left, "FragmentsPill", ProceduralArt.RoundedRect(), CardBg, true);
        Box(fragPill.rectTransform, new Vector2(0.32f, 1f), new Vector2(0.57f, 1f), new Vector2(0f, -120f), new Vector2(0f, -28f));
        fragmentsText = PillContent(fragPill, IconLoader.Load("fragmento-celestial"), 54f);

        var gemPill = NewImage(left, "OssosPill", ProceduralArt.RoundedRect(), CardBg, true);
        Box(gemPill.rectTransform, new Vector2(0.59f, 1f), new Vector2(0.84f, 1f), new Vector2(0f, -120f), new Vector2(0f, -28f));
        ossosText = PillContent(gemPill, IconLoader.Load("osso-celestial"));

        var gear = NewButton(left, "SettingsButton", "", 28, CardBg, Color.white, out _, out _);
        Place((RectTransform)gear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(92f, 92f));
        var gearIcon = NewImage(gear.transform, "Gear", ProceduralArt.Gear(), Color.white);
        Box(gearIcon.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 14f), new Vector2(-14f, -14f));
        gear.onClick.AddListener(() => SetSettingsOpen(true));

        // Where a game would show the stage: the income, as "n/s".
        // The dark copy behind is its drop shadow (the font's outline shader variant is not available).
        cpsShadow = NewText(left, "CpsShadow", "0/s", 84, ProceduralArt.Hex("#5A2E12"), TextAlignmentOptions.Center, FontStyles.Bold);
        Box(cpsShadow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(4f, -256f), new Vector2(4f, -146f));
        cpsText = NewText(left, "Cps", "0/s", 84, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Box(cpsText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -250f), new Vector2(0f, -140f));
        foreach (var t in new[] { cpsText, cpsShadow })
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = 40f;
            t.fontSizeMax = 84f;
        }

        buffChip = NewImage(left, "BuffChip", ProceduralArt.RoundedRect(), Bad, true);
        Box(buffChip.rectTransform, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, -316f), new Vector2(0f, -260f));
        buffText = NewText(buffChip.transform, "Buff", "", 32, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Box(buffText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
        buffText.enableAutoSizing = true;
        buffText.fontSizeMin = 18f;
        buffText.fontSizeMax = 32f;
        buffChip.gameObject.SetActive(false);

        BuildGround(left);

        rig = new DogRig(left, new Vector2(0.5f, 0.4f));
        var trigger = rig.Root.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(data => OnDogDown((PointerEventData)data));
        trigger.triggers.Add(entry);

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
        Place(border.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 640f));
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

        var resetBtn = NewButton(panel.transform, "ResetButton", "", 38, ProceduralArt.Hex("#8A5A2B"), Color.white, out resetBg, out resetText);
        Place((RectTransform)resetBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -344f), new Vector2(640f, 96f));
        resetBtn.onClick.AddListener(OnResetClicked);

        var closeBtn = NewButton(panel.transform, "CloseButton", "Fechar", 38, TabOn, Ink, out _, out closeText);
        Place((RectTransform)closeBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -476f), new Vector2(640f, 96f));
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

    void SetSettingsOpen(bool open)
    {
        resetConfirmTimer = 0f;
        RefreshButtons();
        settingsRoot.gameObject.SetActive(open);
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
        ossosRow.go.GetComponent<LayoutElement>().preferredHeight = 120f;
        ossosRow.go.GetComponent<LayoutElement>().minHeight = 120f;

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
        goldenRect.sizeDelta = new Vector2(170f, 170f);
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
        counterText.text = NumberFormat.Format(petiscos);
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
            bool affordable = s.Petiscos >= cost;

            row.icon.color = Color.white;
            row.bg.color = affordable ? RowOn : RowOff;
            row.name.text = GameDefs.NameOf(def);
            row.desc.text = GameDefs.DescOf(def);
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

        var safe = Screen.safeArea;
        safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

        if (portrait)
        {
            AnchorsScaled(leftPanel, 0f, PortraitShopHeight, 1f, 1f, GameScale);
            Anchors(rightPanel, 0f, 0f, 1f, PortraitShopHeight);
            Box(tabBuildingsRt, new Vector2(0f, 0.895f), new Vector2(0.31f, 0.985f), new Vector2(14f, 0f), new Vector2(-4f, 0f));
            Box(tabUpgradesRt, new Vector2(0.31f, 0.895f), new Vector2(0.62f, 0.985f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
            Box(tabCeuRt, new Vector2(0.62f, 0.895f), new Vector2(devEnabled ? 0.82f : 1f, 0.985f), new Vector2(4f, 0f), new Vector2(devEnabled ? -4f : -14f, 0f));
            Box(tabDevRt, new Vector2(0.82f, 0.895f), new Vector2(1f, 0.985f), new Vector2(4f, 0f), new Vector2(-14f, 0f));
            Box(buyBarRt, new Vector2(0f, 0.81f), new Vector2(1f, 0.885f), new Vector2(14f, 0f), new Vector2(-14f, 0f));
        }
        else
        {
            AnchorsScaled(leftPanel, 0f, 0f, LeftWidth, 1f, GameScale);
            Anchors(rightPanel, LeftWidth, 0f, 1f, 1f);
            Box(tabBuildingsRt, new Vector2(0f, 0.915f), new Vector2(0.31f, 0.985f), new Vector2(14f, 0f), new Vector2(-4f, 0f));
            Box(tabUpgradesRt, new Vector2(0.31f, 0.915f), new Vector2(0.62f, 0.985f), new Vector2(4f, 0f), new Vector2(-4f, 0f));
            Box(tabCeuRt, new Vector2(0.62f, 0.915f), new Vector2(devEnabled ? 0.82f : 1f, 0.985f), new Vector2(4f, 0f), new Vector2(devEnabled ? -4f : -14f, 0f));
            Box(tabDevRt, new Vector2(0.82f, 0.915f), new Vector2(1f, 0.985f), new Vector2(4f, 0f), new Vector2(-14f, 0f));
            Box(buyBarRt, new Vector2(0f, 0.845f), new Vector2(1f, 0.905f), new Vector2(14f, 0f), new Vector2(-14f, 0f));
        }

        ApplyScrollBox();
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
        UpdateGround(dt);
    }

    // Ground under the dog's paws, filling the panel down to its bottom edge (behind the buttons);
    // its marks scroll left to match the stride.
    void BuildGround(Transform parent)
    {
        var band = NewImage(parent, "Ground", null, new Color(0.88f, 0.54f, 0.24f, 0.55f));
        groundBand = band.rectTransform;
        groundBand.anchorMin = Vector2.zero;
        groundBand.anchorMax = new Vector2(1f, 0.4f);
        groundBand.offsetMin = Vector2.zero;
        groundBand.offsetMax = new Vector2(0f, -215f);

        var edge = NewImage(groundBand, "Edge", null, new Color(0.72f, 0.4f, 0.16f, 0.6f));
        edge.rectTransform.anchorMin = new Vector2(0f, 1f);
        edge.rectTransform.anchorMax = new Vector2(1f, 1f);
        edge.rectTransform.pivot = new Vector2(0.5f, 1f);
        edge.rectTransform.sizeDelta = new Vector2(0f, 5f);

        for (int i = 0; i < 40; i++)
        {
            var mark = NewImage(groundBand, "Mark", ProceduralArt.RoundedRect(), new Color(0.72f, 0.4f, 0.16f, 0.45f), true);
            var rt = mark.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // x from the left edge, y down from the top
            rt.sizeDelta = new Vector2(Random.Range(40f, 110f), Random.Range(7f, 12f));
            groundMarks.Add(rt);
        }
    }

    bool groundScattered;

    // The band's size depends on the screen, so the marks are spread over it on first use.
    void ScatterGroundMarks(float width, float height)
    {
        foreach (var mark in groundMarks)
            mark.anchoredPosition = new Vector2(Random.Range(-60f, width), -Random.Range(22f, Mathf.Max(30f, height - 12f)));
        groundScattered = true;
    }

    void UpdateGround(float dt)
    {
        float width = groundBand.rect.width;
        float height = groundBand.rect.height;
        if (width <= 1f || height <= 1f) return;
        if (!groundScattered) ScatterGroundMarks(width, height);

        float move = rig.StrideSpeed * dt;
        foreach (var mark in groundMarks)
        {
            var p = mark.anchoredPosition;
            p.x -= move;
            if (p.x < -60f)
            {
                p.x += width + 120f;
                p.y = -Random.Range(22f, Mathf.Max(30f, height - 12f));
            }
            mark.anchoredPosition = p;
        }
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

    void SpawnFloat(Vector2 screenPos, string text, Color color)
    {
        foreach (var f in floats)
        {
            if (f.active) continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(floatLayer, screenPos, null, out var local);
            f.active = true;
            f.maxLife = f.life = 1f;
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
        float x = area.width > 280f ? Random.Range(area.xMin + 140f, area.xMax - 140f) : area.center.x;
        float y = area.height > 320f ? Random.Range(area.yMin + 160f, area.yMax - 160f) : area.center.y;
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
