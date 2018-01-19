using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.BuildLogging.UI
{
    internal class BuildLogTableEventProcessor : TableControlEventProcessorBase
    {
        private readonly BuildLoggingToolWindow _toolWindow;

        public BuildLogTableEventProcessor(BuildLoggingToolWindow toolWindow)
        {
            _toolWindow = toolWindow;
        }

        public override void PostprocessNavigate(ITableEntryHandle entryHandle, TableEntryNavigateEventArgs e) => _toolWindow.ExploreLog(entryHandle);

        public override void PreprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            ProjectSystemToolsPackage.UpdateQueryStatus();

            base.PreprocessSelectionChanged(e);
        }
    }
}
