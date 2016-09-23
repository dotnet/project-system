// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    [Export(typeof(IVsContainedLanguageComponentsFactory))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicVsContainedLanguageComponentsFactory : VsContainedLanguageComponentsFactoryBase
    {
        private static Guid VisualBasicLanguageServiceGuid = new Guid("e34acdc0-baae-11d0-88bf-00a0c9110049");

        [ImportingConstructor]
        public VisualBasicVsContainedLanguageComponentsFactory(SVsServiceProvider serviceProvider,
                                                        IUnconfiguredProjectVsServices projectServices)
            : base(serviceProvider, projectServices, VisualBasicLanguageServiceGuid)
                                                        
        {
        }
    }
}
