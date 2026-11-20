[Setup]
AppId={{FD01271C-30B2-4A69-93A2-5F18AFA22D71}
AppName=СИ — Домашняя игра
AppVersion=1.5.2
AppPublisher=SI LAN community build
DefaultDirName={autopf}\SI-LAN
DefaultGroupName=СИ — Домашняя игра
OutputDir=outputs
OutputBaseFilename=SI-LAN-Setup
SetupIconFile=SI-master\src\SIGame\SIGame\game.ico
UninstallDisplayIcon={app}\SIGame.exe
Compression=lzma2/fast
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
WizardStyle=modern
DisableProgramGroupPage=yes
LicenseFile=SI-master\LICENSE

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"
Name: "lan"; Description: "Разрешить игре входящие подключения в домашней сети"

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "log\*,*.pdb"
Source: "MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\СИ — Домашняя игра"; Filename: "{app}\SIGame.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\СИ — Домашняя игра"; Filename: "{app}\SIGame.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; Parameters: "/silent /install"; StatusMsg: "Подготовка компонента мультимедиа…"; Flags: waituntilterminated
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""SI-LAN Local Game"""; Flags: runhidden; Tasks: lan
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""SI-LAN Local Game"" dir=in action=allow program=""{app}\SIGame.exe"" protocol=TCP localport=6999,7000 profile=private remoteip=localsubnet"; Flags: runhidden; Tasks: lan
Filename: "{app}\SIGame.exe"; Description: "Запустить игру"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""SI-LAN Local Game"""; Flags: runhidden
