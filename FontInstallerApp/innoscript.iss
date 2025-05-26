[Setup]
AppName=Font Installer App
AppVersion=1.0
DefaultDirName={autopf}\FontInstallerApp
DefaultGroupName=FontInstallerApp
OutputDir=dist
OutputBaseFilename=FontInstallerInstaller
Compression=lzma
SolidCompression=yes

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Font Installer App"; Filename: "{app}\FontInstallerApp.exe"
Name: "{group}\Uninstall Font Installer App"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\FontInstallerApp.exe"; Description: "Launch after install"; Flags: nowait postinstall skipifsilent
