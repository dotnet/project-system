using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export]
    internal class TempProjectFileList
    {
        private IList<string> _openedFiles = new List<string>();
        private object _lock = new object();

        /// <summary>
        /// Adds a file to the list of opened project files that need to be cleaned on solution close.
        /// </summary>
        public void AddFile(string newFile)
        {
            lock (_lock)
            {
                _openedFiles.Add(newFile);

            }
        }

        /// <summary>
        /// Gets a snapshot of all opened files.
        /// </summary>
        public IList<string> GetOpenedFiles()
        {
            lock (_lock)
            {
                return _openedFiles.Select(f => f).ToList();
            }
        }
    }
}
