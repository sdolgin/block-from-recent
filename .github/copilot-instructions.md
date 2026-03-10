# Copilot Instructions — Block From Recent

These instructions are for GitHub Copilot (Claude Opus 4.6) working on the **Block From Recent** codebase.

## Project Overview

Block From Recent is a **Windows system tray utility** that automatically removes unwanted files from the Windows Recent Files folder (`%AppData%\Microsoft\Windows\Recent`) based on user-defined exclusion rules. It also cleans matching entries from Windows AutomaticDestinations jump list files.

This is a **single-user, single-machine desktop app** — not a web service, not a library, not cross-platform. All code runs on Windows with full trust.

### Key facts

- **Target framework:** `net8.0-windows` (WinForms)
- **C# version:** 12 (the default for .NET 8 — do not change `<LangVersion>`)
- **Nullable reference types:** enabled
- **Implicit usings:** enabled
- **No dependency injection container** — the app is intentionally simple; objects are composed manually in `Program.cs` and `TrayApplicationContext`
- **No async in the current codebase** — the app uses `FileSystemWatcher` events, `System.Timers.Timer`, and synchronous file I/O. Introduce async only when explicitly needed (e.g., replacing `Thread.Sleep`)
- **Single solution, single project** (plus a future test project `BlockFromRecent.Tests`)

## Architecture

```
Program.cs                    — Entry point: mutex, config load, run TrayApplicationContext
App/
  TrayApplicationContext.cs   — System tray icon, context menu, owns RecentFileCleaner
  SettingsForm.cs             — WinForms settings dialog (rules CRUD, checkboxes, test/save)
Config/
  AppConfig.cs                — POCO config model + ExclusionRule + RuleType enum
  AppPaths.cs                 — Central location for %AppData%\BlockFromRecent\ paths
  ConfigManager.cs            — JSON serialization with source-generated context
Core/
  ExclusionEngine.cs          — Rule matching: path prefix + glob pattern
  RecentFileWatcher.cs        — FileSystemWatcher wrapper with debounce
  RecentFileCleaner.cs        — Orchestrates watcher + engine + jump list cleaning
  JumpListCleaner.cs          — OLE compound file parsing for AutomaticDestinations
  ShortcutResolver.cs         — Pure managed .lnk (MS-SHLLINK) binary parser
  Log.cs                      — Simple file logger (%AppData%\BlockFromRecent\block-from-recent.log)
Startup/
  AutoStartManager.cs         — Registry HKCU\...\Run management
Resources/
  app.ico                     — Application icon
```

### Data flow

1. `RecentFileWatcher` monitors `%AppData%\Microsoft\Windows\Recent` for new `.lnk` files via `FileSystemWatcher`
2. Events are debounced (500ms) and forwarded to `RecentFileCleaner.HandleNewRecentFile`
3. `ShortcutResolver` parses the `.lnk` binary to extract the target file path
4. `ExclusionEngine` checks the target path against all configured rules
5. If matched, the `.lnk` is deleted and `JumpListCleaner` removes matching entries from AutomaticDestinations compound files
6. `SHChangeNotify` tells Explorer to refresh its cached view

### Threading model

- **UI thread:** WinForms message loop (`Application.Run`), `SettingsForm`, `TrayApplicationContext`
- **Timer thread:** `System.Timers.Timer.Elapsed` fires `ProcessPendingFiles` on a thread pool thread
- **FileSystemWatcher:** `Created`/`Renamed` events fire on thread pool threads
- Rule updates from the UI and rule reads from the watcher thread share `ExclusionEngine._rules` — be aware of the threading boundary

## Coding Conventions

### Style

- **File-scoped namespaces** — all files use `namespace X;` (not `namespace X { }`)
- **No `this.` qualifier** — fields use `_camelCase` prefix
- **Fields:** `private readonly` where possible, `_camelCase` naming
- **Constants:** `PascalCase` for `const`, `private const` preferred
- **Properties:** `PascalCase`, auto-properties preferred
- **Methods:** `PascalCase`, keep them short and focused
- **Event fields:** `public event Action<T>?` pattern (not `EventHandler`)
- **Logging:** Use `Log.Info/Warn/Error/Debug` — Debug-level is gated by `Log.Verbose` flag
- **String interpolation:** use `$""` for log messages, not `string.Format`

### Patterns used in this codebase

- **Static helper classes** for cross-cutting concerns: `Log`, `AppPaths`, `ConfigManager`, `AutoStartManager`, `JumpListCleaner`, `ShortcutResolver`
- **Instance classes** for stateful components: `ExclusionEngine`, `RecentFileWatcher`, `RecentFileCleaner`, `TrayApplicationContext`, `SettingsForm`
- **`IDisposable`** on classes that own `FileSystemWatcher` or `Timer`
- **Source-generated JSON** via `JsonSerializerContext` (`AppConfigJsonContext`) — do not use reflection-based `JsonSerializer` overloads
- **P/Invoke** for `SHChangeNotify` in `JumpListCleaner` — keep interop minimal and well-documented

### Patterns NOT used (intentionally)

- **No DI container** — not warranted for this app size
- **No interfaces** — classes are concrete; only add interfaces if needed for testability of external dependencies (file system, registry)
- **No `ILogger`/`ILoggerFactory`** — `Log` is a simple static class by design
- **No MVVM/data binding** — `SettingsForm` is manual WinForms layout in code (no `.Designer.cs`)
- **No `async/await`** in the current codebase (except future changes)

## What to Avoid

- **Do not add NuGet packages** unless strictly necessary. This app is intentionally lightweight (only `Microsoft.Extensions.FileSystemGlobbing` and `OpenMcdf`).
- **Do not introduce abstractions/interfaces** just for the sake of patterns. Only add them when there's a concrete need (e.g., mocking file system operations in tests).
- **Do not change the target framework** (`net8.0-windows`) or LangVersion unless explicitly asked.
- **Do not use reflection-based JSON serialization** — use the existing `AppConfigJsonContext` source generator. If you add new serializable types, add them to the context.
- **Do not add `async void`** methods. If converting to async, use `async Task` and handle exceptions.
- **Do not swallow exceptions silently** — at minimum log at `Debug` level. The codebase has some existing `catch { }` blocks that are tracked as an open issue.
- **Do not wrap calls in unnecessary try/catch** — let exceptions bubble unless you have a specific recovery strategy.
- **Do not add cloud/web/API concerns** — this is a local desktop utility.
- **Do not modify the Inno Setup installer script** (`installer.iss`) without understanding the publish output structure.

## Building & Running

```powershell
# Framework-dependent build (requires .NET 8 runtime)
dotnet build src/BlockFromRecent/BlockFromRecent.csproj

# Self-contained single-file publish
dotnet publish src/BlockFromRecent/BlockFromRecent.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Build installer (requires Inno Setup 6 on PATH)
iscc src/BlockFromRecent/installer.iss
```

The app writes its config and log to `%AppData%\BlockFromRecent\`. The executable can run from any directory.

## Testing

There is currently **no test project** (tracked as an open issue). When one is added:

- Use **xUnit** with `net8.0-windows` TFM
- Project name: `BlockFromRecent.Tests`
- Name tests by behavior: `WhenTargetMatchesPathPrefixThenIsExcludedReturnsTrue`
- Follow Arrange-Act-Assert
- Test `ExclusionEngine`, `ShortcutResolver.ResolveTargetFromBytes`, and `ConfigManager` through their public APIs
- Avoid file system I/O in tests — use the `*FromBytes` overload for `ShortcutResolver`
- Run with: `dotnet test`

## Key Domain Knowledge

### Windows Recent Files

- Location: `%AppData%\Microsoft\Windows\Recent` (`Environment.SpecialFolder.Recent`)
- Files are `.lnk` shortcuts created by Windows when the user opens any file
- The `.lnk` binary format is specified by [MS-SHLLINK](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-shllink)
- Deleting a `.lnk` from this folder removes the entry from Quick Access/Recent in File Explorer

### AutomaticDestinations (Jump Lists)

- Location: `%AppData%\Microsoft\Windows\Recent\AutomaticDestinations`
- Files are OLE Compound Documents (`.automaticDestinations-ms`)
- Each file corresponds to an application's jump list (identified by AppUserModelID hash)
- Windows 11 File Explorer populates its "Recent" view from these, independent of the `.lnk` files
- Parsed using the `OpenMcdf` library

### SHChangeNotify

- After deleting files or modifying jump lists, `SHChangeNotify(SHCNE_UPDATEDIR, ...)` tells Explorer to refresh its cached view
- Without this, deleted items may still appear in Explorer until next natural refresh

### Exclusion Rules

- **PathPrefix**: Case-insensitive prefix match on the resolved target path (backslash-normalized). Example: `\\synology\media\`
- **GlobPattern**: Uses `Microsoft.Extensions.FileSystemGlobbing.Matcher`. Simple extension patterns (`*.mp4`) match against filename only; path-based patterns match against the full path.

## Config File

Located at `%AppData%\BlockFromRecent\config.json`. Serialized with `System.Text.Json` source generation. Structure:

```json
{
  "Rules": [
    { "Pattern": "\\\\server\\share\\", "Type": "PathPrefix" },
    { "Pattern": "*.mp4", "Type": "GlobPattern" }
  ],
  "AutoStart": true,
  "ScanOnStartup": true,
  "VerboseLogging": false
}
```

If you add new properties to `AppConfig`, they must have sensible defaults so existing config files deserialize correctly without the new fields.

## Common Tasks

### Adding a new exclusion rule type

1. Add the new value to the `RuleType` enum in [AppConfig.cs](src/BlockFromRecent/Config/AppConfig.cs)
2. Add the matching logic in `ExclusionEngine.IsExcluded()` as a new `case` in the `switch`
3. Add a button/prompt for it in `SettingsForm`
4. The JSON serialization uses `UseStringEnumConverter` so new enum values are handled automatically
5. Write tests for the new matching behavior

### Adding a new config property

1. Add the property to `AppConfig` with a default value
2. Wire it in `SettingsForm` (checkbox/control + save logic)
3. Apply it in `TrayApplicationContext.OnConfigSaved` or wherever it takes effect
4. Existing config files without the new field will deserialize with the default — no migration needed

### Modifying the Settings UI

- `SettingsForm` uses manual WinForms layout (no designer). All controls are positioned with explicit `Location` and `Size`.
- DPI-aware: `AutoScaleDimensions = new SizeF(96F, 96F)` + `AutoScaleMode = AutoScaleMode.Dpi`
- When adding controls, adjust `ClientSize` and position existing controls to prevent overlap
- Test at 100%, 125%, and 150% DPI scaling
