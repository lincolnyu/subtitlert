using System;
using System.Threading.Tasks;
using Windows.Storage;
using SubtitleAnalyzer.Models;

namespace SubtitleRT.Models
{
    public class PlayerPageModel : SubtitleFileModel
    {
        #region Delegates

        public delegate void CurrentIndexChangedEventHandler(object sender, int oldIndex, int newIndex);

        #endregion

        #region Fields

        private int _currentIndex = -1;
        private int _requestedIndex;
        
        private bool _isSubtitleOn;

        private bool _stopPlaying;

        private TimeSpan _currentPlayTime;
        private bool _isPlaying;

        private static readonly TimeSpan TimeSlice = TimeSpan.FromMilliseconds(50);

        #endregion

        #region Constructors

        public PlayerPageModel(IStorageFile file) : base(file)
        {
        }

        #endregion

        #region Properties

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

        /// <summary>
        ///  Gets and sets the current index
        /// </summary>
        /// <remarks>
        ///  NOTE setting this property changes the selection but doesn't affect current playback
        /// </remarks>
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

        #endregion
    }
}
