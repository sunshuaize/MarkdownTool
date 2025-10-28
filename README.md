# Markdown 编辑器

一个基于 WPF 的现代化 Markdown 编辑器,提供实时预览、文件管理和丰富的编辑功能。

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## ✨ 功能特性

### 核心功能
- **📝 实时预览** - 支持编辑器与预览的同步显示,所见即所得
- **📁 文件管理** - 树形视图浏览文件夹,支持无限层级子目录
- **🎯 智能目录** - 自动生成目录结构,点击快速定位到对应标题
- **🎨 现代化UI** - 基于 WPF UI 的 Fluent Design 设计风格

### 编辑功能
- **标题插入** - 支持 H1-H6 六级标题
- **文本格式** - 粗体、斜体、删除线
- **列表支持** - 无序列表、有序列表、任务列表
- **丰富插入** - 链接、图片、代码块、引用、表格、分隔线
- **快捷键** - Ctrl+B (粗体)、Ctrl+I (斜体) 等常用快捷键
- **代码高亮** - 支持多种编程语言的代码块语法高亮

### 视图管理
- **灵活布局** - 支持双栏(编辑+预览)、仅编辑器、仅预览三种模式
- **侧边栏** - 可隐藏/显示的文件浏览器
- **目录面板** - 可调整宽度的智能目录导航
- **窗口状态** - 自适应最大化,避免内容溢出

## 🖼️ 界面预览

### 主界面
- 顶部菜单栏提供完整的文件操作
- 精简的工具栏,功能按类别分组
- 左侧可选的文件浏览器
- 中央编辑区域
- 右侧实时预览(带目录)
- 底部状态栏显示光标位置和字数统计

### 三种视图模式
1. **双栏模式** - 编辑器和预览并排显示
2. **编辑器模式** - 专注写作,隐藏预览
3. **预览模式** - 专注阅读,显示目录导航

## 🚀 快速开始

### 环境要求
- Windows 10/11
- .NET 8.0 SDK 或更高版本
- WebView2 运行时 (Windows 11 自带)

### 安装步骤

1. **克隆仓库**
```bash
git clone https://github.com/yourusername/MarkdownTool.git
cd MarkdownTool
```

2. **还原依赖**
```bash
dotnet restore
```

3. **编译项目**
```bash
dotnet build
```

4. **运行程序**
```bash
dotnet run --project MarkdownTool
```

或直接在 Visual Studio 2022 中打开 `MarkdownTool.sln` 并运行。

## 📖 使用指南

### 基本操作

**文件管理**
- `文件 -> 新建` - 创建新文档
- `文件 -> 打开` - 打开单个 Markdown 文件
- `文件 -> 打开文件夹` - 在侧边栏浏览整个文件夹
- `文件 -> 保存` / `另存为` - 保存文档

**编辑功能**
- 点击工具栏按钮,选择相应的 Markdown 语法插入
- 使用快捷键快速格式化文本:
  - `Ctrl+B` - 粗体
  - `Ctrl+I` - 斜体

**视图切换**
- 点击状态栏的"切换视图"按钮,在三种模式间切换
- 点击"切换侧边栏"显示/隐藏文件浏览器
- 在预览模式下,点击"切换目录"显示/隐藏导航

**目录导航**
- 自动解析文档中的标题生成目录
- 单击目录项即可跳转到对应位置
- 目标标题会高亮显示 1 秒

### 高级功能

**表格插入**
- 选择"插入 -> 表格"
- 在对话框中输入行数(1-20)和列数(1-10)
- 自动生成表格框架

**代码块**
- 选择文本后点击"插入 -> 代码"插入行内代码
- 选择多行后点击"插入 -> 代码"可指定语言并生成代码块

**文件树过滤**
- 文件浏览器只显示 Markdown 文件(`.md`, `.markdown`)
- 自动过滤隐藏和系统文件夹
- 支持无限层级子目录

## 🛠️ 技术栈

### 核心技术
- **[WPF](https://docs.microsoft.com/zh-cn/dotnet/desktop/wpf/)** - .NET 8.0 桌面应用框架
- **[WPF UI](https://wpfui.lepo.co/)** - 现代化 Fluent Design UI 库
- **[Markdig](https://github.com/xoofx/markdig)** - 高性能 Markdown 解析引擎
- **[WebView2](https://developer.microsoft.com/zh-cn/microsoft-edge/webview2/)** - 基于 Chromium 的 Web 渲染控件

### 项目结构
```
MarkdownTool/
├── MarkdownTool/
│   ├── App.xaml                  # 应用程序定义
│   ├── App.xaml.cs               # 应用程序逻辑
│   ├── MainWindow.xaml           # 主窗口UI
│   ├── MainWindow.xaml.cs        # 主窗口逻辑
│   ├── FileTreeItem.cs           # 文件树数据模型
│   ├── BoolToIconConverter.cs   # 图标转换器
│   └── MarkdownTool.csproj       # 项目文件
├── MarkdownTool.sln              # 解决方案文件
└── README.md                     # 项目文档
```

## 🎯 核心实现

### Markdown 渲染
使用 Markdig 将 Markdown 转换为 HTML,通过 WebView2 渲染:
```csharp
var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UseAutoIdentifiers()
    .Build();
var html = Markdown.ToHtml(markdown, pipeline);
```

### 目录生成
通过正则表达式从 HTML 中提取标题及其 ID:
```csharp
var pattern = @"<h([1-6])(?:\s+id=""([^""]*)"")?\s*>([^<]+)</h[1-6]>";
// 提取标题层级、ID 和文本,构建树形目录
```

### 响应式布局
使用 Grid 和 GridSplitter 实现可调整的三栏布局:
- 左侧: 文件浏览器(可隐藏)
- 中间: 编辑器/预览(可切换)
- 右侧目录: 仅在预览模式显示(可隐藏)

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request!

### 开发流程
1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

### 代码规范
- 遵循 C# 编码规范
- 为新功能添加注释
- 保持代码整洁,功能模块化

## 📝 待办事项

- [ ] 支持更多主题切换(暗色模式)
- [ ] 导出为 PDF/HTML
- [ ] Markdown 语法高亮编辑器
- [ ] 图片粘贴自动保存
- [ ] Git 集成
- [ ] 多标签页支持
- [ ] 自定义 CSS 样式
- [ ] 插件系统

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🙏 致谢

- [WPF UI](https://wpfui.lepo.co/) - 提供优秀的 UI 组件库
- [Markdig](https://github.com/xoofx/markdig) - 强大的 Markdown 解析器
- [WebView2](https://developer.microsoft.com/zh-cn/microsoft-edge/webview2/) - 现代化的 Web 渲染引擎

## 📧 联系方式

如有问题或建议,欢迎通过以下方式联系:

- 提交 [Issue](https://github.com/yourusername/MarkdownTool/issues)
- 发送邮件至 your.email@example.com

---

⭐ 如果这个项目对你有帮助,请给个 Star!
