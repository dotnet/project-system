// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemContract(ProjectSystemContractScope.ProjectService, ProjectSystemContractProvider.Private, Cardinality = ImportCardinality.ExactlyOne)]
    internal interface IVsOnlineServices
    {
        /// <summary>
        /// Indicates whether or not the client is connected to VS Online.
        /// </summary>
        bool ConnectedToVSOnline { get; }
    }
}
