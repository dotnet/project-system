// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.CSharp
{
    /// <summary>
    ///     Provides the C# implementation of <see cref="IItemTypeGuidProvider"/>.
    /// </summary>
    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpProjectTypeGuidProvider : IItemTypeGuidProvider
    {
        [ImportingConstructor]
        public CSharpProjectTypeGuidProvider()
        {
        }

        public Guid ProjectTypeGuid
        {
            get { return ProjectType.LegacyCSharpGuid; }
        }
    }
}
