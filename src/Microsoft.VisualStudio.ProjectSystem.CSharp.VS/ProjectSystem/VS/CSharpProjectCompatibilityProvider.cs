// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Checks a legacy VB project for compability with the new project system.
    /// </summary>
    [SupportedProjectTypeGuid(ProjectType.LegacyCSharp)]
    [Export(ExportContractNames.Extensions.SupportedProjectTypeGuid)]
    [Export(typeof(IFlavoredProjectCompatibilityProvider))]
    [ProjectTypeGuidFilter(ProjectType.LegacyCSharp)]
    [AppliesTo(ProjectCapabilities.AlwaysApplicable)]
    internal class CSharpProjectCompatibilityProvider : IFlavoredProjectCompatibilityProvider
    {
        [ImportingConstructor]
        public CSharpProjectCompatibilityProvider()
        {
        }

        public Task<bool> IsProjectCompatibleAsync(ProjectRootElement project)
        {
            return TaskResult.True;
        }

        public Task<bool> IsProjectNeedBeUpgradedAsync(ProjectRootElement project)
        {
            // We need to fill this out: https://github.com/dotnet/roslyn/issues/11285
            return TaskResult.False;
        }
    }
}
