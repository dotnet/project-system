using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Automation;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    /// <summary>
    /// This class handles being notified whenever a source file is saved by the hierarchy
    /// </summary>
    [Export(typeof(IFileActionHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class TempPEFileSaveWatcher : IFileActionHandler
    {
        private readonly VSBuildManager _buildManager;
        private readonly Lazy<ITempPEBuildManager> _tempPEBuildManager;

        [ImportingConstructor]
        public TempPEFileSaveWatcher(
            [Import(typeof(BuildManager))]VSBuildManager buildManager,
            Lazy<ITempPEBuildManager> tempPEBuildManager)
        {
            _buildManager = buildManager;
            _tempPEBuildManager = tempPEBuildManager;
        }

        public async Task<bool> TryHandleFileSavedAsync(IProjectTree tree, string newFilePath, bool saveAs)
        {
            // We don't want to be the one to realise the Lazy<ITempPEManager> so ignore changes in projects that don't need TempPE
            if (!_buildManager.ProjectNeedsTempPE) return false;

            string path = tree.BrowseObjectProperties.ItemName;
            // Save As will come through as a file rename so we don't need to double handle
            if (!saveAs && path != null)
            {
                // The magic of MEF means that the instance we get from .Value is the same as the build managers instance, so it has all of the info it needs
                await _tempPEBuildManager.Value.TryFireTempPEDirtyAsync(path);
            }

            // we're just listening in here, not taking action, so return false always
            return false;
        }
    }
}
