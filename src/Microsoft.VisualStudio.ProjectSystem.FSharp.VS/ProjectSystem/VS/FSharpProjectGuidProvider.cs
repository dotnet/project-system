// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Packaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides the Visual Basic implementation of <see cref="IItemTypeGuidProvider"/>.
    /// </summary>
    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapability.FSharp)]
    internal class FSharpProjectGuidProvider : IItemTypeGuidProvider
    {
        private static readonly Guid s_fsharpProjectType = new Guid(FSharpProjectSystemPackage.LegacyProjectTypeGuid);

        [ImportingConstructor]
        public FSharpProjectGuidProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
        }

        public Guid ProjectTypeGuid
        {
            get { return s_fsharpProjectType; }
        }

    }
}
