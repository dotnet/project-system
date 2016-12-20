using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    internal interface IFrameOpenCloseListener
    {
        Task InitializeEventsAsync();

        Task DisposeAsync();
    }
}
