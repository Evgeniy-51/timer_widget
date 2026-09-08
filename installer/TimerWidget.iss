#define MyAppName "Timer Widget"
#define MyAppExe "TimerWidget.exe"
#define MyAppVersion "0.1"

[Setup]
AppId={{8F3C2A91-6B0E-4D7A-9C11-TIMERWIDGET01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\TimerWidget
DefaultGroupName={#MyAppName}
OutputDir=..\InstallerOutput
OutputBaseFilename=TimerWidget-Setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExe}
ShowLanguageDialog=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
english.AutoStart=Start with Windows
russian.AutoStart=Запускать с Windows

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:AutoStart}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "TimerWidget"; ValueData: """{app}\{#MyAppExe}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\TimerWidget"

[Code]
procedure FixCheckList(List: TNewCheckListBox);
begin
  List.BorderStyle := bsNone;
  List.Offset := ScaleX(8);
end;

procedure InitializeWizard;
begin
  FixCheckList(WizardForm.TasksList);
  FixCheckList(WizardForm.RunList);
end;
