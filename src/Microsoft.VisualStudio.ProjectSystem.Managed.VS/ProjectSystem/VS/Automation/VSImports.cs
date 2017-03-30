using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using VSLangProj;
using System.Linq;
using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    internal class VSImports : OnceInitializedOnceDisposedAsync,
        Imports
    {
        private readonly ActiveConfiguredProject<ConfiguredProject> _activeConfiguredProject;
        private readonly IProjectLockService _lockService;
        private readonly VSLangProj.VSProject _vsProject;
        private readonly ImportsEvents _importsEvents;

        private List<string> _importsList;

        [ImportingConstructor]
        public VSImports(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            IUnconfiguredProjectCommonServices commonServices,
            ActiveConfiguredProject<ConfiguredProject> configuredProject,
            IProjectLockService lockService,
            ImportsEvents importEvents)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            _vsProject = vsProject;
            _activeConfiguredProject = configuredProject;
            _lockService = lockService;
            _importsEvents = importEvents;
        }

        [Export(typeof(Imports))]
        [AppliesTo(ProjectCapability.VisualBasic)]
        public VSImports ImportsImpl
        {
            get
            {
                return this;
            }
        }

        [ProjectAutoLoad(startAfter: ProjectLoadCheckpoint.ProjectFactoryCompleted)]
        [AppliesTo(ProjectCapability.VisualBasic)]
        public Task Initialize()
        {
            return InitializeCoreAsync(CancellationToken.None);
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            using (var access = await _lockService.ReadLockAsync())
            {
                var project = await access.GetProjectAsync(_activeConfiguredProject.Value, cancellationToken).ConfigureAwait(false);
                _importsList = project.GetItems("Import")
                                      .Select(p => p.EvaluatedInclude)
                                      .ToList();
            }

            _importsEvents.ImportAdded += _importsEvents_ImportAdded;
            _importsEvents.ImportRemoved += _importsEvents_ImportRemoved;
        }

        private void _importsEvents_ImportRemoved(string bstrImport)
        {
            var index = _importsList.IndexOf(bstrImport);
            if (index < 0)
            {
                throw new ArgumentException($"{bstrImport} is not present in the List of Imported Namespaces", nameof(bstrImport));
            }

            Remove(index);
        }

        private void _importsEvents_ImportAdded(string bstrImport)
        {
            Add(bstrImport);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            _importsEvents.ImportAdded -= _importsEvents_ImportAdded;
            _importsEvents.ImportRemoved -= _importsEvents_ImportRemoved;

            return Task.CompletedTask;
        }

        public string Item(int lIndex)
        {
            return _importsList.ElementAt(lIndex);
        }

        public void Add(string bstrImport)
        {
            _importsList.Add(bstrImport);
        }

        public void Remove(object index)
        {
            _importsList.RemoveAt((int)index);
        }

        public IEnumerator GetEnumerator()
        {
            return _importsList.GetEnumerator();
        }

        public DTE DTE => _vsProject.DTE;

        public object Parent => throw new System.NotImplementedException();

        public Project ContainingProject => _vsProject.Project;

        public int Count => _importsList.Count;
    }
}
