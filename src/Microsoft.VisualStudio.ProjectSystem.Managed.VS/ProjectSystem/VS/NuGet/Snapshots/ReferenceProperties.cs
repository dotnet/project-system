// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class ReferenceProperties : VsItemList<IVsReferenceProperty>, IVsReferenceProperties
    {
        public ReferenceProperties() : base() { }

        public ReferenceProperties(IEnumerable<IVsReferenceProperty> collection) : base(collection) { }

        protected override string GetKeyForItem(IVsReferenceProperty value) => value.Name;
    }
}
