; Block From Recent - Inno Setup Installer Script
; Builds a single setup EXE that installs the self-contained app

#define MyAppName "Block From Recent"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Block From Recent Contributors"
#define MyAppURL "https://github.com/sdolgin/block-from-recent"
#define MyAppExeName "BlockFromRecent.exe"

[Setup]
AppId={{B7F2D4E1-3A5C-4D8E-9F12-6E8A1B3C5D7F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\..\LICENSE
OutputDir=..\..\installer\output
OutputBaseFilename=BlockFromRecent-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
UsedUserAreasWarning=no
WizardStyle=modern
WizardImageFile=Resources\wizard-image.bmp
WizardSmallImageFile=Resources\wizard-small-image.bmp
SetupIconFile=Resources\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "autostart"; Description: "Start with Windows"; GroupDescription: "Additional options:"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Registry]
; Auto-start registry entry (only if task selected)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BlockFromRecent"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\BlockFromRecent"

[Code]
// Kill running instance before install/uninstall
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('taskkill', '/f /im ' + '{#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('taskkill', '/f /im ' + '{#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := True;
end;
