# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"Caramel Dog Clicker" (project folder and repo still say caramelo-clicker): a cookie-clicker in Unity 6 (6000.3.27f1, 2D URP template) that ships **only as a WebGL build**. The player clicks a caramelo dog for "petiscos" (treats). UI text is Portuguese (pt-BR). Sibling project `../caramelo-clicker-3` is the original web (Phaser/TypeScript) game; only bring assets from it when the user asks.

## Working with the Editor (Unity CLI)

Drive a live Editor with the `unity` CLI rather than editing scene/asset files; the project has the `com.unity.pipeline` package. Always pass `--project-path "$PWD"` (and `--caller plugin --skill <name>` on `unity command`).

```bash
unity status --until-ready --project-path "$PWD"          # wait for the Editor
unity recompile --project-path "$PWD"                     # prints compile errors (empty table = clean)
unity command editor.playmode start|stop --project-path "$PWD"
unity command script eval_file --file x.cs --project-path "$PWD"   # run C# in the Editor
unity command console get --project-path "$PWD" -- --tail 10 --level error
unity command build run --project-path "$PWD" -- --target WebGL --outputPath Build/WebGL --confirm true
unity command build status --project-path "$PWD" --format json     # poll until "completed"
```

There are no automated tests. Verify by entering Play mode and checking the console for errors.

Gotchas learned the hard way:
- **Stop Play mode before recompiling.** A domain reload during Play mode loses `GameController`'s state and floods the console with `NullReferenceException` in `GameController.Update`.
- `script eval` takes a statement block: no `using` directives, fully qualify types (`UnityEngine.GameObject`, not `GameObject`).
- `capture game_view` stretches the image to 1280x720, so it distorts non-16:9 sizes. For a faithful screenshot, run `UnityEngine.ScreenCapture.CaptureScreenshot("Assets/x.png")` in Play mode, then delete the file (and its `.meta`).
- To preview phone sizes, change the Game View size via reflection on `UnityEditor.GameViewSizes` (custom sizes "Celular 1080x2340" and "Celular 2340x1080" already exist in the Game tab).
- Testing mutates the Editor's `PlayerPrefs` save (key `caramelo_clicker_save_v3`). Reset it before finishing (`GameState.ResetAll()` via reflection on `GameController.state`, then `PlayerPrefs.DeleteAll()`).
- The active build target is WebGL. Release settings (Brotli + Decompression Fallback on, High stripping, no exceptions, Wasm 2023, 1 GB max memory) are already applied in Player Settings.

To try a build locally: `python -m http.server 55553 -d Build/WebGL` then open `http://localhost:55553`. `Build/` is gitignored.

To test on a phone, build a debug APK (the Android module is installed; IL2CPP, minSdk 25, signed with the debug key, package still `com.DefaultCompany.urp_2d`, so not store-ready) and install it with adb:

```bash
unity command build run --project-path "$PWD" --caller plugin --skill unity-cli --detach -- --target Android --outputPath Build/Android/CarameloClicker.apk --confirm true
unity command build status --project-path "$PWD" --format json     # poll until "completed" (~8 min the first time)
```
```powershell
$adb = "C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
& $adb devices -l                                                  # phone needs USB debugging on and authorized
& $adb install -r Build\Android\CarameloClicker.apk
& $adb shell monkey -p com.DefaultCompany.urp_2d -c android.intent.category.LAUNCHER 1
```

The Android build switches the Editor's active build target. **Switch back to WebGL afterwards** (`UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(UnityEditor.BuildTargetGroup.WebGL, UnityEditor.BuildTarget.WebGL)` via `script eval_file`). The build report's `totalSizeBytes` sums intermediate files, so check the real APK size on disk (~39 MB).

## Architecture

Everything lives in `Assets/Scripts`, with **no prefabs and almost nothing in the scene**: the scene only holds a `Game` object with `GameController`, which builds the entire uGUI hierarchy at runtime. Layout changes mean editing code, not the scene.

- `GameController` (the only MonoBehaviour): creates the `EventSystem` (Input System UI module), `GameState`, `GameAudio`, `GameUI`; ticks them in `Update` (dt clamped to 0.25s); autosaves every 5s and on pause/focus loss/quit.
- `GameState`: pure game logic (currency, buildings, upgrades, buffs, golden-bone effects, save/load via `JsonUtility` into `PlayerPrefs`, offline earnings capped at 8h at 50%). Production multipliers are cached and recomputed lazily via a dirty flag.
- `PrestigeDefs` + prestige in `GameState`: "Renascimento" (code still says Ascend/Ascensions) resets a run for Ossos Celestiais = floor(cbrt(lifetime petiscos / 1e8)) minus ossos already earned. Each osso ever earned gives +2% production (spending does not remove it); unspent ossos buy permanent "bênçãos" (production, click, discount, start petiscos, golden-bone speed, offline). `TotalEarned` is the current run, `LifetimeBase` the earlier ones. UI is the "Renascer" tab in `GameUI`; the save format only gained fields, so old saves still load.
- `GameDefs`: static data for the 28 buildings and ~119 upgrades. Upgrades are generated (click/percent/global lists plus 4 tiers per building, all with the same costs/unlock counts). 20 buildings plus their tier names/flavor text and icons come from the web game (`../caramelo-clicker-3`, names/descriptions/icons only, never its numbers); the 7 originals are procedural-icon buildings. Icons are PNGs in `Resources/Icons` (loaded by `IconLoader`). **Static initializer order matters**: `TierTexts` and the tier tables must be declared before `Upgrades`. Building order in the list is the shop order, so changing it or the building count invalidates saves: bump `SaveKey` in `GameState`.
- `GameUI`: plain class that builds and updates the whole interface (shop tabs, scroll rows, floating texts, falling bones, golden bone, ground strip). Not a MonoBehaviour; `GameController` calls `Update(dt)`. Responsive: `ApplyLayout()` switches between landscape (game left / shop right) and portrait (game top / shop bottom), swaps the `CanvasScaler` reference resolution (1920x1080 vs 1080x1920) and honors `Screen.safeArea`. It re-runs whenever the screen size changes. A transparent `TapArea` covers the whole game area and is the only click target for the dog: it sits under everything, so any element that blocks raycasts (buttons, the shop) is not a click on the dog, while HUD pills and text let taps through.
- `DogRig`: the Caramelo sprite animation and its state machine. Gait selection uses an "effort" value `log10(1 + cps)` plus a boost from recent click rate; thresholds are in `EffortMin`. He sits when there is no income and no sustained clicking (with hysteresis). Frames come from `Resources/Caramelo/caramelo-sheet.png`, a compact 10-column sheet cut from the web game's `caramelo.png`; the frame index ranges for each clip are documented at the top of `DogRig.cs`. It also exposes `StrideSpeed`, which `GameUI` uses to scroll the ground.
- `SkyParallax`: the sky behind the game area (plain class, built by `GameUI.BuildBackground`, sized in `ApplyLayout`). Art is `Resources/Clouds/<set>/1..n.png`, back to front: layer 1 is the fixed opaque sky, the others are `RawImage`s whose UVs scroll with the dog's `StrideSpeed` plus a slow drift, faster for nearer layers. The 8 sets run as a loop of a day (`Day` array: day, bright day, hazy afternoon, sunset, two dusks, night, dawn); each holds 5 min, then crossfades for 1 min into the next. The loop position starts from the wall clock, so it keeps moving between sessions; set the public `Clock` to jump to a moment when testing (reflection on `GameUI.sky`). Only the sets on screen are loaded (`Resources.UnloadAsset` on the rest). Textures need Repeat wrap on U, Point filter and no compression; a layer with a visible seam at its edges can use Mirror wrap instead (7/4 does), which `SkyParallax` detects. To add a set, drop the PNGs in `Resources/Clouds/<n>/`, import them like the others and add it to `Day`.
- `ProceduralArt`: draws all other sprites at startup from signed-distance functions (bone, golden bone, building icons, rounded 9-slice panel, gradient). `Painter` supports non-square sprites; shapes are in units where the sprite height spans -1..1.
- `GameAudio`: synthesizes the sound effects with `AudioClip.Create`; mute state is stored in `PlayerPrefs`.
- Combat (`CombatStats`, `Enemy`, `HealthBar`, `GameUI.Hero.cs`): tapping the dog no longer earns petiscos (`ClicksEarnPetiscos` in `GameUI`); tapping the flying demon is an attack. `CombatStats` holds level/XP/HP and the six attributes (Força, Vitalidade, Inteligência, Destreza, Sorte, Agilidade) with derived formulas (attack, defense, crit, hit chance); the dog's copy is saved apart in `PlayerPrefs` (`caramelo_hero_v1`, via `GameController.SaveAll`). Killing the demon gives XP; each level gives 3 attribute points, spent on the attributes screen (the "Atributos" button under the gear). The game is a run: the dog runs (`DogRig.Travelling`, sprint gait even with no income) toward a demon that starts `Enemy.StartDistance` = 100 m away (1 m = `PixelsPerMeter` = 40 px of ground, so it closes at the dog's stride speed). At 300 px it is in range: the dog stops in his fighting stance (`idle_1`, frames 69-85 of the sheet, taken from the web game's `caramelo.png`) and the two fight. The dog attacks by tapping the demon; the demon attacks every 2.4 s. A dead demon respawns at 100 m at the dog's level, scaled from `EnemyTemplate` (a bit weaker than the dog). If the dog's HP hits 0 he heals fully and the demon goes back to 100 m, with no penalty. Its sheets live in `Resources/Enemy` (copied from the `Flying Demon 2D Pixel Art` pack, 81x71 frames; flying, hurt, attack and death).
- `NumberFormat`: pt-BR number formatting done by hand (WebGL has limited culture data): dots for thousands, comma decimals, suffixes "mi", "bi", ...

## Conventions

- TextMeshPro for all text. Its default font lacks the ellipsis glyph, so text uses `TextOverflowModes.Truncate`; a text box must be at least one line tall or Truncate hides the text.
- Keep each MonoBehaviour alone in a `.cs` file named after its class (required for player builds).
- Pixel art (`Caramelo` sheet) uses point filtering and no compression; set the texture importer accordingly for any new pixel-art file.
