// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;

using Moq;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    internal class MockIDependenciesTreeServices : IDependenciesTreeServices
    {
        public IProjectTree CreateTree(
            string caption,
            IProjectPropertiesContext itemContext,
            IPropertySheet propertySheet = null,
            IRule browseObjectProperties = null,
            ProjectImageMoniker icon = null,
            ProjectImageMoniker expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = default(ProjectTreeFlags?))
        {
            return new TestProjectTree()
            {
                Caption = caption,
                FilePath = itemContext.File ?? caption,
                BrowseObjectProperties = browseObjectProperties,
                Icon = icon,
                ExpandedIcon = expandedIcon,
                Visible = visible,
                Flags = flags.Value,
                IsProjectItem = true
            };
        }

        public IProjectTree CreateTree(
            string caption,
            string filePath,
            IRule browseObjectProperties = null,
            ProjectImageMoniker icon = null,
            ProjectImageMoniker expandedIcon = null,
            bool visible = true,
            ProjectTreeFlags? flags = default(ProjectTreeFlags?))
        {
            return new TestProjectTree()
            {
                Caption = caption,
                FilePath = filePath,
                BrowseObjectProperties = browseObjectProperties,
                Icon = icon,
                ExpandedIcon = expandedIcon,
                Visible = visible,
                Flags = flags.Value,
                IsProjectItem = false
            };
        }

        public Task<IRule> GetRuleAsync(IDependency dependency, IProjectCatalogSnapshot catalogs)
        {
            var mockRule = new Mock<IRule>(MockBehavior.Strict);
            mockRule.Setup(x => x.Name).Returns(dependency.SchemaItemType);
            return Task.FromResult(mockRule.Object);
        }
    }
}
