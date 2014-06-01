using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SubtitleRT.Helpers;
using SubtitleRT.Models;
using SubtitleRT.ViewModels;

namespace SubtitleRT
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class PlayerPage
    {
        #region Fields

        readonly ApplicationDataContainer _roamingData = ApplicationData.Current.RoamingSettings;
        readonly ApplicationDataContainer _localData = ApplicationData.Current.LocalSettings;

        private IStorageFile _file;

        private PlayerPageModel _model;

        private PlayerPageViewModel _viewModel;

        #endregion

        #region Constructors

        public PlayerPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see>
        ///     <cref>GridCS.Common.NavigationHelper.LoadState</cref>
        /// </see>
        /// and <see>
        ///     <cref>GridCS.Common.NavigationHelper.SaveState</cref>
        /// </see>
        /// .
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _file = (StorageFile)e.Parameter;
            _model = new PlayerPageModel(_file);
            _viewModel = new PlayerPageViewModel(_model, this);
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;


            _viewModel.NavigationHelper.OnNavigatedTo(e);

            DataContext = _viewModel;

            SetupAdditionalHandlers();

            LoadEncoding();
            await _model.ParseFile();
            LoadIndexIfAvailable();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            SaveEncoding();
            SaveIndexForResume();

            UnsetupAdditionalHandlers();
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            _viewModel.NavigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void BtnHome_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (MainPage));
        }

        private async void BtnPlay_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_model.IsPlaying)
            {
                BtnPlay.Style = (Style) Application.Current.Resources["PauseAppBarButtonStyle"];
                await _model.Play();
            }
            else
            {
                BtnPlay.Style = (Style) Application.Current.Resources["PlayAppBarButtonStyle"];
                await _model.StopPlaying();
            }
        }

        private void LstSubtitles_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedVm = (SubtitleItemViewModel) e.ClickedItem;
            var index = _viewModel.Subtitles.IndexOf(clickedVm);
            _viewModel.CurrentIndex = index;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "CurrentIndex")
            {
                var index = _viewModel.CurrentIndex;
                if (index >= 0 && _model.IsPlaying)
                {
                    UpdateScrollView(index);
                }
            }
        }

        private void LstSubtitles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LstSubtitles.SelectedIndex = -1; // clear selection as we don't need it
// ReSharper disable PossibleNullReferenceException
            TopAppBar.IsOpen = true;
            BottomAppBar.IsOpen = true;
// ReSharper restore PossibleNullReferenceException
        }

        private void TxtTime_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTime.Text))
            {
                TxtTime.Text = "00:00:00.000";
            }
        }

        private void MainSearch_OnQuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            var text = args.QueryText;
            var startIndex = _model.CurrentIndex + 1;
            if (startIndex < 0) startIndex = 0;
            var target = _model.Search(text, startIndex);
            if (target >= 0)
            {
                _model.CurrentIndex = target;
                UpdateScrollView(target);
            }
        }

        private void UpdateScrollView(int index)
        {
            if (LstSubtitles.ActualHeight > 500)
            {
                // show 2 ahead
                index -= 2;
                if (index < 0) index = 0;
            }
            Debug.Assert(LstSubtitles.Items != null, "LstSubtitles.Items != null");
            var item = LstSubtitles.Items[index];
            LstSubtitles.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
        }

        private void SetupAdditionalHandlers()
        {
            Window.Current.CoreWindow.KeyDown += CoreWindowOnKeyDown;
        }

        private void UnsetupAdditionalHandlers()
        {
            Window.Current.CoreWindow.KeyDown -= CoreWindowOnKeyDown;
        }

        private void CoreWindowOnKeyDown(CoreWindow sender, KeyEventArgs args)
        {
            var stCtrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            var ctrlDown = ((stCtrl & CoreVirtualKeyStates.Down) != 0);
            if (ctrlDown && args.VirtualKey == VirtualKey.F)
            {
                // Ctrl+F
                if (TopAppBar != null)
                {
                    TopAppBar.IsOpen = true;
                    MainSearch.FocusOnKeyboardInput = true;
                    TopAppBar.Closed += (o, o1) =>
                    {
                        MainSearch.FocusOnKeyboardInput = false;
                    };
                }
                if (BottomAppBar != null)
                {
                    BottomAppBar.Opened += (o, o1) =>
                    {
                        MainSearch.FocusOnKeyboardInput = false;
                    };
                }
            }            
        }

        private void LoadIndexIfAvailable()
        {
            var fileName = _file.Path;
            string token;
            if (StorageHelper.FileNameToToken.TryGetValue(fileName, out token))
            {
                var key = string.Format("Resume_{0}", token);
                object val;
                int index = -1;
                if (_localData.Values.TryGetValue(key, out val) && val is int)
                {
                    index = (int) val;
                }
                else if (_roamingData.Values.TryGetValue(key, out val) && val is int)
                {
                    index = (int)val;
                }
                if (index >= 0 && LstSubtitles.Items != null && index < LstSubtitles.Items.Count)
                {
                    _model.CurrentIndex = index;
                    UpdateScrollView(index);
                }
            }
        }

        private void SaveIndexForResume()
        {
            var fileName = _file.Path;
            string token;
            if (StorageHelper.FileNameToToken.TryGetValue(fileName, out token))
            {
                var key = string.Format("Resume_{0}", token);
                _localData.Values[key] = _model.CurrentIndex;
                _roamingData.Values[key] = _model.CurrentIndex;
            }
        }

        private void LoadEncoding()
        {
            var fileName = _file.Path;
            string token;
            if (StorageHelper.FileNameToToken.TryGetValue(fileName, out token))
            {
                var key = string.Format("TextEnc_{0}", token);
                object val;
                string encname = null;
                if (_localData.Values.TryGetValue(key, out val) && val is string)
                {
                    encname = (string)val;
                }
                else if (_roamingData.Values.TryGetValue(key, out val) && val is string)
                {
                    encname = (string)val;
                }
                _model.EncodingName = encname;
            }
        }

        public void SaveEncoding()
        {
            var fileName = _file.Path;
            string token;
            if (StorageHelper.FileNameToToken.TryGetValue(fileName, out token))
            {
                var key = string.Format("TextEnc_{0}", token);
                if (_model.EncodingName != null)
                {
                    _localData.Values[key] = _model.EncodingName;
                    _roamingData.Values[key] = _model.EncodingName;
                }
                else
                {
                    _localData.Values.Remove(key);
                    _roamingData.Values.Remove(key);
                }
            }
        }

        #endregion
    }
}
