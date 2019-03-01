// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem
{
    // WORKAROUND: https://github.com/dotnet/project-system/issues/4626
    internal static class ProjectSystemContractScope2
    {
        /// <summary>
        ///     The global scope that is shared across all of the host.
        /// </summary>
        public const ProjectSystemContractScope Global = (ProjectSystemContractScope)(-1);
    }
}
