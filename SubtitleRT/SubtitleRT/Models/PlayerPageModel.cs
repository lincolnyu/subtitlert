using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
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

        #endregion

        #region Constructors

        public PlayerPageModel(IStorageFile file)
        {
            _file = file;
            Subtitles = new ObservableCollection<SubtitleItemModel>();
            ParseFile();
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

        private async void ParseFile()
        {
            Subtitles.Clear();
            var text = await FileIO.ReadTextAsync(_file);
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

                    var sbContent = new StringBuilder();
                    // contents
                    while (true)
                    {
                        var contentLine = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(contentLine))
                        {
                            break;
                        }
                        sbContent.AppendLine(contentLine);
                    }

                    var item = new SubtitleItemModel
                    {
                        Index = index++,
                        StartTime = st,
                        EndTime = et,
                        Content = sbContent.ToString(),
                    };

                    // trim the last new line characters
                    item.Content = item.Content.TrimEnd('\r', '\n');

                    Subtitles.Add(item);
                }
            }
        }

        #endregion
    }
}
