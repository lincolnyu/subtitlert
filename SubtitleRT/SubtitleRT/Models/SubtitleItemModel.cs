using System;
using Apollo;

namespace SubtitleRT.Models
{
    public class SubtitleItemModel : BaseModel
    {
        #region Fields

        private int _index;

        private TimeSpan _startTime;

        private TimeSpan _endTime;

        private string _content;

        #endregion

        #region Properties

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (!Equals(_index, value))
                {
                    _index = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                if (!Equals(_startTime, value))
                {
                    _startTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan EndTime
        {
            get
            {
                return _endTime;
            }
            set
            {
                if (!Equals(_endTime, value))
                {
                    _endTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public String Content
        {
            get
            {
                return _content;
            }
            set
            {
                if (!Equals(_content, value))
                {
                    _content = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion
    }
}
