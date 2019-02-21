// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides the C# implementation of <see cref="IItemTypeGuidProvider"/>.
    /// </summary>
    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpProjectTypeGuidProvider : IItemTypeGuidProvider
    {
        [ImportingConstructor]
        public CSharpProjectTypeGuidProvider(UnconfiguredProject project)
        {
        }

        public Guid ProjectTypeGuid
        {
            get { return ProjectType.LegacyCSharpGuid; }
        }
    }
}
