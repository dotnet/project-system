// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

using NuGet.SolutionRestoreManager;

namespace Microsoft.VisualStudio.ProjectSystem.VS.NuGet
{
    internal class ReferenceItem : IVsReferenceItem
    {
        public ReferenceItem(string name, IEnumerable<IVsReferenceProperty> properties)
        {
            Requires.NotNullOrEmpty(name, nameof(name));
            Requires.NotNull(properties, nameof(properties));

            Name = name;
            Properties = new ReferenceProperties(properties);
        }

        public string Name { get; }

        public IVsReferenceProperties Properties { get; }
    }
}
