using Microsoft.VisualStudio.ProjectSystem;

namespace Microsoft.VisualStudio
{
    internal interface IProjectCreationInfoService
    {
        bool IsNewlyCreated(UnconfiguredProject project);
    }
}
