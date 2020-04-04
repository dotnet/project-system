// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget
{
    internal enum RuleSource
    {
        /// <summary>
        ///     Rule data sourced by evaluation.
        /// </summary>
        Evaluation,

        /// <summary>
        ///     Rule data sourced by both evaluation and design-time build,
        ///     joined by project version to ensure consistency.
        /// </summary>
        Joint
    }
}
