using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The fight side of the game: Caramelo's attributes, XP and levels, the demon he attacks, the name/HP bar
// over his head and the attributes screen. The dog's progress is saved apart from the idle-game save.
public partial class GameUI
{
    const string HeroSaveKey = "caramelo_hero_v1";

    static readonly CombatStats EnemyTemplate = new CombatStats(1, 6, 6, 2, 4, 3, 6); // level, str, vit, int, dex, luck, agi: stronger than the dog
    static readonly Stat[] StatOrder = { Stat.Strength, Stat.Vitality, Stat.Intelligence, Stat.Dexterity, Stat.Luck, Stat.Agility };
    static readonly string[] StatNames = { "Força", "Vitalidade", "Inteligência", "Destreza", "Sorte", "Agilidade" };

    CombatStats dogStats;
    EnemyWave wave;
    int shownWave = -1;
    RectTransform xpBarFillRt;
    TextMeshProUGUI xpBarText;
    HealthBar dogBar;

    Button heroButton;
    Image heroButtonBg;
    TextMeshProUGUI heroButtonText;

    RectTransform heroRoot, xpFillRt;
    TextMeshProUGUI heroTitle, xpText, pointsText, derivedText, heroCloseText;
    readonly TextMeshProUGUI[] statLabel = new TextMeshProUGUI[6];
    readonly TextMeshProUGUI[] statValue = new TextMeshProUGUI[6];
    readonly Button[] statPlus = new Button[6];

    static CombatStats NewDogStats()
    {
        return new CombatStats(1, 5, 5, 3, 5, 5, 5);
    }

    // ---------------------------------------------------------------- save

    void LoadHero()
    {
        dogStats = NewDogStats();
        string json = PlayerPrefs.GetString(HeroSaveKey, "");
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            JsonUtility.FromJsonOverwrite(json, dogStats);
            dogStats.Level = Mathf.Max(1, dogStats.Level);
            dogStats.ClampHp();
        }
        catch (System.Exception)
        {
            dogStats = NewDogStats(); // a broken save starts over instead of blocking the game
        }
    }

    // Writes to PlayerPrefs; the caller flushes them (GameState.Save does).
    public void SaveHero()
    {
        PlayerPrefs.SetString(HeroSaveKey, JsonUtility.ToJson(dogStats));
    }

    void ResetHero()
    {
        dogStats = NewDogStats();
        wave.TargetLevel = 1;
        wave.Reset();
        RefreshHeroLabels();
        SaveHero();
        PlayerPrefs.Save();
    }

    // ---------------------------------------------------------------- in the game area

    void BuildHero(Transform left)
    {
        wave = new EnemyWave(left, new Vector2(0.5f, 0.4f), EnemyTemplate, dogStats.Level);
        wave.Tapped = AttackEnemy;
        wave.AttackLanded = EnemyAttacks;

        BuildXpBar(left);

        // Name and HP over the dog's head, like the demon's.
        dogBar = new HealthBar(rig.Root, new Vector2(0.5f, 0.5f), new Vector2(0f, DogRig.GroundY + 175f), ProceduralArt.Hex("#5BC46A"));

        heroButton = NewButton(left, "HeroButton", "Atributos", 34, ProceduralArt.Hex("#8A5A2B"), Color.white, out heroButtonBg, out heroButtonText);
        // Under the settings gear, clear of the stats pill at the bottom.
        Place((RectTransform)heroButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -130f), new Vector2(250f, 72f));
        heroButton.onClick.AddListener(() => SetHeroOpen(true));

        RefreshHeroLabels();
    }

    // Experience bar along the bottom of the game area, above the stats pill.
    void BuildXpBar(Transform left)
    {
        var back = NewImage(left, "XpBar", ProceduralArt.RoundedRect(), new Color(0.12f, 0.05f, 0.02f, 0.85f), true);
        Place(back.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 98f), new Vector2(720f, 36f));
        var fill = NewImage(back.transform, "Fill", ProceduralArt.RoundedRect(), ProceduralArt.Hex("#4FB3E8"), true);
        xpBarFillRt = fill.rectTransform;
        Box(xpBarFillRt, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        xpBarText = NewText(back.transform, "Text", "", 26, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(xpBarText.rectTransform);
    }

    void UpdateHero(float dt)
    {
        wave.Tick(dt, rig.StrideSpeed * dt);
        if (wave.Number != shownWave) RefreshHeroLabels();

        // He catches his breath while running to the next demon.
        if (!wave.Engaged && dogStats.Hp < dogStats.MaxHp)
            dogStats.Hp = Mathf.Min(dogStats.MaxHp, dogStats.Hp + dogStats.MaxHp * 0.08f * dt);
        dogBar.SetFill(dogStats.Hp / dogStats.MaxHp);
    }

    // Tapping the demon is an attack from Caramelo.
    void AttackEnemy(Enemy enemy)
    {
        sfx.PlayClick();
        rig.Pop();
        bool wasAlive = enemy.Alive;
        var hit = dogStats.Attacks(enemy.Stats);
        enemy.TakeHit(hit);

        var pos = enemy.ScreenPosition;
        if (hit.Missed) SpawnFloat(pos, Loc.T("Errou"), new Color(0.85f, 0.85f, 0.85f));
        else if (hit.Critical) SpawnFloat(pos, Loc.T("Crítico!") + " -" + hit.Damage, new Color(1f, 0.85f, 0.2f));
        else SpawnFloat(pos, "-" + hit.Damage, Color.white);

        if (wasAlive && !enemy.Alive) OnEnemyKilled(enemy);
    }

    // The demon's blow lands (or misses) on Caramelo.
    void EnemyAttacks(Enemy enemy)
    {
        var hit = enemy.Stats.Attacks(dogStats);
        var pos = dogBar.ScreenPosition + new Vector2(0f, 60f);
        if (hit.Missed)
        {
            SpawnFloat(pos, Loc.T("Esquivou!"), new Color(0.85f, 0.85f, 0.85f));
            return;
        }

        dogStats.TakeDamage(hit.Damage);
        sfx.PlayError();
        rig.Pop();
        SpawnFloat(pos, (hit.Critical ? Loc.T("Crítico!") + " " : "") + "-" + hit.Damage, new Color(1f, 0.45f, 0.4f));
        if (dogStats.IsDead) OnDogDefeated();
    }

    // No penalty: he recovers, the wave goes back down the road and he runs on.
    void OnDogDefeated()
    {
        dogStats.Hp = dogStats.MaxHp;
        wave.Restart();
        ShowToast(Loc.T("Caramelo foi derrotado! Ele se recupera e segue em frente."), 4f);
    }

    void OnEnemyKilled(Enemy enemy)
    {
        int xp = enemy.XpReward;
        SpawnFloat(enemy.ScreenPosition + new Vector2(0f, -70f), "+" + xp + " XP", new Color(0.6f, 0.9f, 1f));

        int levels = dogStats.AddXp(xp);
        if (levels > 0)
        {
            wave.TargetLevel = dogStats.Level;
            SpawnFloat(dogBar.ScreenPosition + new Vector2(0f, 60f), Loc.T("Subiu de nível!"), new Color(1f, 0.85f, 0.2f));
            sfx.PlayBuy();
            rig.Poke();
        }

        RefreshHeroLabels();
        SaveHero();
    }

    // Name over the dog and the attributes button (it lights up while there are points to spend).
    void RefreshHeroLabels()
    {
        dogBar.SetLabel(Loc.T("Caramelo") + " " + Loc.T("Nv.") + dogStats.Level);

        bool points = dogStats.StatPoints > 0;
        heroButtonText.text = Loc.T("Atributos") + (points ? " +" + dogStats.StatPoints : "");
        heroButtonBg.color = points ? TabOn : ProceduralArt.Hex("#8A5A2B");
        heroButtonText.color = points ? Ink : Color.white;

        shownWave = wave.Number;
        xpBarFillRt.anchorMax = new Vector2(Mathf.Clamp01((float)dogStats.Xp / dogStats.XpToNext), 1f);
        xpBarText.text = Loc.T("Nv.") + dogStats.Level + "   XP " + dogStats.Xp + " / " + dogStats.XpToNext + "   -   " + Loc.T("Onda") + " " + wave.Number;

        if (heroRoot != null && heroRoot.gameObject.activeSelf) RefreshHeroPanel();
    }

    void ApplyHeroLanguage()
    {
        if (dogBar == null) return;
        wave.RefreshLabels();
        RefreshHeroLabels();
        RefreshHeroPanel();
    }

    // ---------------------------------------------------------------- attributes screen

    void BuildHeroPanel()
    {
        heroRoot = NewRect("Attributes", canvasRect);
        Stretch(heroRoot);

        var dim = NewImage(heroRoot, "Dim", null, new Color(0f, 0f, 0f, 0.6f), false, true);
        Stretch(dim.rectTransform);
        var dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(() => SetHeroOpen(false));

        var border = NewImage(heroRoot, "Panel", ProceduralArt.RoundedRect(), CardBorder, true, true);
        Place(border.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 740f));
        var panel = NewImage(border.transform, "Inner", ProceduralArt.RoundedRect(), CardBg, true);
        Box(panel.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));

        var top = new Vector2(0.5f, 1f);
        var topLeft = new Vector2(0f, 1f);

        heroTitle = NewText(panel.transform, "Title", "", 54, CardText, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(heroTitle.rectTransform, top, top, new Vector2(0f, -22f), new Vector2(900f, 76f));

        // XP bar with its numbers inside.
        var xpBack = NewImage(panel.transform, "XpBack", ProceduralArt.RoundedRect(), ProceduralArt.Hex("#1F1209"), true);
        Place(xpBack.rectTransform, top, top, new Vector2(0f, -104f), new Vector2(880f, 34f));
        var xpFill = NewImage(xpBack.transform, "XpFill", ProceduralArt.RoundedRect(), ProceduralArt.Hex("#4FB3E8"), true);
        xpFillRt = xpFill.rectTransform;
        Box(xpFillRt, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        xpText = NewText(xpBack.transform, "XpText", "", 24, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(xpText.rectTransform);

        pointsText = NewText(panel.transform, "Points", "", 34, TabOn, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(pointsText.rectTransform, top, top, new Vector2(0f, -146f), new Vector2(900f, 46f));

        for (int i = 0; i < StatOrder.Length; i++)
        {
            float y = -204f - i * 64f;
            statLabel[i] = NewText(panel.transform, "StatName", "", 34, CardText, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Place(statLabel[i].rectTransform, topLeft, topLeft, new Vector2(50f, y), new Vector2(270f, 56f));
            statValue[i] = NewText(panel.transform, "StatValue", "", 36, Color.white, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            Place(statValue[i].rectTransform, topLeft, topLeft, new Vector2(320f, y), new Vector2(80f, 56f));

            var plus = NewButton(panel.transform, "Plus", "+", 44, TabOn, Ink, out _, out _);
            Place((RectTransform)plus.transform, topLeft, topLeft, new Vector2(420f, y), new Vector2(56f, 56f));
            var stat = StatOrder[i];
            plus.onClick.AddListener(() =>
            {
                if (dogStats.Spend(stat)) sfx.PlayBuy();
                else sfx.PlayError();
                RefreshHeroLabels();
                RefreshHeroPanel();
                SaveHero();
            });
            statPlus[i] = plus;
        }

        derivedText = NewText(panel.transform, "Derived", "", 28, CardText, TextAlignmentOptions.TopLeft);
        derivedText.lineSpacing = 18f;
        Place(derivedText.rectTransform, topLeft, topLeft, new Vector2(520f, -204f), new Vector2(460f, 390f));

        var close = NewButton(panel.transform, "CloseButton", "Fechar", 38, TabOn, Ink, out _, out heroCloseText);
        Place((RectTransform)close.transform, top, top, new Vector2(0f, -620f), new Vector2(640f, 90f));
        close.onClick.AddListener(() => SetHeroOpen(false));

        heroRoot.gameObject.SetActive(false);
    }

    void SetHeroOpen(bool open)
    {
        heroRoot.gameObject.SetActive(open);
        if (open) RefreshHeroPanel();
    }

    void RefreshHeroPanel()
    {
        if (heroRoot == null) return;

        heroTitle.text = Loc.T("Caramelo") + "  " + Loc.T("Nv.") + dogStats.Level;
        xpFillRt.anchorMax = new Vector2(Mathf.Clamp01((float)dogStats.Xp / dogStats.XpToNext), 1f);
        xpText.text = "XP " + dogStats.Xp + " / " + dogStats.XpToNext;
        pointsText.text = Loc.T("Pontos de atributo") + ": " + dogStats.StatPoints;

        for (int i = 0; i < StatOrder.Length; i++)
        {
            statLabel[i].text = Loc.T(StatNames[i]);
            statValue[i].text = dogStats.Get(StatOrder[i]).ToString();
            statPlus[i].interactable = dogStats.StatPoints > 0;
        }

        var foe = wave.Preview;
        derivedText.text =
            "HP: " + Mathf.CeilToInt(dogStats.Hp) + " / " + Mathf.CeilToInt(dogStats.MaxHp) + "\n" +
            Loc.T("Ataque") + ": " + Dec(dogStats.Attack) + "\n" +
            Loc.T("Defesa") + ": " + Dec(dogStats.Defense) + "\n" +
            Loc.T("Chance de crítico") + ": " + Percent(dogStats.CritChance) + "\n" +
            Loc.T("Dano crítico") + ": x" + Dec(dogStats.CritMultiplier) + "\n" +
            Loc.T("Acerto no demônio") + ": " + Percent(dogStats.HitChance(foe)) + "\n" +
            Loc.T("Esquiva do demônio") + ": " + Percent(1f - foe.HitChance(dogStats));

        heroCloseText.text = Loc.T("Fechar");
    }

    // pt-BR decimal comma without relying on the platform's culture data.
    static string Dec(float v)
    {
        string t = v.ToString("0.##", CultureInfo.InvariantCulture);
        return Loc.IsEnglish ? t : t.Replace('.', ',');
    }

    static string Percent(float v)
    {
        return Mathf.RoundToInt(v * 100f) + "%";
    }
}
