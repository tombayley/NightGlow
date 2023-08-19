#define AppName "Night Glow"
#define AppExe "NightGlow.exe"
#define AppVersion "1.1.0"

[Setup]
AppId={{8B87A5F9-0C03-4B92-9080-DDCF75DC18A4}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher="Tom Bayley"
AppPublisherURL="https://github.com/tombayley/NightGlow"
AppSupportURL="https://github.com/tombayley/NightGlow/issues"
AppUpdatesURL="https://github.com/tombayley/NightGlow/releases"
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
SetupIconFile=..\NightGlow\Assets\app-icon.ico
OutputDir=bin\
OutputBaseFilename=NightGlow-Installer
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern

[Files]
Source: "Source\*"; DestDir: "{app}";

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{group}\{#AppName} on Github"; Filename: "https://github.com/tombayley/NightGlow"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NightGlow"; ValueData: "{app}\{#AppExe}"
Root: HKLM; Subkey: "Software\Microsoft\Windows NT\CurrentVersion\ICM"; ValueType: dword; ValueName: "GdiICMGammaRange"; ValueData: "256"

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Name: "{userappdata}\NightGlow"; Type: filesandordirs
