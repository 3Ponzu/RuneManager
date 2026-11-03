[Setup]
AppName=RuneManager
AppVersion=1.0.0
DefaultDirName={autopf}\RuneManager
DefaultGroupName=RuneManager
UninstallDisplayIcon={app}\RuneManager.exe
Compression=lzma2
SolidCompression=yes
OutputDir=InstallerOutput
OutputBaseFilename=RuneManager_Setup

[Files]
Source: "src\Runemanager\bin\Release\net10.0-windows\win-x64\publish\RuneManager.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\RuneManager"; Filename: "{app}\RuneManager.exe"
Name: "{autodesktop}\RuneManager"; Filename: "{app}\RuneManager.exe"

[Run]
Filename: "{app}\RuneManager.exe"; Description: "{cm:LaunchProgram,RuneManager}"; Flags: nowait postinstall skipifsilent