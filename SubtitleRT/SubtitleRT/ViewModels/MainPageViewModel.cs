using System.Collections.ObjectModel;
using Apollo;
using SubtitleRT.Models;

namespace SubtitleRT.ViewModels
{
    public class MainPageViewModel : BaseViewModel<MainPageModel>
    {
        #region Constructors

        public MainPageViewModel(MainPageModel model) : base(model)
        {
            RecentFiles = new ObservableCollection<RecentFileViewModel>();
            new ListSync(RecentFiles, model.RecentFiles, o => new RecentFileViewModel((RecentFile)o));
        }

        #endregion

        #region Properties

        public ObservableCollection<RecentFileViewModel> RecentFiles
        {
            get; private set;
        }

        #endregion
    }
}
