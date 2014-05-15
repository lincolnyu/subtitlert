using System.Collections.Generic;
using System.IO;
using Apollo;
using SubtitleRT.Models;

namespace SubtitleRT.ViewModels
{
    public class RecentFileViewModel : BaseViewModel<RecentFile>
    {
        #region Constructors

        static RecentFileViewModel()
        {
            var info = ViewModelInfoRegistry.Instance.ViewModelToInfo[typeof (RecentFileViewModel)] = new ViewModelInfo
            {
                AllowTrivialMapping = true
            };
            info.PropertyMapper["FilePath"] = new HashSet<string> { "FileName", "FileFolder" };
        }

        public RecentFileViewModel(RecentFile model)
            : base(model)
        {
        }

        #endregion

        #region Properties

        public string FilePath
        {
            get
            {
                return Model.FilePath;
            }
        }

        public string FileName
        {
            get
            {
                return Path.GetFileName(Model.FilePath);
            }
        }

        public string FileFolder
        {
            get
            {
                return Path.GetDirectoryName(Model.FilePath);
            }
        }

        #endregion
    }
}
