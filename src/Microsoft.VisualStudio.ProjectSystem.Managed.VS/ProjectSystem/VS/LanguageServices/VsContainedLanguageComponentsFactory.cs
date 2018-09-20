// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

using IOleAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    [Export(typeof(IVsContainedLanguageComponentsFactory))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class VsContainedLanguageComponentsFactory : IVsContainedLanguageComponentsFactory
    {
        private readonly IVsService<SAsyncServiceProvider, IOleAsyncServiceProvider> _serviceProvider;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectHostProvider _projectHostProvider;
        private readonly IActiveWorkspaceProjectContextHost _projectContextHost;
        private readonly AsyncLazy<IVsContainedLanguageFactory> _containedLanguageFactory;

        [ImportingConstructor]
        public VsContainedLanguageComponentsFactory(IVsService<SAsyncServiceProvider, IOleAsyncServiceProvider> serviceProvider,
                                                    IUnconfiguredProjectVsServices projectVsServices,
                                                    IProjectHostProvider projectHostProvider,
                                                    IActiveWorkspaceProjectContextHost projectContextHost)
        {
            _serviceProvider = serviceProvider;
            _projectVsServices = projectVsServices;
            _projectHostProvider = projectHostProvider;
            _projectContextHost = projectContextHost;

            _containedLanguageFactory = new AsyncLazy<IVsContainedLanguageFactory>(GetContainedLanguageFactoryAsync, projectVsServices.ThreadingService.JoinableTaskFactory);
        }

        public int GetContainedLanguageFactoryForFile(string filePath,
                                                      out IVsHierarchy hierarchy,
                                                      out uint itemid,
                                                      out IVsContainedLanguageFactory containedLanguageFactory)
        {
            (itemid, hierarchy, containedLanguageFactory) = _projectVsServices.ThreadingService.ExecuteSynchronously(() =>
            {
                return GetContainedLanguageFactoryForFileAsync(filePath);
            });

            return (hierarchy == null || containedLanguageFactory == null) ? HResult.Fail : HResult.OK;
        }

        private async Task<(HierarchyId itemid, IVsHierarchy hierarchy, IVsContainedLanguageFactory containedLanguageFactory)> GetContainedLanguageFactoryForFileAsync(string filePath)
        {
            await _projectContextHost.InitializeAsync()
                                      .ConfigureAwait(true);

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            var priority = new VSDOCUMENTPRIORITY[1];
            HResult result = _projectVsServices.VsProject.IsDocumentInProject(filePath, out int isFound, priority, out uint itemid);
            if (result.Failed || isFound == 0)
                return (HierarchyId.Nil, null, null);

            Assumes.False(itemid == HierarchyId.Nil);

            IVsContainedLanguageFactory containedLanguageFactory = await _containedLanguageFactory.GetValueAsync()
                                                                                                  .ConfigureAwait(true);

            if (containedLanguageFactory == null)
                return (HierarchyId.Nil, null, null);

            var hierarchy = (IVsHierarchy)_projectHostProvider.UnconfiguredProjectHostObject.ActiveIntellisenseProjectHostObject;
            if (hierarchy == null)
                return (HierarchyId.Nil, null, null);

            return (itemid, hierarchy, containedLanguageFactory);
        }

        private async Task<IVsContainedLanguageFactory> GetContainedLanguageFactoryAsync()
        {
            Guid languageServiceId = await GetLanguageServiceId().ConfigureAwait(true);
            if (languageServiceId == Guid.Empty)
                return null;

            IOleAsyncServiceProvider serviceProvider = await _serviceProvider.GetValueAsync()
                                                                             .ConfigureAwait(true);

            object service = await serviceProvider.QueryServiceAsync(ref languageServiceId);

            // NOTE: While this type is implemented in Roslyn, we force the cast on 
            // the UI thread because they are free to change this to an STA object
            // which would result in an RPC call from a background thread.
            await _projectVsServices.ThreadingService.SwitchToUIThread();

            return (IVsContainedLanguageFactory)service;
        }

        private async Task<Guid> GetLanguageServiceId()
        {
            ConfigurationGeneral properties = await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync()
                                                                                                        .ConfigureAwait(true);

            return await properties.LanguageServiceId.GetValueAsGuidAsync()
                                                     .ConfigureAwait(true);
        }
    }
}
