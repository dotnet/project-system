﻿using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// This class handles being notified whenever a source file is saved by the hierarchy
    /// </summary>
    [Export(typeof(IFileActionHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class TempPEFileSaveWatcher : IFileActionHandler
    {
        private readonly ITempPEBuildManager _tempPEBuildManager;

        [ImportingConstructor]
        public TempPEFileSaveWatcher(ITempPEBuildManager tempPEBuildManager)
        {
            _tempPEBuildManager = tempPEBuildManager;
        }

        public async Task<bool> TryHandleFileSavedAsync(IProjectTree tree, string newFilePath, bool saveAs)
        {
            string path = tree.BrowseObjectProperties.ItemName;
            // Save As will come through as a file rename so we don't need to double handle
            if (!saveAs && path != null)
            {
                await _tempPEBuildManager.NotifySourceFileDirtyAsync(path);
            }

            // we're just listening in here, not taking action, so return false always
            return false;
        }
    }
}
