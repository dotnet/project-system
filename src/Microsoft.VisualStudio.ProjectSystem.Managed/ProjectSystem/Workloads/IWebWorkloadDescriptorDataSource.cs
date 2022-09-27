// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Workloads
{
    /// <summary>
    /// Project value data source for instances of <see cref="WorkloadDescriptor"/>.
    /// 
    /// Detect if DotNetCoreRazor and either WPF or WinForms are in the project.
    /// 
    /// This is to handle scenarios where Visual Studio developer may have an install of VS with only the desktop workload
    /// and a developer may open a WPF/WinForms project (or edit an existing one) to be able to create a hybrid app (WPF + Blazor web)
    /// 
    /// When the developer takes this action *and* does not have the Web Workload installed in their VS instance the IPA should prompt to install web workload
    /// </summary>
    [ProjectSystemContract(ProjectSystemContractScope.ConfiguredProject, ProjectSystemContractProvider.Private, Cardinality = Composition.ImportCardinality.ExactlyOne)]
    internal interface IWebWorkloadDescriptorDataSource : IProjectValueDataSource<ISet<WorkloadDescriptor>>
    {
    }
}
