// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Retargetting
{
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.System, Cardinality = Composition.ImportCardinality.ZeroOrMore)]
    internal interface IProjectRetargetCheckProvider
    {
        TargetDescriptionBase? Check(IImmutableDictionary<string, IProjectRuleSnapshot> projectState);
    }
}
