using System;
using Windows.UI.Xaml;

namespace SubtitleRT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PrivacyFlyout
    {
        #region Constructors

        public PrivacyFlyout()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        private void OnLayoutUpdated(object sender, object o)
        {
            var width = PrivacyInfoFlyout.ActualWidth;
            var height = PrivacyInfoFlyout.ActualHeight;
            // TODO need to find out the pre-defined margin
            EmbeddedPage.Width = width - 80;
            EmbeddedPage.Height = height - LinkButton.ActualHeight - 180;
        }

        public void LoadWeb(Uri uri)
        {
            EmbeddedPage.Visibility = Visibility.Visible;
            EmbeddedPage.Navigate(uri);

            LinkButton.NavigateUri = uri;
        }

        #endregion
    }
}
