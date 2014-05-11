using System.Collections.ObjectModel;
using Apollo;
using SubtitleRT.Models;

namespace SubtitleRT.ViewModels
{
    public class RecentFilesViewModel : BaseViewModel<RecentFiles>
    {
        #region Constructors

        public RecentFilesViewModel(RecentFiles model) : base(model)
        {
            RecentFiles = new ObservableCollection<RecentFileViewModel>();
            new ListSync(RecentFiles, model.Files, o => new RecentFileViewModel((RecentFile)o));
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
