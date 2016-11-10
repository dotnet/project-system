using Microsoft.Build.Evaluation;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal interface IMsBuildModelWatcher
    {
        Task InitializeAsync(string tempFile);
        void ProjectXmlHandler(object sender, ProjectXmlChangedEventArgs args);
        void Dispose();
    }
}