; SimControlCentre Installer Script for Inno Setup
; https://jrsoftware.org/isinfo.php

#define MyAppName "SimControlCentre"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Dave Cunliffe"
#define MyAppURL "https://github.com/dcunliffe1980/SimControlCentre"
#define MyAppExeName "SimControlCentre.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{8A7D9B2C-3E4F-5A6B-7C8D-9E0F1A2B3C4D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE.txt
OutputDir=Installers
OutputBaseFilename=SimControlCentre-Setup-v{#MyAppVersion}
;SetupIconFile=SimControlCentre\Resources\icon.ico
Compression=lzma2
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
Source: "SimControlCentre\bin\Release\Publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "SimControlCentre\bin\Release\Publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SimControlCentre\bin\Release\Publish\*.json"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "QUICKSTART.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Quick Start Guide"; Filename: "{app}\QUICKSTART.md"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Start with Windows if user selected the option
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Check if .NET 8 Desktop Runtime is installed
function IsDotNet8Installed: Boolean;
var
  ResultCode: Integer;
begin
  // Check if dotnet command exists and can find .NET 8
  Result := Exec('cmd.exe', '/c dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 8"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  if not IsDotNet8Installed then
  begin
    if MsgBox('.NET 8 Desktop Runtime is required but not installed.' + #13#10 + #13#10 + 
              'Would you like to download and install it now?' + #13#10 + #13#10 +
              '(The installer will open the download page in your browser)', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0/runtime', '', '', SW_SHOW, ewNoWait, ErrorCode);
      MsgBox('Please install .NET 8 Desktop Runtime, then run this installer again.', mbInformation, MB_OK);
      Result := False;
    end
    else
    begin
      Result := False;
    end;
  end
  else
  begin
    Result := True;
  end;
end;

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\SimControlCentre"
