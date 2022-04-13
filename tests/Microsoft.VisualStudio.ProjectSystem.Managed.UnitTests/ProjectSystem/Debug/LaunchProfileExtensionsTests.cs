// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfileExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsInMemoryProfile_ILaunchProfile(bool isInMemory)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                doNotPersist: isInMemory);

            var lp = (ILaunchProfile)data;
            Assert.Equal(isInMemory, lp.IsInMemoryObject());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsInMemoryProfile_IWritableLaunchProfile(bool isInMemory)
        {
            var data = new WritableLaunchProfile()
            {
                DoNotPersist = isInMemory
            };

            var lp = (IWritableLaunchProfile)data;
            Assert.Equal(isInMemory, lp.IsInMemoryObject());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, false)]
        public void IsNativeDebuggingEnabled_ILaunchProfile(bool? value, bool expected)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                otherSettings: value == null ? null : new Dictionary<string, object> { [LaunchProfileExtensions.NativeDebuggingProperty] = value.Value });

            Assert.Equal(expected, data.IsNativeDebuggingEnabled());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, false)]
        public void IsSqlDebuggingEnabled_ILaunchProfile(bool? value, bool expected)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                otherSettings: value == null ? null : new Dictionary<string, object> { [LaunchProfileExtensions.SqlDebuggingProperty] = value.Value});

            Assert.Equal(expected, data.IsSqlDebuggingEnabled());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, false)]
        public void IsRemoteDebugEnabled_ILaunchProfile(bool? value, bool expected)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                otherSettings: value == null ? null : new Dictionary<string, object> { [LaunchProfileExtensions.RemoteDebugEnabledProperty] = value.Value});

            Assert.Equal(expected, data.IsRemoteDebugEnabled());
        }

        [Theory]
        [InlineData("host", "host")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void RemoteDebugMachine_ILaunchProfile(string? value, string? expected)
        {
            var data = new LaunchProfile(
                name: null,
                commandName: null,
                otherSettings: value == null ? null : new Dictionary<string, object> { [LaunchProfileExtensions.RemoteDebugMachineProperty] = value});

            Assert.Equal(expected, data.RemoteDebugMachine());
        }
    }
}
