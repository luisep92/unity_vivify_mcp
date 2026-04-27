# unity-mcp — Unity 2019.4 minimal port

Fork minimal de [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) adaptado a **Unity 2019.4 LTS**, motivado por el ecosistema Vivify de Beat Saber (la doc oficial de Vivify recomienda 2019.4.28f1 para máxima compatibilidad con BS).

El upstream declara `"unity": "2021.3"` y depende de APIs y sintaxis post-2019: UI Toolkit con USS post-2021, `com.unity.nuget.newtonsoft-json` (registry 2020.1+), 41+ sites de C# 8/9 (switch expressions, range syntax, `using` declarations…). Este fork strippa lo que no usamos y reescribe el resto a APIs disponibles en 2019.4 + C# 7.3.

> **Estado**: operativo end-to-end contra `VivifyTemplate` (Unity 2019.4.28f1, Beat Saber 1.34.2 + mods Aeroluna). Validado con `read_console`, `refresh_unity`, `execute_code` desde Claude Code via stdio.

## Cómo se usa (TL;DR)

Asume que tienes Unity 2019.4 instalado, [`uv`](https://docs.astral.sh/uv/) en PATH, y Claude Code (CLI o extensión VSCode).

```bash
# 1. Clona el fork
git clone git@github.com:luisep92/unity_vivify_mcp.git unity-mcp
cd unity-mcp

# 2. Engancha el upstream original (opcional, para futuros rebases / PR)
git remote add upstream https://github.com/CoplayDev/unity-mcp.git

# 3. Registra el server Python en Claude Code (scope user para que la extensión VSCode lo vea)
claude mcp add -s user unity-mcp -- uv run --directory <ruta-absoluta>/unity-mcp/Server mcp-for-unity --transport stdio

# 4. En tu proyecto Unity, añade el package en Packages/manifest.json:
#    "com.coplaydev.unity-mcp": "file:<ruta-relativa-al-Packages>/unity-mcp/MCPForUnity"
```

Abre Unity con tu proyecto. En `Window > MCP For Unity`:

1. **Use Stdio Transport** (la primera vez — fija el modo en EditorPrefs).
2. **Toggle Bridge** (arranca el listener TCP en port 6400).
3. **Bridge Status** debe loggear `running: True (mode: stdio, port: 6400)`.

Reinicia Claude Code (cierra y reabre la extensión VSCode o el CLI). En la nueva sesión deben aparecer las herramientas `mcp__unity-mcp__*` (`read_console`, `refresh_unity`, `execute_code`, `manage_asset`, `manage_animation`, etc.). Smoke test rápido: pide a Claude `read_console` con count 5; debe devolver entradas reales de la Console de Unity.

### Prerequisitos del proyecto host

- **Newtonsoft.Json.dll en algún sitio del proyecto.** El asmdef de este package lo lista como `precompiledReferences` con `overrideReferences: false`, así que recoge cualquier DLL llamado `Newtonsoft.Json.dll` que esté en el proyecto. VivifyTemplate ya trae uno en `Assets/VivifyTemplate/Exporter/Dependencies/`. Si tu proyecto no lo trae, copia uno (NuGet 13.0.x net45) a `Plugins/` del proyecto.
- **Unity 2019.4.x.** Versiones más nuevas pueden funcionar pero pierdes el motivo del fork; usa upstream directamente si estás en 2021.3+.

## Qué se borró respecto a upstream

Strip agresivo orientado a Claude Code stdio + workflow Vivify. Los commits están etiquetados `Drop ...` para cherry-pick.

| Borrado | Motivo |
|---|---|
| `Editor/Windows/` (wizard UI Toolkit completo) | UI Toolkit con USS/UXML post-2021 |
| `Editor/Setup/` (Roslyn installer, skill installer, sync) | No necesario; configuramos a mano |
| 21 client configurators (`Cursor`, `Windsurf`, `Codex`, `Cline`, `Gemini`, etc.) | Solo usamos `claude mcp add` |
| `Editor/Clients/` entero | El Claude Code configurator nunca se invocaba sin la wizard |
| `Editor/Migrations/`, `Editor/Dependencies/`, `McpCiBoot.cs` | Fresh install, no migration/CI |
| Services no-core: `PackageDeployment`, `PackageUpdate`, `PackageJobManager`, `ServerManagement`, `TestRunner`, `TestJobManager`, `ResourceDiscovery`, `EditorPrefsWindowService`, `ClientConfigurationService` | El server Python lo lanza Claude Code; Unity es listener pasivo |
| `Editor/Tools/Build/` + `ManageBuild.cs` | Vivify usa su propio build (F5) |
| `Editor/Tools/{ProBuilder, Profiler, Vfx, Graphics}/` | APIs post-2020 (`LightingSettings`, `ProfilerCategory`, `Unity.Profiling.LowLevel.Unsafe`) o no-aplicables |
| `Editor/Tools/{ManagePackages, ManageUI, ManageTexture, BatchExecute, RunTests, GetTestJob}.cs` | Fuera de scope para Aline |
| `Editor/Resources/Scene/{Volumes, RenderingStats, RendererFeatures}Resource.cs` | Dependían de Graphics tools borradas |
| `Editor/Resources/Tests/` | Dependía de TestRunner |
| `External/Tommy.cs` (~2k LOC TOML parser) | Solo lo usaba `CodexConfigHelper` (borrado) |
| `Helpers/{McpConfigurationHelper, CodexConfigHelper, ConfigJsonBuilder}.cs` | Solo usados por `Editor/Clients/` |
| HTTP transport handlers (`HttpAutoStartHandler`, `HttpBridgeReloadHandler`) | Stdio basta para single-agent Claude Code |

## Qué se mantuvo

Listener bridge stdio + dispatcher de comandos + un subset de tools válidos para el workflow Vivify:

- **Tools Animation**: `ControllerCreate`, `ControllerLayers`, `ControllerBlendTrees`, `ClipCreate`, `ClipPresets`, `AnimatorRead`, `AnimatorControl`, `ManageAnimation`.
- **Tools Asset/Material/Shader/Script**: `ManageAsset`, `ManageMaterial`, `ManageShader`, `ManageScript`, `ManageScriptableObject`.
- **Tools Prefab/Scene/GameObject**: `ManagePrefabs`, `ManageScene`, `ManageGameObject`, `ManageComponents`, `FindGameObjects`.
- **Tools editor**: `RefreshUnity`, `ReadConsole`, `ExecuteCode`, `ExecuteMenuItem`, `ManageEditor`, `UnityReflect`.
- **Tools Camera/Physics**: `ManageCamera` (Cinemachine), `ManagePhysics` (sin tocar — funciona en 2019.4).
- **Bridge**: `StdioBridgeHost` + `TransportManager` + `BridgeControlService` + `MCPServiceLocator` (pelado).
- **Server Python**: sin cambios. El upstream Python es 2019-agnostic.

## Qué se reescribió a C# 7.3

Unity 2019.4 fija el lenguaje en C# 7.3. Los siguientes patrones fueron reemplazados en todos los ficheros mantenidos:

- **Switch expressions** (`x switch { ... }`) → switch statements clásicos.
- **Negated declaration patterns** (`x is not T y`) → `!(x is T y)`.
- **Null-coalescing assignment** (`??=`) → `if (x == null) x = ...`.
- **Target-typed `new()`** → constructor explícito (`new Dictionary<K,V>()`).
- **Range/index syntax** (`s[..n]`, `s[n..]`, `s[..^n]`) → `Substring` / `Skip`.
- **`using` declarations** (`using var x = ...;`) → `using (var x = ...) { ... }` blocks.
- **`#nullable disable`** directives → eliminadas (no soportadas por el preprocesador 7.3).
- **Null-forgiving operator** (`x!.Foo`) → eliminado (asumiendo no-null).

## Qué se shim-eó a APIs 2019.4

- `PrefabStageUtility` y `PrefabStage` viven en `UnityEditor.Experimental.SceneManagement` (no en `UnityEditor.SceneManagement` — eso es 2020.1+).
- `PrefabStage.assetPath` → `PrefabStage.prefabAssetPath`.
- `PrefabStageUtility.OpenPrefab(string)` → `AssetDatabase.OpenAsset(prefabAsset)` + `GetCurrentPrefabStage()`.
- `string.Contains(char)` → `string.Contains(string)`.
- `string.Replace(string, string, StringComparison)` → `string.Replace(string, string)`.
- `string.Contains(string, StringComparison)` → `IndexOf(string, StringComparison) >= 0`.
- `Math.Clamp(int, int, int)` → `Math.Max(min, Math.Min(max, value))`.
- `Task.IsCompletedSuccessfully` → `Task.Status == TaskStatus.RanToCompletion`.
- `Dictionary.Remove(TKey, out TValue)` → `TryGetValue` + `Remove(TKey)`.
- `MaterialPropertyBlock.HasColor` → heurística con `GetColor` y comparación contra `Color.clear`.
- `Object.FindObjectsOfType(Type, bool includeInactive)` → `FindObjectsOfType(Type)` (perdemos `includeInactive` en 2019.4).
- `Selection.count` → `Selection.objects.Length`.

## Cambios de comportamiento

- **Stdio es transport por defecto** (era HTTP). El minimal port solo tiene sentido como single-agent contra Claude Code; HTTP requería el `ServerManagementService` que se borró.
- **Menu Items reducidos** (`Window > MCP For Unity`):
  - `Toggle Bridge` — arranca/para el listener stdio.
  - `Bridge Status` — loggea estado, modo, port.
  - `Use Stdio Transport` / `Use HTTP Transport` — flip de `EditorPrefs.UseHttpTransport`.
- **`execute_code` con retry-on-bad-DLL.** El compilador CodeDom de .NET Framework abortaba la compilación entera al encontrar una DLL con metadata corrupta (la `Newtonsoft.Json.dll` legacy de VivifyTemplate). Ahora el tool detecta el patrón "Metadata file 'X' does not contain valid metadata", droppea esa DLL del set de referencias y reintenta hasta 8 veces.

## Limitaciones conocidas

- **No `run_tests`** — Unity Test Framework no expuesto vía MCP (TestRunnerService borrado). Si lo necesitas, cherry-pick desde upstream.
- **No Graphics tools** — sin volumes, light baking, render stats, renderer features. Las APIs son post-2020.
- **`execute_code` salta DLLs ilegibles silenciosamente.** Las DLLs droppeadas no aparecen en el reference set; código que use sus tipos falla de compilación con un mensaje confuso. Caso real: VivifyTemplate's `Newtonsoft.Json.dll` (legacy PE32 Mono build) — usa `Newtonsoft.Json.Linq` desde tools mantenidos del package, no desde `execute_code` ad-hoc.
- **`includeInactive` no funciona en `FindObjectsOfType`** — Unity 2019.4 no expone esa overload. Los tools que la pidan ignoran el parámetro.
- **Sin wizard UI** — toda la configuración via menu items + EditorPrefs + `claude mcp add` desde terminal.

## Relación con upstream

```bash
# Inspeccionar el delta del fork
git log --oneline upstream/beta..HEAD

# Sync con upstream (cuando suban changes)
git fetch upstream
git rebase upstream/beta   # o cherry-pick selectivo

# Para PR upstream: hacer fork de CoplayDev/unity-mcp en GitHub UI,
# añadirlo como remote (e.g. coplay-fork), pushear el branch y abrir PR contra `beta`.
```

Los commits están organizados con un cambio conceptual cada uno y mensajes descriptivos en inglés, pensados para que un PR upstream sea limpio. La superficie afectada es grande — más viable como reference que como merge directo.

## Estructura del repo

```
MCPForUnity/                  # Package Unity (lo que se referencia desde manifest.json)
  Editor/                     # Bridge + tools + services
  Runtime/                    # Helpers serializables (Newtonsoft converters, etc.)
  package.json                # unity: 2019.4 (no 2021.3)
Server/                       # Python MCP server (sin cambios respecto a upstream)
  src/main.py
  pyproject.toml              # mcp-for-unity = "main:main"
CLAUDE.md                     # Doc original del upstream
README.md                     # Este archivo
```

## Troubleshooting

**El menú `Window > MCP For Unity` no aparece tras instalar el package.**
Compilación silenciosa fallida. Mira si `Library/ScriptAssemblies/` existe en tu proyecto Unity — si no hay DLLs ahí, hay un conflict de DLL o asmdef roto. Causa típica: dos `Newtonsoft.Json.dll` solapando plataformas. Fix: dejar solo uno.

**`Toggle Bridge` arranca pero falla con "Connection failed... WebSocket".**
Estás en modo HTTP. `Window > MCP For Unity > Use Stdio Transport`, luego Toggle Bridge.

**`claude mcp list` muestra unity-mcp Connected pero la extensión VSCode no ve los tools.**
Scope problem. Reinstala con `claude mcp add -s user ...` (no scope-local).

**`execute_code` falla con "Metadata file '...Newtonsoft.Json.dll' does not contain valid metadata".**
Bug ya corregido — actualiza al último commit del fork.
