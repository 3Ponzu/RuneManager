[Setup]
AppName=RuneManager
AppVersion=1.0.0
AppPublisher=iitak
DefaultDirName={autopf}\RuneManager
DefaultGroupName=RuneManager
UninstallDisplayIcon={app}\RuneManager.exe
OutputBaseFilename=RuneManager_Setup
Compression=lzma2/max
SolidCompression=yes
SetupIconFile=app.ico
WizardStyle=modern

[Files]
; ビルドされた publish フォルダ内のすべてのファイルを同梱対象にします
Source: "bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; スタートメニューとデスクトップにショートカットを作成します
Name: "{group}\RuneManager"; Filename: "{app}\RuneManager.exe"
Name: "{autodesktop}\RuneManager"; Filename: "{app}\RuneManager.exe"

[Run]
; インストール完了画面で「RuneManagerを起動する」チェックボックスを表示します
Filename: "{app}\RuneManager.exe"; Description: "{cm:LaunchProgram,RuneManager}"; Flags: nowait postinstall skipifsilent