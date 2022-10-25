// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework.Actions;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// <para>
    /// Handles Project Query API actions that target the <see cref="ILaunchProfile"/>.
    /// </para>
    /// <para>
    /// Specifically, this type is responsible for creating the appropriate <see cref="IQueryActionExecutor"/>
    /// for a given <see cref="ExecutableStep"/>, and all further processing is handled by that executor.
    /// </para>
    /// </summary>
    [QueryDataProvider(LaunchProfileType.TypeName, ProjectModel.ModelName)]
    [QueryActionProvider(ProjectModelActionNames.SetLaunchProfilePropertyValue, typeof(SetLaunchProfilePropertyValue))]
    [QueryDataProviderZone(ProjectModelZones.Cps)]
    [Export(typeof(IQueryActionProvider))]
    internal sealed class LaunchProfileActionProvider : IQueryActionProvider
    {
        public IQueryActionExecutor CreateQueryActionDataTransformer(ExecutableStep executableStep)
        {
            Requires.NotNull(executableStep, nameof(executableStep));

            return executableStep.Action switch
            {
                ProjectModelActionNames.SetLaunchProfilePropertyValue => new SetLaunchProfilePropertyAction((SetLaunchProfilePropertyValue)executableStep),

                _ => throw new InvalidOperationException($"{nameof(ProjectActionProvider)} does not handle action '{executableStep.Action}'.")
            };
        }
    }
}
