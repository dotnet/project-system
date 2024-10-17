// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Setup;

public static class SetupComponentRegistrationServiceTests
{
    [Theory]
    [InlineData("2.0.0", "Microsoft.Net.Core.Component.SDK.2.1")]
    [InlineData("2.1.0", "Microsoft.Net.Core.Component.SDK.2.1")]
    [InlineData("2.2.0", "Microsoft.Net.Core.Component.SDK.2.1")]
    [InlineData("3.0.0", "Microsoft.NetCore.Component.Runtime.3.1")]
    [InlineData("3.1.0", "Microsoft.NetCore.Component.Runtime.3.1")]
    [InlineData("5.0.0", "Microsoft.NetCore.Component.Runtime.5.0")]
    [InlineData("6.0.0", "Microsoft.NetCore.Component.Runtime.6.0")]
    [InlineData("7.0.0", "Microsoft.NetCore.Component.Runtime.7.0")]
    [InlineData("8.0.0", "Microsoft.NetCore.Component.Runtime.8.0")]
    [InlineData("9.0.0", "Microsoft.NetCore.Component.Runtime.9.0")]
    [InlineData("2.0", null)]
    [InlineData("", null)]
    [InlineData(".", null)]
    [InlineData(".1", null)]
    [InlineData(".1.", null)]
    [InlineData(".1.2", null)]
    [InlineData("..", null)]
    [InlineData("A.B.C", null)]
    [InlineData("1.B", null)]
    [InlineData("foo", null)]
    [InlineData("v2.0", null)]
    [InlineData("10.0.0", "Microsoft.NetCore.Component.Runtime.10.0")]
    [InlineData("10.0.1", "Microsoft.NetCore.Component.Runtime.10.0")]
    [InlineData("10.0.1-rc1", "Microsoft.NetCore.Component.Runtime.10.0")]
    public static void TryGetNetCoreRuntimeComponentId(string versionString, string? componentId)
    {
        Assert.Equal(componentId, SetupComponentRegistrationService.TryGetNetCoreRuntimeComponentId(versionString));
    }

    [Theory]
    // Main cases
    [InlineData("Microsoft.NetCore.Component.Runtime.3.1", true)]
    [InlineData("Microsoft.Net.Core.Component.SDK.2.1", true)]
    // These are not known runtime component IDs but would be matched anyway
    [InlineData("Microsoft.NetCore.Component.Runtime.3.1.4", true)]
    [InlineData("Microsoft.NetCore.Component.Runtime.3.1.4-rc1", true)]
    public static void IsRuntimeComponentId(string componentId, bool expected)
    {
        Assert.Equal(expected, SetupComponentRegistrationService.IsRuntimeComponentId(componentId));
    }
}
