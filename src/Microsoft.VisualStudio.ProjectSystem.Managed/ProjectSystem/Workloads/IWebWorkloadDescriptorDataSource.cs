// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Workloads
{
    /// <summary>
    /// Project value data source for instances of <see cref="WorkloadDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Detects if <see cref="ProjectCapability.DotNetRazor"/> and either <see cref="ProjectCapability.WPF"/> or <see cref="ProjectCapability.WindowsForms"/>
    /// capabilities are in the project.
    /// </para>
    /// <para>
    /// Handles scenarios where Visual Studio developer may have an install of VS with only the desktop workload
    /// and a developer may open a WPF/WinForms project (or edit an existing one) to be able to create a hybrid app (WPF + Blazor web).
    /// </para>
    /// <para>
    /// When the developer takes this action *and* does not have the Web Workload installed in their VS instance the IPA should prompt to install web workload.
    /// </para>
    /// </remarks>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IWebWorkloadDescriptorDataSource : IProjectValueDataSource<ISet<WorkloadDescriptor>>
    {
    }
}
