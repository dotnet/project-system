// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Notifications;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

public sealed class PackageRestoreCycleDetectorTests
{
    [Fact]
    public async Task NoCycleOnRepeatedValue()
    {
        var instance = CreateInstance();
        var hash = CreateHash(0);

        for (int i = 0; i < 100; i++)
        {
            Assert.False(await instance.IsCycleDetectedAsync(hash, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Cycle_TwoHashes_Detected()
    {
        var hash0 = CreateHash(0);
        var hash1 = CreateHash(1);

        var instance = CreateInstance();
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.True(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
    }

    [Fact]
    public async Task Cycle_TwoHashes_IgnoredWhenFeatureDisabled()
    {
        var hash0 = CreateHash(0);
        var hash1 = CreateHash(1);

        var instance = CreateInstance(isEnabled: false); // disabled

        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None)); // ignored
    }

    [Fact]
    public async Task Cycle_ThreeHashes_Wat()
    {
        var hash0 = CreateHash(0);
        var hash1 = CreateHash(1);
        var hash2 = CreateHash(2);

        var instance = CreateInstance();

        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash2, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.True(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
    }

    [Fact(Skip = "The implementation cannot detect this pattern, though it should")]
    public async Task Cycle_ThreeHashes_Detected()
    {
        var hash0 = CreateHash(0);
        var hash1 = CreateHash(1);
        var hash2 = CreateHash(2);

        var instance = CreateInstance();
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash2, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash2, CancellationToken.None));
        Assert.False(await instance.IsCycleDetectedAsync(hash0, CancellationToken.None));
        Assert.True(await instance.IsCycleDetectedAsync(hash1, CancellationToken.None));
    }

    private static Hash CreateHash(byte b) => new(new[] { b });

    private static PackageRestoreCycleDetector CreateInstance(bool isEnabled = true)
    {
        var project = UnconfiguredProjectFactory.CreateWithActiveConfiguredProjectProvider(IProjectThreadingServiceFactory.Create());

        var projectSystemOptions = new Mock<IProjectSystemOptions>();
        projectSystemOptions.Setup(o => o.GetDetectNuGetRestoreCyclesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isEnabled);

        var telemetryService = new Mock<ITelemetryService>();
        var nonModelNotificationService = new Mock<INonModalNotificationService>();

        return new PackageRestoreCycleDetector(
            project,
            telemetryService.Object,
            projectSystemOptions.Object,
            nonModelNotificationService.Object);
    }
}
