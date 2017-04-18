// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.ProjectSystem.VS.ConnectionPoint;
using VSLangProj;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation
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
        private const string importItemTypeName = "Import";

        private readonly ActiveConfiguredProject<ConfiguredProject> _activeConfiguredProject;
        private readonly IProjectThreadingService _threadingService;
        private readonly IProjectLockService _lockService;
        private readonly VSLangProj.VSProject _vsProject;
        private readonly IUnconfiguredProjectVsServices _unconfiguredProjectVSServices;
        private readonly VisualBasicImportsList _importsList;

        public event _dispImportsEvents_ImportAddedEventHandler ImportAdded;
        public event _dispImportsEvents_ImportRemovedEventHandler ImportRemoved;

        [ImportingConstructor]
        public VisualBasicVSImports(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            IProjectThreadingService threadingService,
            ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject,
            IProjectLockService lockService,
            IUnconfiguredProjectVsServices unconfiguredProjectVSServices,
            VisualBasicImportsList importsList)
        {
            Requires.NotNull(vsProject, nameof(vsProject));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(activeConfiguredProject, nameof(activeConfiguredProject));
            Requires.NotNull(lockService, nameof(lockService));
            Requires.NotNull(unconfiguredProjectVSServices, nameof(unconfiguredProjectVSServices));
            Requires.NotNull(importsList, nameof(importsList));

            _vsProject = vsProject;
            _activeConfiguredProject = activeConfiguredProject;
            _lockService = lockService;
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
                    using (var access = await _lockService.WriteLockAsync())
                    {
                        var project = await access.GetProjectAsync(ConfiguredProject).ConfigureAwait(true);
                        await access.CheckoutAsync(project.Xml.ContainingProject.FullPath).ConfigureAwait(true);
                        project.AddItem(importItemTypeName, bstrImport);
                    }
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
            if (index is int indexInt && _importsList.IsPresent(indexInt) ||
                index is string removeImport && _importsList.IsPresent(removeImport))
            {
                string importRemoved = null;
                _threadingService.ExecuteSynchronously(async () =>
                {
                    using (var access = await _lockService.WriteLockAsync())
                    {
                        Microsoft.Build.Evaluation.ProjectItem importProjectItem = null;
                        var project = await access.GetProjectAsync(ConfiguredProject).ConfigureAwait(true);
                        await access.CheckoutAsync(project.Xml.ContainingProject.FullPath).ConfigureAwait(true);
                        if (index is string removeImport1)
                        {
                            importProjectItem = project.GetItems(importItemTypeName)
                                                       .First(i => string.Compare(removeImport1, i.EvaluatedInclude, StringComparison.OrdinalIgnoreCase) == 0);
                        }
                        else if (index is int indexInt1)
                        {
                            importProjectItem = project.GetItems(importItemTypeName)
                                                       .OrderBy(i => i.EvaluatedInclude)
                                                       .ElementAt((indexInt1 - 1));
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(false, $"Parameter {nameof(index)} is niether an int nor a string");
                            throw new ArgumentException(string.Format("Parameter {0} is niether an int nor a string", index), nameof(index));
                        }

                        if (importProjectItem.IsImported)
                        {
                            throw new ArgumentException(string.Format(VisualBasicVSResources.ImportsFromTargetCannotBeDeleted, index.ToString()), nameof(index));
                        }

                        importRemoved = importProjectItem.EvaluatedInclude;
                        project.RemoveItem(importProjectItem);
                    }
                });

                OnImportRemoved(importRemoved);
            }
            else
            {
                throw new ArgumentException(string.Format("{0} - Namespace is not present or index is out of bounds", index), nameof(index));
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
            var importAdded = ImportAdded;
            if (importAdded != null)
            {
                importAdded(importNamespace);
            }
        }

        internal virtual void OnImportRemoved(string importNamespace)
        {
            var importRemoved = ImportRemoved;
            if (importRemoved != null)
            {
                importRemoved(importNamespace);
            }
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
