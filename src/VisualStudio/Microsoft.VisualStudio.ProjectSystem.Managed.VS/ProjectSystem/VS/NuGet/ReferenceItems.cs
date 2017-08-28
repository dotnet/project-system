// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class ReferenceItems : VsItemList<IVsReferenceItem>, IVsReferenceItems
    {
        public ReferenceItems() : base() { }

        public ReferenceItems(IEnumerable<IVsReferenceItem> collection) : base(collection) { }

        protected override string GetKeyForItem(IVsReferenceItem value) => value.Name;
    }
}
