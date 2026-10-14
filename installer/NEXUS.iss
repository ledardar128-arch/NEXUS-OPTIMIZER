#define AppName "NEXUS OPTIMIZER"
#define AppVersion "1.0.0"
#define AppExe "NEXUS.Optimizer.exe"

[Setup]
AppId={{B94D7A7C-3D36-4F56-8E77-1B4F2D38B922}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=NEXUS
DefaultDirName={autopf}\NEXUS OPTIMIZER
DefaultGroupName=NEXUS OPTIMIZER
OutputDir=..\artifacts
OutputBaseFilename=NEXUS_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayName={#AppName}
SetupLogging=yes

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le Bureau"; GroupDescription: "Raccourcis :"; Flags: checkedonce

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\NEXUS OPTIMIZER"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\NEXUS OPTIMIZER"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Lancer NEXUS OPTIMIZER"; Flags: postinstall nowait skipifsilent
