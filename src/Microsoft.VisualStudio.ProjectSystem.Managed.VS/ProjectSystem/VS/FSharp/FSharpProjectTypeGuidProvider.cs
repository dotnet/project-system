// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.FSharp
{
    /// <summary>
    ///     Provides the Visual Basic implementation of <see cref="IItemTypeGuidProvider"/>.
    /// </summary>
    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapability.FSharp)]
    internal class FSharpProjectTypeGuidProvider : IItemTypeGuidProvider
    {
        [ImportingConstructor]
        public FSharpProjectTypeGuidProvider()
        {
        }

        public Guid ProjectTypeGuid
        {
            get { return ProjectType.LegacyFSharpGuid; }
        }
    }
}
