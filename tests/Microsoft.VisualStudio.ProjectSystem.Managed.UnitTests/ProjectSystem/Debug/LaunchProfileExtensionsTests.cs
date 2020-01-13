// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var data = new WritableLaunchProfile("Name", "CommandName")
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
