# Block From Recent

A lightweight Windows system tray utility that automatically removes unwanted files from **Windows Recent Files** (`%AppData%\Microsoft\Windows\Recent`) based on user-defined exclusion rules.

## Features

- 🔍 **Real-time monitoring** — Uses `FileSystemWatcher` to detect new entries instantly
- 🧹 **Startup scan** — Cleans existing Recent files on launch
- 📁 **Path prefix rules** — Exclude entire folders (e.g., `\\server\share\`, `D:\Private\`)
- 🌐 **Glob pattern rules** — Exclude by pattern (e.g., `*.mp4`, `**\temp\*`)
- 🖥️ **System tray app** — Runs quietly in the background
- ⚙️ **Simple settings UI** — Add, edit, remove rules with a few clicks
- 🚀 **Auto-start with Windows** — Optional startup registration via Registry
- 🔒 **Single instance** — Prevents duplicate processes
- 📦 **Portable** — Single EXE, no installer required

## Installation

### Option 1: Installer (Recommended)
1. Download `BlockFromRecent-Setup-x.x.x.exe` from [Releases](https://github.com/sdolgin/block-from-recent/releases)
2. Run the installer — choose install location and options
3. Optionally check **Start with Windows** during setup
4. The app launches automatically after install

To uninstall: use **Add or Remove Programs** in Windows Settings, or run the uninstaller from the Start Menu.

### Option 2: Portable EXE
1. Download `BlockFromRecent.exe` from [Releases](https://github.com/sdolgin/block-from-recent/releases)
2. Place it in a permanent folder (e.g., `C:\Tools\BlockFromRecent\`)
3. Run it — a shield icon appears in the system tray

### Option 3: Build from Source
```bash
# Clone the repository
git clone https://github.com/sdolgin/block-from-recent.git
cd block-from-recent

# Build (framework-dependent — requires .NET 8 runtime)
dotnet publish src/BlockFromRecent/BlockFromRecent.csproj -c Release

# Or build self-contained single file (no .NET runtime required)
dotnet publish src/BlockFromRecent/BlockFromRecent.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Build the installer (requires Inno Setup 6)
iscc src/BlockFromRecent/installer.iss
```

## Usage

1. **Launch** the app — it runs in the system tray (shield icon)
2. **Double-click** the tray icon or right-click → **Settings**
3. **Add exclusion rules**:
   - **Path Prefix**: Any file whose path starts with this prefix is removed from Recent  
     Example: `\\synology\media\` removes all files opened from that NAS share
   - **Glob Pattern**: Wildcard matching on file paths  
     Example: `*.mp4` removes all MP4 files from Recent
4. Click **Save**
5. *Optional*: Check **Start with Windows** to auto-launch

### Testing Rules
Click **🔍 Test Rules** in Settings to see how many existing Recent files would match your current rules — without deleting anything.

### Manual Scan
Right-click the tray icon → **Run Scan Now** to immediately clean Recent files matching your rules.

## Configuration

Settings are stored in `%AppData%\BlockFromRecent\config.json`:

```json
{
  "Rules": [
    {
      "Pattern": "\\\\synology\\media\\",
      "Type": "PathPrefix"
    },
    {
      "Pattern": "*.mp4",
      "Type": "GlobPattern"
    }
  ],
  "AutoStart": true,
  "ScanOnStartup": true
}
```

You can edit this file manually if you prefer.

## Requirements

- Windows 10/11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (for framework-dependent build)
- Or use the self-contained build (no runtime needed, larger file)

## How It Works

1. A `FileSystemWatcher` monitors `%AppData%\Microsoft\Windows\Recent` for new `.lnk` files
2. When a new shortcut appears, the app parses the `.lnk` binary format (MS-SHLLINK) to read the target path
3. The target path is checked against all exclusion rules (path prefix + glob matching)
4. If it matches any rule, the `.lnk` file is silently deleted

## License

[MIT](LICENSE)
