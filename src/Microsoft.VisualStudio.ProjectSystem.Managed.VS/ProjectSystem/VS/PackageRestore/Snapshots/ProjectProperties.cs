// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.PackageRestore
{
    /// <summary>
    ///     Immutable collection of <see cref="IVsProjectProperty"/> objects.
    /// </summary>
    internal class ProjectProperties : ImmutablePropertyCollection<IVsProjectProperty>, IVsProjectProperties
    {
        public ProjectProperties(IEnumerable<IVsProjectProperty> items)
            : base(items, item => item.Name)
        {
        }
    }
}
