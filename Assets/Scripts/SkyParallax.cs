using UnityEngine;
using UnityEngine.UI;

// Sky behind the game area: a day that runs through sets of layered clouds (Resources/Clouds/<set>/1..n.png, back to
// front). The first layer is the fixed sky; the others tile sideways and scroll with parallax, faster the nearer they
// are, following the dog's stride plus a slow drift of their own. The sets take turns in a loop: each holds for a few
// minutes, then crossfades into the next. The loop runs on the wall clock, so the sky also moves on between sessions.
public class SkyParallax
{
    // The order of the day: set number and how many layers it has.
    static readonly (int set, int layers)[] Day =
    {
        (1, 4), // day
        (5, 5), // bright day
        (6, 6), // hazy afternoon
        (7, 4), // sunset
        (2, 3), // lavender dusk
        (8, 6), // purple dusk
        (3, 4), // night
        (4, 4), // dawn
    };

    const float HoldSeconds = 300f;   // how long a sky stays as it is
    const float FadeSeconds = 60f;    // how long the crossfade into the next one takes
    const int MaxLayers = 6;
    const float ImageAspect = 576f / 324f;
    const float FollowFar = 0.02f, FollowNear = 0.14f;  // share of the stride speed, back layer to front layer
    const float DriftFar = 4f, DriftNear = 14f;         // canvas px/s on top of that

    // One drawn sky. Two are kept so the next one can fade in over the current one.
    class Slot
    {
        public RectTransform root;
        public CanvasGroup group;
        public RawImage[] imgs = new RawImage[MaxLayers];
        public float[] offset = new float[MaxLayers];
        public float[] period = new float[MaxLayers]; // 2 for mirrored textures, which repeat every two widths
        public float[] follow = new float[MaxLayers];
        public float[] drift = new float[MaxLayers];
        public int count;
        public int dayIndex = -1;
    }

    readonly RectTransform root;
    Slot a, b;

    // Seconds into the loop. Starts from the wall clock; the Editor can set it to look at a given moment.
    public double Clock;

    public RectTransform Root => root;

    public SkyParallax(Transform parent)
    {
        root = NewRect("Sky", parent);
        Stretch(root);
        a = NewSlot("SkyA");
        b = NewSlot("SkyB");
        Clock = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

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

    Slot NewSlot(string name)
    {
        var slot = new Slot { root = NewRect(name, root) };
        Stretch(slot.root);
        slot.group = slot.root.gameObject.AddComponent<CanvasGroup>();
        slot.group.blocksRaycasts = false;
        for (int i = 0; i < MaxLayers; i++)
        {
            var rt = NewRect("Layer" + (i + 1), slot.root);
            Stretch(rt);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.raycastTarget = false;
            slot.imgs[i] = img;
        }
        slot.root.gameObject.SetActive(false);
        return slot;
    }

    void Show(Slot slot, int dayIndex)
    {
        Clear(slot);
        var def = Day[dayIndex];
        slot.dayIndex = dayIndex;
        slot.count = Mathf.Min(def.layers, MaxLayers);
        for (int i = 0; i < slot.count; i++)
        {
            var tex = Resources.Load<Texture2D>("Clouds/" + def.set + "/" + (i + 1));
            if (tex == null) Debug.LogError("Cloud layer not found at Resources/Clouds/" + def.set + "/" + (i + 1));
            var img = slot.imgs[i];
            img.texture = tex;
            img.uvRect = new Rect(0f, 0f, 1f, 1f);
            img.gameObject.SetActive(true);

            // Layer 1 is the fixed sky; the rest scroll slower or faster by depth.
            float depth = (i - 1f) / Mathf.Max(1f, slot.count - 2f);
            slot.follow[i] = i == 0 ? 0f : Mathf.Lerp(FollowFar, FollowNear, depth);
            slot.drift[i] = i == 0 ? 0f : Mathf.Lerp(DriftFar, DriftNear, depth);
            slot.period[i] = tex != null && tex.wrapModeU == TextureWrapMode.Mirror ? 2f : 1f;
            slot.offset[i] = 0f;
        }
        slot.root.gameObject.SetActive(true);
    }

    // Empties a slot and frees its textures; only the sets on screen stay in memory.
    void Clear(Slot slot)
    {
        for (int i = 0; i < MaxLayers; i++)
        {
            var img = slot.imgs[i];
            var tex = img.texture;
            img.texture = null;
            img.gameObject.SetActive(false);
            if (tex != null) Resources.UnloadAsset(tex);
        }
        slot.root.gameObject.SetActive(false);
        slot.dayIndex = -1;
        slot.count = 0;
    }

    public void Update(float dt, float strideSpeed)
    {
        Clock += dt;
        PickSkies();

        var rect = root.rect;
        if (rect.width <= 1f || rect.height <= 1f) return;

        // Each image is drawn at the region's height; its width then spans this share of the texture.
        float uvWidth = Mathf.Min(1f, rect.width / rect.height / ImageAspect);
        float uvPerPx = uvWidth / rect.width;

        Scroll(a, dt, strideSpeed, uvWidth, uvPerPx);
        if (b.dayIndex >= 0) Scroll(b, dt, strideSpeed, uvWidth, uvPerPx);
    }

    // Decides which set is current and, while one is fading in, which one comes next and how far along it is.
    void PickSkies()
    {
        double span = HoldSeconds + FadeSeconds;
        double p = Clock % (span * Day.Length);
        int cur = (int)(p / span);
        float within = (float)(p - cur * span);
        float t = within > HoldSeconds ? Mathf.SmoothStep(0f, 1f, (within - HoldSeconds) / FadeSeconds) : 0f;

        if (a.dayIndex != cur)
        {
            if (b.dayIndex == cur) (a, b) = (b, a); // the fade just finished: the new sky becomes the current one
            else Show(a, cur);
            a.root.SetAsFirstSibling();
        }
        a.group.alpha = 1f;

        if (t > 0f)
        {
            int next = (cur + 1) % Day.Length;
            if (b.dayIndex != next)
            {
                Show(b, next);
                b.root.SetAsLastSibling();
            }
            b.group.alpha = t;
        }
        else if (b.dayIndex >= 0)
        {
            Clear(b);
        }
    }

    static void Scroll(Slot slot, float dt, float strideSpeed, float uvWidth, float uvPerPx)
    {
        for (int i = 1; i < slot.count; i++)
        {
            slot.offset[i] = Mathf.Repeat(slot.offset[i] + (strideSpeed * slot.follow[i] + slot.drift[i]) * dt * uvPerPx, slot.period[i]);
            slot.imgs[i].uvRect = new Rect(slot.offset[i], 0f, uvWidth, 1f);
        }
    }
}
