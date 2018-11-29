using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.TempPE
{
    [Export(typeof(IFileActionHandler))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class TempPEFileSaveWatcher : IFileActionHandler
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly ITempPEBuildManager _tempPEBuildManager;

        [ImportingConstructor]
        public TempPEFileSaveWatcher(IProjectThreadingService threadingService, ITempPEBuildManager tempPEBuildManager)
        {
            _threadingService = threadingService;
            _tempPEBuildManager = tempPEBuildManager;
        }

        public async Task<bool> TryHandleFileSavedAsync(IProjectTree tree, string newFilePath, bool saveAs)
        {
            await _threadingService.SwitchToUIThread();

            var path = tree.BrowseObjectProperties.ItemName;

            if (path != null)
            {
                _tempPEBuildManager.TryFireTempPEDirty(path);
            }

            // we're just listening in here, not taking action, so return false always
            return false;
        }
    }
}
