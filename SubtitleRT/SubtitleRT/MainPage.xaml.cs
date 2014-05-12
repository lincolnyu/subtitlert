using System;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
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
        private MainPageModel _model;

        public MainPage()
        {
            InitializeComponent();

            _model  = new MainPageModel();
            _model.UpdateRecentFiles();
            DataContext = new MainPageViewModel(_model);
        }

        private void RecentList_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (RecentFileViewModel) e.ClickedItem;
            var file = item.Model.File;
            Frame.Navigate(typeof(PlayerPage), file);
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
                Frame.Navigate(typeof(PlayerPage), file);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _model.UpdateRecentFiles();
        }
    }
}
