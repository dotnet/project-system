// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.ProjectSystem.VS.LanguageServices
{
    [Export(typeof(IVsContainedLanguageComponentsFactory))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpVsContainedLanguageComponentsFactory : VsContainedLanguageComponentsFactoryBase
    {
        [ImportingConstructor]
        public CSharpVsContainedLanguageComponentsFactory(
            IUnconfiguredProjectCommonServices commonServices,
            SVsServiceProvider serviceProvider,
            IUnconfiguredProjectVsServices projectServices,
            IProjectHostProvider projectHostProvider,
            ILanguageServiceHost languageServiceHost)
            : base(commonServices, serviceProvider, projectServices, projectHostProvider, languageServiceHost)
        {
        }
    }
}
