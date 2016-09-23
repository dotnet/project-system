// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    internal abstract class VsContainedLanguageComponentsFactoryBase : IVsContainedLanguageComponentsFactory
    {
        public VsContainedLanguageComponentsFactoryBase(SVsServiceProvider serviceProvider,
                                                        IUnconfiguredProjectVsServices projectServices,
                                                        Guid languageServiceGuid)
        {
            ServiceProvider = serviceProvider;
            ProjectServices = projectServices;
            LanguageServiceGuid = languageServiceGuid;
        }

        private Guid LanguageServiceGuid { get; }
        private SVsServiceProvider ServiceProvider { get; }
        private IUnconfiguredProjectVsServices ProjectServices { get; }

        /// <summary>
        ///     Gets an object that represents a host-specific IVsContainedLanguageFactory implementation.
        ///     Note: currently we have only one target framework and IVsHierarchy and itemId is returned as 
        ///     they are from the unconfigured project. Later when combined intellisense is implemented, depending
        ///     on implementation we might need to have a logic that returns IVsHierarchy and itemId specific to 
        ///     currently active target framework (thats how it was in Dev14's dnx/dotnet project system)
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

            ProjectServices.ThreadingService.JoinableTaskFactory.Run(async () =>
            {
                await ProjectServices.ThreadingService.JoinableTaskFactory.SwitchToMainThreadAsync();

                var priority = new VSDOCUMENTPRIORITY[1];
                int isFound;
                HResult result = ProjectServices.VsProject.IsDocumentInProject(filePath, 
                                                                               out isFound, 
                                                                               priority, 
                                                                               out myItemId);
                if (result.Failed || isFound == 0)
                {
                    return;
                }
             
                myHierarchy = ProjectServices.VsHierarchy;

                var oleServiceProvider = ServiceProvider.GetService(typeof(IOLEServiceProvider)) as IOLEServiceProvider;
                if (oleServiceProvider == null)
                {
                    return;
                }

                myContainedLanguageFactory = (IVsContainedLanguageFactory)PackageUtilities.QueryService(
                                                oleServiceProvider,
                                                LanguageServiceGuid);
            });

            hierarchy = myHierarchy;
            itemid = myItemId;
            containedLanguageFactory = myContainedLanguageFactory;

            return (myHierarchy == null || containedLanguageFactory == null) 
                ? VSConstants.E_FAIL 
                : VSConstants.S_OK;
        }
    }
}
