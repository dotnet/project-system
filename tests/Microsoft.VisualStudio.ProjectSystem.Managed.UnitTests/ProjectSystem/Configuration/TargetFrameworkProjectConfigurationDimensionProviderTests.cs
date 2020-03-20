// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    public sealed class TargetFrameworkProjectConfigurationDimensionProviderTests : ProjectConfigurationDimensionProviderTestBase
    {
        protected override string PropertyName => "TargetFrameworks";
        protected override string DimensionName => "TargetFramework";
        protected override string? DimensionDefaultValue => null;

        private protected override BaseProjectConfigurationDimensionProvider CreateInstance(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);

            return CreateInstance(projectAccessor);
        }

        private protected override BaseProjectConfigurationDimensionProvider CreateInstance(IProjectAccessor projectAccessor)
        {
            return new TargetFrameworkProjectConfigurationDimensionProvider(projectAccessor);
        }
    }
}
