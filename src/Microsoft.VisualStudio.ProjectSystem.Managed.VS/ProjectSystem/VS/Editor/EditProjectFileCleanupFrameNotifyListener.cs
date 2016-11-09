using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal class EditProjectFileCleanupFrameNotifyListener : IVsWindowFrameNotify2
    {
        private readonly string _tempFile;
        private readonly IFileSystem _fileSystem;
        private readonly IMsBuildModelWatcher _watcher;

        public EditProjectFileCleanupFrameNotifyListener(string tempFile, IFileSystem fileSystem, IMsBuildModelWatcher watcher)
        {
            _tempFile = tempFile;
            _fileSystem = fileSystem;
            _watcher = watcher;
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            _watcher.Dispose();
            _fileSystem.RemoveDirectory(Path.GetDirectoryName(_tempFile), true);
            return VSConstants.S_OK;
        }
    }
}
