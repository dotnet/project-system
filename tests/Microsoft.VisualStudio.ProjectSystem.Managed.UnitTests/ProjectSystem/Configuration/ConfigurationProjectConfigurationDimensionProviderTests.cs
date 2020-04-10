// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Configuration
{
    public sealed class ConfigurationProjectConfigurationDimensionProviderTests : ProjectConfigurationDimensionProviderTestBase
    {
        protected override string PropertyName => "Configurations";
        protected override string DimensionName => "Configuration";
        protected override string? DimensionDefaultValue => "Debug";

        private protected override BaseProjectConfigurationDimensionProvider CreateInstance(string projectXml)
        {
            var projectAccessor = IProjectAccessorFactory.Create(projectXml);

            return CreateInstance(projectAccessor);
        }

        private protected override BaseProjectConfigurationDimensionProvider CreateInstance(IProjectAccessor projectAccessor)
        {
            return new ConfigurationProjectConfigurationDimensionProvider(projectAccessor);
        }
    }
}
