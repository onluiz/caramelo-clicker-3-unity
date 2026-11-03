using System.Runtime.InteropServices;
using UnityEngine;

// Device vibration: the browser's Vibration API in the WebGL build (Plugins/WebGL/Vibrate.jslib), Handheld.Vibrate on a native phone.
// Browsers without it (desktop, iOS Safari) simply ignore the call.
public static class Haptics
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void WebVibrateBlast();
#endif

    // The player's switch in the settings; on by default.
    public static bool Enabled { get; private set; } = PlayerPrefs.GetInt("caramelo_vibration", 1) == 1;

    public static void SetEnabled(bool on)
    {
        Enabled = on;
        PlayerPrefs.SetInt("caramelo_vibration", on ? 1 : 0);
    }

    // Three pulses that match the comet's three explosions.
    public static void Blast()
    {
        if (!Enabled) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        WebVibrateBlast();
#elif UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
