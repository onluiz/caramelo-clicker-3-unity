using System;
using UnityEngine;

// All sprites are drawn at startup from signed distance functions, so the game ships with no image files.
public static class ProceduralArt
{
    static Sprite bone, goldenBone, blob, rounded, gradient, celestial, gear;
    static readonly Sprite[] icons = new Sprite[11];

    public static Color Hex(string html)
    {
        ColorUtility.TryParseHtmlString(html, out var c);
        return c;
    }

    static class Sdf
    {
        public static float Circle(Vector2 p, Vector2 c, float r) { return (p - c).magnitude - r; }

        public static float Ellipse(Vector2 p, Vector2 c, float rx, float ry)
        {
            var q = new Vector2((p.x - c.x) / rx, (p.y - c.y) / ry);
            return (q.magnitude - 1f) * Mathf.Min(rx, ry);
        }

        public static float Box(Vector2 p, Vector2 c, Vector2 half, float round = 0f)
        {
            var d = new Vector2(Mathf.Abs(p.x - c.x) - half.x + round, Mathf.Abs(p.y - c.y) - half.y + round);
            return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - round;
        }

        public static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        public static float Ring(Vector2 p, Vector2 c, float r, float thickness)
        {
            return Mathf.Abs((p - c).magnitude - r) - thickness;
        }

        public static float Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s = Mathf.Sign((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
            return Mathf.Max(Edge(p, a, b, s), Mathf.Max(Edge(p, b, c, s), Edge(p, c, a, s)));
        }

        static float Edge(Vector2 p, Vector2 a, Vector2 b, float s)
        {
            var e = b - a;
            var n = new Vector2(e.y, -e.x).normalized * s;
            return Vector2.Dot(p - a, n);
        }
    }

    class Painter
    {
        // Shapes are described in units where the sprite height spans -1..1 (width scales with the aspect ratio).
        readonly int width, height;
        readonly Color[] px;

        public Painter(int size) : this(size, size) { }

        public Painter(int width, int height)
        {
            this.width = width;
            this.height = height;
            px = new Color[width * height];
        }

        // Composites one anti-aliased shape over what is already painted. `grow` inflates the shape (used for outlines).
        public void Fill(Func<Vector2, float> sdf, Color col, float grow = 0f)
        {
            float unit = height * 0.5f;
            float pixel = 1f / unit;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = new Vector2((x + 0.5f - width * 0.5f) / unit, (y + 0.5f - height * 0.5f) / unit);
                    float a = Mathf.Clamp01(0.5f - (sdf(p) - grow) / pixel) * col.a;
                    if (a <= 0f) continue;

                    int i = y * width + x;
                    var dst = px[i];
                    float outA = a + dst.a * (1f - a);
                    float k = dst.a * (1f - a);
                    px[i] = new Color(
                        (col.r * a + dst.r * k) / outA,
                        (col.g * a + dst.g * k) / outA,
                        (col.b * a + dst.b * k) / outA,
                        outA);
                }
            }
        }

        public void Shape(Func<Vector2, float> sdf, Color fill, Color outline, float outlineWidth)
        {
            Fill(sdf, outline, outlineWidth);
            Fill(sdf, fill);
        }

        public Sprite ToSprite(float ppu = 100f, Vector4 border = default)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
        }
    }

    static float BoneSdf(Vector2 p)
    {
        float a = 35f * Mathf.Deg2Rad;
        float c = Mathf.Cos(a), s = Mathf.Sin(a);
        var q = new Vector2(c * p.x + s * p.y, -s * p.x + c * p.y);
        float d = Sdf.Capsule(q, new Vector2(-0.45f, 0f), new Vector2(0.45f, 0f), 0.15f);
        d = Mathf.Min(d, Sdf.Circle(q, new Vector2(-0.62f, 0.16f), 0.2f));
        d = Mathf.Min(d, Sdf.Circle(q, new Vector2(-0.62f, -0.16f), 0.2f));
        d = Mathf.Min(d, Sdf.Circle(q, new Vector2(0.62f, 0.16f), 0.2f));
        d = Mathf.Min(d, Sdf.Circle(q, new Vector2(0.62f, -0.16f), 0.2f));
        return d;
    }

    public static Sprite Bone()
    {
        if (bone != null) return bone;
        var P = new Painter(96);
        P.Shape(BoneSdf, Hex("#FFF6E0"), Hex("#8A5A2B"), 0.05f);
        return bone = P.ToSprite();
    }

    public static Sprite GoldenBone()
    {
        if (goldenBone != null) return goldenBone;
        var P = new Painter(128);
        P.Fill(BoneSdf, new Color(1f, 0.85f, 0.2f, 0.35f), 0.14f);
        P.Shape(BoneSdf, Hex("#FFD23F"), Hex("#B8860B"), 0.05f);
        P.Fill(p => Sdf.Ellipse(p, new Vector2(-0.12f, 0.1f), 0.22f, 0.05f), new Color(1f, 1f, 1f, 0.6f));
        return goldenBone = P.ToSprite();
    }

    // Placeholder icon for the Céu tab (a crescent moon), deliberately unlike the golden bone.
    public static Sprite CelestialPlaceholder()
    {
        if (celestial != null) return celestial;
        var P = new Painter(96);
        P.Shape(p => Mathf.Max(Sdf.Circle(p, Vector2.zero, 0.8f), -Sdf.Circle(p, new Vector2(0.38f, 0.2f), 0.68f)),
            Hex("#B9A7FF"), Hex("#2E2466"), 0.05f);
        return celestial = P.ToSprite();
    }

    // Settings cog: a disc with eight teeth and a hole.
    public static Sprite Gear()
    {
        if (gear != null) return gear;
        var P = new Painter(96);
        P.Shape(p =>
        {
            float d = Sdf.Circle(p, Vector2.zero, 0.58f);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                float c = Mathf.Cos(a), sn = Mathf.Sin(a);
                var q = new Vector2(c * p.x + sn * p.y, -sn * p.x + c * p.y);
                d = Mathf.Min(d, Sdf.Box(q, new Vector2(0.7f, 0f), new Vector2(0.2f, 0.15f)));
            }
            return Mathf.Max(d, -Sdf.Circle(p, Vector2.zero, 0.26f));
        }, Hex("#EDE3D3"), Hex("#2B1A10"), 0.06f);
        return gear = P.ToSprite();
    }

    public static Sprite Blob()
    {
        if (blob != null) return blob;
        var P = new Painter(64);
        P.Fill(p => Sdf.Circle(p, Vector2.zero, 0.95f), Color.white);
        return blob = P.ToSprite();
    }

    public static Sprite RoundedRect()
    {
        if (rounded != null) return rounded;
        var P = new Painter(64);
        P.Fill(p => Sdf.Box(p, Vector2.zero, new Vector2(0.97f, 0.97f), 0.5f), Color.white);
        return rounded = P.ToSprite(100f, new Vector4(20, 20, 20, 20));
    }

    public static Sprite Gradient(Color top, Color bottom)
    {
        if (gradient != null) return gradient;
        const int h = 256;
        var tex = new Texture2D(2, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        for (int y = 0; y < h; y++)
        {
            var c = Color.Lerp(bottom, top, y / (h - 1f));
            tex.SetPixel(0, y, c);
            tex.SetPixel(1, y, c);
        }
        tex.Apply(false, true);
        return gradient = Sprite.Create(tex, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), 100f);
    }

    public static Sprite Icon(int index)
    {
        if (icons[index] != null) return icons[index];

        var line = Hex("#5A2E12");
        var P = new Painter(96);
        const float w = 0.06f;

        switch (index)
        {
            case 0:
                P.Shape(BoneSdf, Hex("#FFF6E0"), line, w);
                break;
            case 1: // bowl with kibble
                P.Shape(p => Mathf.Max(Sdf.Circle(p, new Vector2(0f, 0.25f), 0.45f), 0.05f - p.y), Hex("#B5722F"), line, 0.03f);
                P.Shape(p => Mathf.Max(Sdf.Ellipse(p, new Vector2(0f, 0.05f), 0.8f, 0.6f), p.y - 0.05f), Hex("#D9534F"), line, w);
                P.Shape(p => Sdf.Box(p, new Vector2(0f, 0.05f), new Vector2(0.85f, 0.07f), 0.07f), Hex("#EE7773"), line, 0.03f);
                break;
            case 2: // leash
                P.Shape(p => Sdf.Ring(p, new Vector2(-0.1f, 0.2f), 0.45f, 0.09f), Hex("#D9534F"), line, 0.04f);
                P.Shape(p => Sdf.Capsule(p, new Vector2(0.2f, -0.15f), new Vector2(0.65f, -0.65f), 0.1f), Hex("#D9534F"), line, 0.04f);
                P.Shape(p => Sdf.Circle(p, new Vector2(0.65f, -0.65f), 0.16f), Hex("#C0C0C0"), line, 0.04f);
                break;
            case 3: // pet shop
                P.Shape(p => Sdf.Box(p, new Vector2(0f, -0.25f), new Vector2(0.62f, 0.45f)), Hex("#FFE0A8"), line, w);
                P.Shape(p => Sdf.Tri(p, new Vector2(-0.82f, 0.18f), new Vector2(0.82f, 0.18f), new Vector2(0f, 0.85f)), Hex("#D9534F"), line, w);
                P.Shape(p => Sdf.Box(p, new Vector2(0f, -0.4f), new Vector2(0.16f, 0.3f)), Hex("#8A5A2B"), line, 0.03f);
                break;
            case 4: // biscuit
                P.Shape(p => Sdf.Circle(p, Vector2.zero, 0.75f), Hex("#E0A95F"), line, w);
                foreach (var c in new[] { new Vector2(-0.3f, 0.3f), new Vector2(0.3f, 0.35f), new Vector2(0.05f, -0.05f), new Vector2(-0.35f, -0.3f), new Vector2(0.38f, -0.25f) })
                {
                    var cc = c;
                    P.Fill(p => Sdf.Circle(p, cc, 0.1f), Hex("#5A2E12"));
                }
                break;
            case 5: // factory
                P.Shape(p => Sdf.Box(p, new Vector2(0.45f, 0.25f), new Vector2(0.13f, 0.4f)), Hex("#9A9A9A"), line, 0.04f);
                P.Shape(p => Sdf.Box(p, new Vector2(0f, -0.3f), new Vector2(0.78f, 0.38f)), Hex("#C8A27A"), line, w);
                P.Fill(p => Sdf.Box(p, new Vector2(-0.4f, -0.25f), new Vector2(0.14f, 0.14f)), Hex("#FFE066"));
                P.Fill(p => Sdf.Box(p, new Vector2(0f, -0.25f), new Vector2(0.14f, 0.14f)), Hex("#FFE066"));
                P.Fill(p => Sdf.Box(p, new Vector2(0.4f, -0.25f), new Vector2(0.14f, 0.14f)), Hex("#FFE066"));
                P.Fill(p => Sdf.Circle(p, new Vector2(0.55f, 0.8f), 0.13f), new Color(0.9f, 0.9f, 0.9f, 0.85f));
                break;
            case 6: // drumstick
                P.Shape(p => Sdf.Capsule(p, new Vector2(0.05f, -0.05f), new Vector2(0.55f, -0.55f), 0.09f), Hex("#FFF6E0"), line, 0.04f);
                P.Shape(p => Sdf.Circle(p, new Vector2(0.62f, -0.5f), 0.12f), Hex("#FFF6E0"), line, 0.04f);
                P.Shape(p => Sdf.Circle(p, new Vector2(0.5f, -0.62f), 0.12f), Hex("#FFF6E0"), line, 0.04f);
                P.Shape(p => Sdf.Ellipse(p, new Vector2(-0.18f, 0.18f), 0.52f, 0.46f), Hex("#B5522B"), line, w);
                P.Fill(p => Sdf.Ellipse(p, new Vector2(-0.3f, 0.32f), 0.2f, 0.1f), new Color(1f, 0.8f, 0.6f, 0.5f));
                break;
            case 7: // star
                P.Shape(p => p.magnitude - (0.5f + 0.2f * Mathf.Cos(5f * (Mathf.Atan2(p.y, p.x) - Mathf.PI / 2f))), Hex("#FFD23F"), line, w);
                break;
            case 8: // portal
                P.Shape(p => Sdf.Circle(p, Vector2.zero, 0.8f), Hex("#5B34B8"), line, w);
                P.Fill(p => Sdf.Circle(p, Vector2.zero, 0.6f), Hex("#8E63F0"));
                P.Fill(p => Sdf.Circle(p, Vector2.zero, 0.4f), Hex("#C7A8FF"));
                P.Fill(p => Sdf.Circle(p, Vector2.zero, 0.2f), Hex("#F4ECFF"));
                break;
            default: // paw
                var brown = Hex("#8A4B1F");
                P.Shape(p => Sdf.Ellipse(p, new Vector2(0f, -0.25f), 0.42f, 0.34f), brown, line, 0.04f);
                P.Shape(p => Sdf.Ellipse(p, new Vector2(-0.58f, 0.08f), 0.15f, 0.2f), brown, line, 0.04f);
                P.Shape(p => Sdf.Ellipse(p, new Vector2(-0.22f, 0.45f), 0.16f, 0.21f), brown, line, 0.04f);
                P.Shape(p => Sdf.Ellipse(p, new Vector2(0.22f, 0.45f), 0.16f, 0.21f), brown, line, 0.04f);
                P.Shape(p => Sdf.Ellipse(p, new Vector2(0.58f, 0.08f), 0.15f, 0.2f), brown, line, 0.04f);
                break;
        }

        return icons[index] = P.ToSprite();
    }
}
