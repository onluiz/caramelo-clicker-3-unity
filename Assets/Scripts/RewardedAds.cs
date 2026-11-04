using System;
using UnityEngine;
#if UNITY_ANDROID
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

// Rewarded video ads. On Android it asks for consent (UMP) and then keeps one AdMob ad preloaded; in the Editor
// it pretends an ad is always ready and always rewards; in every other build (WebGL) there are no ads at all.
// Development builds use Google's test ad, release builds the real unit, so the real one is never clicked by us.
public static class RewardedAds
{
    const string TestUnit = "ca-app-pub-3940256099942544/5224354917";
    const string MoneyBagUnit = "ca-app-pub-4380393767380236/8285391492";
    const float RetrySeconds = 30f;

#if UNITY_ANDROID
    static RewardedAd ad;
    static bool started, sdkReady, loading, ready;
    static float retryAt;

    public static bool Ready => Application.isEditor || ready;

    static string Unit => Debug.isDebugBuild ? TestUnit : MoneyBagUnit;

    // Call once at startup; it never blocks and every failure just leaves the ads off.
    public static void Begin()
    {
        if (started || Application.isEditor) return;
        started = true;
        try
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            ConsentInformation.Update(new ConsentRequestParameters(), _ =>
                ConsentForm.LoadAndShowConsentFormIfRequired(__ =>
                {
                    if (ConsentInformation.CanRequestAds()) InitializeSdk();
                }));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Ads disabled: " + e.Message);
        }
    }

    static void InitializeSdk()
    {
        MobileAds.Initialize(_ =>
        {
            sdkReady = true;
            Load();
        });
    }

    // Keeps trying to have an ad ready, a little apart when loading fails (no network, no fill).
    public static void Tick()
    {
        if (sdkReady && !ready && !loading && Time.realtimeSinceStartup >= retryAt) Load();
    }

    static void Load()
    {
        if (loading) return;
        loading = true;
        RewardedAd.Load(Unit, new AdRequest(), (loaded, error) =>
        {
            loading = false;
            if (error != null || loaded == null)
            {
                retryAt = Time.realtimeSinceStartup + RetrySeconds;
                return;
            }
            ad = loaded;
            ready = true;
        });
    }

    // done(true) only when the player watched the ad to the end.
    public static void Show(Action<bool> done)
    {
        if (Application.isEditor)
        {
            done(true);
            return;
        }

        if (!ready || ad == null || !ad.CanShowAd())
        {
            done(false);
            return;
        }

        var current = ad;
        ad = null;
        ready = false;
        bool rewarded = false, finished = false;

        void Finish()
        {
            if (finished) return;
            finished = true;
            current.Destroy();
            retryAt = 0f;
            done(rewarded);
        }

        current.OnAdFullScreenContentClosed += Finish;
        current.OnAdFullScreenContentFailed += _ =>
        {
            rewarded = false;
            Finish();
        };
        current.Show(_ => rewarded = true);
    }
#else
    public static bool Ready => Application.isEditor;

    public static void Begin() { }

    public static void Tick() { }

    public static void Show(Action<bool> done)
    {
        done(Application.isEditor);
    }
#endif
}
