// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Mocks;

namespace Microsoft.VisualStudio.ProjectSystem.Properties;

public class WPFValueProviderTests
{
    [Theory]
    [InlineData(WPFValueProvider.StartupURIPropertyName, "true", WPFValueProvider.WinExeOutputTypeValue, true, false)]
    [InlineData(WPFValueProvider.ShutdownModePropertyName, "true", WPFValueProvider.WinExeOutputTypeValue, false, true)]
    [InlineData(WPFValueProvider.StartupURIPropertyName, "false", WPFValueProvider.WinExeOutputTypeValue, false, false)]
    [InlineData(WPFValueProvider.StartupURIPropertyName, "true", "Exe", false, false)]
    [InlineData(WPFValueProvider.ShutdownModePropertyName, "false", WPFValueProvider.WinExeOutputTypeValue, false, false)]
    [InlineData(WPFValueProvider.ShutdownModePropertyName, "true", "Exe", false, false)]
    public async Task WhenGettingAProperty_ValidateTheCorrectMethodsAreCalled(string propertyName, string useWPFPropertyValue, string outputTypeValue, bool getStartupUriShouldBeCalled, bool getShutdownModeShouldBeCalled)
    {
        string startupUriValue = "Alpha.xaml";
        string shutdownModeValue = "Beta";

        bool getStartupUriCalled = false;
        bool getShutdownModeCalled = false;

        var applicationXamlFileAccessor = IApplicationXamlFileAccessorFactory.Create(
            getStartupUri: () =>
            {
                getStartupUriCalled = true;
                return Task.FromResult<string?>(startupUriValue);
            },
            getShutdownMode: () =>
            {
                getShutdownModeCalled = true;
                return Task.FromResult<string?>(shutdownModeValue);
            });

        IEnumerable<string> capabilities = new List<string> { "UseWPF" };

        var projectcapabilitiesScope = IProjectCapabilitiesScopeFactory.Create(capabilities);
                
        var unconfiguredProject = UnconfiguredProjectFactory.Create(@"C:\Test\Path\Here", scope: projectcapabilitiesScope);

        var provider = new WPFValueProvider(applicationXamlFileAccessor, unconfiguredProject);

        var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(new Dictionary<string, string?>
        {
            { PropertyNames.UseWPF, useWPFPropertyValue },
            { WPFValueProvider.OutputTypePropertyName, outputTypeValue }
        });

        var result = await provider.OnGetUnevaluatedPropertyValueAsync(propertyName, unevaluatedPropertyValue: "Doesn't matter", defaultProperties);

        Assert.Equal(expected: getStartupUriShouldBeCalled, actual: getStartupUriCalled);
        Assert.Equal(expected: getShutdownModeShouldBeCalled, actual: getShutdownModeCalled);

        if (getStartupUriShouldBeCalled)
        {
            Assert.Equal(expected: startupUriValue, actual: result);
        }

        if (getShutdownModeShouldBeCalled)
        {
            Assert.Equal(expected: shutdownModeValue, actual: result);
        }
    }

    [Theory]
    [InlineData(WPFValueProvider.StartupURIPropertyName, "true", WPFValueProvider.WinExeOutputTypeValue, true, false)]
    [InlineData(WPFValueProvider.ShutdownModePropertyName, "true", WPFValueProvider.WinExeOutputTypeValue, false, true)]
    [InlineData(WPFValueProvider.StartupURIPropertyName, "false", WPFValueProvider.WinExeOutputTypeValue, false, false)]
    [InlineData(WPFValueProvider.StartupURIPropertyName, "true", "Exe", false, false)]
    [InlineData(WPFValueProvider.ShutdownModePropertyName, "false", WPFValueProvider.WinExeOutputTypeValue, false, false)]
    [InlineData(WPFValueProvider.ShutdownModePropertyName, "true", "Exe", false, false)]
    public async Task WhenSettingAProperty_ValidateTheCorrectMethodsAreCalled(string propertyName, string useWPFPropertyValue, string outputTypeValue, bool setStartupUriShouldBeCalled, bool setShutdownModeShouldBeCalled)
    {
        bool setStartupUriCalled = false;
        bool setShutdownModeCalled = false;

        string? setValue = null;

        var applicationXamlFileAccessor = IApplicationXamlFileAccessorFactory.Create(
            setStartupUri: (newStartupUri) =>
            {
                setStartupUriCalled = true;
                setValue = newStartupUri;
                return Task.CompletedTask;
            },
            setShutdownMode: (newShutdownMode) =>
            {
                setShutdownModeCalled = true;
                setValue = newShutdownMode;
                return Task.CompletedTask;
            });

        IEnumerable<string> capabilities;

        capabilities = (bool.Parse(useWPFPropertyValue) ? new List<string>() {  "UseWPF" } : new List<string>());

        var projectcapabilitiesScope = IProjectCapabilitiesScopeFactory.Create(capabilities);

        var unconfiguredProject = UnconfiguredProjectFactory.Create(@"C:\Test\Path\Here", scope: projectcapabilitiesScope);

        var provider = new WPFValueProvider(applicationXamlFileAccessor, unconfiguredProject);

        var defaultProperties = IProjectPropertiesFactory.CreateWithPropertiesAndValues(new Dictionary<string, string?>
        {
            { PropertyNames.UseWPF, useWPFPropertyValue },
            { PropertyNames.OutputTypeMSBuild, outputTypeValue }
        });

        var result = await provider.OnSetPropertyValueAsync(propertyName, unevaluatedPropertyValue: "NewValue", defaultProperties);

        Assert.Null(result);

        Assert.Equal(expected: setStartupUriShouldBeCalled, actual: setStartupUriCalled);
        Assert.Equal(expected: setShutdownModeShouldBeCalled, actual: setShutdownModeCalled);

        if (setStartupUriShouldBeCalled || setShutdownModeCalled)
        {
            Assert.Equal(expected: "NewValue", actual: setValue);
        }
    }
}
