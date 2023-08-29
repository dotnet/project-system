// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

public class CompoundTargetFrameworkValueProviderTests
{
    [Fact]
    public async Task WhenChangingTargetFramework_PlatformPropertiesAreDeleted()
    {
        // Previous target framework properties
        var propertiesAndValues = new Dictionary<string, string?>
        {
            { "InterceptedTargetFramework", ".net5.0-windows" },
            { "TargetPlatformIdentifier", "windows" },
            { "TargetPlatformVersion", "7.0" }
        };

        // New target framework properties: only projects targeting .NET 5 or higher use platform properties.
        var projectProperties = ProjectPropertiesFactory.Create(
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkProperty, "netcoreapp1.0", null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkMonikerProperty, ".NETCoreApp,Version=v1.0", null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkIdentifierProperty, "NETCoreApp", null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetPlatformIdentifierProperty, null, null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetPlatformVersionProperty, null, null));
        
        var iProjectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(propertiesAndValues);
        var configuredProject = projectProperties.ConfiguredProject;

        var provider = new CompoundTargetFrameworkValueProvider(projectProperties, configuredProject);
        await provider.OnSetPropertyValueAsync("InterceptedTargetFramework", ".NETCoreApp,Version=v1.0", iProjectProperties);

        // The value of the property we surface in the UI is stored as a moniker.
        var actualTargetFramework = await provider.OnGetEvaluatedPropertyValueAsync("InterceptedTargetFramework", "", iProjectProperties);
        Assert.Equal(".NETCoreApp,Version=v1.0", actualTargetFramework);

        var actualPlatformIdentifier = await provider.OnGetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetPlatformIdentifierProperty, "", iProjectProperties);
        Assert.Equal("", actualPlatformIdentifier);

        var actualPlatformVersion = await provider.OnGetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetPlatformVersionProperty, "", iProjectProperties);
        Assert.Equal("", actualPlatformVersion);
    }

    [Fact]
    public async Task WhenChangingTargetFrameworkHigherThanNet5_PlatformPropertiesAreKept()
    {
        // Previous target framework properties
        var propertiesAndValues = new Dictionary<string, string?>
        {
            { "InterceptedTargetFramework", ".net5.0-windows" },
            { "TargetPlatformIdentifier", "windows" },
            { "TargetPlatformVersion", "7.0" }
        };

        // New target framework properties: only projects targeting .NET 5 or higher use platform properties.
        var projectProperties = ProjectPropertiesFactory.Create(
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkProperty, "net6.0", null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkMonikerProperty, ".NETCoreApp,Version=v6.0", null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkIdentifierProperty, "NETCoreApp", null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetPlatformIdentifierProperty, "Windows", null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetPlatformVersionProperty, "7.0", null));

        var iProjectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(propertiesAndValues);
        var configuredProject = projectProperties.ConfiguredProject;

        var provider = new CompoundTargetFrameworkValueProvider(projectProperties, configuredProject);
        await provider.OnSetPropertyValueAsync("InterceptedTargetFramework", ".NETCoreApp,Version=v6.0", iProjectProperties);

        // The value of the property we surface in the UI is stored as a moniker.
        var actualTargetFramework = await provider.OnGetEvaluatedPropertyValueAsync("InterceptedTargetFramework", "", iProjectProperties);
        Assert.Equal(".NETCoreApp,Version=v6.0", actualTargetFramework);

        var actualPlatformIdentifier = await provider.OnGetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetPlatformIdentifierProperty, "", iProjectProperties);
        Assert.Equal("Windows", actualPlatformIdentifier);

        var actualPlatformVersion = await provider.OnGetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetPlatformVersionProperty, "", iProjectProperties);
        Assert.Equal("7.0", actualPlatformVersion);
    }
}
