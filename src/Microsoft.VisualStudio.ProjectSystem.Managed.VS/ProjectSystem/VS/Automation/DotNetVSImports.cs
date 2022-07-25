// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using EnvDTE;
using Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
{
    [Export(typeof(Imports))]
    [Export(typeof(ImportsEvents))]
    [Export(typeof(DotNetVSImports))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [Order(Order.Default)]
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private)]
    internal class DotNetVSImports : ConnectionPointContainer,
                               IEventSource<_dispImportsEvents>,
                               Imports,
                               ImportsEvents
    {
        private const string ImportItemTypeName = "Import";

        private readonly IActiveConfiguredValue<ConfiguredProject> _activeConfiguredProject;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectAccessor _projectAccessor;
        private readonly VSLangProj.VSProject _vsProject;
        private readonly IUnconfiguredProjectVsServices _unconfiguredProjectVSServices;
        private readonly DotNetNamespaceImportsList _importsList;

        public event _dispImportsEvents_ImportAddedEventHandler? ImportAdded;
        public event _dispImportsEvents_ImportRemovedEventHandler? ImportRemoved;

        [ImportingConstructor]
        public DotNetVSImports(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            IProjectThreadingService threadingService,
            IActiveConfiguredValue<ConfiguredProject> activeConfiguredProject,
            IProjectAccessor projectAccessor,
            IUnconfiguredProjectVsServices unconfiguredProjectVSServices,
            DotNetNamespaceImportsList importsList)
        {
            _vsProject = vsProject;
            _activeConfiguredProject = activeConfiguredProject;
            _projectAccessor = projectAccessor;
            _threadingService = threadingService;
            _unconfiguredProjectVSServices = unconfiguredProjectVSServices;
            _importsList = importsList;

            AddEventSource(this);
        }

        private ConfiguredProject ConfiguredProject => _activeConfiguredProject.Value;

        public string Item(int lIndex)
        {
            return _importsList.Item(lIndex);
        }

        public void Add(string bstrImport)
        {
            if (!_importsList.IsPresent(bstrImport))
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    await _projectAccessor.OpenProjectXmlForWriteAsync(_unconfiguredProjectVSServices.Project, project =>
                    {
                        project.AddItem(ImportItemTypeName, bstrImport);
                    });

                    await _importsList.ReceiveLatestSnapshotAsync();
                });
            }
            else
            {
                throw new ArgumentException(string.Format("{0} - Namespace is already imported", bstrImport), nameof(bstrImport));
            }
        }

        public void Remove(object index)
        {
            string? importToRemove = index switch
            {
                int indexInt when _importsList.IsPresent(indexInt) => _importsList.Item(indexInt),
                string removeImport when _importsList.IsPresent(removeImport) => removeImport,
                _ => null
            };

            if (importToRemove is not null)
            {
                _threadingService.ExecuteSynchronously(async () =>
                {
                    await _projectAccessor.OpenProjectForWriteAsync(ConfiguredProject, project =>
                    {
                        Microsoft.Build.Evaluation.ProjectItem importProjectItem = project.GetItems(ImportItemTypeName)
                                                                                          .First(i => string.Equals(importToRemove, i.EvaluatedInclude, StringComparisons.ItemNames));

                        if (importProjectItem.IsImported)
                        {
                            throw new ArgumentException(string.Format(VSResources.ImportsFromTargetCannotBeDeleted, importToRemove), nameof(index));
                        }

                        project.RemoveItem(importProjectItem);
                    });

                    await _importsList.ReceiveLatestSnapshotAsync();
                });
            }
            else
            {
                throw new ArgumentException(string.Format("{0} - index is neither an Int nor a String, or the Namepsace was not found", index), nameof(index));
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _importsList.GetEnumerator();
        }

        public DTE DTE => _vsProject.DTE;

        public Project ContainingProject => _vsProject.Project;

        public int Count => _importsList.Count;

        public object Parent => _unconfiguredProjectVSServices.VsHierarchy;

        internal virtual void OnImportAdded(string importNamespace)
        {
            ImportAdded?.Invoke(importNamespace);
        }

        internal virtual void OnImportRemoved(string importNamespace)
        {
            ImportRemoved?.Invoke(importNamespace);
        }

        public void OnSinkAdded(_dispImportsEvents sink)
        {
            Requires.NotNull(sink, nameof(sink));

            ImportAdded += new _dispImportsEvents_ImportAddedEventHandler(sink.ImportAdded);
            ImportRemoved += new _dispImportsEvents_ImportRemovedEventHandler(sink.ImportRemoved);
        }

        public void OnSinkRemoved(_dispImportsEvents sink)
        {
            ImportAdded -= new _dispImportsEvents_ImportAddedEventHandler(sink.ImportAdded);
            ImportRemoved -= new _dispImportsEvents_ImportRemovedEventHandler(sink.ImportRemoved);
        }
    }
}
