using System.ComponentModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
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
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var file = (StorageFile)e.Parameter;
            _model = new PlayerPageModel(file);
            _viewModel = new PlayerPageViewModel(_model, this);
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;

            _viewModel.NavigationHelper.OnNavigatedTo(e);
            DataContext = _viewModel;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
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

                BtnPlay.Style = (Style)Application.Current.Resources["PauseAppBarButtonStyle"];
                await _model.Play();
            }
            else
            {
                BtnPlay.Style = (Style)Application.Current.Resources["PlayAppBarButtonStyle"];
                await _model.StopPlaying();
            }
        }

        private void LstSubtitles_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedVm = (SubtitleItemViewModel)e.ClickedItem;
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
            }
        }

        private void LstSubtitles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LstSubtitles.SelectedIndex = -1;    // clear selection as we don't need it
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

        #endregion
    }
}
