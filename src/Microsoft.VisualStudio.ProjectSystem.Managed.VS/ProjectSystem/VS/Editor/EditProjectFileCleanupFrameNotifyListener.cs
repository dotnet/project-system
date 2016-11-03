using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal class EditProjectFileCleanupFrameNotifyListener : IVsWindowFrameNotify2
    {
        private readonly string _tempFile;
        private readonly IFileSystem _fileSystem;

        public EditProjectFileCleanupFrameNotifyListener(string tempFile, IFileSystem fileSystem)
        {
            _tempFile = tempFile;
            _fileSystem = fileSystem;
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            _fileSystem.RemoveFile(_tempFile);
            return VSConstants.S_OK;
        }
    }
}
