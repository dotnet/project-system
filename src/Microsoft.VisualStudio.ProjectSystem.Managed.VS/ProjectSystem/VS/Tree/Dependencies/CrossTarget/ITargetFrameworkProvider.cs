// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget
{
    internal interface ITargetFrameworkProvider
    {
        /// <summary>
        /// Parses full tfm or short framework name and returns ITargetFramework instance.
        /// Note: it can throw if framework name has invalid format so we catch it here and 
        /// return null always, to avoid exception. Consumers should handle null.
        /// </summary>
        ITargetFramework GetTargetFramework(string shortOrFullName);

        /// <summary>
        /// Given a target framework it tries to determine which of the given list of other framworks 
        /// is most compatible/closest to the target. If no compatible frameworks, returns null.
        /// </summary>
        ITargetFramework GetNearestFramework(ITargetFramework targetFramework, IEnumerable<ITargetFramework> otherFrameworks);
    }
}
