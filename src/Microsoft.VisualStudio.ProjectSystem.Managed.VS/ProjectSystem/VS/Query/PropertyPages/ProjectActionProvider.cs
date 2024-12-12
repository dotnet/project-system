﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Query.Execution;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework.Actions;
using Microsoft.VisualStudio.ProjectSystem.Query.Metadata;
using Microsoft.VisualStudio.ProjectSystem.Query.Providers;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query;

/// <summary>
/// <para>
/// Handles Project Query API actions that target the <see cref="ProjectSystem.Query.IProjectSnapshot"/>.
/// </para>
/// <para>
/// Specifically, this type is responsible for creating the appropriate <see cref="IQueryActionExecutor"/>
/// for a given <see cref="ExecutableStep"/>, and all further processing is handled by that executor.
/// </para>
/// </summary>
[QueryDataProvider(ProjectSystem.Query.Metadata.ProjectType.TypeName, ProjectModel.ModelName)]
[QueryActionProvider(ProjectModelActionNames.SetEvaluatedUIPropertyValue, typeof(SetEvaluatedUIPropertyValue))]
[QueryActionProvider(ProjectModelActionNames.SetUnevaluatedUIPropertyValue, typeof(SetUnevaluatedUIPropertyValue))]
[QueryActionProvider(ProjectModelActionNames.AddLaunchProfile, typeof(AddLaunchProfile))]
[QueryActionProvider(ProjectModelActionNames.RemoveLaunchProfile, typeof(RemoveLaunchProfile))]
[QueryActionProvider(ProjectModelActionNames.RenameLaunchProfile, typeof(RenameLaunchProfile))]
[QueryActionProvider(ProjectModelActionNames.DuplicateLaunchProfile, typeof(DuplicateLaunchProfile))]
[QueryDataProviderZone(ProjectModelZones.Cps)]
[Export(typeof(IQueryActionProvider))]
internal sealed class ProjectActionProvider : IQueryActionProvider
{
    public IQueryActionExecutor CreateQueryActionDataTransformer(ExecutableStep executableStep)
    {
        Requires.NotNull(executableStep);

        return executableStep.Action switch
        {
            ProjectModelActionNames.SetEvaluatedUIPropertyValue => new ProjectSetEvaluatedUIPropertyValueAction((SetEvaluatedUIPropertyValue)executableStep),
            ProjectModelActionNames.SetUnevaluatedUIPropertyValue => new ProjectSetUnevaluatedUIPropertyValueAction((SetUnevaluatedUIPropertyValue)executableStep),

            ProjectModelActionNames.AddLaunchProfile => new AddLaunchProfileAction((AddLaunchProfile)executableStep),
            ProjectModelActionNames.RemoveLaunchProfile => new RemoveLaunchProfileAction((RemoveLaunchProfile)executableStep),
            ProjectModelActionNames.RenameLaunchProfile => new RenameLaunchProfileAction((RenameLaunchProfile)executableStep),
            ProjectModelActionNames.DuplicateLaunchProfile => new DuplicateLaunchProfileAction((DuplicateLaunchProfile)executableStep),

            _ => throw new InvalidOperationException($"{nameof(ProjectActionProvider)} does not handle action '{executableStep.Action}'.")
        };
    }
}
