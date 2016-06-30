// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectPropertiesProviders
{
    [ProjectSystemTrait]
    public class ImplicitProjectPropertiesProviderTests
    {
        [Fact]
        public void Constructor_NullDelegatedProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("provider", () => 
            {
                new ImplicitProjectPropertiesProvider(null);
            });
        }

        [Fact]
        public void Provider_SetsPropertyIfPresent()
        {
            var delegateProperties = IProjectPropertiesFactory
                .CreateWithPropertyAndSet("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            var provider = new ImplicitProjectPropertiesProvider(delegateProvider);
            var properties = provider.GetProperties("path/to/project.testproj", null, null);

            // calls delegate above with matching values
            properties.SetPropertyValueAsync("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");
        }

        [Fact]
        public void Provider_IgnoresPropertyIfAbsent()
        {
            var delegateProperties = IProjectPropertiesFactory
                .CreateWithProperty("SomeOtherProperty");
            var delegateProvider = IProjectPropertiesProviderFactory.Create(delegateProperties);

            var provider = new ImplicitProjectPropertiesProvider(delegateProvider);
            var properties = provider.GetProperties("path/to/project.testproj", null, null);

            // does not call the set property on the delegated property above
            properties.SetPropertyValueAsync("ProjectGuid", "7259e9ef-87d1-45a5-95c6-3a8432d23776");
        }
    }
}
