// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
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
