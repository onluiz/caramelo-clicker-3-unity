using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Mini caramelos that drop from the sky where the player taps, land behind Caramelo and follow him:
// they run in place while he runs, trot to catch up when they are away from their spot, and sit when he sits.
// Plain class built by GameUI; they use Caramelo's own frames at a smaller scale.
public class MiniDogs
{
    public const int MaxDogs = 24;
    const float Scale = 1.5f;
    const float Gravity = 2600f, MaxFall = 1800f;
    const float HomeNear = 220f;     // closest a mini stands behind Caramelo's centre
    const float CatchUpSpeed = 520f;
    const float CatchUpFps = 20f;

    enum Mode { Fall, Go, Run, Sit, Leave }

    class Mini
    {
        public RectTransform rt, shadow;
        public Image image;
        public Mode mode;
        public float x, y, vy, laneY;
        public float homeFrac;       // 0..1: how far behind Caramelo this one stands
        public float phase;          // desynchronises the animations
        public float animT;
        public float landT;          // squash after landing
        public float react, reactDelay;
        public bool wantRun;
        public int face = 1;
        public float leaveDelay, leaveT;
        public double bornX;         // world x (how far Caramelo had run) when it was born
    }

    readonly RectTransform root, panel;
    readonly DogRig rig;
    readonly List<Mini> dogs = new List<Mini>();

    public RectTransform Root => root;

    public MiniDogs(RectTransform parent, DogRig rig)
    {
        this.rig = rig;
        panel = parent;
        root = new GameObject("MiniDogs", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.4f); // same origin as Caramelo
        root.sizeDelta = Vector2.zero;
        root.anchoredPosition = Vector2.zero;
    }

    public int Count => dogs.Count;

    // A new mini falls from above the tap point. worldX is how far Caramelo has run so far.
    public void Spawn(Vector2 screenPos, double worldX)
    {
        if (dogs.Count >= MaxDogs) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, null, out var local);
        float halfW = panel.rect.width * 0.5f;

        var go = new GameObject("Mini", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.sizeDelta = new Vector2(DogRig.CellPx * Scale, DogRig.CellPx * Scale);
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;

        var shadowGo = new GameObject("Shadow", typeof(RectTransform));
        var srt = (RectTransform)shadowGo.transform;
        srt.SetParent(rt, false);
        srt.sizeDelta = new Vector2(80f, 12f);
        srt.anchoredPosition = new Vector2(0f, -DogRig.FeetBelow * Scale - 2f);
        var simg = shadowGo.AddComponent<Image>();
        simg.sprite = ProceduralArt.Blob();
        simg.color = new Color(0.3f, 0.12f, 0f, 0.22f);
        simg.raycastTarget = false;
        srt.SetAsFirstSibling(); // under the dog sprite itself
        shadowGo.SetActive(false);

        var m = new Mini
        {
            rt = rt,
            shadow = srt,
            image = img,
            mode = Mode.Fall,
            x = Mathf.Clamp(local.x, -halfW + 90f, halfW - 90f),
            y = panel.rect.height * 0.6f + 120f,
            laneY = DogRig.GroundY + Random.Range(-10f, 45f) + DogRig.FeetBelow * Scale,
            homeFrac = Random.value,
            phase = Random.value * 10f,
            wantRun = rig.Running,
            bornX = worldX,
            reactDelay = Random.Range(0.05f, 0.7f),
        };
        dogs.Add(m);
        Place(m);
        SortByDepth();
    }

    // Dogs further down the screen are drawn in front (all of them stay behind Caramelo).
    void SortByDepth()
    {
        dogs.Sort((a, b) => b.laneY.CompareTo(a.laneY));
        for (int i = 0; i < dogs.Count; i++) dogs[i].rt.SetSiblingIndex(i);
    }

    float HomeX(Mini m)
    {
        float span = Mathf.Clamp(panel.rect.width * 0.5f - 110f - HomeNear, 60f, 560f);
        return -(HomeNear + m.homeFrac * span);
    }

    static void Place(Mini m)
    {
        m.rt.anchoredPosition = new Vector2(m.x, m.y);
    }

    // The puppies on the ground stay at the daycare (they walk in and vanish). Only those that have run a full lap of
    // the map since they were born are let in, so tapping right in front of the daycare does not feed it. Returns how many went in.
    public int DropOff(double worldX, double lapPx)
    {
        int n = 0;
        foreach (var m in dogs)
        {
            if (m.mode == Mode.Fall || m.mode == Mode.Leave) continue;
            if (worldX - m.bornX < lapPx) continue;
            m.mode = Mode.Leave;
            m.leaveDelay = Random.Range(0f, 0.5f);
            m.leaveT = 0f;
            m.face = 1;
            n++;
        }
        return n;
    }

    // Running past the playground: the oldest puppies on the ground (up to 'max') go in and stay there. Returns how many went in.
    public int EnterPark(int max)
    {
        var free = dogs.FindAll(m => m.mode != Mode.Fall && m.mode != Mode.Leave);
        free.Sort((a, b) => a.bornX.CompareTo(b.bornX));
        int n = 0;
        foreach (var m in free)
        {
            if (n >= max) break;
            m.mode = Mode.Leave;
            m.leaveDelay = Random.Range(0f, 0.5f);
            m.leaveT = 0f;
            m.face = 1;
            n++;
        }
        return n;
    }

    public void Tick(float dt)
    {
        bool running = rig.Running;
        for (int i = dogs.Count - 1; i >= 0; i--)
        {
            var m = dogs[i];
            m.animT += dt;

            if (m.mode == Mode.Leave)
            {
                // Fixed in the world: the ground scrolls under them while they hop in through the door and fade.
                m.x -= rig.StrideSpeed * dt;
                if (m.leaveDelay > 0f) m.leaveDelay -= dt;
                else m.leaveT += dt / 0.5f;
                if (m.leaveT >= 1f)
                {
                    Object.Destroy(m.rt.gameObject);
                    dogs.RemoveAt(i);
                    continue;
                }
                float k = m.leaveT;
                m.image.color = new Color(1f, 1f, 1f, 1f - k);
                m.shadow.GetComponent<Image>().color = new Color(0.3f, 0.12f, 0f, 0.22f * (1f - k));
                m.rt.localScale = new Vector3(1f - 0.4f * k, 1f - 0.4f * k, 1f);
                SetFrame(m, DogRig.SeatedStart, DogRig.SeatedCount, DogRig.FramesPerSecondSeated);
                Place(m);
                continue;
            }

            if (m.mode == Mode.Fall)
            {
                m.vy = Mathf.Min(MaxFall, m.vy + Gravity * dt);
                m.y -= m.vy * dt;
                if (m.y <= m.laneY)
                {
                    m.y = m.laneY;
                    m.vy = 0f;
                    m.landT = 1f;
                    m.shadow.gameObject.SetActive(true);
                    m.mode = Mode.Go;
                }
                SetFrame(m, 45, 12, CatchUpFps); // legs flailing on the way down
            }
            else
            {
                // Follow Caramelo's gait with a small personal delay, so the pack does not move as one.
                if (running != m.wantRun)
                {
                    m.react += dt;
                    if (m.react >= m.reactDelay)
                    {
                        m.wantRun = running;
                        m.react = 0f;
                        m.reactDelay = Random.Range(0.05f, 0.7f);
                    }
                }
                else m.react = 0f;

                float dx = HomeX(m) - m.x;
                bool away = Mathf.Abs(dx) > (m.mode == Mode.Go ? 6f : 26f);
                var next = away ? Mode.Go : (m.wantRun ? Mode.Run : Mode.Sit);
                if (next != m.mode)
                {
                    m.mode = next;
                    m.animT = 0f;
                }

                if (m.mode == Mode.Go)
                {
                    float v = Mathf.Clamp(dx * 2.5f, -CatchUpSpeed, CatchUpSpeed);
                    m.x += v * dt;
                    m.face = dx >= 0f ? 1 : -1;
                    SetFrame(m, 45, 12, CatchUpFps);
                }
                else if (m.mode == Mode.Run)
                {
                    m.face = 1;
                    DogRig.GaitFrames(rig.Gait, out int start, out int count);
                    SetFrame(m, start, count, rig.AnimFps);
                }
                else
                {
                    m.face = 1;
                    float f = m.animT * DogRig.FramesPerSecondSit;
                    if (f < DogRig.SitFrames) m.image.sprite = rig.Frame((int)f);
                    else SetFrame(m, DogRig.SeatedStart, DogRig.SeatedCount, DogRig.FramesPerSecondSeated, DogRig.SitFrames);
                }
            }

            if (m.landT > 0f) m.landT = Mathf.Max(0f, m.landT - dt * 6f);
            m.rt.localScale = new Vector3(m.face * (1f + 0.18f * m.landT), 1f - 0.18f * m.landT, 1f);
            Place(m);
        }
    }

    // Loops a clip at the given speed. 'timeOffsetFrames' lets a clip start after another one has played.
    void SetFrame(Mini m, int start, int count, float fps, float timeOffsetFrames = 0f)
    {
        float t = m.animT * fps - timeOffsetFrames + m.phase * count;
        int i = Mathf.FloorToInt(t) % count;
        if (i < 0) i += count;
        m.image.sprite = rig.Frame(start + i);
    }
}
