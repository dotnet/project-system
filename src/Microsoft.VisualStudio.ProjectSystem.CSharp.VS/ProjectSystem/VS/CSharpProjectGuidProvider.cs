// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Packaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides the C# implementation of <see cref="IItemTypeGuidProvider"/>.
    /// </summary>
    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpProjectGuidProvider : IItemTypeGuidProvider
    {
        private static readonly Guid s_csharpProjectType = new Guid(CSharpProjectSystemPackage.LegacyProjectTypeGuid);

        [ImportingConstructor]
        public CSharpProjectGuidProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
        }

        public Guid ProjectTypeGuid
        {
            get { return s_csharpProjectType; }
        }
    }
}
