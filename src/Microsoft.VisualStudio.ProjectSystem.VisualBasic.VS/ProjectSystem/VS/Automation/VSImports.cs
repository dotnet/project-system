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
    [Order(10)]
    internal class VSImports : ConnectionPointContainer,
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

        public event _dispImportsEvents_ImportAddedEventHandler ImportAdded;
        public event _dispImportsEvents_ImportRemovedEventHandler ImportRemoved;

        [ImportingConstructor]
        public VSImports(
            [Import(ExportContractNames.VsTypes.CpsVSProject)] VSLangProj.VSProject vsProject,
            IProjectThreadingService threadingService,
            ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject,
            IProjectLockService lockService,
            IUnconfiguredProjectVsServices unconfiguredProjectVSServices)
        {
            Requires.NotNull(vsProject, nameof(vsProject));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(activeConfiguredProject, nameof(activeConfiguredProject));
            Requires.NotNull(lockService, nameof(lockService));
            Requires.NotNull(unconfiguredProjectVSServices, nameof(unconfiguredProjectVSServices));

            _vsProject = vsProject;
            _activeConfiguredProject = activeConfiguredProject;
            _lockService = lockService;
            _threadingService = threadingService;
            _unconfiguredProjectVSServices = unconfiguredProjectVSServices;

            AddEventSource(this);
        }

        private ConfiguredProject ConfiguredProject => _activeConfiguredProject.Value;

        public string Item(int lIndex)
        {
            return _threadingService.ExecuteSynchronously(async () =>
            {
                using (var access = await _lockService.ReadLockAsync())
                {
                    var project = await access.GetProjectAsync(ConfiguredProject).ConfigureAwait(true);
                    var importsList = project.GetItems(importItemTypeName)
                                             .Select(p => p.EvaluatedInclude)
                                             .ToList();
                    return importsList.ElementAt(lIndex - 1);
                }
            });
        }

        public void Add(string bstrImport)
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

        public void Remove(object index)
        {
            string importRemoved = null;
            _threadingService.ExecuteSynchronously(async () =>
            {
                using (var access = await _lockService.WriteLockAsync())
                {
                    Microsoft.Build.Evaluation.ProjectItem importProjectItem = null;
                    var project = await access.GetProjectAsync(ConfiguredProject).ConfigureAwait(true);
                    await access.CheckoutAsync(project.Xml.ContainingProject.FullPath).ConfigureAwait(true);
                    if (index is int indexInt)
                    {
                        importProjectItem = project.GetItems(importItemTypeName)
                                                   .ElementAt((indexInt - 1));
                    }
                    else if (index is string removeImport)
                    {
                        importProjectItem = project.GetItems(importItemTypeName)
                                                   .First(i => string.Compare(removeImport, i.EvaluatedInclude, StringComparison.OrdinalIgnoreCase) == 0);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(false, $"Parameter {nameof(index)} is niether an int nor a string");
                        return;
                    }

                    if (importProjectItem.IsImported)
                    {
                        throw new ArgumentException(string.Format(VSPackage.ImportParamNotFound, index.ToString()), nameof(index));
                    }

                    importRemoved = importProjectItem.EvaluatedInclude;
                    project.RemoveItem(importProjectItem);
                }
            });

            OnImportRemoved(importRemoved);
        }

        public IEnumerator GetEnumerator()
        {
            return _threadingService.ExecuteSynchronously(async () =>
            {
                using (var access = await _lockService.ReadLockAsync())
                {
                    var project = await access.GetProjectAsync(ConfiguredProject).ConfigureAwait(true);
                    return project.GetItems(importItemTypeName)
                                  .Select(p => p.EvaluatedInclude)
                                  .GetEnumerator();
                }
            });
        }

        public DTE DTE => _vsProject.DTE;

        public Project ContainingProject => _vsProject.Project;

        public int Count
        {
            get
            {
                return _threadingService.ExecuteSynchronously(async () =>
                {
                    using (var access = await _lockService.ReadLockAsync())
                    {
                        var project = await access.GetProjectAsync(ConfiguredProject).ConfigureAwait(true);
                        return project.GetItems(importItemTypeName).Count;
                    }
                });
            }
        }

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
