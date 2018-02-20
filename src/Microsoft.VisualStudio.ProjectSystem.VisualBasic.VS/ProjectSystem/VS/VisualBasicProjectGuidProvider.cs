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
    [AppliesTo(ProjectCapabilities.VB)]
    internal class VisualBasicProjectGuidProvider : IItemTypeGuidProvider
    {
        private static readonly Guid s_visualBasicProjectType = new Guid(VisualBasicProjectSystemPackage.LegacyProjectTypeGuid);

        [ImportingConstructor]
        public VisualBasicProjectGuidProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
        }

        public Guid ProjectTypeGuid
        {
            get { return s_visualBasicProjectType; }
        }
    }
}
