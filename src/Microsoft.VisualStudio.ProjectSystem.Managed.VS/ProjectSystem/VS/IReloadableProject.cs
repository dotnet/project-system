using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    /// Interface passed to the IProjectReloadManager by reloadable projects. Used by the ProjectReloadManager to 
    /// have the project reload itself from a mewer project file om disk
    /// </summary>
    internal interface IReloadableProject
    {
        string ProjectFile { get; }
        IVsHierarchy VsHierarchy { get; }

        Task<ProjectReloadResult> ReloadProjectAsync();
    }
}