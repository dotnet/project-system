using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;

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
            _fileSystem.RemoveDirectory(Path.GetDirectoryName(_tempFile), true);
            return VSConstants.S_OK;
        }
    }
}
