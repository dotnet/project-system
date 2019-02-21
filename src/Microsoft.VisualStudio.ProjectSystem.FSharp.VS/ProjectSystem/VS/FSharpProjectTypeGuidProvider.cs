// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides the Visual Basic implementation of <see cref="IItemTypeGuidProvider"/>.
    /// </summary>
    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapability.FSharp)]
    internal class FSharpProjectTypeGuidProvider : IItemTypeGuidProvider
    {
        [ImportingConstructor]
        public FSharpProjectTypeGuidProvider(UnconfiguredProject project)
        {
        }

        public Guid ProjectTypeGuid
        {
            get { return ProjectType.LegacyFSharpGuid; }
        }

    }
}
