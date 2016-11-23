using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using System.IO;
using System;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Editor
{
    [Export]
    internal class TempProjectFileCleanupSolutionListener : OnceInitializedOnceDisposedAsync, IVsSolutionEvents
    {
        private readonly TempProjectFileList _tempFiles;
        private readonly IServiceProvider _serviceProvider;
        private readonly IProjectThreadingService _threadingService;
        private uint _solutionCookie;
        private IFileSystem _fileSystem;

        [ImportingConstructor]
        public TempProjectFileCleanupSolutionListener(TempProjectFileList tempFiles,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IProjectThreadingService threadingService,
            IFileSystem fileSystem) :
            base(threadingService.JoinableTaskContext)
        {
            _tempFiles = tempFiles;
            _serviceProvider = serviceProvider;
            _threadingService = threadingService;
            _fileSystem = fileSystem;
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
        public Task Initialize()
        {
            return InitializeAsync(CancellationToken.None);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _threadingService.SwitchToUIThread();
            var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
            Verify.HResult(solution.AdviseSolutionEvents(this, out _solutionCookie));
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                await _threadingService.SwitchToUIThread();
                var solution = _serviceProvider.GetService<IVsSolution, SVsSolution>();
                solution.UnadviseSolutionEvents(_solutionCookie);
            }
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            foreach (var tempPath in _tempFiles.GetOpenedFiles())
            {
                _fileSystem.RemoveDirectory(Path.GetDirectoryName(tempPath), true);
            }

            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }
    }
}
