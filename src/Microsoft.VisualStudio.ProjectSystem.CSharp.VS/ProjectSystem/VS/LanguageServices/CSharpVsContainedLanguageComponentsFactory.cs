// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    [Export(typeof(IVsContainedLanguageComponentsFactory))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpVsContainedLanguageComponentsFactory : VsContainedLanguageComponentsFactoryBase
    {
        private static Guid CSharpLanguageServiceGuid = new Guid("694dd9b6-b865-4c5b-ad85-86356e9c88dc");
        [ImportingConstructor]
        public CSharpVsContainedLanguageComponentsFactory(SVsServiceProvider serviceProvider,
                                                        IUnconfiguredProjectVsServices projectServices,
                                                        IProjectHostProvider projectHostProvider)
            : base(serviceProvider, projectServices, projectHostProvider, CSharpLanguageServiceGuid)
        {
        }
    }
}
