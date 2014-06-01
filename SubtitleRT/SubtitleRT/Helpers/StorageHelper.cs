using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.Storage.AccessCache;

namespace SubtitleRT.Helpers
{
    public static class StorageHelper
    {
        #region Nested types

        public delegate bool RemovePredicate(StorageFile f);

        #endregion

        #region Fields

        /// <summary>
        ///  keeps a look up from file name to token
        ///  since storage file LRU is app wide, it's defined as a global variable
        /// </summary>
        public static readonly Dictionary<string, string> FileNameToToken = new Dictionary<string, string>();

        #endregion

        #region Methods

        // TODO this seems deprecated
        /// <summary>
        ///  Ensures that the app is not in snapped state
        /// </summary>
        /// <returns>True if the app is not snapped</returns>
        public static bool EnsureUnsnapped()
        {
            // FilePicker APIs will not work if the application is in a snapped state.
            // If an app wants to show a FilePicker while snapped, it must attempt to unsnap first
            var unsnapped = ((ApplicationView.Value != ApplicationViewState.Snapped) || ApplicationView.TryUnsnap());
            if (!unsnapped)
            {
                // TODO notify the user
                // NotifyUser("Cannot unsnap the sample.", NotifyType.StatusMessage);
            }

            return unsnapped;
        }

        /// <summary>
        ///  Removes redundant items from recent file LRU
        /// </summary>
        /// <returns>
        ///  A look up that maps file name to token
        /// </returns>
        public static async Task VerifyRecentLRU()
        {
            FileNameToToken.Clear();
            var list = StorageApplicationPermissions.MostRecentlyUsedList;
            var tokensToRemove = new List<string>();

            foreach (var item in list.Entries.Reverse())
            {
                try
                {
                    var f = await list.GetFileAsync(item.Token);
                    if (FileNameToToken.ContainsKey(f.Path))
                    {
                        tokensToRemove.Add(item.Token);
                    }
                    else
                    {
                        FileNameToToken[f.Path] = item.Token;
                    }
                }
                catch (Exception)
                {
                    tokensToRemove.Add(item.Token);
                }
            }
            foreach (var token in tokensToRemove)
            {
                list.Remove(token);
            }
        }

        /// <summary>
        ///  Clear temporary items from the recent file LRU list
        /// </summary>
        public static async Task ClearTempRecentLRU()
        {
            await RemoveFromRecentLRU(f => f.IsTempFile(), true);
        }

        public static async Task RemoveFromRecentLRU(RemovePredicate predicate, bool deleteFile = false)
        {
            var list = StorageApplicationPermissions.MostRecentlyUsedList;
            var tokensToRemove = new List<string>();

            foreach (var item in list.Entries.Reverse())
            {
                try
                {
                    var f = await list.GetFileAsync(item.Token);
                    if (!predicate(f)) continue;
                    tokensToRemove.Add(item.Token);
                    if (deleteFile && f.IsAvailable)
                    {
                        await f.DeleteAsync();
                    }
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch (Exception)
                // ReSharper restore EmptyGeneralCatchClause
                {
                }
            }
            foreach (var token in tokensToRemove)
            {
                if (list.ContainsItem(token))
                {
                    list.Remove(token);
                }
            }
        }

        public static bool IsTempFile(this IStorageFile file)
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            var folderPath = folder.Path;
            var filePath = file.Path;
            return filePath.Contains(folderPath);
        }

        public static void AddToRecent(this IStorageFile file)
        {
            var list = StorageApplicationPermissions.MostRecentlyUsedList;
            string token;
            if (FileNameToToken.TryGetValue(file.Name, out token))
            {
                list.Remove(token);
            }
            list.Add(file);
        }

        #endregion
    }
}
