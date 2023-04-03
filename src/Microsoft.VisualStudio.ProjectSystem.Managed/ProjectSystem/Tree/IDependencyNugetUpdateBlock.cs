// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;

namespace Microsoft.VisualStudio.ProjectSystem.Tree;

[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal interface IDependencyNugetUpdateBlock : IProjectValueDataSource<Dictionary<string, DiagnosticLevel>>
{
}
