// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using IOleAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    [Export(typeof(IVsContainedLanguageComponentsFactory))]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class VsContainedLanguageComponentsFactory : IVsContainedLanguageComponentsFactory
    {
        private readonly IAsyncServiceProvider _serviceProvider;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectHostProvider _projectHostProvider;
        private readonly ILanguageServiceHost _languageServiceHost;
        private readonly AsyncLazy<IVsContainedLanguageFactory> _containedLanguageFactory;

        [ImportingConstructor]
        public VsContainedLanguageComponentsFactory(
            IUnconfiguredProjectCommonServices commonServices,
            IAsyncServiceProvider serviceProvider,
            IUnconfiguredProjectVsServices projectServices,
            IProjectHostProvider projectHostProvider,
            ILanguageServiceHost languageServiceHost)
        {
            _serviceProvider = serviceProvider;
            _projectVsServices = projectServices;
            _projectHostProvider = projectHostProvider;
            _languageServiceHost = languageServiceHost;

            _containedLanguageFactory = new AsyncLazy<IVsContainedLanguageFactory>(GetContainedLanguageFactoryAsync, projectServices.ThreadingService.JoinableTaskFactory);
        }

        public int GetContainedLanguageFactoryForFile(string filePath,
                                                      out IVsHierarchy hierarchy,
                                                      out uint itemid,
                                                      out IVsContainedLanguageFactory containedLanguageFactory)
        {
            var result = _projectVsServices.ThreadingService.ExecuteSynchronously(() =>
            {
                return GetContainedLanguageFactoryForFileAsync(filePath);
            });

            hierarchy = result.hierarchy;
            itemid = result.itemid;
            containedLanguageFactory = result.containedLanguageFactory;

            return (hierarchy == null || containedLanguageFactory == null) ? HResult.Fail : HResult.OK;
        }

        private async Task<(IVsHierarchy hierarchy, uint itemid, IVsContainedLanguageFactory containedLanguageFactory)> GetContainedLanguageFactoryForFileAsync(string filePath)
        {
            await _languageServiceHost.InitializeAsync()
                                      .ConfigureAwait(true);

            await _projectVsServices.ThreadingService.SwitchToUIThread();

            var priority = new VSDOCUMENTPRIORITY[1];
            HResult result = _projectVsServices.VsProject.IsDocumentInProject(filePath, out int isFound, priority, out uint itemid);
            if (result.Failed || isFound == 0)
                return (null, HierarchyId.Nil, null);

            var hierarchy = (IVsHierarchy)_projectHostProvider.UnconfiguredProjectHostObject.ActiveIntellisenseProjectHostObject;

            IVsContainedLanguageFactory containedLanguageFactory = await _containedLanguageFactory.GetValueAsync()
                                                                                                  .ConfigureAwait(true);

            return (hierarchy, itemid, containedLanguageFactory);
        }

        private async Task<IVsContainedLanguageFactory> GetContainedLanguageFactoryAsync()
        {
            Guid? languageServiceId = await GetLanguageServiceId().ConfigureAwait(true);
            if (languageServiceId == null)
                return null;

            var serviceProvider = (IOleAsyncServiceProvider)await _serviceProvider.GetServiceAsync(typeof(SAsyncServiceProvider))
                                                                                  .ConfigureAwait(true);

            Guid clsid = languageServiceId.Value;

            return (IVsContainedLanguageFactory)await serviceProvider.QueryServiceAsync(ref clsid);
        }

        private async Task<Guid?> GetLanguageServiceId()
        {
            ConfigurationGeneral properties = await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync()
                                                                                                        .ConfigureAwait(true);

            string languageServiceIdString = (string)await properties.LanguageServiceId.GetValueAsync()
                                                                                       .ConfigureAwait(true);
            if (string.IsNullOrEmpty(languageServiceIdString))
                return null;

            if (!Guid.TryParse(languageServiceIdString, out Guid languageServiceId))
            {
                return null;
            }

            return languageServiceId;
        }
    }
}
