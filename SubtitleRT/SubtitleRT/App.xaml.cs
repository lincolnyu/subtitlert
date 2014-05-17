#define EMBEDDED_PRIVACY_STATEMENT

using System;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227
using SubtitleRT.Helpers;

namespace SubtitleRT
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame
                {
                    // Set the default language
                    Language = Windows.Globalization.ApplicationLanguages.Languages[0]
                };

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                InitializeRequirements();
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        #region Capabilities

        private void InitializeRequirements()
        {
            SettingsPane.GetForCurrentView().CommandsRequested += OnSettingsPaneCommandRequested;
        }

        private void OnSettingsPaneCommandRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // Add commands
            args.Request.ApplicationCommands.Add(new SettingsCommand("privacyID", "Privacy Statement", ShowPrivacyStatement));
            args.Request.ApplicationCommands.Add(new SettingsCommand("reviewID", "Rate and Review", DoRateAndReview));
        }

        private void DoRateAndReview(IUICommand command)
        {
            RateAndReview();
        }

        private async void RateAndReview()
        {
            // NOTE make sure it's the same as the package family name shown in the Package.appxmanifest
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:REVIEW?PFN=62356quanbenSoft.SubtitleRT_kxms4zn09601g"));
        }

        private
#if !EMBEDDED_PRIVACY_STATEMENT
 async
#endif
 void ShowPrivacyStatement(IUICommand command)
        {
            var uri = new Uri(@"http://members.dodo.com.au/yiweiyu/apps/privacy/subtitle-rt.html");
#if EMBEDDED_PRIVACY_STATEMENT
            var privacyFlyout = new PrivacyFlyout();
            privacyFlyout.LoadWeb(uri);
            privacyFlyout.ShowIndependent();
#else
            await Windows.System.Launcher.LaunchUriAsync(uri);
#endif
        }

        protected async override void OnFileActivated(FileActivatedEventArgs args)
        {
            base.OnFileActivated(args);

            var file = args.Files.FirstOrDefault() as StorageFile; // only 1 file is supported
            if (file == null)
            {
                return; // an unknown error, not sure what should be the supposed behaviour
            }

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame
                {
                    // Set the default language
                    Language = Windows.Globalization.ApplicationLanguages.Languages[0]
                };

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                InitializeRequirements();
            }

            rootFrame.Navigate(typeof(PlayerPage), file);

            // Ensure the current window is active
            Window.Current.Activate();

            await StorageHelper.VerifyRecentLRU();
            file.AddToRecent();
        }

        #endregion
    }
}
