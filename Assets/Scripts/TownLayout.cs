using System.Collections.Generic;
using UnityEngine;

// The town as one prefab (Resources/TownLayout): every building prefab sits inside it as a child, and slotSpacing is the
// distance between slots along the road. TownProps reads the children (which buildings exist, in draw order) and the
// spacing at runtime. Opening the prefab shows the whole town in a row, at real scale: change slotSpacing and the
// buildings re-space themselves (also on the component's context menu: "Arrange buildings").
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
    void OnValidate()
    {
        if (Application.isPlaying || UnityEditor.EditorUtility.IsPersistent(this)) return;
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Arrange(); };
    }

    [ContextMenu("Arrange buildings")]
    public void Arrange()
    {
        if (Application.isPlaying) return;
        var props = Buildings();
        var slot = SlotIndices(props);
        for (int i = 0; i < props.Length; i++)
        {
            var rt = (RectTransform)props[i].transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(slot[i] * slotSpacing, 0f);
            props[i].RefreshPreview();
        }
    }
#endif
}
