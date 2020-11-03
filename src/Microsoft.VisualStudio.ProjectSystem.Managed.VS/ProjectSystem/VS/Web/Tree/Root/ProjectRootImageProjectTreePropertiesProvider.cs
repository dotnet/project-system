// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web.Tree
{
    internal class ProjectRootImageProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        private readonly ProjectImageMoniker _image;

        public ProjectRootImageProjectTreePropertiesProvider(ProjectImageMoniker image)
        {
            _image = image;
        }

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (propertyValues.Flags.Contains(ProjectTreeFlags.Common.ProjectRoot))
            {
                propertyValues.Icon = _image;
            }
        }
    }
}
