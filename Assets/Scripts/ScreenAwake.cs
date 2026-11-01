using UnityEngine;
using UnityEngine.InputSystem;

// Controls how long the phone screen stays on without touches. The game is idle, so by default
// it never lets the screen sleep; the player can pick a timeout in the settings. After the chosen
// idle time the game hands control back to the OS, which dims and locks as usual (any touch resets it).
public class ScreenAwake
{
    const string PrefKey = "caramelo_screen_awake";

    // Seconds of no touches before the screen may sleep. 0 = never.
    public static readonly int[] Options = { 0, 60, 120, 300, 600, 1800 };

    public int Index { get; private set; }

    float idle;

    public ScreenAwake()
    {
        int saved = PlayerPrefs.GetInt(PrefKey, 0);
        Index = Mathf.Clamp(saved, 0, Options.Length - 1);
        Apply(true);
    }

    public void Next()
    {
        Index = (Index + 1) % Options.Length;
        PlayerPrefs.SetInt(PrefKey, Index);
        idle = 0f;
        Apply(true);
    }

    public string Label()
    {
        int s = Options[Index];
        if (s == 0) return Loc.T("Tela: sempre ligada");
        return Loc.F("Tela: apaga em {0} min", s / 60);
    }

    public void Update(float dt)
    {
        if (Options[Index] == 0) return;

        if (Touching()) idle = 0f;
        else idle += dt;

        Apply(idle < Options[Index]);
    }

    static bool Touching()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.press.isPressed) return true;
        var mouse = Mouse.current;
        return mouse != null && (mouse.leftButton.isPressed || mouse.delta.ReadValue() != Vector2.zero);
    }

    void Apply(bool keepOn)
    {
        Screen.sleepTimeout = keepOn ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
    }
}
