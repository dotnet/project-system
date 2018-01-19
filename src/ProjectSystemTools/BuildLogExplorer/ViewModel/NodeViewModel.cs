using Microsoft.VisualStudio.ProjectSystem.LogModel;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogExplorer.ViewModel
{
    internal abstract class NodeViewModel : BaseViewModel
    {
        protected abstract Node Node { get; }

        public virtual bool IsPrimary => false;

        public string Elapsed => $"{Node.EndTime - Node.StartTime:mm':'ss'.'ff}";

        public Result Result => Node.Result;
    }
}
