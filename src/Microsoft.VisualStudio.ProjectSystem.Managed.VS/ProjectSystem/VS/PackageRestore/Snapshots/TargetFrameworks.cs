// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Immutable collection of <see cref="IVsTargetFrameworkInfo"/> objects.
    /// </summary>
    internal class TargetFrameworks : ImmutablePropertyCollection<IVsTargetFrameworkInfo2>, IVsTargetFrameworks2
    {
        public TargetFrameworks(IEnumerable<IVsTargetFrameworkInfo2> items)
            : base(items, item => item.TargetFrameworkMoniker)
        {
        }
    }
}
