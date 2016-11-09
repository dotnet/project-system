using Microsoft.Build.Evaluation;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal interface IMsBuildModelWatcher
    {
        void Initialize(string tempFile);
        void ProjectXmlHandler(object sender, ProjectXmlChangedEventArgs args);
        void Dispose();
    }
}