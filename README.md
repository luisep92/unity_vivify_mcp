# unity-mcp — Unity 2019.4 minimal port

Minimal fork of [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) adapted to **Unity 2019.4 LTS**, motivated by the Vivify Beat Saber ecosystem (Vivify's official docs recommend 2019.4.28f1 for maximum BS compatibility).

Upstream declares `"unity": "2021.3"` and depends on post-2019 APIs and syntax: UI Toolkit with post-2021 USS, `com.unity.nuget.newtonsoft-json` (registry 2020.1+), 41+ C# 8/9 sites (switch expressions, range syntax, `using` declarations…). This fork strips out what we don't use and rewrites the rest to APIs available in 2019.4 + C# 7.3.

> **Status**: working end-to-end against `VivifyTemplate` (Unity 2019.4.28f1, Beat Saber 1.34.2 + Aeroluna mods). Validated with `read_console`, `refresh_unity`, `execute_code` from Claude Code via stdio.

## Why Unity 2019.4

Vivify (the Beat Saber asset-bundle modding bridge) recommends **Unity 2019.4.28f1** as its target editor — that's the version BS's modding pipeline expects, and newer editors drift in the asset bundle format the game's loader accepts.

Unity 2019.4 is end-of-life. Unity Hub flags it explicitly, and security advisories on the editor itself are no longer back-ported. **That matters for runtime applications; it doesn't matter for this fork.** The scope here is a local Editor workflow — opening a Unity project on your own machine to author asset bundles for an offline modding tool. There's no runtime, no network surface, no untrusted input. The EoL trade-off is acceptable in that envelope and not transferable to anyone shipping a game on 2019.4.

If you're not in the Vivify / Beat Saber world, you almost certainly want [upstream `CoplayDev/unity-mcp`](https://github.com/CoplayDev/unity-mcp) on Unity 2021.3+ instead.

## How to use it (TL;DR)

Assumes you have Unity 2019.4 installed, [`uv`](https://docs.astral.sh/uv/) on PATH, and Claude Code (CLI or VSCode extension).

```bash
# 1. Clone the fork
git clone git@github.com:luisep92/unity_vivify_mcp.git unity-mcp
cd unity-mcp

# 2. Hook up the original upstream (optional, for future rebases / PR)
git remote add upstream https://github.com/CoplayDev/unity-mcp.git

# 3. Register the Python server in Claude Code (user scope so the VSCode extension sees it)
claude mcp add -s user unity-mcp -- uv run --directory <absolute-path>/unity-mcp/Server mcp-for-unity --transport stdio

# 4. In your Unity project, add the package in Packages/manifest.json:
#    "com.coplaydev.unity-mcp": "file:<relative-path-to-Packages>/unity-mcp/MCPForUnity"
```

Open Unity with your project. In `Window > MCP For Unity`:

1. **Use Stdio Transport** (first time — pins the mode in EditorPrefs).
2. **Toggle Bridge** (starts the TCP listener on port 6400).
3. **Bridge Status** should log `running: True (mode: stdio, port: 6400)`.

Restart Claude Code (close and reopen the VSCode extension or the CLI). The new session should expose the `mcp__unity-mcp__*` tools (`read_console`, `refresh_unity`, `execute_code`, `manage_asset`, `manage_animation`, etc.). Quick smoke test: ask Claude to run `read_console` with count 5; it should return real entries from the Unity Console.

### Host project prerequisites

- **Newtonsoft.Json.dll somewhere in the project.** This package's asmdef lists it as `precompiledReferences` with `overrideReferences: false`, so it picks up any DLL named `Newtonsoft.Json.dll` present in the project. VivifyTemplate already ships one at `Assets/VivifyTemplate/Exporter/Dependencies/`. If your project doesn't include one, copy a NuGet 13.0.x net45 build to the project's `Plugins/`.
- **Unity 2019.4.x.** Newer versions may work but defeat the purpose of the fork; use upstream directly if you're on 2021.3+.

## What was removed from upstream

Aggressive strip targeting Claude Code stdio + Vivify workflow. Commits are tagged `Drop ...` to be cherry-pickable.

| Removed | Reason |
|---|---|
| `Editor/Windows/` (full UI Toolkit wizard) | UI Toolkit with USS/UXML is post-2021 |
| `Editor/Setup/` (Roslyn installer, skill installer, sync) | Not needed; configured manually |
| 21 client configurators (`Cursor`, `Windsurf`, `Codex`, `Cline`, `Gemini`, etc.) | We only use `claude mcp add` |
| All of `Editor/Clients/` | The Claude Code configurator was never invoked without the wizard |
| `Editor/Migrations/`, `Editor/Dependencies/`, `McpCiBoot.cs` | Fresh install, no migration/CI |
| Non-core services: `PackageDeployment`, `PackageUpdate`, `PackageJobManager`, `ServerManagement`, `TestRunner`, `TestJobManager`, `ResourceDiscovery`, `EditorPrefsWindowService`, `ClientConfigurationService` | The Python server is launched by Claude Code; Unity is a passive listener |
| `Editor/Tools/Build/` + `ManageBuild.cs` | Vivify uses its own build (F5) |
| `Editor/Tools/{ProBuilder, Profiler, Vfx, Graphics}/` | Post-2020 APIs (`LightingSettings`, `ProfilerCategory`, `Unity.Profiling.LowLevel.Unsafe`) or not applicable |
| `Editor/Tools/{ManagePackages, ManageUI, ManageTexture, BatchExecute, RunTests, GetTestJob}.cs` | Out of scope for Aline |
| `Editor/Resources/Scene/{Volumes, RenderingStats, RendererFeatures}Resource.cs` | Depended on the removed Graphics tools |
| `Editor/Resources/Tests/` | Depended on TestRunner |
| `External/Tommy.cs` (~2k LOC TOML parser) | Only used by `CodexConfigHelper` (removed) |
| `Helpers/{McpConfigurationHelper, CodexConfigHelper, ConfigJsonBuilder}.cs` | Only used by `Editor/Clients/` |
| HTTP transport handlers (`HttpAutoStartHandler`, `HttpBridgeReloadHandler`) | stdio is enough for single-agent Claude Code |

## What stayed

stdio bridge listener + command dispatcher + a subset of tools valid for the Vivify workflow:

- **Animation tools**: `ControllerCreate`, `ControllerLayers`, `ControllerBlendTrees`, `ClipCreate`, `ClipPresets`, `AnimatorRead`, `AnimatorControl`, `ManageAnimation`.
- **Asset/Material/Shader/Script tools**: `ManageAsset`, `ManageMaterial`, `ManageShader`, `ManageScript`, `ManageScriptableObject`.
- **Prefab/Scene/GameObject tools**: `ManagePrefabs`, `ManageScene`, `ManageGameObject`, `ManageComponents`, `FindGameObjects`.
- **Editor tools**: `RefreshUnity`, `ReadConsole`, `ExecuteCode`, `ExecuteMenuItem`, `ManageEditor`, `UnityReflect`.
- **Camera/Physics tools**: `ManageCamera` (Cinemachine), `ManagePhysics` (untouched — works on 2019.4).
- **Bridge**: `StdioBridgeHost` + `TransportManager` + `BridgeControlService` + `MCPServiceLocator` (stripped down).
- **Python server**: unchanged. Upstream Python is 2019-agnostic.

## What was rewritten to C# 7.3

Unity 2019.4 fixes the language at C# 7.3. The following patterns were replaced across all retained files:

- **Switch expressions** (`x switch { ... }`) → classic switch statements.
- **Negated declaration patterns** (`x is not T y`) → `!(x is T y)`.
- **Null-coalescing assignment** (`??=`) → `if (x == null) x = ...`.
- **Target-typed `new()`** → explicit constructor (`new Dictionary<K,V>()`).
- **Range/index syntax** (`s[..n]`, `s[n..]`, `s[..^n]`) → `Substring` / `Skip`.
- **`using` declarations** (`using var x = ...;`) → `using (var x = ...) { ... }` blocks.
- **`#nullable disable`** directives → removed (not supported by the 7.3 preprocessor).
- **Null-forgiving operator** (`x!.Foo`) → removed (assuming non-null).

## What was shimmed to 2019.4 APIs

- `PrefabStageUtility` and `PrefabStage` live in `UnityEditor.Experimental.SceneManagement` (not `UnityEditor.SceneManagement` — that's 2020.1+).
- `PrefabStage.assetPath` → `PrefabStage.prefabAssetPath`.
- `PrefabStageUtility.OpenPrefab(string)` → `AssetDatabase.OpenAsset(prefabAsset)` + `GetCurrentPrefabStage()`.
- `string.Contains(char)` → `string.Contains(string)`.
- `string.Replace(string, string, StringComparison)` → `string.Replace(string, string)`.
- `string.Contains(string, StringComparison)` → `IndexOf(string, StringComparison) >= 0`.
- `Math.Clamp(int, int, int)` → `Math.Max(min, Math.Min(max, value))`.
- `Task.IsCompletedSuccessfully` → `Task.Status == TaskStatus.RanToCompletion`.
- `Dictionary.Remove(TKey, out TValue)` → `TryGetValue` + `Remove(TKey)`.
- `MaterialPropertyBlock.HasColor` → heuristic using `GetColor` and comparison against `Color.clear`.
- `Object.FindObjectsOfType(Type, bool includeInactive)` → `FindObjectsOfType(Type)` (loses `includeInactive` on 2019.4).
- `Selection.count` → `Selection.objects.Length`.

## Behaviour changes

- **stdio is the default transport** (was HTTP). The minimal port only makes sense as single-agent against Claude Code; HTTP required `ServerManagementService`, which was removed.
- **Reduced menu items** (`Window > MCP For Unity`):
  - `Toggle Bridge` — starts/stops the stdio listener.
  - `Bridge Status` — logs state, mode, port.
  - `Use Stdio Transport` / `Use HTTP Transport` — flips `EditorPrefs.UseHttpTransport`.
- **`execute_code` with retry-on-bad-DLL.** The .NET Framework CodeDom compiler aborted the entire compilation when it hit a DLL with corrupt metadata (the legacy `Newtonsoft.Json.dll` from VivifyTemplate). The tool now detects the "Metadata file 'X' does not contain valid metadata" pattern, drops that DLL from the reference set, and retries up to 8 times.

## Known limitations

- **No `run_tests`** — Unity Test Framework not exposed via MCP (TestRunnerService removed). If you need it, cherry-pick from upstream.
- **No Graphics tools** — no volumes, light baking, render stats, renderer features. Those APIs are post-2020.
- **`execute_code` silently skips unreadable DLLs.** Dropped DLLs don't appear in the reference set; code that uses their types fails to compile with a confusing message. Real case: VivifyTemplate's `Newtonsoft.Json.dll` (legacy PE32 Mono build) — use `Newtonsoft.Json.Linq` from the package's retained tools, not from ad-hoc `execute_code`.
- **`includeInactive` does nothing in `FindObjectsOfType`** — Unity 2019.4 doesn't expose that overload. Tools that pass it ignore the parameter.
- **No wizard UI** — all configuration via menu items + EditorPrefs + `claude mcp add` from the terminal.

## Upstream relationship

```bash
# Inspect the fork delta
git log --oneline upstream/beta..HEAD

# Sync with upstream (when they ship changes)
git fetch upstream
git rebase upstream/beta   # or selective cherry-pick

# For an upstream PR: fork CoplayDev/unity-mcp in the GitHub UI,
# add it as a remote (e.g. coplay-fork), push the branch, and open a PR against `beta`.
```

Commits are organised one conceptual change each, with descriptive English messages, intended to make a clean upstream PR. The affected surface is large — more viable as a reference than as a direct merge.

## Repo layout

```
MCPForUnity/                  # Unity package (what manifest.json references)
  Editor/                     # Bridge + tools + services
  Runtime/                    # Serializable helpers (Newtonsoft converters, etc.)
  package.json                # unity: 2019.4 (not 2021.3)
Server/                       # Python MCP server (unchanged from upstream)
  src/main.py
  pyproject.toml              # mcp-for-unity = "main:main"
CLAUDE.md                     # Original upstream doc
README.md                     # This file
```

## Troubleshooting

**The `Window > MCP For Unity` menu doesn't appear after installing the package.**
Silent compilation failure. Check whether `Library/ScriptAssemblies/` exists in your Unity project — if there are no DLLs there, there's a DLL conflict or a broken asmdef. Typical cause: two `Newtonsoft.Json.dll` files with overlapping platforms. Fix: keep only one.

**`Toggle Bridge` starts but fails with "Connection failed... WebSocket".**
You're in HTTP mode. Go to `Window > MCP For Unity > Use Stdio Transport`, then Toggle Bridge.

**`claude mcp list` shows unity-mcp Connected but the VSCode extension doesn't see the tools.**
Scope problem. Reinstall with `claude mcp add -s user ...` (not local scope).

**`execute_code` fails with "Metadata file '...Newtonsoft.Json.dll' does not contain valid metadata".**
Already fixed — update to the latest commit on the fork.
