// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
