// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Construction;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Checks a legacy VB project for compability with the new project system.
    /// </summary>
    [SupportedProjectTypeGuid(CSharpProjectSystemPackage.LegacyProjectTypeGuid)]
    [Export(ExportContractNames.Extensions.SupportedProjectTypeGuid)]
    [Export(typeof(IFlavoredProjectCompatibilityProvider))]
    [ProjectTypeGuidFilter(CSharpProjectSystemPackage.LegacyProjectTypeGuid)]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class CSharpProjectCompatibilityProvider : IFlavoredProjectCompatibilityProvider
    {
        [ImportingConstructor]
        public CSharpProjectCompatibilityProvider()
        {
        }

        public Task<bool> IsProjectCompatibleAsync(ProjectRootElement project)
        {
            return Task.FromResult(true);
        }

        public Task<bool> IsProjectNeedBeUpgradedAsync(ProjectRootElement project)
        {
            return Task.FromResult(true);
        }
    }
}
