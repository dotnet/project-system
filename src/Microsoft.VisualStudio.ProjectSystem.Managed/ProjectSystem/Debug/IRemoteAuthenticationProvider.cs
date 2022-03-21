// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ZeroOrMore)]
    internal interface IRemoteAuthenticationProvider
    {
        /// <summary>
        /// Name displayed in the App Designer. Should be localized
        /// </summary>
        string DisplayName { get; }
        /// <summary>
        /// Name that is serialized to the launchSettings.json file. Shouldn't be localized
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Guid passed to the debugger
        /// </summary>
        Guid PortSupplierGuid { get; }
        /// <summary>
        /// Guid used for to specify and read the authentication mode for the Remote Discovery Dialog
        /// </summary>
        Guid AuthenticationModeGuid { get; }
        /// <summary>
        /// Allows the authentication provider to influence the Remote Discovery Dialog
        /// </summary>
        uint AdditionalRemoteDiscoveryDialogFlags { get; }
    }
}
