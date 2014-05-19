using System;

namespace SubtitleRT.Helpers
{
    public static class FormatHelper
    {
        public static string TimeSpanToDisplayString(this TimeSpan ts)
        {
            var major = ts.ToString(@"hh\:mm\:ss");
            var mili = string.Format(".{0:000}", ts.Milliseconds);
            return major + mili;
        }

        public static string TimeSpanToSrtString(this TimeSpan ts)
        {
            var major = ts.ToString(@"hh\:mm\:ss");
            var mili = string.Format(",{0:000}", ts.Milliseconds);
            return major + mili;
        }

        public static string SrtStringToTimeSpanString(this string srtString)
        {
            return srtString.Replace(',', '.');
        }

        public static bool TryConvertSrtStringToTimeSpan(this string srtString, out TimeSpan timeSpan)
        {
            var s = srtString.SrtStringToTimeSpanString();
            return TimeSpan.TryParse(s, out timeSpan);
        }
    }
}
