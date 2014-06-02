using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Apollo;
using SubtitleRT.Helpers;

namespace SubtitleRT.Models
{
    public class PlayerPageModel : BaseModel
    {
        #region Delegates

        public delegate void CurrentIndexChangedEventHandler(object sender, int oldIndex, int newIndex);

        #endregion

        #region Fields

        private readonly IStorageFile _file;

        private int _currentIndex = -1;
        private int _requestedIndex;
        
        private bool _isSubtitleOn;

        private bool _stopPlaying;

        private TimeSpan _currentPlayTime;
        private bool _isPlaying;

        private static readonly TimeSpan TimeSlice = TimeSpan.FromMilliseconds(50);

        private string _encodingName;

        private bool _parsing;

        #endregion

        #region Constructors

        public PlayerPageModel(IStorageFile file)
        {
            _file = file;
            Subtitles = new ObservableCollection<SubtitleItemModel>();
        }

        #endregion

        #region Properties

        public ObservableCollection<SubtitleItemModel> Subtitles { get; private set; }

        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
        }

        public TimeSpan CurrentPlayTime
        {
            get
            {
                return _currentPlayTime;
            }
            set
            {
                if (_currentPlayTime != value)
                {
                    _currentPlayTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentIndex
        {
            get
            {
                return _currentIndex;
            }
            set
            {
                if (_currentIndex != value)
                {
                    var oldValue = _currentIndex;
                    _currentIndex = value;
                    OnPropertyChanged();
                    if (CurrentIndexChanged != null)
                    {
                        CurrentIndexChanged(this, oldValue, value);
                    }
                }
            }
        }

        public int RequestedIndex
        {
            get
            {
                return _requestedIndex;
            }
            set
            {
                // This is done regarless if the index is to change since it alreays leads to the resetting of the play time
                _requestedIndex = value;
                if (_requestedIndex >= 0)
                {
                    if (IsPlaying)
                    {
                        PlayFromRequestedIndex(_requestedIndex);
                    }
                    else
                    {
                        CurrentIndex = value;
                        CurrentPlayTime = Subtitles[_requestedIndex].StartTime;
                    }
                }
                else
                {
                    StopToStartPiont();
                }
            }
        }

        public bool IsSubtitleOn
        {
            get
            {
                return _isSubtitleOn;
            }
            set
            {
                if (_isSubtitleOn != value)
                {
                    _isSubtitleOn = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EncodingName
        {
            get
            {
                return _encodingName;
            }
            set
            {
                if (_encodingName != value)
                {
                    _encodingName = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Events

        public event CurrentIndexChangedEventHandler CurrentIndexChanged;

        #endregion

        #region Methods

        public async void PlayFromRequestedIndex(int index)
        {
            await StopPlaying();
            CurrentPlayTime = Subtitles[_requestedIndex].StartTime;
            await Play();
        }

        public async void PlayFromStart()
        {
            CurrentPlayTime = TimeSpan.Zero;
            await Play();
        }

        public async Task Play()
        {
            await Play(CurrentPlayTime);
        }


        public async void StopToStartPiont()
        {
            if (IsPlaying)
            {
                await StopPlaying();
            }
            CurrentIndex = -1;
            CurrentPlayTime = TimeSpan.Zero;
        }

        public async Task Play(TimeSpan from)
        {
            var startTime = DateTime.UtcNow;
            _stopPlaying = false;
            _isPlaying = true;
            IsSubtitleOn = false;
            CurrentIndex = -1;

            // move to the point
            var index = 0;
            for (; index < Subtitles.Count; index++)
            {
                var subtitle = Subtitles[index];
                if (subtitle.EndTime >= from)
                {
                    break;
                }
            }

            for (; index < Subtitles.Count && !_stopPlaying; index++)
            {
                var subtitle = Subtitles[index];

                var startDiff = subtitle.StartTime - from;
                while (true)
                {
                    var currTime = DateTime.UtcNow;
                    var actualDiff = currTime - startTime;
                    CurrentPlayTime = from + actualDiff;
                    if (actualDiff < startDiff)
                    {
                        var wait = startDiff - actualDiff < TimeSlice ? startDiff - actualDiff : TimeSlice;
                        await Task.Delay(wait);
                    }
                    else
                    {
                        break;
                    }
                    if (_stopPlaying)
                    {
                        break;
                    }
                }
               
                CurrentIndex = index;
                IsSubtitleOn = true;

                var endDiff = subtitle.EndTime - from;
                while (true)
                {
                    var currTime = DateTime.UtcNow;
                    var actualDiff = currTime - startTime;
                    CurrentPlayTime = from + actualDiff;
                    if (actualDiff < endDiff)
                    {
                        var wait = endDiff - actualDiff < TimeSlice ? endDiff - actualDiff : TimeSlice;
                        await Task.Delay(wait);
                    }
                    else
                    {
                        break;
                    }
                    if (_stopPlaying)
                    {
                        break;
                    }
                }
                IsSubtitleOn = false;
            }

            IsSubtitleOn = false;
            CurrentIndex = -1;
            _isPlaying = false;
        }

        public async Task StopPlaying()
        {
            _stopPlaying = true;
            while (_isPlaying)
            {
                await Task.Delay(TimeSlice);
            }
        }

        public async Task ParseFile()
        {
            if (_parsing)
            {
                return;
            }
            _parsing = true;
            var ext = Path.GetExtension(_file.Name).ToLower();
            if (ext == ".sub")
            {
                await ParseFileSub();
            }
            else
            {
                // all others are treated as SRT 
                await ParseFileSrt();
            }
            _parsing = false;
        }

        private async Task<string> GetFileContent()
        {
            string text = null;
            try
            {
                if (EncodingName == null)
                {
                    text = await FileIO.ReadTextAsync(_file);
                }
            }
            catch (Exception)
            {
                text = null;
            }

            if (text == null)
            {
                if (EncodingName == null)
                {
                    var md = new MessageDialog("This is not a Unicode subtitle file, encoding needs to be specified. 'Windows-1252' is used for now. You can change it on the page later.");
                    await md.ShowAsync();
                    EncodingName = "Windows-1252";
                }
                var enc = Encoding.GetEncoding(EncodingName);
                var buffer = await FileIO.ReadBufferAsync(_file);
                var bytes = buffer.ToArray();
                var dec = enc.GetDecoder();
                var charCount = dec.GetCharCount(bytes, 0, bytes.Length);
                var chars = new char[charCount];
                dec.GetChars(bytes, 0, bytes.Length, chars, 0);
                var sbText = new StringBuilder();
                foreach (var ch in chars)
                {
                    sbText.Append(ch);
                }
                text = sbText.ToString();
            }
            return text;
        }

        private async Task ParseFileSub()
        {
            Subtitles.Clear();
            var text = await GetFileContent();
            var index = 0;
            var firstLineRead = false;
            var ratio = 0.0;
            using (var sr = new StringReader(text))
            {
                while (true)
                {
                    var line = sr.ReadLine();
                    if (line == null) break;

                    if (line.Length < 1 || line[0] != '{') continue;

                    var delim1 = line.IndexOf("}{", StringComparison.Ordinal);
                    if (delim1 < 0) continue;

                    var snum1 = line.Substring(1, delim1 - 1);

                    var delim2 = line.IndexOf("}", delim1+2, StringComparison.Ordinal);
                    if (delim2 < 0) continue;

                    var snum2 = line.Substring(delim1 + 2, delim2 - delim1 - 2);

                    int inum1, inum2;
                    if (!int.TryParse(snum1, out inum1))
                    {
                        continue;
                    }

                    if (!int.TryParse(snum1, out inum1))
                    {
                        continue;
                    }

                    if (!int.TryParse(snum2, out inum2))
                    {
                        continue;
                    }

                    var stext = line.Substring(delim2 + 1);

                    if (!firstLineRead)
                    {
                        // inum1 and inum2 are supposed to be 1s

                        if (!double.TryParse(stext, out ratio) || ratio <= 0)
                        {
                            break;
                        }

                        firstLineRead = true;
                        continue;
                    }

                    var msecStart = inum1*1000/ratio;
                    var startTime = TimeSpan.FromMilliseconds(msecStart);
                    var msecEnd = inum2*1000/ratio;
                    var endTime = TimeSpan.FromMilliseconds(msecEnd);

                    string plainContent;
                    object richContent;
                    ParseSubContent(stext, out plainContent, out richContent);

                    var item = new SubtitleItemModel
                    {
                        Index = index++,
                        StartTime = startTime,
                        EndTime = endTime,
                        Content = plainContent,
                        RichContent = richContent
                    };

                    // trim the last new line characters
                    item.Content = item.Content.TrimEnd('\r', '\n');

                    Subtitles.Add(item);
                }
            }
        }

        private async Task ParseFileSrt()
        {
            Subtitles.Clear();
            var text = await GetFileContent();
            var index = 0;
            using (var sr = new StringReader(text))
            {
                while (true)
                {
                    var lineItemNo = sr.ReadLine();
                    if (lineItemNo == null) break;

                    int num;
                    if (!int.TryParse(lineItemNo, out num))
                    {
                        break;
                    }
                    // this number is currently ignored

                    var lineTime = sr.ReadLine();
                    if (lineTime == null) break;

                    const string delimiter = "-->";
                    var posDelim = lineTime.IndexOf(delimiter, StringComparison.Ordinal);
                    if (posDelim < 0)
                    {
                        break;
                    }
                    var ststr = lineTime.Substring(0, posDelim).Trim();
                    var etstr = lineTime.Substring(posDelim + delimiter.Length).Trim();

                    TimeSpan st, et;
                    if (!ststr.TryConvertSrtStringToTimeSpan(out st))
                    {
                        break;
                    }
                    if (!etstr.TryConvertSrtStringToTimeSpan(out et))
                    {
                        break;
                    }

                    string plainContent;
                    object richContent;
                    ParseSrtContent(sr, out plainContent, out richContent);

                    var item = new SubtitleItemModel
                    {
                        Index = index++,
                        StartTime = st,
                        EndTime = et,
                        Content = plainContent,
                        RichContent = richContent
                    };

                    Subtitles.Add(item);
                }
            }
        }

        private void ParseSubContent(string stext, out string plainContent, out object richContent)
        {
            var textSegs = stext.Split('|');
            var sbContent = new StringBuilder();
            foreach (var seg in textSegs)
            {
                sbContent.AppendLine(seg);
            }
            plainContent = sbContent.ToString();
            //<TextBlock Text="{Binding Content}" TextWrapping="WrapWholeWords" Margin="8"  FontSize="28"/>
            richContent = new TextBlock
            {
                Text = plainContent,
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(8),
                FontSize = 28
            };
        }

        private void ParseSrtContent(StringReader sr, out string plainContent, out object richContent)
        {
            var sbContent = new StringBuilder();
            var lines = new List<string>();
            // contents
            while (true)
            {
                var contentLine = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(contentLine))
                {
                    break;
                }

                sbContent.AppendLine(contentLine);
                lines.Add(contentLine);
            }
            plainContent = sbContent.ToString();
            // trim the last new line characters

            plainContent = plainContent.TrimEnd('\r', '\n');

            var needProcess = plainContent.Contains("<i>") || plainContent.Contains("<b>");
            plainContent = plainContent.Replace("</i>", "");
            plainContent = plainContent.Replace("</b>", "");
            if (needProcess)
            {
                plainContent = plainContent.Replace("<i>", "");
                plainContent = plainContent.Replace("<b>", "");

                var bold = false;
                var italic = false;
                var rtb = new RichTextBlock
                {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(8),
                    FontSize = 28
                };
                var p = new Paragraph();
                rtb.Blocks.Add(p);
                foreach (var line in lines)
                {
                    var index = 0;
                    while (index < line.Length)
                    {
                        var lastIndex = index;
                        index = line.IndexOf('<', index);

                        // TODO further optimize...
                        if (index < 0)
                        {
                            ProcessSrtTo(p, line.Substring(lastIndex, line.Length - lastIndex), bold, italic);
                            index = line.Length;
                        }
                        else if (line.Substring(index).StartsWith("<i>"))
                        {
                            ProcessSrtTo(p, line.Substring(lastIndex, index - lastIndex), bold, italic);
                            italic = true;
                            index += 3;
                        }
                        else if (line.Substring(index).StartsWith("</i>"))
                        {
                            ProcessSrtTo(p, line.Substring(lastIndex, index - lastIndex), bold, italic);
                            italic = false;
                            index += 4;
                        }
                        else if (line.Substring(index).StartsWith("<b>"))
                        {
                            ProcessSrtTo(p, line.Substring(lastIndex, index - lastIndex), bold, italic);
                            bold = true;
                            index += 3;
                        }
                        else if (line.Substring(index).StartsWith("</b>"))
                        {
                            ProcessSrtTo(p, line.Substring(lastIndex, index - lastIndex), bold, italic);
                            bold = false;
                            index += 4;
                        }
                    }
                }
                richContent = rtb;
            }
            else
            {
                richContent = new TextBlock
                {
                    Text = plainContent,
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(8),
                    FontSize = 28
                };
            }
        }

        private void ProcessSrtTo(Paragraph p, string str, bool bold, bool italic)
        {
            if (str == "")
            {
                return;
            }

            Span firstSpan = null;
            Span lastSpan = null;
            if (bold)
            {
                var b = new Bold();
                firstSpan = b;
                lastSpan = b;
            }
            if (italic)
            {
                var i = new Italic();
                if (firstSpan == null)
                {
                    firstSpan = i;
                }
                if (lastSpan != null)
                {
                    lastSpan.Inlines.Add(i);
                }
                lastSpan = i;
            }

            var run = new Run
            {
                Text = str
            };
            if (lastSpan != null)
            {
                p.Inlines.Add(firstSpan);
                lastSpan.Inlines.Add(run);
            }
            else
            {
                p.Inlines.Add(run);
            }
        }

        public int Search(string query, int startIndex)
        {
            var queryLc = query.ToLower();
            for (var i = startIndex; i < Subtitles.Count; i++)
            {
                var subtitle = Subtitles[i];
                if (string.IsNullOrWhiteSpace(subtitle.Content))
                {
                    continue;
                }
                var contentLc = subtitle.Content.ToLower();
                if (Match(contentLc, queryLc))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool Match(string source, string query)
        {
            var trimmedQuery = query.Trim();
            var isWord = true;
            foreach (var c in trimmedQuery)
            {
                if (char.IsWhiteSpace(c))
                {
                    isWord = false;
                }
            }

            if (!isWord)
            {
                return source.Contains(query);
            }

            var index = 0;
            while (index + trimmedQuery.Length <= source.Length)
            {
                index = source.IndexOf(trimmedQuery, index, StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }
                // make sure the characters before and after are whitespaces
                if (index > 0)
                {
                    var charBefore = source[index - 1];
                    if (!char.IsWhiteSpace(charBefore))
                    {
                        index += trimmedQuery.Length;
                        continue;
                    }
                }
                if (index + trimmedQuery.Length < source.Length)
                {
                    var charAfter = source[index + trimmedQuery.Length];
                    if (!char.IsWhiteSpace(charAfter))
                    {
                        index += trimmedQuery.Length;
                        continue;
                    }
                }
                return true;
            }
            return false;
        }

        #endregion
    }
}
