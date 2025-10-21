// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Win32;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

public class RegistryServiceTests
{
    [Fact]
    public void GetValue_WhenKeyDoesNotExist_ReturnsNull()
    {
        var service = new RegistryService();

        string? result = service.GetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"SOFTWARE\NonExistent\Key\Path",
            "NonExistentValue");

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_WhenValueDoesNotExist_ReturnsNull()
    {
        var service = new RegistryService();

        // Use a key that exists but with a non-existent value
        string? result = service.GetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion",
            "NonExistentValue_" + Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void GetValue_WhenKeyExists_ReturnsValue()
    {
        var service = new RegistryService();

        // Try to read a well-known registry value that should exist on Windows
        string? result = service.GetValue(
            RegistryHive.LocalMachine,
            RegistryView.Registry32,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion",
            "ProgramFilesDir");

        // On a Windows machine, this should return a path
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }
}
