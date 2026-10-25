using System.Collections.Generic;
using UnityEngine;

// Loads pixel-art icons from Resources/Icons and wraps them in sprites (cached by name).
public static class IconLoader
{
    static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Load(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        // With domain reload off, sprites from an earlier Play session are destroyed but still cached:
        // compare with Unity's null check (not ??) and reload them.
        if (Cache.TryGetValue(name, out var cached) && cached != null) return cached;

        Sprite sprite = null;
        var tex = Resources.Load<Texture2D>("Icons/" + name);
        if (tex != null)
        {
            // Small sprites are pixel art and stay crisp; the larger ones are smoothed.
            tex.filterMode = Mathf.Max(tex.width, tex.height) <= 64 ? FilterMode.Point : FilterMode.Bilinear;
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        else
        {
            Debug.LogError("Icon not found at Resources/Icons/" + name);
        }

        Cache[name] = sprite;
        return sprite;
    }

    // The PNG when a file is given, otherwise the procedural icon.
    public static Sprite For(string file, int proceduralIndex)
    {
        var sprite = Load(file);
        return sprite != null ? sprite : ProceduralArt.Icon(proceduralIndex);
    }
}
