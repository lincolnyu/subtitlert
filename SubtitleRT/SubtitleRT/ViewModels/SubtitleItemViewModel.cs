using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Apollo;
using SubtitleRT.Models;

namespace SubtitleRT.ViewModels
{
    public class SubtitleItemViewModel : BaseViewModel<SubtitleItemModel>
    {
        #region Fields

        private readonly PlayerPageViewModel _parentViewModel;

        private Brush _itemColor;

        #endregion

        #region Constructors

        static SubtitleItemViewModel()
        {
            var info = new ViewModelInfo
            {
                AllowTrivialMapping = true
            };
            ViewModelInfoRegistry.Instance.ViewModelToInfo[typeof(SubtitleItemViewModel)] = info;
        }

        public SubtitleItemViewModel(SubtitleItemModel model, PlayerPageViewModel parentViewModel)
            : base(model)
        {
            _parentViewModel = parentViewModel;
        }

        #endregion

        #region Properties

        public int ItemNumber
        {
            get
            {
                return Model.Index + 1;
            }
        }

        public TimeSpan StartTime
        {
            get
            {
                return Model.StartTime;
            }
        }

        public TimeSpan EndTime
        {
            get
            {
                return Model.EndTime;
            }
        }

        public string Content
        {
            get
            {
                return Model.Content;
            }
        }

        public object RichContent
        {
            get
            {
                return Model.RichContent;
            }
        }

        public Visibility ExtraInfoVisibility
        {
            get
            {
                return _parentViewModel.ShowTimestamps ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Brush ItemColor
        {
            get
            {
                return _itemColor;
            }
            set
            {
                if (_itemColor != value)
                {
                    _itemColor = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        public void RaiseExtraInfoVisibilityChange()
        {
// ReSharper disable once ExplicitCallerInfoArgument
            OnPropertyChanged("ExtraInfoVisibility");
        }

        #endregion
    }
}
