// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    public class LaunchProfileExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsInMemoryProfile_ILaunchProfile(bool isInMemory)
        {
            var data = new LaunchProfile()
            {
                DoNotPersist = isInMemory
            };

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
            var data = new LaunchProfile
            {
                OtherSettings = value == null ? null : ImmutableStringDictionary<object>.EmptyOrdinal.Add(LaunchProfileExtensions.NativeDebuggingProperty, value.Value)
            };

            Assert.Equal(expected, data.IsNativeDebuggingEnabled());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, false)]
        public void IsSqlDebuggingEnabled_ILaunchProfile(bool? value, bool expected)
        {
            var data = new LaunchProfile
            {
                OtherSettings = value == null ? null : ImmutableStringDictionary<object>.EmptyOrdinal.Add(LaunchProfileExtensions.SqlDebuggingProperty, value.Value)
            };

            Assert.Equal(expected, data.IsSqlDebuggingEnabled());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        [InlineData(null, false)]
        public void IsRemoteDebugEnabled_ILaunchProfile(bool? value, bool expected)
        {
            var data = new LaunchProfile
            {
                OtherSettings = value == null ? null : ImmutableStringDictionary<object>.EmptyOrdinal.Add(LaunchProfileExtensions.RemoteDebugEnabledProperty, value.Value)
            };

            Assert.Equal(expected, data.IsRemoteDebugEnabled());
        }

        [Theory]
        [InlineData("host", "host")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void RemoteDebugMachine_ILaunchProfile(string? value, string? expected)
        {
            var data = new LaunchProfile
            {
                OtherSettings = value == null ? null : ImmutableStringDictionary<object>.EmptyOrdinal.Add(LaunchProfileExtensions.RemoteDebugMachineProperty, value)
            };

            Assert.Equal(expected, data.RemoteDebugMachine());
        }
    }
}
