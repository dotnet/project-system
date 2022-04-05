// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Properties
{
    /// <summary>
    ///     Concrete implementation of <see cref="IPageMetadata"/>.
    /// </summary>
    internal class ProjectDesignerPageMetadata : IPageMetadata
    {
        public ProjectDesignerPageMetadata(Guid pageGuid, int pageOrder, bool hasConfigurationCondition)
        {
            if (pageGuid == Guid.Empty)
                throw new ArgumentException(null, nameof(pageGuid));

            PageGuid = pageGuid;
            PageOrder = pageOrder;
            HasConfigurationCondition = hasConfigurationCondition;
        }

        public string? Name => null;

        public bool HasConfigurationCondition { get; }

        public Guid PageGuid { get; }

        public int PageOrder { get; }
    }
}
