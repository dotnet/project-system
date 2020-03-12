// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    public sealed class PlatformProjectConfigurationDimensionProviderTests : ProjectConfigurationDimensionProviderTestBase
    {
        protected override string PropertyName => "Platforms";
        protected override string DimensionName => "Platform";
        protected override string? DimensionDefaultValue => "AnyCPU";

        private protected override BaseProjectConfigurationDimensionProvider CreateInstance(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);

            return CreateInstance(projectAccessor);
        }

        private protected override BaseProjectConfigurationDimensionProvider CreateInstance(IProjectAccessor projectAccessor)
        {
            return new PlatformProjectConfigurationDimensionProvider(projectAccessor);
        }
    }
}
