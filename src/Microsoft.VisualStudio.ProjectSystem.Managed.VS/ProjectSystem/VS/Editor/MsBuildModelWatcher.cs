using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export(typeof(MsBuildModelWatcher))]
    internal class MsBuildModelWatcher : OnceInitializedOnceDisposedAsync
    {
        private readonly IProjectThreadingService _threadingService;
        private readonly IFileSystem _fileSystem;
        private readonly IMsBuildAccessor _accessor;
        private readonly UnconfiguredProject _unconfiguredProject;
        private string _tempFile;

        [ImportingConstructor]
        public MsBuildModelWatcher(IProjectThreadingService threadingService, 
            IFileSystem fileSystem, 
            IMsBuildAccessor accessor, 
            UnconfiguredProject unconfiguredProject) :
            base(threadingService.JoinableTaskContext)
        {
            _threadingService = threadingService;
            _fileSystem = fileSystem;
            _accessor = accessor;
            _unconfiguredProject = unconfiguredProject;
        }

        public void Initialize(string tempFile)
        {
            _tempFile = tempFile;
            _threadingService.ExecuteSynchronously(() => InitializeAsync());
        }

        public void ProjectXmlHandler(object sender, ProjectXmlChangedEventArgs args)
        {
            var xml = args.ProjectXml.RawXml;
            _fileSystem.WriteAllText(_tempFile, xml);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _accessor.SubscribeProjectXmlChangedEventAsync(_unconfiguredProject, ProjectXmlHandler).ConfigureAwait(false);
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
                await _accessor.UnsubscribeProjectXmlChangedEventAsync(_unconfiguredProject, ProjectXmlHandler).ConfigureAwait(false);
        }
    }
}
