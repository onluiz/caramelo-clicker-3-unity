using UnityEngine;
using UnityEngine.UI;

// Plays Caramelo's pixel-art animations. He sits while there is no income, then walks, runs and sprints
// as petiscos/second grow. Fast clicking pushes him up a gear and speeds up the animation.
//
// Frames come from Resources/Caramelo/caramelo-sheet.png, a compact sheet cut from the web game's
// caramelo.png (120x120 cells, 10 per row): sit_1 (0-16), walk_2_loop (17-27), walk_1 (28-44),
// sprint_1_loop (45-56), sprint_2_loop (57-68).
public class DogRig
{
    const int CellSize = 120;
    const int Columns = 10;
    const int FrameCount = 69;

    // Pixel-art scale: 3 screen pixels per sprite pixel.
    const float Scale = 3f;
    // The feet sit on row 88 (from the top) of each 120px cell, i.e. 28 rows below the cell centre.
    const float FeetBelowCentre = 28f * Scale;
    public const float GroundY = -280f; // paws offset from the anchor (the ground strip top is at -215)

    struct Clip
    {
        public int start, count;
        public bool loop;
        public Clip(int start, int count, bool loop) { this.start = start; this.count = count; this.loop = loop; }
    }

    static readonly Clip Sit = new Clip(0, 17, false);
    static readonly Clip Seated = new Clip(6, 11, true); // last 11 frames of sit_1
    static readonly Clip[] GaitClips =
    {
        new Clip(17, 11, true), // walk_2_loop
        new Clip(45, 12, true), // sprint_1_loop
        new Clip(57, 12, true), // sprint_2_loop
    };
    static readonly Clip WalkHappy = new Clip(28, 17, true); // walk_1, used for the upper half of the walk gait

    // Animation speed (frames/second) inside each gait, and where each gait sits on the effort scale.
    static readonly float[] FpsMin = { 8f, 12f, 15f };
    static readonly float[] FpsMax = { 13f, 18f, 24f };
    static readonly float[] EffortMin = { 0.03f, 1.2f, 2.6f };
    static readonly float[] EffortMax = { 1.2f, 2.6f, 5f };

    const float SeatedFps = 8f;
    const float SitFps = 14f;
    const float ClickDecay = 1.2f;
    const float GaitHysteresis = 0.35f;
    const float StridePixelsPerFrame = 36f;

    readonly RectTransform rootRt, spriteRt;
    readonly Image spriteImage;
    readonly Sprite[] frames = new Sprite[FrameCount];
    readonly RectTransform[] speedLines = new RectTransform[6];
    readonly Image[] speedLineImages = new Image[6];
    readonly float[] speedLineY = new float[6];

    Clip clip = Seated;
    bool standing;
    float frameTime;
    float fps = SeatedFps;
    float clickEnergy, punch;
    float movingBlend;
    float effortSmooth;
    int gait;
    bool walkHappy;
    int shownFrame = -1;


    // Horizontal speed (px/s) the ground should scroll at so the feet look planted.
    public float StrideSpeed { get; private set; }

    public DogRig(Transform parent, Vector2 anchor)
    {
        rootRt = NewRect("Dog", parent);
        rootRt.anchorMin = rootRt.anchorMax = anchor;
        rootRt.sizeDelta = new Vector2(700f, 520f);
        rootRt.anchoredPosition = Vector2.zero;

        var shadowRt = NewRect("Shadow", rootRt);
        shadowRt.anchoredPosition = new Vector2(0f, GroundY - 4f);
        shadowRt.sizeDelta = new Vector2(200f, 22f);
        var shadowImg = shadowRt.gameObject.AddComponent<Image>();
        shadowImg.sprite = ProceduralArt.Blob();
        shadowImg.color = new Color(0.3f, 0.12f, 0f, 0.2f);
        shadowImg.raycastTarget = false;

        for (int i = 0; i < speedLines.Length; i++)
        {
            var rt = NewRect("SpeedLine", rootRt);
            rt.sizeDelta = new Vector2(Random.Range(120f, 220f), 6f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ProceduralArt.RoundedRect();
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;
            speedLines[i] = rt;
            speedLineImages[i] = img;
            speedLineY[i] = Random.Range(-120f, 160f);
            rt.anchoredPosition = new Vector2(Random.Range(-500f, -150f), speedLineY[i]);
        }

        LoadFrames();

        spriteRt = NewRect("Caramelo", rootRt);
        spriteRt.sizeDelta = new Vector2(CellSize * Scale, CellSize * Scale);
        spriteImage = spriteRt.gameObject.AddComponent<Image>();
        spriteImage.raycastTarget = false;
        spriteImage.preserveAspect = true;
        ShowFrame(Seated.start + Seated.count - 1);
        spriteRt.anchoredPosition = new Vector2(0f, GroundY + FeetBelowCentre);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    void LoadFrames()
    {
        var tex = Resources.Load<Texture2D>("Caramelo/caramelo-sheet");
        if (tex == null)
        {
            Debug.LogError("Caramelo sprite sheet not found at Resources/Caramelo/caramelo-sheet");
            return;
        }

        tex.filterMode = FilterMode.Point;
        for (int i = 0; i < FrameCount; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            // Texture space starts at the bottom-left, the sheet is laid out from the top-left.
            var rect = new Rect(col * CellSize, tex.height - (row + 1) * CellSize, CellSize, CellSize);
            frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }

    void ShowFrame(int index)
    {
        if (index == shownFrame) return;
        shownFrame = index;
        spriteImage.sprite = frames[index];
    }

    public void Poke()
    {
        clickEnergy += 1f;
        punch = 1f;
    }

    public void Tick(float dt, double cps)
    {
        clickEnergy *= Mathf.Exp(-dt * ClickDecay);
        float clickRate = clickEnergy * ClickDecay;
        float boost = 0.3f * Mathf.Max(0f, clickRate - 1f);

        // Stand up for any income or sustained clicking; sit back down once both are gone.
        if (!standing && (cps > 0.0 || boost > 0.25f)) standing = true;
        else if (standing && cps <= 0.0 && boost < 0.1f) standing = false;

        // Smooth the effort so single clicks speed the stride up gradually instead of flipping clips.
        float baseEffort = Mathf.Log10(1f + (float)cps);
        float rawEffort = baseEffort + boost;
        float follow = rawEffort > effortSmooth ? 2.5f : 1.2f;
        effortSmooth = Mathf.Lerp(effortSmooth, rawEffort, 1f - Mathf.Exp(-dt * follow));
        float effort = effortSmooth;

        // Hysteresis: move up at the threshold, only come back down after dropping below it by a margin.
        while (gait < 2 && effort >= EffortMin[gait + 1]) gait++;
        while (gait > 0 && effort < EffortMin[gait] - GaitHysteresis) gait--;
        if (baseEffort > 0.7f) walkHappy = true;
        else if (baseEffort < 0.5f) walkHappy = false;

        float t = Mathf.InverseLerp(EffortMin[gait], EffortMax[gait], effort);
        float clickSpeed = 1f + 0.06f * Mathf.Min(clickRate, 12f);

        Clip desired;
        float targetFps;
        if (standing)
        {
            // The walk variant depends on income only, so clicking never swaps the walk cycle.
            desired = gait == 0 && walkHappy ? WalkHappy : GaitClips[gait];
            targetFps = Mathf.Lerp(FpsMin[gait], FpsMax[gait], t) * clickSpeed;
        }
        else if (IsMoving(clip))
        {
            desired = Sit; // lie down first, then settle into the looping seated pose
            targetFps = SitFps;
        }
        else
        {
            desired = clip; // keep sitting
            targetFps = clip.loop ? SeatedFps : SitFps;
        }

        if (desired.start != clip.start)
        {
            // Keep the same point in the cycle when changing between moving clips, so gait changes don't pop.
            float progress = clip.count > 0 ? frameTime / clip.count : 0f;
            frameTime = IsMoving(clip) && IsMoving(desired) ? progress * desired.count : 0f;
            clip = desired;
        }

        fps = Mathf.Lerp(fps, targetFps, 1f - Mathf.Exp(-dt * 6f));
        frameTime += fps * dt;

        if (clip.loop)
        {
            frameTime %= clip.count;
        }
        else if (frameTime >= clip.count)
        {
            clip = Seated;
            frameTime = 0f;
        }

        ShowFrame(clip.start + Mathf.Min((int)frameTime, clip.count - 1));

        movingBlend = Mathf.MoveTowards(movingBlend, IsMoving(clip) ? 1f : 0f, dt * 3f);
        StrideSpeed = IsMoving(clip) ? fps * StridePixelsPerFrame : 0f;

        // Click feedback: a small pop and hop.
        punch = Mathf.Max(0f, punch - dt * 5f);
        rootRt.localScale = Vector3.one * (1f + 0.05f * punch);
        spriteRt.anchoredPosition = new Vector2(0f, GroundY + FeetBelowCentre + 18f * punch);

        UpdateSpeedLines(dt, gait == 2 && standing ? Mathf.InverseLerp(0f, 0.5f, t + 0.3f) * movingBlend : 0f);
    }

    static bool IsMoving(Clip c)
    {
        return c.start != Sit.start && c.start != Seated.start;
    }

    void UpdateSpeedLines(float dt, float amount)
    {
        for (int i = 0; i < speedLines.Length; i++)
        {
            var rt = speedLines[i];
            var p = rt.anchoredPosition;
            p.x -= (900f + 1000f * amount) * dt;
            if (p.x < -520f)
            {
                p.x = Random.Range(-200f, -120f);
                speedLineY[i] = Random.Range(-120f, 160f);
            }
            p.y = speedLineY[i];
            rt.anchoredPosition = p;
            speedLineImages[i].color = new Color(1f, 1f, 1f, 0.55f * Mathf.Clamp01(amount));
        }
    }
}
