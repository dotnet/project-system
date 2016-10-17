// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using NuGet.SolutionRestoreManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class ReferenceItems : VsItemList<IVsReferenceItem>, IVsReferenceItems
    {
        public ReferenceItems() : base() { }

        public ReferenceItems(IEnumerable<IVsReferenceItem> collection) : base(collection) { }

        protected override String GetKeyForItem(IVsReferenceItem value) => value.Name;
    }
}
