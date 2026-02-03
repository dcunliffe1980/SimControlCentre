; SimControlCentre SELF-CONTAINED Installer Script
; Includes .NET 8 runtime (larger file, but no dependencies)

#define MyAppName "SimControlCentre"
#define MyAppVersion "1.2.0"
#define MyAppPublisher "Dave Cunliffe"
#define MyAppURL "https://github.com/dcunliffe1980/SimControlCentre"
#define MyAppExeName "SimControlCentre.exe"

[Setup]
AppId={{8A7D9B2C-3E4F-5A6B-7C8D-9E0F1A2B3C4D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE.txt
OutputDir=Installers
OutputBaseFilename=SimControlCentre-Setup-Standalone-v{#MyAppVersion}
;SetupIconFile=SimControlCentre\Resources\icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start with Windows"; GroupDescription: "Startup Options:"; Flags: unchecked

[Files]
; Single self-contained executable (includes .NET runtime)
Source: "SimControlCentre\bin\Release\net8.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "QUICKSTART.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Quick Start Guide"; Filename: "{app}\QUICKSTART.md"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  ConfigBackupPath: String;
  ShouldRestoreConfig: Boolean;

function InitializeSetup: Boolean;
var
  ConfigPath: String;
  Answer: Integer;
begin
  Result := True;
  ShouldRestoreConfig := False;
  
  // Check if config file exists
  ConfigPath := ExpandConstant('{localappdata}\SimControlCentre\appsettings.json');
  
  if FileExists(ConfigPath) then
  begin
    Answer := MsgBox('An existing configuration file was found.' + #13#10 + #13#10 + 
                     'Do you want to keep your current settings?' + #13#10 + #13#10 + 
                     'Yes = Keep your hotkeys, channels, and app settings' + #13#10 + 
                     'No = Start fresh with default settings', 
                     mbConfirmation, MB_YESNO);
    
    if Answer = IDYES then
    begin
      // Backup config file
      ConfigBackupPath := ExpandConstant('{tmp}\appsettings.json.backup');
      FileCopy(ConfigPath, ConfigBackupPath, False);
      ShouldRestoreConfig := True;
    end;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigPath: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Restore config if user wanted to keep it
    if ShouldRestoreConfig then
    begin
      ConfigPath := ExpandConstant('{localappdata}\SimControlCentre\appsettings.json');
      
      // Create directory if it doesn't exist
      ForceDirectories(ExtractFilePath(ConfigPath));
      
      // Restore backup
      FileCopy(ConfigBackupPath, ConfigPath, False);
    end;
  end;
end;

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\SimControlCentre"

