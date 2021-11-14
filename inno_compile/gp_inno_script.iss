; Inno Script for Group Processor installation

#define MyAppName "CEE Group Processor"
#define MyAppPublisher "CEE Travel Systems"
#define MyAppURL "http://www.cee-systems.com/"
#define MyAppVersion GetFileVersion('.\source\GroupProcessor.exe')
#define MyGroup "CEE Applications/Group Processor"
#define SourceDir ".\source\"
#define SubDir "cs"
#define MyAppExeName "GroupProcessor.exe"
#define MyAppDestination "c:\fp\swdir\CEE_Apps\GroupProcessor"
;#define DesktopToolbar "toolbar.exe"
#define SmartpointTrigger "sp_trigger.exe"
#define MyIcon "gp.ico"
;#define ToolbarIcon "gp.bmp"
#define IniFile "gp.ini"
#define CeeGroup "CEE Applications"
#define CeeDir "c:\fp\swdir\CEE_Apps\"

[Setup]
AppId={{CEE_GROUP_PROCESSOR}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={#MyAppDestination}
DefaultGroupName={#MyGroup}
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=gp_setup_{#MyAppVersion}
SetupIconFile={#SourceDir}{#MyIcon}
Compression=lzma
SolidCompression=yes
WizardImageFile={#SourceDir}image_white.bmp
WizardSmallImageFile={#SourceDir}small.bmp
UninstallLogMode=overwrite
AlwaysShowGroupOnReadyPage=True
AlwaysShowDirOnReadyPage=True
RestartIfNeededByRun=False
DisableDirPage=auto
PrivilegesRequired=none
DisableWelcomePage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"

[CustomMessages]
;english.ToolBar=Add Group Processor %nto Galileo Desktop Toolbar?
;english.AddBar = Add Group Processor to Galileo Desktop Toolbar
;english.RemoveBar = Remove Group Processor from Galileo Desktop Toolbar
;czech.ToolBar=Pøidat Group Processor %ndo nabídkové lišty Galileo Desktopu?
;czech.AddBar = Pøidat Group Processor do nabídky Galileo Desktopu
;czech.RemoveBar = Odebrat Group Processor z nabídky Galileo Desktopu
english.Trigger=Add Group Processor %nto Smartpoint Quick Commands?
english.AddTrigger = Add Group Processor to Smartpoint Quick Commands
english.RemoveTrigger = Remove Group Processor from Smartpoint Quick Commands
czech.Trigger=Pøidat Group Processor %ndo Smartpoint Quick Commands?
czech.AddTrigger = Pøidat Group Processor do Smartpoint Quick Commands
czech.RemoveTrigger = Odebrat Group Processor ze Smartpoint Quick Commands


[Files]
Source: "{#SourceDir}{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
;Source: "{#SourceDir}{#ToolbarIcon}"; DestDir: "{app}"; Flags: ignoreversion
;Source: "{#SourceDir}{#DesktopToolbar}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}{#SubDir}\*"; DestDir: "{app}\{#SubDir}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourceDir}{#SmartpointTrigger}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
;Name: "{group}\{cm:AddBar}"; Filename: "{app}\{#DesktopToolbar}"; Parameters: "{app} -{language}"
;Name: "{group}\{cm:RemoveBar}"; Filename: "{app}\{#DesktopToolbar}"; Parameters: "{app} -{language} -u"
Name: "{group}\{cm:AddTrigger}"; Filename: "{app}\{#SmartpointTrigger}"; Parameters: "{app} -{language}"
Name: "{group}\{cm:RemoveTrigger}"; Filename: "{app}\{#SmartpointTrigger}"; Parameters: "{app} -{language} -u"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

[Run]
;Filename: "{app}\{#DesktopToolbar}"; Parameters: "{app} -{language} -silent"; Description: "{cm:ToolBar,{#StringChange("Set Toolbar", '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Check: NotUpgrade
Filename: "{app}\{#SmartpointTrigger}"; Parameters: "{app} -{language} -silent"; Description: "{cm:Trigger,{#StringChange("Set Trigger", '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Check: NotUpgrade

[UninstallRun]
;Filename: "{app}\{#DesktopToolbar}"; Parameters: "-u -{language} -silent";
Filename: "{app}\{#SmartpointTrigger}"; Parameters: "-u -{language} -silent";

[UninstallDelete]
Type: files; Name: "{app}\{#IniFile}";
Type: dirifempty; Name: "{app}\cs\"; 
Type: dirifempty; Name: "{app}\"; 
Type: files; Name: "{userappdata}\GroupProcessor\{#IniFile}";
Type: dirifempty; Name: "{userappdata}\GroupProcessor\"; 
Type: dirifempty; Name: "{commonprograms}\{#CeeGroup}"; 
Type: dirifempty; Name: "{userprograms}\{#CeeGroup}";  
Type: dirifempty; Name: "{#CeeDir}";

[Code]

var
  AlreadyInstalled: Boolean;

procedure InitializeWizard(); 
  begin 
    AlreadyInstalled := false;
  end;

function GetInstalledVersion(): String;
  var
    UnInstPath: String;
  begin
    Result := '';
    UnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
    RegQueryStringValue(HKLM, UnInstPath, 'DisplayVersion', Result)
  end;

function NextButtonClick(PageId: Integer): Boolean;
  var
    Version: String;
    Message: String;
  begin
    Result := True;
    if (PageId = wpWelcome) then
    begin
      Version := GetInstalledVersion();
      if Version <> '' then
      begin
        AlreadyInstalled := true;
        if (ExpandConstant('{language}') = 'czech') then
          Message := 'Nalezena nainstalovaná verze ' + Version + #13#10#13#10 'Nahradit novou verzi ' + ExpandConstant('{#MyAppVersion}') + '?'
        else
          Message := 'Installed version ' + Version + ' found.' #13#10#13#10 'Replace with new version ' + ExpandConstant('{#MyAppVersion}') + '?';
        if MsgBox(Message, mbConfirmation, MB_YESNO) = IDNO then
        begin
          WizardForm.Close;
          exit;
        end;    
      end;
    end;
  end;
   
function NotUpgrade(): Boolean;
  begin
    Result := not(AlreadyInstalled);
  end;       
 
