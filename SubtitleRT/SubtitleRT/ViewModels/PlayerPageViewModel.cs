using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Apollo;
using SubtitleRT.Common;
using SubtitleRT.Models;

namespace SubtitleRT.ViewModels
{
    public class PlayerPageViewModel : BaseViewModel<PlayerPageModel>
    {
        #region Fields

        #region Navigation related

        private readonly NavigationHelper _navigationHelper;
        private readonly ObservableDictionary _defaultViewModel = new ObservableDictionary();

        #endregion

        private bool _showTimeSteps;

        private static readonly Color UnplayedItemColor = Colors.Transparent;
        private static readonly Color HilightedItemColor = Colors.Green;
        private static readonly Color InactiveItemColor = Colors.DarkOliveGreen;


        private string _selectedEncoding;

        #endregion

        #region Constructors

        static PlayerPageViewModel()
        {
            ViewModelInfoRegistry.Instance.ViewModelToInfo.Add(typeof (PlayerPageViewModel), new ViewModelInfo
            {
                AllowTrivialMapping = true
            });
        }

        public PlayerPageViewModel(PlayerPageModel model, Page page) : base(model)
        {
            _navigationHelper = new NavigationHelper(page);
            _navigationHelper.LoadState += navigationHelper_LoadState;
            _navigationHelper.SaveState += navigationHelper_SaveState;

            model.CurrentIndexChanged += ModelOnCurrentIndexChanged;

            Subtitles = new ObservableCollection<SubtitleItemViewModel>();
// ReSharper disable once ObjectCreationAsStatement
            new ListSync(Subtitles, Model.Subtitles, st => new SubtitleItemViewModel((SubtitleItemModel) st, this)
            {
                ItemColor = new SolidColorBrush(
                    Model.CurrentIndex>=0 && Model.CurrentIndex<Model.Subtitles.Count && Model.Subtitles[Model.CurrentIndex] == st ? 
                    HilightedItemColor : UnplayedItemColor)
            });

            PopulateSupportedEncodings();
        }

        #endregion

        #region Properties

        #region Navigation related

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return _defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return _navigationHelper; }
        }

        #endregion

        public ObservableCollection<SubtitleItemViewModel> Subtitles
        {
            get; private set;
        }

        public int CurrentIndex
        {
            get
            {
                return Model.CurrentIndex;
            }
            set
            {
                Model.RequestedIndex = value;
            }
        }

        public TimeSpan CurrentPlayTime
        {
            get
            {
                return Model.CurrentPlayTime;
            }
            set
            {
                if (Model.CurrentPlayTime != value)
                {
                    Model.CurrentPlayTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowTimestamps
        {
            get
            {
                return _showTimeSteps;
            }
            set
            {
                if (_showTimeSteps != value)
                {
                    _showTimeSteps = value;
                    OnPropertyChanged();
                    foreach (var subt in Subtitles)
                    {
                        subt.RaiseExtraInfoVisibilityChange();
                    }
                }
            }
        }

        public List<string> SupportedEncodings
        {
            get; private set;
        }

        public string SelectedEncoding
        {
            get
            {
                return _selectedEncoding;
            }
            set
            {
                if (_selectedEncoding != value)
                {
                    _selectedEncoding = value;
                    UpdateEncodingToModel();
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        #region Navigation related

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        private void ModelOnCurrentIndexChanged(object sender, int oldIndex, int newIndex)
        {
            if (oldIndex >= 0)
            {
                var oldItem = Subtitles[oldIndex];
                oldItem.ItemColor = new SolidColorBrush(UnplayedItemColor);
            }
            if (newIndex >= 0)
            {
                var newItem = Subtitles[newIndex];
                newItem.ItemColor = Model.IsSubtitleOn
                        ? new SolidColorBrush(HilightedItemColor)
                        : new SolidColorBrush(InactiveItemColor);
            }
        }

        protected override void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            base.ModelOnPropertyChanged(sender, args);

            switch (args.PropertyName)
            {
                case "IsSubtitleOn":
                    Subtitles[CurrentIndex].ItemColor = Model.IsSubtitleOn 
                        ? new SolidColorBrush(HilightedItemColor)
                        : new SolidColorBrush(InactiveItemColor);
                    break;
                case "EncodingName":
                {
                    var en = Model.EncodingName;
                    if (en == null)
                    {
                        SelectedEncoding = SupportedEncodings[0];
                    }
                    else
                    {
                        foreach (var encoding in SupportedEncodings)
                        {
                            if (encoding == en)
                            {
                                SelectedEncoding = encoding;
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        }

        #endregion

        private async void UpdateEncodingToModel()
        {
            if (SelectedEncoding == SupportedEncodings[0])
            {
                Model.EncodingName = null;
            }
            else
            {
                Model.EncodingName = SelectedEncoding;
            }
            await Model.ParseFile(); //update the display
        }

        private void PopulateSupportedEncodings()
        {
            SupportedEncodings = new List<string>
            {
                "None (Unicode)",
                "Big5",
                "EUC-KR",
                "GB2312",
                "Shift-JIS",
                "Windows-1250",
                "Windows-1252"
            };
            SelectedEncoding = SupportedEncodings[0];
        }

        #endregion
    }
}
