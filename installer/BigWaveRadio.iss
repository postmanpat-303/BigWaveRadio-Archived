; BigWave Radio - Windows setup script (Inno Setup 6.3+/7)
; Build the self-contained release first (note: --self-contained is required;
; without it dotnet build produces a framework-dependent app that prompts
; for a .NET runtime during setup):
;   dotnet build src\BigWaveRadio.App\BigWaveRadio.App.csproj ^
;     -p:Platform=x64 -c Release -r win-x64 --self-contained
; Stage it (the build folder is the complete self-contained app; the
; publish\ subfolder inside it is excluded):
;   robocopy src\BigWaveRadio.App\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64 ^
;     installer\staging /E /XD publish
; then compile this script. Output: dist\BigWaveRadio-Setup-0.3.exe

#define AppName "BigWave Radio"
#define AppVersion "0.3"
#define AppPublisher "BigWave Radio"
#define AppExe "BigWaveRadio.App.exe"
#define AppId "594FCDA1-B446-4DD7-A168-E575F2C64FE9"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppVerName={#AppName} {#AppVersion}
DefaultDirName={autopf64}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=..\dist
OutputBaseFilename=BigWaveRadio-Setup-0.3
SetupIconFile=..\src\BigWaveRadio.App\Assets\bigwave.ico
UninstallDisplayIcon={app}\{#AppExe}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes
VersionInfoVersion=0.3.0.0
VersionInfoDescription=BigWave Radio Setup
VersionInfoProductName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "staging\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"; IconFilename: "{app}\Assets\bigwave.ico"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon; IconFilename: "{app}\Assets\bigwave.ico"

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
