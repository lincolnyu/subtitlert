using Windows.Storage;
using Apollo;

namespace SubtitleRT.Models
{
    public class RecentFile : BaseModel
    {
        #region Fields

        private IStorageFile _file;

        private string _filePath;

        #endregion

        #region Properties

        public string FilePath
        {
            get
            {
                return _filePath;
            }
            protected set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public IStorageFile File
        {
            get
            {
                return _file;
            }
            set
            {
                if (_file != value)
                {
                    _file = value;
                    FilePath = _file.Path;
                }
            }
        }

        public string Token { get; set; }

        #endregion
    }
}
