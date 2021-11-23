// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    /// <summary>
    /// An update of <see cref="ILaunchSettingsProvider"/> that provides versioned data
    /// and is joinable. Ideally, everywhere <see cref="ILaunchSettingsProvider.SourceBlock"/>
    /// provides data that feeds into another data flow block we should use this instead,
    /// as joinable data flow blocks can coordinate their work in such a way as to avoid
    /// deadlocks.
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IVersionedLaunchSettingsProvider : IProjectValueDataSource<ILaunchSettings>, ILaunchSettingsProvider3
    {
    }
}
