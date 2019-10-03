// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
