// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio
{
    /// <summary>
    /// Specifies various constant values needed in the cloud environment.
    /// </summary>
    internal static class CloudEnvironment
    {
        /// <summary>
        /// Name of the Solution Explorer view responsible for showing the contents of a
        /// solution (as opposed to the contents of a folder).
        /// </summary>
        public const string LiveShareSolutionView = "LiveShareSolutionView";

        /// <summary>
        /// Guid indicating that the selected node in Solution Explorer is a project.
        /// </summary>
        public static readonly Guid SolutionViewProjectGuid = new Guid("F9806588-A88E-4429-8BFD-228795DB3894");
    }
}
