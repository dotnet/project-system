﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Imaging.FSharp;

public class FSharpProjectImageProviderTests
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        new FSharpProjectImageProvider();
    }

    [Fact]
    public void GetProjectImage_NullAsKey_ThrowsArgumentNull()
    {
        var provider = CreateInstance();

        Assert.Throws<ArgumentNullException>("key", () =>
        {
            provider.GetProjectImage(null!);
        });
    }

    [Fact]
    public void GetProjectImage_EmptyAsKey_ThrowsArgument()
    {
        var provider = CreateInstance();

        Assert.Throws<ArgumentException>("key", () =>
        {
            provider.GetProjectImage(string.Empty);
        });
    }

    [Fact]
    public void GetProjectImage_UnrecognizedKeyAsKey_ReturnsNull()
    {
        var provider = CreateInstance();

        var result = provider.GetProjectImage("Unrecognized");

        Assert.Null(result);
    }

    [Theory]
    [InlineData(ProjectImageKey.ProjectRoot)]
    public void GetProjectImage_RecognizedKeyAsKey_ReturnsNonNull(string key)
    {
        var provider = CreateInstance();

        var result = provider.GetProjectImage(key);

        Assert.NotNull(result);
    }

    private static FSharpProjectImageProvider CreateInstance()
    {
        return new FSharpProjectImageProvider();
    }
}
