// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Packaging;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Checks a legacy C# project for compability with the new project system.
    /// </summary>
    [SupportedProjectTypeGuid(VisualBasicProjectSystemPackage.LegacyProjectTypeGuid)]
    [Export(ExportContractNames.Extensions.SupportedProjectTypeGuid)]
    [Export(typeof(IFlavoredProjectCompatibilityProvider))]
    [ProjectTypeGuidFilter(VisualBasicProjectSystemPackage.LegacyProjectTypeGuid)]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class VisualBasicProjectCompatibilityProvider : IFlavoredProjectCompatibilityProvider
    {
        [ImportingConstructor]
        public VisualBasicProjectCompatibilityProvider()
        {
        }

        public Task<bool> IsProjectCompatibleAsync(ProjectRootElement project)
        {
            return Task.FromResult(true);
        }

        public Task<bool> IsProjectNeedBeUpgradedAsync(ProjectRootElement project)
        {
            // We need to fill this out: https://github.com/dotnet/roslyn/issues/11285
            return Task.FromResult(false);
        }
    }
}
