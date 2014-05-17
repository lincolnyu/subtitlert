using System;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SubtitleRT.Helpers;
using SubtitleRT.Models;
using SubtitleRT.ViewModels;

namespace SubtitleRT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage
    {
        #region Fields

        private readonly MainPageModel _model;

        #endregion

        #region Constructors

        public MainPage()
        {
            InitializeComponent();

            _model  = new MainPageModel();
            _model.UpdateRecentFiles();
            DataContext = new MainPageViewModel(_model);
        }

        #endregion

        #region Methods

        private async void RecentList_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (RecentFileViewModel) e.ClickedItem;
            var file = item.Model.File;
            if (file != null)
            {
                file.AddToRecent();
                Frame.Navigate(typeof(PlayerPage), file);
            }
            else
            {
                var msg = new MessageDialog("Error loading the specified recent file which may no longer exist.");
                await msg.ShowAsync();
            }
        }

        private async void BtnLoadFile_OnClick(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            filePicker.FileTypeFilter.Add(".srt");
            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                file.AddToRecent();
                Frame.Navigate(typeof (PlayerPage), file);
            }
            // if it's null it's very likely cancelled by the user
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _model.UpdateRecentFiles();
        }

        #endregion
    }
}
