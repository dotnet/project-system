// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    internal partial class AbstractEvaluationCommandLineHandler
    {
        /// <summary>
        ///     Represents a set of differences made to a project along with a version.
        /// </summary>
        [DebuggerDisplay("{Version}")]
        private readonly struct VersionedProjectChangeDiff
        {
            public readonly IComparable Version;
            public readonly IProjectChangeDiff Difference;

            public VersionedProjectChangeDiff(IComparable version, IProjectChangeDiff difference)
            {
                Assumes.NotNull(version);
                Assumes.NotNull(difference);

                Version = version;
                Difference = difference;
            }
        }
    }
}
