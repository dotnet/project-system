// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    /// <summary>
    ///     Immutable collection of <see cref="IVsTargetFrameworkInfo"/> objects.
    /// </summary>
    internal class TargetFrameworks : ImmutablePropertyCollection<IVsTargetFrameworkInfo>, IVsTargetFrameworks
    {
        public TargetFrameworks(IEnumerable<IVsTargetFrameworkInfo> items)
            : base(items)
        {
        }

        protected override string GetKeyForItem(IVsTargetFrameworkInfo value) => value.TargetFrameworkMoniker;
    }
}
