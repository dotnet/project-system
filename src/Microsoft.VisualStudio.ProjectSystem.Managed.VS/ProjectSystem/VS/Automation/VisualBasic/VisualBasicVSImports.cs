// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    [Export(typeof(Imports))]
    [Export(typeof(ImportsEvents))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    [Order(Order.Default)]
    internal class VisualBasicVSImports : ConnectionPointContainer,
                               IEventSource<_dispImportsEvents>,
                               Imports,
                               ImportsEvents
    {
        private const string ImportItemTypeName = "Import";

        private readonly ActiveConfiguredProject<ConfiguredProject> _activeConfiguredProject;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectAccessor _projectAccessor;
        private readonly VSLangProj.VSProject _vsProject;
        private readonly IUnconfiguredProjectVsServices _unconfiguredProjectVSServices;
        private readonly VisualBasicNamespaceImportsList _importsList;

        public event _dispImportsEvents_ImportAddedEventHandler? ImportAdded;
        public event _dispImportsEvents_ImportRemovedEventHandler? ImportRemoved;

        [ImportingConstructor]
        public VisualBasicVSImports(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            IProjectThreadingService threadingService,
            ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject,
            IProjectAccessor projectAccessor,
            IUnconfiguredProjectVsServices unconfiguredProjectVSServices,
            VisualBasicNamespaceImportsList importsList)
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
                _threadingService.ExecuteSynchronously(() =>
                {
                    return _projectAccessor.OpenProjectXmlForWriteAsync(_unconfiguredProjectVSServices.Project, project =>
                    {
                        project.AddItem(ImportItemTypeName, bstrImport);
                    });
                });

                OnImportAdded(bstrImport);
            }
            else
            {
                throw new ArgumentException(string.Format("{0} - Namespace is already imported", bstrImport), nameof(bstrImport));
            }
        }

        public void Remove(object index)
        {
            bool intIndexPresent = index is int indexInt && _importsList.IsPresent(indexInt);
            bool stringIndexPresent = index is string removeImport && _importsList.IsPresent(removeImport);

            if (intIndexPresent || stringIndexPresent)
            {
                string? importRemoved = null;
                _threadingService.ExecuteSynchronously(() =>
                {
                    return _projectAccessor.OpenProjectForWriteAsync(ConfiguredProject, project =>
                    {
                        Microsoft.Build.Evaluation.ProjectItem importProjectItem;
                        if (index is string removeImport1)
                        {
                            importProjectItem = project.GetItems(ImportItemTypeName)
                                                       .First(i => string.Equals(removeImport1, i.EvaluatedInclude, StringComparisons.ItemNames));
                        }
                        else if (index is int indexInt1)
                        {
                            importProjectItem = project.GetItems(ImportItemTypeName)
                                                       .OrderBy(i => i.EvaluatedInclude)
                                                       .ElementAt(indexInt1 - 1);
                        }
                        else
                        {
                            // Cannot reach this point, since index has to be Int or String
                            throw Assumes.NotReachable();
                        }

                        if (importProjectItem.IsImported)
                        {
                            throw new ArgumentException(string.Format(VSResources.ImportsFromTargetCannotBeDeleted, index.ToString()), nameof(index));
                        }

                        importRemoved = importProjectItem.EvaluatedInclude;
                        project.RemoveItem(importProjectItem);
                    });
                });

                Assumes.NotNull(importRemoved);

                OnImportRemoved(importRemoved);
            }
            else if (index is string)
            {
                throw new ArgumentException(string.Format("{0} - Namespace is not present ", index), nameof(index));
            }
            else
            {
                throw new ArgumentException(string.Format("{0} - index is neither an Int nor a String", index), nameof(index));
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
