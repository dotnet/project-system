// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
        public static readonly Guid SolutionViewProjectGuid = new("F9806588-A88E-4429-8BFD-228795DB3894");
    }
}
