// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
