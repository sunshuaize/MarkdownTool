using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Markdig;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;
using MessageBoxImage = System.Windows.MessageBoxImage;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TreeViewItem = System.Windows.Controls.TreeViewItem;

namespace MarkdownTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        private string? currentFilePath = null;
        private bool isContentChanged = false;
        private readonly MarkdownPipeline markdownPipeline;
        private System.Windows.Threading.DispatcherTimer? updateTimer;
        private string? currentFolderPath = null;
        private ObservableCollection<FileTreeItem> fileTree = new ObservableCollection<FileTreeItem>();
        private bool isEditorVisible = false;
        private bool isPreviewVisible = true;
        private bool isSidebarVisible = false;
        private int viewMode = 2; // 0: 双栏, 1: 仅编辑器, 2: 仅预览
        private bool isClosingHandled = false; // 防止重复提示
        private bool isTocVisible = true; // 目录默认显示

        public MainWindow()
        {
            InitializeComponent();
            
            // 订阅窗口状态改变事件
            this.StateChanged += MainWindow_StateChanged;
            
            // 订阅窗口关闭事件
            this.Closing += MainWindow_Closing;
            
            // 初始化 Markdown 管道，支持高级特性
            markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseAutoIdentifiers() // 为标题自动生成ID
                .Build();

            // 初始化 WebView2
            InitializeWebView();
            
            // 设置键盘快捷键
            SetupKeyboardShortcuts();
            
            // 初始化更新计时器（防抖）
            updateTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            updateTimer.Tick += UpdateTimer_Tick;

            // 初始化文件树
            FileTreeView.ItemsSource = fileTree;

            // 设置侧边栏默认隐藏
            UpdateSidebarVisibility();

            // 设置默认视图为仅预览
            UpdateViewVisibility();

            // 设置初始内容为空
            MarkdownTextBox.Text = "";
        }

        private async void InitializeWebView()
        {
            try
            {
                await PreviewWebView.EnsureCoreWebView2Async(null);
                UpdatePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化 WebView2 失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupKeyboardShortcuts()
        {
            // Ctrl+N - 新建
            InputBindings.Add(new KeyBinding(
                new RelayCommand(NewFile_Click),
                new KeyGesture(Key.N, ModifierKeys.Control)));

            // Ctrl+O - 打开
            InputBindings.Add(new KeyBinding(
                new RelayCommand(OpenFile_Click),
                new KeyGesture(Key.O, ModifierKeys.Control)));

            // Ctrl+S - 保存
            InputBindings.Add(new KeyBinding(
                new RelayCommand(SaveFile_Click),
                new KeyGesture(Key.S, ModifierKeys.Control)));

            // Ctrl+Shift+S - 另存为
            InputBindings.Add(new KeyBinding(
                new RelayCommand(SaveAsFile_Click),
                new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)));

            // Ctrl+Shift+O - 打开文件夹
            InputBindings.Add(new KeyBinding(
                new RelayCommand(OpenFolder_Click),
                new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift)));

            // Ctrl+B - 粗体
            InputBindings.Add(new KeyBinding(
                new RelayCommand(InsertBold_Click),
                new KeyGesture(Key.B, ModifierKeys.Control)));

            // Ctrl+I - 斜体
            InputBindings.Add(new KeyBinding(
                new RelayCommand(InsertItalic_Click),
                new KeyGesture(Key.I, ModifierKeys.Control)));
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            updateTimer?.Stop();
            UpdatePreview();
        }

        private void MarkdownTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isContentChanged = true;
            UpdateStatusBar();
            
            // 使用防抖更新预览
            updateTimer?.Stop();
            updateTimer?.Start();
        }

        private void MarkdownTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateCursorPosition();
        }

        private void UpdatePreview()
        {
            if (PreviewWebView?.CoreWebView2 == null) return;

            try
            {
                string markdown = MarkdownTextBox.Text;
                // Markdig会自动为标题添加ID(因为使用了UseAutoIdentifiers)
                string html = Markdown.ToHtml(markdown, markdownPipeline);
                
                string fullHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 900px;
            margin: 0 auto;
            padding: 20px;
            background-color: #ffffff;
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
            color: #1a1a1a;
        }}
        h1 {{
            font-size: 2em;
            border-bottom: 2px solid #eaecef;
            padding-bottom: 0.3em;
        }}
        h2 {{
            font-size: 1.5em;
            border-bottom: 1px solid #eaecef;
            padding-bottom: 0.3em;
        }}
        h3 {{ font-size: 1.25em; }}
        h4 {{ font-size: 1em; }}
        h5 {{ font-size: 0.875em; }}
        h6 {{ font-size: 0.85em; color: #6a737d; }}
        
        p {{
            margin-bottom: 16px;
        }}
        
        a {{
            color: #0366d6;
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        
        code {{
            background-color: #f6f8fa;
            border-radius: 3px;
            padding: 0.2em 0.4em;
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
            font-size: 85%;
            color: #e83e8c;
        }}
        
        pre {{
            background-color: #f6f8fa;
            border-radius: 6px;
            padding: 16px;
            overflow: auto;
            line-height: 1.45;
        }}
        
        pre code {{
            background-color: transparent;
            padding: 0;
            color: #333;
            font-size: 100%;
        }}
        
        blockquote {{
            border-left: 4px solid #dfe2e5;
            color: #6a737d;
            padding: 0 15px;
            margin: 0 0 16px 0;
        }}
        
        ul, ol {{
            padding-left: 2em;
            margin-bottom: 16px;
        }}
        
        li {{
            margin-bottom: 0.25em;
        }}
        
        table {{
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 16px;
        }}
        
        table th, table td {{
            border: 1px solid #dfe2e5;
            padding: 6px 13px;
        }}
        
        table th {{
            background-color: #f6f8fa;
            font-weight: 600;
        }}
        
        table tr:nth-child(even) {{
            background-color: #f6f8fa;
        }}
        
        hr {{
            border: 0;
            border-top: 2px solid #eaecef;
            margin: 24px 0;
        }}
        
        img {{
            max-width: 100%;
            height: auto;
        }}
        
        del {{
            color: #6a737d;
        }}
    </style>
</head>
<body>
    {html}
</body>
</html>";

                PreviewWebView.NavigateToString(fullHtml);
                
                // 更新目录
                UpdateTableOfContents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"渲染预览失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string AddHeadingIds(string html)
        {
            int headingCount = 0;
            // 匹配 <h1>, <h2> 等,可能带有属性或直接闭合
            var result = System.Text.RegularExpressions.Regex.Replace(
                html,
                @"<h([1-6])(\s+[^>]*)?>",
                match =>
                {
                    var level = match.Groups[1].Value;
                    var attributes = match.Groups[2].Value;
                    var id = $"heading-{headingCount++}";
                    
                    // 如果已经有id属性,就不添加了
                    if (attributes.Contains("id="))
                    {
                        return match.Value;
                    }
                    
                    var result = $"<h{level} id=\"{id}\"{attributes}>";
                    System.Diagnostics.Debug.WriteLine($"Added ID: {result}");
                    return result;
                }
            );
            
            // 调试:输出处理后的HTML片段
            var headings = System.Text.RegularExpressions.Regex.Matches(result, @"<h[1-6][^>]*>.*?</h[1-6]>");
            System.Diagnostics.Debug.WriteLine($"Total headings found: {headings.Count}");
            foreach (System.Text.RegularExpressions.Match h in headings)
            {
                System.Diagnostics.Debug.WriteLine($"Heading: {h.Value}");
            }
            
            return result;
        }

        private void UpdateStatusBar()
        {
            // 更新字数统计
            int wordCount = MarkdownTextBox.Text.Length;
            int lineCount = MarkdownTextBox.LineCount;
            WordCountText.Text = $"字数: {wordCount} | 行数: {lineCount}";
        }

        private void UpdateCursorPosition()
        {
            int line = MarkdownTextBox.GetLineIndexFromCharacterIndex(MarkdownTextBox.CaretIndex) + 1;
            int column = MarkdownTextBox.CaretIndex - MarkdownTextBox.GetCharacterIndexFromLineIndex(line - 1) + 1;
            LineColumnText.Text = $"行: {line}, 列: {column}";
        }

        // 文件操作
        private void NewFile_Click(object? sender = null, RoutedEventArgs? e = null)
        {
            if (isContentChanged)
            {
                var result = ShowModernMessageBox(
                    "未保存的更改",
                    "当前文档尚未保存，是否保存？",
                    MessageBoxButton.YesNoCancel
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    SaveFile_Click();
                    if (isContentChanged) return; // 用户取消了保存
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            MarkdownTextBox.Text = "";
            currentFilePath = null;
            isContentChanged = false;
            Title = "Markdown 编辑器 - 新建文档";
        }

        private void OpenFile_Click(object? sender = null, RoutedEventArgs? e = null)
        {
            if (isContentChanged)
            {
                var result = ShowModernMessageBox(
                    "未保存的更改",
                    "当前文档尚未保存，是否保存？",
                    MessageBoxButton.YesNoCancel
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    SaveFile_Click();
                    if (isContentChanged) return;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Markdown 文件 (*.md;*.markdown)|*.md;*.markdown|所有文件 (*.*)|*.*",
                Title = "打开 Markdown 文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    currentFilePath = openFileDialog.FileName;
                    MarkdownTextBox.Text = File.ReadAllText(currentFilePath, Encoding.UTF8);
                    isContentChanged = false;
                    Title = $"Markdown 编辑器 - {Path.GetFileName(currentFilePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件失败: {ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveFile_Click(object? sender = null, RoutedEventArgs? e = null)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveAsFile_Click();
            }
            else
            {
                SaveToFile(currentFilePath);
            }
        }

        private void SaveAsFile_Click(object? sender = null, RoutedEventArgs? e = null)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Markdown 文件 (*.md)|*.md|所有文件 (*.*)|*.*",
                DefaultExt = ".md",
                Title = "保存 Markdown 文件"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                currentFilePath = saveFileDialog.FileName;
                SaveToFile(currentFilePath);
            }
        }

        private void SaveToFile(string filePath)
        {
            try
            {
                File.WriteAllText(filePath, MarkdownTextBox.Text, Encoding.UTF8);
                isContentChanged = false;
                Title = $"Markdown 编辑器 - {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            // 最大化时添加边距,避免内容被窗口边框遮挡
            if (this.WindowState == WindowState.Maximized)
            {
                RootGrid.Margin = new Thickness(7);
            }
            else
            {
                RootGrid.Margin = new Thickness(0);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isClosingHandled)
            {
                return;
            }

            if (isContentChanged)
            {
                isClosingHandled = true;
                var result = ShowModernMessageBox(
                    "未保存的更改",
                    "文档已修改但未保存，是否保存？",
                    MessageBoxButton.YesNoCancel
                );

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile_Click(null, null);
                    // 如果用户取消了保存对话框，阻止关闭
                    if (isContentChanged)
                    {
                        e.Cancel = true;
                        isClosingHandled = false;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    isClosingHandled = false;
                }
                // No: 直接关闭，不保存
            }
        }

        private async System.Threading.Tasks.Task<MessageBoxResult> ShowModernMessageBoxAsync(string title, string message, MessageBoxButton buttons)
        {
            var dialog = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
                Width = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            // 根据按钮类型设置
            switch (buttons)
            {
                case MessageBoxButton.YesNoCancel:
                    dialog.PrimaryButtonText = "是";
                    dialog.SecondaryButtonText = "否";
                    dialog.CloseButtonText = "取消";
                    break;
                case MessageBoxButton.YesNo:
                    dialog.PrimaryButtonText = "是";
                    dialog.SecondaryButtonText = "否";
                    break;
                case MessageBoxButton.OKCancel:
                    dialog.PrimaryButtonText = "确定";
                    dialog.CloseButtonText = "取消";
                    break;
                default:
                    dialog.PrimaryButtonText = "确定";
                    break;
            }

            var result = await dialog.ShowDialogAsync();

            // 转换结果
            return result switch
            {
                Wpf.Ui.Controls.MessageBoxResult.Primary => MessageBoxResult.Yes,
                Wpf.Ui.Controls.MessageBoxResult.Secondary => MessageBoxResult.No,
                _ => MessageBoxResult.Cancel
            };
        }

        private MessageBoxResult ShowModernMessageBox(string title, string message, MessageBoxButton buttons)
        {
            return ShowModernMessageBoxAsync(title, message, buttons).GetAwaiter().GetResult();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // 编辑操作
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            MarkdownTextBox.Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            MarkdownTextBox.Redo();
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            MarkdownTextBox.Cut();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            MarkdownTextBox.Copy();
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            MarkdownTextBox.Paste();
        }

        // Markdown 快捷插入
        private void InsertBold_Click(object? sender = null, RoutedEventArgs? e = null)
        {
            InsertMarkdownSyntax("**", "**", "粗体文本");
        }

        private void InsertItalic_Click(object? sender = null, RoutedEventArgs? e = null)
        {
            InsertMarkdownSyntax("*", "*", "斜体文本");
        }

        private void InsertStrikethrough_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdownSyntax("~~", "~~", "删除线文本");
        }

        private void InsertHeading_Click(object sender, RoutedEventArgs e)
        {
            int line = MarkdownTextBox.GetLineIndexFromCharacterIndex(MarkdownTextBox.CaretIndex);
            int lineStart = MarkdownTextBox.GetCharacterIndexFromLineIndex(line);
            MarkdownTextBox.Select(lineStart, 0);
            MarkdownTextBox.SelectedText = "## ";
            MarkdownTextBox.CaretIndex = lineStart + 3;
        }

        private void InsertLink_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdownSyntax("[", "](url)", "链接文本");
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdownSyntax("![", "](url)", "图片描述");
        }

        private void InsertCode_Click(object sender, RoutedEventArgs e)
        {
            // 如果编辑器不可见,先切换到编辑器或双栏模式
            if (!isEditorVisible)
            {
                viewMode = 0;
                isEditorVisible = true;
                isPreviewVisible = true;
                UpdateViewVisibility();
            }

            string selectedText = MarkdownTextBox.SelectedText;
            
            // 检查是否有选中文本且包含换行符（多行）
            if (!string.IsNullOrEmpty(selectedText) && selectedText.Contains('\n'))
            {
                // 多行：插入代码块，并提示输入语言
                var languageDialog = new Window
                {
                    Title = "插入代码块",
                    Width = 320,
                    Height = 160,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize,
                    Background = System.Windows.Media.Brushes.White
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new System.Windows.Controls.TextBlock 
                { 
                    Text = "代码语言 (可选):", 
                    Margin = new Thickness(0, 0, 0, 5) 
                };
                Grid.SetRow(label, 0);

                var langInput = new System.Windows.Controls.TextBox 
                { 
                    Text = "", 
                    Margin = new Thickness(0, 0, 0, 10) 
                };
                Grid.SetRow(langInput, 1);

                var hint = new System.Windows.Controls.TextBlock 
                { 
                    Text = "例如: python, javascript, csharp, java 等",
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                Grid.SetRow(hint, 2);

                var buttonPanel = new System.Windows.Controls.StackPanel 
                { 
                    Orientation = System.Windows.Controls.Orientation.Horizontal, 
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right, 
                    Margin = new Thickness(0, 15, 0, 0) 
                };
                Grid.SetRow(buttonPanel, 3);

                var okButton = new System.Windows.Controls.Button 
                { 
                    Content = "确定", 
                    Width = 80, 
                    Margin = new Thickness(0, 0, 10, 0), 
                    Padding = new Thickness(0, 5, 0, 5) 
                };
                var cancelButton = new System.Windows.Controls.Button 
                { 
                    Content = "取消", 
                    Width = 80, 
                    Padding = new Thickness(0, 5, 0, 5) 
                };

                bool dialogResult = false;
                okButton.Click += (s, args) => { dialogResult = true; languageDialog.Close(); };
                cancelButton.Click += (s, args) => { languageDialog.Close(); };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);

                grid.Children.Add(label);
                grid.Children.Add(langInput);
                grid.Children.Add(hint);
                grid.Children.Add(buttonPanel);

                languageDialog.Content = grid;
                languageDialog.ShowDialog();

                if (dialogResult)
                {
                    string lang = langInput.Text.Trim();
                    string prefix = string.IsNullOrEmpty(lang) ? "```\n" : $"```{lang}\n";
                    InsertMarkdownSyntax(prefix, "\n```", selectedText);
                }
            }
            else
            {
                // 单行或无选中：插入行内代码
                InsertMarkdownSyntax("`", "`", selectedText ?? "代码");
            }
        }

        private void InsertList_Click(object sender, RoutedEventArgs e)
        {
            // 如果编辑器不可见,先切换到编辑器或双栏模式
            if (!isEditorVisible)
            {
                viewMode = 0; // 切换到双栏模式
                isEditorVisible = true;
                isPreviewVisible = true;
                UpdateViewVisibility();
            }

            int caretIndex = MarkdownTextBox.CaretIndex;
            if (caretIndex < 0 || caretIndex > MarkdownTextBox.Text.Length)
            {
                caretIndex = 0;
            }
            
            int line = MarkdownTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            int lineStart = MarkdownTextBox.GetCharacterIndexFromLineIndex(line);
            
            if (lineStart >= 0)
            {
                MarkdownTextBox.Select(lineStart, 0);
                MarkdownTextBox.SelectedText = "- ";
                MarkdownTextBox.CaretIndex = lineStart + 2;
                MarkdownTextBox.Focus();
            }
        }

        private void InsertOrderedList_Click(object sender, RoutedEventArgs e)
        {
            // 如果编辑器不可见,先切换到编辑器或双栏模式
            if (!isEditorVisible)
            {
                viewMode = 0;
                isEditorVisible = true;
                isPreviewVisible = true;
                UpdateViewVisibility();
            }

            int caretIndex = MarkdownTextBox.CaretIndex;
            if (caretIndex < 0 || caretIndex > MarkdownTextBox.Text.Length)
            {
                caretIndex = 0;
            }
            
            int line = MarkdownTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            int lineStart = MarkdownTextBox.GetCharacterIndexFromLineIndex(line);
            
            if (lineStart >= 0)
            {
                MarkdownTextBox.Select(lineStart, 0);
                MarkdownTextBox.SelectedText = "1. ";
                MarkdownTextBox.CaretIndex = lineStart + 3;
                MarkdownTextBox.Focus();
            }
        }

        private void InsertTaskList_Click(object sender, RoutedEventArgs e)
        {
            // 如果编辑器不可见,先切换到编辑器或双栏模式
            if (!isEditorVisible)
            {
                viewMode = 0;
                isEditorVisible = true;
                isPreviewVisible = true;
                UpdateViewVisibility();
            }

            int caretIndex = MarkdownTextBox.CaretIndex;
            if (caretIndex < 0 || caretIndex > MarkdownTextBox.Text.Length)
            {
                caretIndex = 0;
            }
            
            int line = MarkdownTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            int lineStart = MarkdownTextBox.GetCharacterIndexFromLineIndex(line);
            
            if (lineStart >= 0)
            {
                MarkdownTextBox.Select(lineStart, 0);
                MarkdownTextBox.SelectedText = "- [ ] ";
                MarkdownTextBox.CaretIndex = lineStart + 6;
                MarkdownTextBox.Focus();
            }
        }

        private void InsertQuote_Click(object sender, RoutedEventArgs e)
        {
            // 如果编辑器不可见,先切换到编辑器或双栏模式
            if (!isEditorVisible)
            {
                viewMode = 0;
                isEditorVisible = true;
                isPreviewVisible = true;
                UpdateViewVisibility();
            }

            int caretIndex = MarkdownTextBox.CaretIndex;
            if (caretIndex < 0 || caretIndex > MarkdownTextBox.Text.Length)
            {
                caretIndex = 0;
            }
            
            int line = MarkdownTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            int lineStart = MarkdownTextBox.GetCharacterIndexFromLineIndex(line);
            
            if (lineStart >= 0)
            {
                MarkdownTextBox.Select(lineStart, 0);
                MarkdownTextBox.SelectedText = "> ";
                MarkdownTextBox.CaretIndex = lineStart + 2;
                MarkdownTextBox.Focus();
            }
        }

        private void InsertHorizontalRule_Click(object sender, RoutedEventArgs e)
        {
            InsertMarkdownSyntax("\n---\n", "", "");
        }

        private void InsertTable_Click(object sender, RoutedEventArgs e)
        {
            // 创建输入对话框
            var dialog = new Window
            {
                Title = "插入表格",
                Width = 320,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 行数输入
            var rowLabel = new System.Windows.Controls.TextBlock { Text = "行数:", Margin = new Thickness(0, 0, 0, 5) };
            Grid.SetRow(rowLabel, 0);
            
            var rowInput = new System.Windows.Controls.TextBox { Text = "3", Margin = new Thickness(0, 0, 0, 0) };
            Grid.SetRow(rowInput, 1);

            // 列数输入
            var colLabel = new System.Windows.Controls.TextBlock { Text = "列数:", Margin = new Thickness(0, 15, 0, 5) };
            Grid.SetRow(colLabel, 2);
            
            var colInput = new System.Windows.Controls.TextBox { Text = "3", Margin = new Thickness(0, 0, 0, 0) };
            Grid.SetRow(colInput, 3);

            // 按钮面板
            var buttonPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            Grid.SetRow(buttonPanel, 5);

            var okButton = new System.Windows.Controls.Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 10, 0), Padding = new Thickness(0, 5, 0, 5) };
            var cancelButton = new System.Windows.Controls.Button { Content = "取消", Width = 80, Padding = new Thickness(0, 5, 0, 5) };

            bool dialogResult = false;
            okButton.Click += (s, args) => { dialogResult = true; dialog.Close(); };
            cancelButton.Click += (s, args) => { dialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(rowLabel);
            grid.Children.Add(rowInput);
            grid.Children.Add(colLabel);
            grid.Children.Add(colInput);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();

            if (dialogResult)
            {
                if (int.TryParse(rowInput.Text, out int rows) && int.TryParse(colInput.Text, out int cols) &&
                    rows > 0 && rows <= 20 && cols > 0 && cols <= 10)
                {
                    GenerateAndInsertTable(rows, cols);
                }
                else
                {
                    MessageBox.Show("请输入有效的行列数！\n行数: 1-20\n列数: 1-10", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void GenerateAndInsertTable(int rows, int cols)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine();

            // 表头
            sb.Append("|");
            for (int i = 1; i <= cols; i++)
            {
                sb.Append($" 列{i} |");
            }
            sb.AppendLine();

            // 分隔线
            sb.Append("|");
            for (int i = 0; i < cols; i++)
            {
                sb.Append("-----|");
            }
            sb.AppendLine();

            // 数据行
            for (int r = 0; r < rows; r++)
            {
                sb.Append("|");
                for (int c = 0; c < cols; c++)
                {
                    sb.Append(" 内容 |");
                }
                sb.AppendLine();
            }

            InsertMarkdownSyntax(sb.ToString(), "", "");
        }

        private void ParagraphButton_Click(object sender, RoutedEventArgs e)
        {
            // 显示下拉菜单
            ParagraphButton.ContextMenu.PlacementTarget = ParagraphButton;
            ParagraphButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            ParagraphButton.ContextMenu.IsOpen = true;
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            FormatButton.ContextMenu.PlacementTarget = FormatButton;
            FormatButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            FormatButton.ContextMenu.IsOpen = true;
        }

        private void ListButton_Click(object sender, RoutedEventArgs e)
        {
            ListButton.ContextMenu.PlacementTarget = ListButton;
            ListButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            ListButton.ContextMenu.IsOpen = true;
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            InsertButton.ContextMenu.PlacementTarget = InsertButton;
            InsertButton.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            InsertButton.ContextMenu.IsOpen = true;
        }

        private void InsertH1_Click(object sender, RoutedEventArgs e)
        {
            InsertHeadingAtLine(1);
        }

        private void InsertH2_Click(object sender, RoutedEventArgs e)
        {
            InsertHeadingAtLine(2);
        }

        private void InsertH3_Click(object sender, RoutedEventArgs e)
        {
            InsertHeadingAtLine(3);
        }

        private void InsertH4_Click(object sender, RoutedEventArgs e)
        {
            InsertHeadingAtLine(4);
        }

        private void InsertH5_Click(object sender, RoutedEventArgs e)
        {
            InsertHeadingAtLine(5);
        }

        private void InsertH6_Click(object sender, RoutedEventArgs e)
        {
            InsertHeadingAtLine(6);
        }

        private void InsertHeadingAtLine(int level)
        {
            // 如果编辑器不可见,先切换到编辑器或双栏模式
            if (!isEditorVisible)
            {
                viewMode = 0; // 切换到双栏模式
                isEditorVisible = true;
                isPreviewVisible = true;
                UpdateViewVisibility();
            }

            string prefix = new string('#', level) + " ";
            int caretIndex = MarkdownTextBox.CaretIndex;
            
            // 确保caretIndex有效
            if (caretIndex < 0 || caretIndex > MarkdownTextBox.Text.Length)
            {
                caretIndex = 0;
            }
            
            int line = MarkdownTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            int lineStart = MarkdownTextBox.GetCharacterIndexFromLineIndex(line);
            
            // 确保lineStart有效
            if (lineStart >= 0)
            {
                MarkdownTextBox.Select(lineStart, 0);
                MarkdownTextBox.SelectedText = prefix;
                MarkdownTextBox.CaretIndex = lineStart + prefix.Length;
                MarkdownTextBox.Focus();
            }
        }

        private void InsertMarkdownSyntax(string prefix, string suffix, string defaultText)
        {
            // 如果编辑器不可见,先切换到编辑器或双栏模式
            if (!isEditorVisible)
            {
                viewMode = 0; // 切换到双栏模式
                isEditorVisible = true;
                isPreviewVisible = true;
                UpdateViewVisibility();
            }

            int selectionStart = MarkdownTextBox.SelectionStart;
            int selectionLength = MarkdownTextBox.SelectionLength;
            string selectedText = MarkdownTextBox.SelectedText;

            if (string.IsNullOrEmpty(selectedText))
            {
                selectedText = defaultText;
            }

            string newText = prefix + selectedText + suffix;
            MarkdownTextBox.SelectedText = newText;
            
            // 选中插入的文本
            MarkdownTextBox.Select(selectionStart + prefix.Length, selectedText.Length);
            MarkdownTextBox.Focus();
        }

        // 文件夹和树形视图操作
        private void OpenFolder_Click(object? sender = null, RoutedEventArgs? e = null)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择要打开的文件夹";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LoadFolder(dialog.SelectedPath);
                }
            }
        }

        private void LoadFolder(string folderPath)
        {
            currentFolderPath = folderPath;
            fileTree.Clear();

            try
            {
                var rootItem = new FileTreeItem(folderPath);
                rootItem.LoadChildren();
                rootItem.IsExpanded = true;
                fileTree.Add(rootItem);
                
                // 打开文件夹后自动显示侧边栏
                if (!isSidebarVisible)
                {
                    isSidebarVisible = true;
                    UpdateSidebarVisibility();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开文件夹失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileTreeItem item && !item.IsDirectory)
            {
                LoadFileFromTree(item.FullPath);
            }
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is FileTreeItem item)
            {
                item.LoadChildren();
            }
        }

        private void LoadFileFromTree(string filePath)
        {
            if (isContentChanged)
            {
                var result = ShowModernMessageBox(
                    "未保存的更改",
                    "当前文档尚未保存，是否保存？",
                    MessageBoxButton.YesNoCancel
                );

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile_Click();
                    if (isContentChanged) return;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            try
            {
                currentFilePath = filePath;
                MarkdownTextBox.Text = File.ReadAllText(filePath, Encoding.UTF8);
                isContentChanged = false;
                Title = $"Markdown 编辑器 - {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开文件失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 视图切换功能
        private void ToggleEditor_Click(object sender, RoutedEventArgs e)
        {
            isEditorVisible = !isEditorVisible;
            UpdateViewVisibility();
        }

        private void TogglePreview_Click(object sender, RoutedEventArgs e)
        {
            isPreviewVisible = !isPreviewVisible;
            UpdateViewVisibility();
        }

        private void ToggleToc_Click(object sender, RoutedEventArgs e)
        {
            isTocVisible = !isTocVisible;
            UpdateTocVisibility();
        }

        private void UpdateTocVisibility()
        {
            if (isTocVisible)
            {
                TocColumn.Width = new GridLength(280);
                TocColumn.MinWidth = 200;
                TocBorder.Visibility = Visibility.Visible;
            }
            else
            {
                TocColumn.Width = new GridLength(0);
                TocColumn.MinWidth = 0;
                TocBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateViewVisibility()
        {
            // 不允许同时隐藏编辑器和预览
            if (!isEditorVisible && !isPreviewVisible)
            {
                isPreviewVisible = true;
            }

            if (isEditorVisible && isPreviewVisible)
            {
                // 两者都显示 - 左右平分空间
                EditorColumn.MinWidth = 300;
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                EditorSplitterColumn.Width = new GridLength(5);
                PreviewColumn.MinWidth = 300;
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                EditorBorder.Visibility = Visibility.Visible;
                EditorSplitter.Visibility = Visibility.Visible;
                PreviewBorder.Visibility = Visibility.Visible;
            }
            else if (isEditorVisible && !isPreviewVisible)
            {
                // 只显示编辑器 - 占满所有空间
                EditorColumn.MinWidth = 0;
                EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                EditorSplitterColumn.Width = new GridLength(0);
                PreviewColumn.MinWidth = 0;
                PreviewColumn.Width = new GridLength(0);
                EditorBorder.Visibility = Visibility.Visible;
                EditorSplitter.Visibility = Visibility.Collapsed;
                PreviewBorder.Visibility = Visibility.Collapsed;
            }
            else if (!isEditorVisible && isPreviewVisible)
            {
                // 只显示预览 - 占满所有空间
                EditorColumn.MinWidth = 0;
                EditorColumn.Width = new GridLength(0);
                EditorSplitterColumn.Width = new GridLength(0);
                PreviewColumn.MinWidth = 0;
                PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
                EditorBorder.Visibility = Visibility.Collapsed;
                EditorSplitter.Visibility = Visibility.Collapsed;
                PreviewBorder.Visibility = Visibility.Visible;
            }

            // 目录切换按钮在双栏和预览模式下显示
            if (isPreviewVisible)
            {
                TocButtonSeparator.Visibility = Visibility.Visible;
                TocButtonItem.Visibility = Visibility.Visible;
            }
            else
            {
                TocButtonSeparator.Visibility = Visibility.Collapsed;
                TocButtonItem.Visibility = Visibility.Collapsed;
            }
        }

        // 切换视图模式
        private void ToggleView_Click(object sender, RoutedEventArgs e)
        {
            viewMode = (viewMode + 1) % 3;
            
            switch (viewMode)
            {
                case 0: // 双栏模式
                    isEditorVisible = true;
                    isPreviewVisible = true;
                    break;
                case 1: // 仅编辑器
                    isEditorVisible = true;
                    isPreviewVisible = false;
                    break;
                case 2: // 仅预览
                    isEditorVisible = false;
                    isPreviewVisible = true;
                    break;
            }
            
            UpdateViewVisibility();
        }

        // 切换侧边栏显示/隐藏
        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            isSidebarVisible = !isSidebarVisible;
            UpdateSidebarVisibility();
        }

        private void UpdateSidebarVisibility()
        {
            if (isSidebarVisible)
            {
                // 显示侧边栏
                SidebarColumn.Width = new GridLength(250);
                SidebarColumn.MinWidth = 150;
                SidebarSplitterColumn.Width = new GridLength(5);
                SidebarBorder.Visibility = Visibility.Visible;
                SidebarSplitter.Visibility = Visibility.Visible;
            }
            else
            {
                // 隐藏侧边栏
                SidebarColumn.Width = new GridLength(0);
                SidebarColumn.MinWidth = 0;
                SidebarSplitterColumn.Width = new GridLength(0);
                SidebarBorder.Visibility = Visibility.Collapsed;
                SidebarSplitter.Visibility = Visibility.Collapsed;
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "Markdown 编辑器 v1.2\n\n" +
                "一个简单而强大的 Markdown 编辑器\n" +
                "支持实时预览和完整的 Markdown 语法\n" +
                "支持文件夹浏览和树形视图\n" +
                "支持切换显示编辑器/预览\n\n" +
                "技术栈：\n" +
                "- WPF (.NET 8.0)\n" +
                "- Markdig (Markdown 解析)\n" +
                "- WebView2 (预览渲染)\n" +
                "- 目录导航",
                "关于",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // 目录生成和导航
        private void UpdateTableOfContents()
        {
            TocTreeView.Items.Clear();

            if (string.IsNullOrWhiteSpace(MarkdownTextBox.Text))
                return;

            // 先生成HTML以获取实际的ID
            string html = Markdown.ToHtml(MarkdownTextBox.Text, markdownPipeline);
            
            // 从HTML中提取标题及其ID
            var tocItems = ExtractHeadingsFromHtml(html);
            
            if (tocItems.Count == 0)
                return;

            // 构建树形结构
            var rootItems = new List<TocTreeNode>();
            var stack = new Stack<TocTreeNode>();

            foreach (var item in tocItems)
            {
                var node = new TocTreeNode
                {
                    Item = item,
                    Children = new List<TocTreeNode>()
                };

                // 弹出比当前级别大或相等的节点
                while (stack.Count > 0 && stack.Peek().Item.Level >= item.Level)
                {
                    stack.Pop();
                }

                if (stack.Count == 0)
                {
                    rootItems.Add(node);
                }
                else
                {
                    stack.Peek().Children.Add(node);
                }

                stack.Push(node);
            }

            // 将树形结构转换为TreeView
            foreach (var rootNode in rootItems)
            {
                TocTreeView.Items.Add(CreateTreeViewItem(rootNode));
            }
        }

        private TreeViewItem CreateTreeViewItem(TocTreeNode node)
        {
            var treeViewItem = new TreeViewItem
            {
                Header = node.Item.Title,
                Tag = node.Item,
                FontSize = 12
            };

            // 设置缩进样式
            var margin = (node.Item.Level - 1) * 10;
            treeViewItem.Padding = new Thickness(margin, 2, 0, 2);

            // 选中事件
            treeViewItem.Selected += (s, e) =>
            {
                if (e.OriginalSource == treeViewItem && treeViewItem.Tag is TocItem tocItem)
                {
                    ScrollToHeading(tocItem.Id);
                    e.Handled = true;
                }
            };

            foreach (var child in node.Children)
            {
                treeViewItem.Items.Add(CreateTreeViewItem(child));
            }

            return treeViewItem;
        }

        private List<TocItem> ExtractHeadingsFromHtml(string html)
        {
            var tocItems = new List<TocItem>();
            
            // 匹配标题标签: <h1 id="xxx">标题文本</h1>
            var headingPattern = @"<h([1-6])(?:\s+id=""([^""]*)"")?\s*>([^<]+)</h[1-6]>";
            var matches = System.Text.RegularExpressions.Regex.Matches(html, headingPattern);
            
            int index = 0;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var level = int.Parse(match.Groups[1].Value);
                var id = match.Groups[2].Success ? match.Groups[2].Value : $"heading-{index}";
                var title = System.Net.WebUtility.HtmlDecode(match.Groups[3].Value.Trim());
                
                tocItems.Add(new TocItem
                {
                    Level = level,
                    Title = title,
                    LineNumber = index + 1,
                    Id = id
                });
                
                System.Diagnostics.Debug.WriteLine($"Extracted heading: Level={level}, ID={id}, Title={title}");
                index++;
            }
            
            return tocItems;
        }

        private async void ScrollToHeading(string headingId)
        {
            try
            {
                // 转义ID以防止特殊字符导致问题
                var escapedId = headingId.Replace("'", "\\'").Replace("\"", "\\\"");
                
                var script = $@"
                    (function() {{
                        try {{
                            // 尝试使用getElementById
                            var element = document.getElementById('{escapedId}');
                            if (!element) {{
                                // 如果getElementById失败,尝试使用querySelector
                                element = document.querySelector('[id=""{escapedId}""]');
                            }}
                            if (!element) {{
                                // 最后尝试通过标题文本查找
                                var headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
                                for (var i = 0; i < headings.length; i++) {{
                                    if (headings[i].id === '{escapedId}') {{
                                        element = headings[i];
                                        break;
                                    }}
                                }}
                            }}
                            
                            if (element) {{
                                element.scrollIntoView({{ behavior: 'smooth', block: 'start' }});
                                element.style.backgroundColor = '#fff3cd';
                                setTimeout(function() {{ element.style.backgroundColor = ''; }}, 1000);
                                return 'found';
                            }} else {{
                                console.log('Element not found: {escapedId}');
                                console.log('All IDs:', Array.from(document.querySelectorAll('[id]')).map(e => e.id));
                                return 'not found';
                            }}
                        }} catch(e) {{
                            console.error('Error:', e);
                            return 'error: ' + e.message;
                        }}
                    }})();
                ";
                
                System.Diagnostics.Debug.WriteLine($"Scrolling to: {headingId}");
                var result = await PreviewWebView.ExecuteScriptAsync(script);
                System.Diagnostics.Debug.WriteLine($"Scroll result: {result}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ScrollToHeading error: {ex.Message}");
            }
        }

    }

    // 目录项数据类
    public class TocItem
    {
        public int Level { get; set; }
        public string Title { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Id { get; set; } = string.Empty;
    }

    public class TocTreeNode
    {
        public TocItem Item { get; set; } = new TocItem();
        public List<TocTreeNode> Children { get; set; } = new List<TocTreeNode>();
    }

    // 辅助命令类
    public class RelayCommand : ICommand
    {
        private readonly Action<object?, RoutedEventArgs?> _execute;

        public RelayCommand(Action<object?, RoutedEventArgs?> execute)
        {
            _execute = execute;
        }

#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _execute(null, null);
        }
    }
}