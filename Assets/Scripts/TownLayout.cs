using System.Collections.Generic;
using UnityEngine;

// The town as one prefab (Resources/TownLayout): every building prefab sits inside it as a child, and slotSpacing is the
// distance between slots along the road. TownProps reads the children (which buildings exist, in draw order) and the
// spacing at runtime. Opening the prefab shows the whole town in a row, at real scale: change slotSpacing and the
// buildings re-space themselves (also on the component's context menu: "Arrange buildings").
[ExecuteAlways]
public class TownLayout : MonoBehaviour
{
    [Tooltip("World px between building slots (1 m = 10 px).")]
    public float slotSpacing = 540f;

    // The buildings in draw order, which is also the order of the slots along the road.
    public BuildingProp[] Buildings()
    {
        var list = new List<BuildingProp>(GetComponentsInChildren<BuildingProp>(true));
        list.Sort((a, b) => a.drawOrder.CompareTo(b.drawOrder));
        return list.ToArray();
    }

    // Slot index of each building: slots are numbered by first appearance, and goods share their stall's slot.
    public static int[] SlotIndices(BuildingProp[] props)
    {
        var slots = new Dictionary<string, int>();
        var result = new int[props.Length];
        for (int i = 0; i < props.Length; i++)
        {
            string key = string.IsNullOrEmpty(props[i].sharedSlot) ? props[i].building : props[i].sharedSlot;
            if (!slots.ContainsKey(key)) slots[key] = slots.Count;
            result[i] = slots[key];
        }

        return result;
    }

#if UNITY_EDITOR
    bool dirty; // a field changed and the buildings have not been re-arranged yet: do not read their positions as drags

    public void MarkDirty() { dirty = true; }

    void OnEnable()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.update += SyncDrags;
        dirty = true; // positions saved in the prefab may predate the fields: arrange first, then watch for drags
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Arrange(); };
    }

    void OnDisable()
    {
        UnityEditor.EditorApplication.update -= SyncDrags;
    }

    // Moving a building in the Scene view writes its new position back into its offset / yOffset fields.
    void SyncDrags()
    {
        if (this == null || dirty || Application.isPlaying || UnityEditor.EditorUtility.IsPersistent(this)) return;
        var props = Buildings();
        var slot = SlotIndices(props);
        for (int i = 0; i < props.Length; i++)
            props[i].EditorSyncFromPosition(slot[i] * slotSpacing);
    }

    void OnValidate()
    {
        if (Application.isPlaying || UnityEditor.EditorUtility.IsPersistent(this)) return;
        dirty = true;
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Arrange(); };
    }

    [ContextMenu("Arrange buildings")]
    public void Arrange()
    {
        if (Application.isPlaying) return;
        var props = Buildings();
        var slot = SlotIndices(props);

        var self = (RectTransform)transform;
        self.sizeDelta = Vector2.zero;
        self.pivot = Vector2.zero;

        int lastSlot = 0;
        for (int i = 0; i < props.Length; i++)
        {
            lastSlot = Mathf.Max(lastSlot, slot[i]);
            var rt = (RectTransform)props[i].transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            props[i].RefreshPreview();
            rt.anchoredPosition = props[i].EditorExpectedPosition(slot[i] * slotSpacing);
        }

        RefreshGround(lastSlot * slotSpacing);
        dirty = false;
    }

    // Fake ground and a life-size Caramelo so the buildings' heights can be judged. Hidden, never saved nor built.
    // In the game the buildings' base sits on the line the dog's paws touch, so this ground is at y = 0.
    const string GroundName = "EditorGround";

    void RefreshGround(float length)
    {
        var old = transform.Find(GroundName);
        if (old != null) DestroyImmediate(old.gameObject);

        var root = NewPiece(GroundName, transform, new Vector2(-700f, 0f), new Vector2(length + 1400f, 0f), new Vector2(0f, 1f));
        root.gameObject.hideFlags = HideFlags.HideAndDontSave;
        root.SetAsFirstSibling();

        var sand = NewPiece("Sand", root, Vector2.zero, new Vector2(length + 1400f, 160f), new Vector2(0f, 1f));
        sand.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.85f, 0.64f, 0.36f, 0.85f);
        var edge = NewPiece("Edge", root, Vector2.zero, new Vector2(length + 1400f, 4f), new Vector2(0f, 1f));
        edge.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.45f, 0.27f, 0.12f, 1f);

        var tex = Resources.Load<Texture2D>("Caramelo/caramelo-sheet");
        if (tex == null) return;
        // Frame 17 (first walk frame), 120px cells at 3x; his paws are 28 cells-px below the cell centre.
        var dog = NewPiece("Caramelo", root, new Vector2(500f, -96f), new Vector2(360f, 360f), new Vector2(0.5f, 0f));
        var raw = dog.gameObject.AddComponent<UnityEngine.UI.RawImage>();
        raw.texture = tex;
        raw.uvRect = new Rect(7f * 120f / tex.width, 1f - 2f * 120f / tex.height, 120f / tex.width, 120f / tex.height);
    }

    static RectTransform NewPiece(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.hideFlags = HideFlags.HideAndDontSave;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }
#endif
}
