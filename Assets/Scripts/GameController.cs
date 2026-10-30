using UnityEngine;
using UnityEngine.EventSystems;

public class GameController : MonoBehaviour
{
    const float AutoSaveSeconds = 5f;

    GameState state;
    GameAudio sfx;
    GameUI ui;
    float saveTimer;

    void Awake()
    {
        Application.runInBackground = true;
        EnsureEventSystem();
        Loc.Init();

        state = new GameState();
        state.Load();
        sfx = new GameAudio(gameObject);
        ui = new GameUI(state, sfx);
    }

    void Update()
    {
        // Clamp so a long tab-out doesn't dump a huge step into the simulation.
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.25f);
        state.Tick(dt);
        ui.Update(dt);

        saveTimer += dt;
        if (saveTimer >= AutoSaveSeconds)
        {
            saveTimer = 0f;
            state.Save();
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && state != null) state.Save();
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused && state != null) state.Save();
    }

    void OnApplicationQuit()
    {
        if (state != null) state.Save();
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        module.AssignDefaultActions();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}
