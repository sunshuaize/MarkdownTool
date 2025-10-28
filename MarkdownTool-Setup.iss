; Inno Setup 脚本 for MarkdownTool
; 编译此脚本需要安装 Inno Setup 6.0 或更高版本
; 下载地址: https://jrsoftware.org/isdl.php

#define MyAppName "Markdown 编辑器"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "sunshuaize"
#define MyAppURL "https://github.com/sunshuaize/MarkdownTool"
#define MyAppExeName "MarkdownTool.exe"
#define MyAppDescription "一个基于 WPF 的现代化 Markdown 编辑器"

[Setup]
; 应用程序基本信息
AppId={{8B3C9D4E-5F6A-4B2C-9E8D-1A2B3C4D5E6F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppComments={#MyAppDescription}

; 安装路径
DefaultDirName={autopf}\MarkdownTool
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; 许可协议（如果需要可以取消注释）
; LicenseFile=LICENSE.txt

; 输出设置
OutputDir=installer
OutputBaseFilename=MarkdownTool-Setup-v{#MyAppVersion}
SetupIconFile=MarkdownTool\logo\logo.ico

; 压缩设置
Compression=lzma2/max
SolidCompression=yes

; 64位应用程序
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Windows 版本要求
MinVersion=10.0.17763

; 权限
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; UI 设置
WizardStyle=modern
DisableWelcomePage=no

; 卸载设置
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式(&D)"; GroupDescription: "附加图标:"
Name: "quicklaunchicon"; Description: "创建快速启动栏快捷方式(&Q)"; GroupDescription: "附加图标:"; Flags: unchecked

[Files]
; 主程序文件 - 从发布目录复制所有文件
Source: "MarkdownTool\bin\Release\net8.0-windows\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; 注意：你也可以添加额外的文件，如文档、示例等
; Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
; Source: "example.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; 开始菜单图标
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"

; 桌面图标
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; 快速启动图标
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
; 安装完成后运行选项
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Registry]
; 文件关联 - 关联 .md 和 .markdown 文件
Root: HKCU; Subkey: "Software\Classes\.md"; ValueType: string; ValueName: ""; ValueData: "MarkdownTool.Document"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\.markdown"; ValueType: string; ValueName: ""; ValueData: "MarkdownTool.Document"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\MarkdownTool.Document"; ValueType: string; ValueName: ""; ValueData: "Markdown 文档"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\MarkdownTool.Document\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Classes\MarkdownTool.Document\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; 添加到 Windows 上下文菜单（右键菜单）
Root: HKCU; Subkey: "Software\Classes\*\shell\OpenWithMarkdownTool"; ValueType: string; ValueName: ""; ValueData: "使用 Markdown 编辑器打开"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\*\shell\OpenWithMarkdownTool"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Classes\*\shell\OpenWithMarkdownTool\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Code]
// 自定义安装前检查
function InitializeSetup(): Boolean;
begin
  Result := True;
  // 可以在这里添加自定义的安装前检查逻辑
end;

// 自定义卸载前操作
function InitializeUninstall(): Boolean;
begin
  Result := True;
  // 可以在这里添加卸载前的提示或清理逻辑
end;

[UninstallDelete]
// 卸载时删除用户数据（可选）
Type: filesandordirs; Name: "{userappdata}\MarkdownTool"

[Messages]
; 自定义消息
chinesesimplified.WelcomeLabel1=欢迎使用 [name] 安装向导
chinesesimplified.WelcomeLabel2=这将在您的电脑上安装 [name/ver]。%n%n建议您在继续之前关闭所有其他应用程序。
chinesesimplified.FinishedLabel=安装程序已在您的电脑上成功安装了 [name]。

english.WelcomeLabel1=Welcome to [name] Setup Wizard
english.WelcomeLabel2=This will install [name/ver] on your computer.%n%nIt is recommended that you close all other applications before continuing.

