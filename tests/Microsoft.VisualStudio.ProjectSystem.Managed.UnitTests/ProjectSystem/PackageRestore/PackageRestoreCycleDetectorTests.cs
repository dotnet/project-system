// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Notifications;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.PackageRestore;

public sealed class PackageRestoreCycleDetectorTests
{
    [Theory]
    [InlineData("AAAAAAAAAA",     false)]
    [InlineData("ABCDEFGHIJ",     false)]
    [InlineData("ABABABAB",       true)]
    [InlineData("ABCABABAB",      true)]
    [InlineData("ABCABDXYXYXYXY", true)]
    [InlineData("ABCABCABC",      true, Skip = "The implementation cannot detect this pattern, though it should")]
    public async Task IsCycleDetectedAsync(string sequence, bool isCycleDetected)
    {
        var instance = CreateInstance();

        await ValidateAsync(instance, sequence, isCycleDetected);
    }

    private async Task ValidateAsync(PackageRestoreCycleDetector instance, string sequence, bool isCycleDetected)
    {
        for (var i = 0; i < sequence.Length; i++)
        {
            var hash = CreateHash((byte)sequence[i]);

            bool expected = (i == sequence.Length - 1) && isCycleDetected;

            Assert.Equal(expected, await instance.IsCycleDetectedAsync(hash, CancellationToken.None));
        }
    }

    private static Hash CreateHash(byte b) => new(new[] { b });

    private static PackageRestoreCycleDetector CreateInstance()
    {
        var project = UnconfiguredProjectFactory.CreateWithActiveConfiguredProjectProvider(IProjectThreadingServiceFactory.Create());

        var projectSystemOptions = new Mock<IProjectSystemOptions>();

        var telemetryService = new Mock<ITelemetryService>();
        var nonModelNotificationService = new Mock<INonModalNotificationService>();

        return new PackageRestoreCycleDetector(
            project,
            telemetryService.Object,
            nonModelNotificationService.Object);
    }
}
