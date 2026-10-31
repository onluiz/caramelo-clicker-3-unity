using UnityEngine;
using UnityEngine.UI;

// One building (or a piece of one) standing in the town. Each lives in its own prefab under Resources/Buildings,
// so its size, position and animation are edited in the Inspector. Behaviours that only some buildings have
// (patrolling, bobbing) are subclasses: swap the component on the prefab to change how a building moves.
// TownProps instantiates the prefabs ahead of the dog and calls Place every frame.
public class BuildingProp : MonoBehaviour
{
    public const float SizeScale = 3.7f; // every height/offset below is multiplied by this, so buildings tower over the dog
    const float PopSeconds = 0.45f;

    [Header("Which building")]
    [Tooltip("GameDefs building key that unlocks this prop.")]
    public string building;
    [Tooltip("Building whose slot to share (empty = its own), e.g. goods standing on another building's stall.")]
    public string sharedSlot;
    [Tooltip("Props are drawn in this order inside a slot (higher = in front). It also fixes the order of the slots along the road.")]
    public int drawOrder;

    [Header("Art (Resources/Props/<file>.png)")]
    public string file;
    [Tooltip("Sheet layout, row-major from the top. Frame width 0 = a single image.")]
    public int cols = 1;
    public int frames = 1;
    public int frameW, frameH;
    public float fps = 10f;

    [Header("Size and position (canvas px, before SizeScale)")]
    public float height = 100f;
    [Tooltip("Hovering height above the ground.")]
    public float air;
    [Tooltip("Height above the ground for things resting on a counter.")]
    public float lift;
    [Tooltip("Sideways shift inside the building's slot.")]
    public float offset;

    RectTransform rt;
    Image img;
    Sprite[] sprites;
    float pop = 1f;
    float anim;
    int shown = -1;

#if UNITY_EDITOR
    // Editor preview: while a prefab is open (or an instance sits in a scene), a hidden RawImage child shows the first
    // frame at the real size, shifted by offset/air/lift, so the fields can be tuned by eye. It is never saved nor built.
    const string PreviewName = "EditorPreview";

    void OnValidate()
    {
        if (Application.isPlaying || UnityEditor.EditorUtility.IsPersistent(this)) return;
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) RefreshPreview(); };
    }

    public void RefreshPreview()
    {
        if (Application.isPlaying || string.IsNullOrEmpty(file)) return;
        var old = transform.Find(PreviewName);
        if (old != null) DestroyImmediate(old.gameObject);

        var tex = Resources.Load<Texture2D>("Props/" + file);
        if (tex == null) return;

        float fw = frameW > 0 ? frameW : tex.width, fh = frameH > 0 ? frameH : tex.height;
        float h = height * SizeScale;
        var size = new Vector2(h * fw / fh, h);

        var rt0 = (RectTransform)transform;
        rt0.anchorMin = rt0.anchorMax = Vector2.zero;
        rt0.pivot = new Vector2(0.5f, 0f);
        rt0.sizeDelta = size;

        var go = new GameObject(PreviewName, typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
        go.hideFlags = HideFlags.HideAndDontSave;
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(offset, air + lift) * SizeScale;
        var raw = go.GetComponent<UnityEngine.UI.RawImage>();
        raw.texture = tex;
        raw.raycastTarget = false;
        raw.uvRect = new Rect(0f, 1f - fh / tex.height, fw / tex.width, fh / tex.height); // frame 0 is the top-left cell
    }
#endif

    // Called once after instantiation. sprites == null is a prop drawn from child objects (the sale sign).
    public void Setup(Sprite[] cut, bool popIn)
    {
        rt = (RectTransform)transform;
        img = GetComponent<Image>();
        sprites = cut;
        pop = popIn ? 0f : 1f;
        anim = Random.value * (cut != null ? cut.Length : 1);

        if (cut != null)
        {
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            img.raycastTarget = false;
            img.sprite = cut[0];
            float aspect = cut[0].rect.width / cut[0].rect.height;
            float h = height * SizeScale;
            rt.sizeDelta = new Vector2(h * aspect, h);
        }

        OnSetup();
    }

    protected virtual void OnSetup() { }

    // Extra movement on top of the slot position; flip = -1 mirrors the sprite.
    protected virtual void Move(float dt, ref Vector2 shift, ref float flip) { }

    public void Place(float slotScreenX, float dt)
    {
        if (pop < 1f) pop = Mathf.Min(1f, pop + dt / PopSeconds);

        float t = pop - 1f;
        float grow = pop >= 1f ? 1f : 1f + 2.70158f * t * t * t + 1.70158f * t * t; // ease-out-back

        var shift = Vector2.zero;
        float flip = 1f;
        Move(dt, ref shift, ref flip);

        rt.anchoredPosition = new Vector2(slotScreenX + offset * SizeScale + shift.x, (air + lift) * SizeScale + shift.y);
        rt.localScale = new Vector3(grow * flip, grow, 1f);

        if (sprites != null && sprites.Length > 1)
        {
            anim += dt * fps;
            int f = (int)anim % sprites.Length;
            if (f != shown)
            {
                shown = f;
                img.sprite = sprites[f];
            }
        }
    }
}
