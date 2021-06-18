// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension, Cardinality = ImportCardinality.ZeroOrMore)]
    public interface IDefaultLaunchProfileProvider
    {
        ILaunchProfile? CreateDefaultProfile();
    }
}
