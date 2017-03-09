// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using PackageUtilities = Microsoft.VisualStudio.Shell.PackageUtilities;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    internal abstract class VsContainedLanguageComponentsFactoryBase : OnceInitializedOnceDisposedAsync, IVsContainedLanguageComponentsFactory
    {
        private readonly SVsServiceProvider _serviceProvider;
        private readonly IUnconfiguredProjectVsServices _projectVsServices;
        private readonly IProjectHostProvider _projectHostProvider;
        private readonly ILanguageServiceHost _languageServiceHost;

        public VsContainedLanguageComponentsFactoryBase(
            IUnconfiguredProjectCommonServices commonServices,
            SVsServiceProvider serviceProvider,
            IUnconfiguredProjectVsServices projectServices,
            IProjectHostProvider projectHostProvider,
            ILanguageServiceHost languageServiceHost)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            _serviceProvider = serviceProvider;
            _projectVsServices = projectServices;
            _projectHostProvider = projectHostProvider;
            _languageServiceHost = languageServiceHost;
        }

        protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            await _languageServiceHost.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override Task DisposeCoreAsync(bool initialized)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Gets an object that represents a host-specific IVsContainedLanguageFactory implementation and
        ///     IVsHierarchy and itemId specific to currently active target framework.
        /// </summary>
        /// <param name="filePath">Path to a file</param>
        /// <param name="hierarchy">Project hierarchy containing given file for current language service</param>
        /// <param name="itemid">item id of the given file</param>
        /// <param name="containedLanguageFactory">an instance of IVsContainedLanguageFactory specific for current language service</param>
        /// <returns></returns>
        public int GetContainedLanguageFactoryForFile(string filePath, 
                                                      out IVsHierarchy hierarchy, 
                                                      out uint itemid, 
                                                      out IVsContainedLanguageFactory containedLanguageFactory)
        {
            uint myItemId = 0;
            IVsHierarchy myHierarchy = null;
            IVsContainedLanguageFactory myContainedLanguageFactory = null;

            _projectVsServices.ThreadingService.JoinableTaskFactory.Run(async () =>
            {
                await InitializeAsync().ConfigureAwait(false);

                Guid? languageServiceId = await GetLanguageServiceId().ConfigureAwait(false);
                if (languageServiceId == null)
                    return;

                await _projectVsServices.ThreadingService.JoinableTaskFactory.SwitchToMainThreadAsync();

                var priority = new VSDOCUMENTPRIORITY[1];
                HResult result = _projectVsServices.VsProject.IsDocumentInProject(filePath, 
                                                                               out int isFound, 
                                                                               priority, 
                                                                               out myItemId);
                if (result.Failed || isFound == 0)
                {
                    return;
                }
             
                myHierarchy = (IVsHierarchy)_projectHostProvider.UnconfiguredProjectHostObject.ActiveIntellisenseProjectHostObject;

                var oleServiceProvider = _serviceProvider.GetService(typeof(IOLEServiceProvider)) as IOLEServiceProvider;
                if (oleServiceProvider == null)
                {
                    return;
                }

                myContainedLanguageFactory = (IVsContainedLanguageFactory)PackageUtilities.QueryService(
                                                oleServiceProvider,
                                                languageServiceId.Value);
            });

            hierarchy = myHierarchy;
            itemid = myItemId;
            containedLanguageFactory = myContainedLanguageFactory;

            return (myHierarchy == null || containedLanguageFactory == null) 
                ? VSConstants.E_FAIL 
                : VSConstants.S_OK;
        }

        public async Task<Guid?> GetLanguageServiceId()
        {
            var properties = await _projectVsServices.ActiveConfiguredProjectProperties.GetConfigurationGeneralPropertiesAsync()
                                                                                       .ConfigureAwait(false);

            var languageServiceIdString = (string)await properties.LanguageServiceId.GetValueAsync()
                                                                                    .ConfigureAwait(false);
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
