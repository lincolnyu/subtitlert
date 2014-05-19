using System;
using Windows.UI.Xaml.Data;
using SubtitleRT.Helpers;

namespace SubtitleRT.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        #region Methods

        #region IValueConverter members

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType == typeof(string))
            {
                return ((TimeSpan)value).TimeSpanToDisplayString();
            }
            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // NOTE found the targetType is normally 'System.Object'
            TimeSpan ts;
            if (!TimeSpan.TryParse((string) value, out ts))
            {
                ts = TimeSpan.Zero;
            }
            return ts;
        }

        #endregion

        #endregion
    }
}
