// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

public class CompoundTargetFrameworkValueProviderTests
{
    [Theory]
    [InlineData("net6.0-windows7.0", ".NETCoreApp,Version=v6.0", ".NETCoreApp", "Windows", "7.0", true, null, null)] // Kept: changing the useWPF, useWindowsForms and useWinUI keeps the platform properties 
    [InlineData("net6.0-windows7.0", ".NETCoreApp,Version=v6.0", ".NETCoreApp", "Windows", "7.0", null, true, null)] // Kept
    [InlineData("net6.0-windows7.0", ".NETCoreApp,Version=v6.0", ".NETCoreApp", "Windows", "7.0", null, null, true)] // Kept
    [InlineData("net7.0-windows-7.0", ".NETCoreApp,Version=v7.0", ".NETCoreApp", "Windows", "7.0", true, null, null)] // Kept: upgrading tf
    [InlineData("net8.0-windows7.0", ".NETCoreApp,Version=v8.0", ".NETCoreApp", "Windows", "7.0", true, null, null)] // Kept
    [InlineData("net5.0-windows10.0", ".NETCoreApp,Version=v5.0", ".NETCoreApp", "Windows", "10.0", true, null, null)] // Kept: changing platform version
    [InlineData("netcoreapp3.1", ".NETCoreApp,Version=v3.1", ".NETCoreApp", "", "", true, null, null)] // Removed: downgrading tf
    [InlineData("net5.0-android", ".NETCoreApp,Version=v5.0", ".NETCoreApp", "Android", "", null, null, null)] // Removed: changing platform
    public async Task WhenChangingTargetFrameworkProperties_PlatformPropertiesAreKeptOrRemoved(string tf, string tfm, string tfi, string tpi, string tpv, bool? useWF, bool? useWPF, bool? useWUI)
    {
        // Previous target framework properties
        var propertiesAndValues = new Dictionary<string, string?>
        {
            { "InterceptedTargetFramework", ".net5.0-windows" },
            { "TargetPlatformIdentifier", "windows" },
            { "TargetPlatformVersion", "7.0" }
        };
        var iProjectProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(propertiesAndValues);

        // New target framework properties
        var projectProperties = ProjectPropertiesFactory.Create(
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkProperty, tf, null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkMonikerProperty, tfm, null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetFrameworkIdentifierProperty, tfi, null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetPlatformIdentifierProperty, tpi, null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetPlatformVersionProperty, tpv, null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.UseWPFProperty, useWPF, null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.UseWindowsFormsProperty, useWF, null),
            new PropertyPageData(ConfigurationGeneral.SchemaName, ConfigurationGeneral.UseWinUIProperty, useWUI, null));

        var mockProvider = new Mock<CompoundTargetFrameworkValueProvider>(MockBehavior.Strict, projectProperties);
        mockProvider.Setup(m => m.GetTargetFrameworkAliasAsync(tfm)).Returns(Task.FromResult(tf));
        mockProvider.Setup(m => m.OnSetPropertyValueAsync("InterceptedTargetFramework", tfm, iProjectProperties, null)).CallBase();

        var provider = mockProvider.Object;
        await provider.OnSetPropertyValueAsync("InterceptedTargetFramework", tfm, iProjectProperties);

        mockProvider.VerifyAll();
        mockProvider.Setup(m => m.OnGetEvaluatedPropertyValueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProjectProperties>())).CallBase();

        // The value of the property we surface in the UI is stored as a moniker.
        var actualTargetFramework = await provider.OnGetEvaluatedPropertyValueAsync("InterceptedTargetFramework", "", iProjectProperties);
        Assert.Equal(tfm, actualTargetFramework);

        var actualPlatformIdentifier = await provider.OnGetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetPlatformIdentifierProperty, "", iProjectProperties);
        Assert.Equal(tpi, actualPlatformIdentifier);

        var actualPlatformVersion = await provider.OnGetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetPlatformVersionProperty, "", iProjectProperties);
        Assert.Equal(tpv, actualPlatformVersion);

        var actualComputedTargetFramework = await provider.OnGetEvaluatedPropertyValueAsync(ConfigurationGeneral.TargetFrameworkProperty, "", iProjectProperties);
        Assert.Equal(tf, actualComputedTargetFramework);
    }
}
