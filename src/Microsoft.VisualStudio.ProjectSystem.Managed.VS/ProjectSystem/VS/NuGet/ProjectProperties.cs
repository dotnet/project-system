// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class ProjectProperties : VsItemList<IVsProjectProperty>, IVsProjectProperties
    {
        public ProjectProperties() : base() { }

        public ProjectProperties(IEnumerable<IVsProjectProperty> collection) : base(collection) { }

        protected override string GetKeyForItem(IVsProjectProperty value) => value.Name;
    }
}
