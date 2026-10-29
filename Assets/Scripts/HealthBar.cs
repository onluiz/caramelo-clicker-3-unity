using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A small HP bar with a name label above it, hung over a character's head. It is a child of `parent`,
// so it follows (and scales with) it. `pos` is the bar's bottom-centre relative to `anchor` in the parent.
public class HealthBar
{
    const float Width = 150f;
    const float Height = 14f;

    static readonly Color BackColor = new Color(0.12f, 0.05f, 0.02f, 0.85f);

    readonly RectTransform root, fillRt;
    readonly Image back, fill;
    readonly TextMeshProUGUI label, labelShadow;
    readonly Color fillColor;
    float shown = -1f;

    public HealthBar(Transform parent, Vector2 anchor, Vector2 pos, Color fillColor)
    {
        this.fillColor = fillColor;

        back = NewImage("HpBar", parent, BackColor);
        root = back.rectTransform;
        root.anchorMin = root.anchorMax = anchor;
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = pos;
        root.sizeDelta = new Vector2(Width, Height);

        fill = NewImage("Fill", root, fillColor);
        fillRt = fill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        // The dark copy behind is the name's drop shadow (the font's outline shader variant is not available),
        // so it reads on bright skies.
        labelShadow = NewLabel("NameShadow", new Color(0.18f, 0.08f, 0.02f, 1f), new Vector2(2f, -1f));
        label = NewLabel("Name", Color.white, new Vector2(0f, 2f));
    }

    TextMeshProUGUI NewLabel(string name, Color color, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = 26f;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        // The default font has no ellipsis glyph, so truncate.
        t.overflowMode = TextOverflowModes.Truncate;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(280f, 36f);
        rt.anchoredPosition = pos;
        return t;
    }

    static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = ProceduralArt.RoundedRect();
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public void SetLabel(string text)
    {
        label.text = text;
        labelShadow.text = text;
    }

    // `fraction` is HP / max HP.
    public void SetFill(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        if (Mathf.Approximately(fraction, shown)) return;
        shown = fraction;
        fillRt.anchorMax = new Vector2(fraction, 1f);
    }

    public void SetAlpha(float alpha)
    {
        back.color = new Color(BackColor.r, BackColor.g, BackColor.b, BackColor.a * alpha);
        fill.color = new Color(fillColor.r, fillColor.g, fillColor.b, alpha);
        label.alpha = alpha;
        labelShadow.alpha = alpha;
    }

    // Screen position of the bar, for floating texts (the canvas is screen-space overlay).
    public Vector2 ScreenPosition => RectTransformUtility.WorldToScreenPoint(null, root.position);
}
