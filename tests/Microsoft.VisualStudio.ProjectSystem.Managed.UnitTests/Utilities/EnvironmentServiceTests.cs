// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.Utilities;

public class EnvironmentServiceTests
{
    [Fact]
    public void Is64BitOperatingSystem_ReturnsSystemValue()
    {
        var service = new EnvironmentService();

        bool result = service.Is64BitOperatingSystem;

        Assert.Equal(Environment.Is64BitOperatingSystem, result);
    }

    [Theory]
    [InlineData(Environment.SpecialFolder.ProgramFiles)]
    [InlineData(Environment.SpecialFolder.ApplicationData)]
    [InlineData(Environment.SpecialFolder.CommonApplicationData)]
    [InlineData(Environment.SpecialFolder.System)]
    public void GetFolderPath_ReturnsSystemValue(Environment.SpecialFolder folder)
    {
        var service = new EnvironmentService();

        string result = service.GetFolderPath(folder);

        Assert.Equal(Environment.GetFolderPath(folder), result);
    }
}
