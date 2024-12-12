﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

/// <summary>
/// Models changes to a project's dependencies.
/// </summary>
/// <remarks>
/// <para>
/// Used by legacy dependency providers that export <see cref="IProjectDependenciesSubTreeProvider"/>.
/// </para>
/// <para>
/// All dependencies modelled via this interface are unconfigured (do not apply to a specific project
/// configuration).
/// </para>
/// </remarks>
public interface IDependenciesChanges
{
    /// <summary>
    /// Gets models for the set of added and updated dependencies.
    /// </summary>
    IImmutableList<IDependencyModel> AddedNodes { get; }

    /// <summary>
    /// Gets models for the set of removed dependencies.
    /// </summary>
    /// <remarks>
    /// Consumers must only use the <see cref="IDependencyModel.Id"/> and
    /// <see cref="IDependencyModel.ProviderType"/> properties of returned items.
    /// </remarks>
    IImmutableList<IDependencyModel> RemovedNodes { get; }
}
