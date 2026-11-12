; -- Application Information --
#define MyAppName "CreationStudio2026"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Shreyansh Singh"
#define MyAppExeName "CreationStudio2026.exe"

; -- Exact Project & Publish Paths --
#define ProjectRoot "C:\Users\shreyansh singh\Downloads\CreationStudio2026-CubeLogo-ColorExplorer-Terminal\CreationStudio2026"
#define PublishDir ProjectRoot + "\publish_output"

[Setup]
; Unique identifier for CreationStudio2026
AppId={{8F3A9C21-7D4B-4E12-9A6C-1B5E8D2F4C90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

; Uses your custom cube logo icon for the installer wizard and Add/Remove Programs
SetupIconFile={#ProjectRoot}\Assets\CreationStudio.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; Output settings
OutputDir={#ProjectRoot}\InstallerOutput
OutputBaseFilename=CreationStudio2026_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copies your main executable and all published dependencies
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu shortcut with your custom icon
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
; Desktop shortcut with your custom icon
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
