using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage.AccessCache;
using Apollo;
using SubtitleRT.Helpers;

namespace SubtitleRT.Models
{
    public class MainPageModel : BaseModel
    {
        #region Constructors

        public MainPageModel()
        {
            RecentFiles = new ObservableCollection<RecentFile>();
        }

        #endregion
        
        #region Properties

        public ObservableCollection<RecentFile> RecentFiles
        {
            get; private set;
        }

        #endregion

        #region Methods

        public async void UpdateRecentFiles()
        {
            RecentFiles.Clear();
            await StorageHelper.VerifyRecentLRU();
            var list = StorageApplicationPermissions.MostRecentlyUsedList;
            var mruEntries = list.Entries;
            foreach (var entry in mruEntries.Reverse())
            {
                var token = entry.Token;
                var file = await list.GetFileAsync(token);
                var recentFile = new RecentFile
                {
                    File = file,
                    Token = token
                };
                RecentFiles.Insert(0, recentFile);
            }
        }

        #endregion
    }
}
