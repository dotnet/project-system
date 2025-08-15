// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem;

/// <summary>
/// Provides rule-based property access.
/// </summary>
[Export]
[ExcludeFromCodeCoverage]
[ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
internal partial class ProjectPropertiesAccess : StronglyTypedPropertyAccess
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectPropertiesAccess"/> class.
    /// </summary>
    [ImportingConstructor]
    public ProjectPropertiesAccess([Import] ConfiguredProject configuredProject)
        : base(configuredProject)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectPropertiesAccess"/> class.
    /// </summary>
    public ProjectPropertiesAccess(ConfiguredProject configuredProject, string file, string itemType, string itemName)
        : base(configuredProject, file, itemType, itemName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectPropertiesAccess"/> class.
    /// </summary>
    public ProjectPropertiesAccess(ConfiguredProject configuredProject, IProjectPropertiesContext projectPropertiesContext)
        : base(configuredProject, projectPropertiesContext)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectPropertiesAccess"/> class.
    /// </summary>
    public ProjectPropertiesAccess(ConfiguredProject configuredProject, UnconfiguredProject project)
        : base(configuredProject, project)
    {
    }

    public new ConfiguredProject ConfiguredProject
    {
        get { return base.ConfiguredProject; }
    }
}
