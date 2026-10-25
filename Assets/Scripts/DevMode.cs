using System;
using UnityEngine;

// The Dev tab (cheats) only exists outside production: in the Editor, in Development builds,
// and in a WebGL build served from this machine or a private network (local testing).
public static class DevMode
{
    public static bool Enabled
    {
        get
        {
#if UNITY_EDITOR
            return true;
#else
            return Debug.isDebugBuild || IsLocalHost(Application.absoluteURL);
#endif
        }
    }

    static bool IsLocalHost(string url)
    {
        if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        string host = uri.Host.ToLowerInvariant();
        if (host == "localhost" || host.EndsWith(".localhost") || host.EndsWith(".local")) return true;
        if (!System.Net.IPAddress.TryParse(host, out var ip)) return false;
        var b = ip.GetAddressBytes();
        if (b.Length != 4) return false;
        return b[0] == 127 || b[0] == 10 || (b[0] == 192 && b[1] == 168) || (b[0] == 172 && b[1] >= 16 && b[1] <= 31);
    }
}
