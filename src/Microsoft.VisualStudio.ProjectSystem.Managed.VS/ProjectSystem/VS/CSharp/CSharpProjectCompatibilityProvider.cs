// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.ProjectSystem.VS.CSharp
{
    /// <summary>
    ///     Checks a legacy VB project for compatibility with the new project system.
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
