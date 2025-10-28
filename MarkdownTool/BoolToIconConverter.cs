using System;
using System.Globalization;
using System.Windows.Data;

namespace MarkdownTool
{
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDirectory)
            {
                return isDirectory ? "📁" : "📄";
            }
            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

