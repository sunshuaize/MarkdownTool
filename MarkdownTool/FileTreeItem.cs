using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MarkdownTool
{
    public class FileTreeItem : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;

        public string Name { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public ObservableCollection<FileTreeItem> Children { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public FileTreeItem(string fullPath)
        {
            FullPath = fullPath;
            Name = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(Name))
            {
                Name = fullPath; // 用于根目录
            }
            IsDirectory = Directory.Exists(fullPath);
            Children = new ObservableCollection<FileTreeItem>();

            if (IsDirectory)
            {
                // 添加占位符，用于延迟加载
                Children.Add(new FileTreeItem("") { Name = "加载中..." });
            }
        }

        public void LoadChildren()
        {
            if (!IsDirectory || Children.Count != 1 || Children[0].FullPath != "")
            {
                return; // 已经加载过了
            }

            Children.Clear();

            try
            {
                var items = new List<FileTreeItem>();

                // 加载所有子目录（递归显示）
                var directories = Directory.GetDirectories(FullPath);
                foreach (var dir in directories)
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        // 跳过隐藏文件夹和系统文件夹
                        if (dirInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                            dirInfo.Attributes.HasFlag(FileAttributes.System))
                        {
                            continue;
                        }

                        // 检查该目录或其子目录是否包含 Markdown 文件
                        if (HasMarkdownFiles(dir))
                        {
                            items.Add(new FileTreeItem(dir));
                        }
                    }
                    catch
                    {
                        // 忽略无权访问的目录
                    }
                }

                // 加载当前目录的 Markdown 文件
                var markdownExtensions = new[] { "*.md", "*.markdown" };
                foreach (var extension in markdownExtensions)
                {
                    var files = Directory.GetFiles(FullPath, extension);
                    foreach (var file in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (!fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
                            {
                                items.Add(new FileTreeItem(file));
                            }
                        }
                        catch
                        {
                            // 忽略无权访问的文件
                        }
                    }
                }

                // 排序：目录在前，文件在后，各自按名称排序
                var sortedItems = items
                    .OrderByDescending(item => item.IsDirectory)
                    .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var item in sortedItems)
                {
                    Children.Add(item);
                }

                // 如果没有找到任何内容，显示提示
                if (Children.Count == 0)
                {
                    Children.Add(new FileTreeItem("") { Name = "(无 Markdown 文件)", IsDirectory = false });
                }
            }
            catch (UnauthorizedAccessException)
            {
                Children.Add(new FileTreeItem("") { Name = "(无权访问)", IsDirectory = false });
            }
            catch (Exception ex)
            {
                Children.Add(new FileTreeItem("") { Name = $"错误: {ex.Message}", IsDirectory = false });
            }
        }

        /// <summary>
        /// 检查目录或其子目录是否包含 Markdown 文件
        /// </summary>
        private bool HasMarkdownFiles(string directoryPath)
        {
            try
            {
                // 检查当前目录
                if (Directory.GetFiles(directoryPath, "*.md").Length > 0 ||
                    Directory.GetFiles(directoryPath, "*.markdown").Length > 0)
                {
                    return true;
                }

                // 递归检查子目录
                var subdirectories = Directory.GetDirectories(directoryPath);
                foreach (var subdir in subdirectories)
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(subdir);
                        // 跳过隐藏和系统文件夹
                        if (dirInfo.Attributes.HasFlag(FileAttributes.Hidden) ||
                            dirInfo.Attributes.HasFlag(FileAttributes.System))
                        {
                            continue;
                        }

                        if (HasMarkdownFiles(subdir))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // 忽略无权访问的目录
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

