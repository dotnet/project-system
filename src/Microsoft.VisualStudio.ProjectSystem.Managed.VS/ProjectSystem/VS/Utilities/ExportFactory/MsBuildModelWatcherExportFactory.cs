using Microsoft.VisualStudio.ProjectSystem.VS.Editor;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities.ExportFactory
{
    [Export(typeof(IExportFactory<MsBuildModelWatcher>))]
    class MsBuildModelWatcherExportFactory : IExportFactory<MsBuildModelWatcher>
    {
        private readonly ExportFactory<MsBuildModelWatcher> _factory;

        [ImportingConstructor]
        public MsBuildModelWatcherExportFactory(ExportFactory<MsBuildModelWatcher> factory)
        {
            _factory = factory;
        }

        public MsBuildModelWatcher CreateExport()
        {
            return _factory.CreateExport().Value;
        }
    }
}
