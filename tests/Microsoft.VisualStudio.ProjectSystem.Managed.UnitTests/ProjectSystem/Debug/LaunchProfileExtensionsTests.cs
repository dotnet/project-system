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
            var data = new WritableLaunchProfile()
            {
                DoNotPersist = isInMemory
            };

            var lp = (IWritableLaunchProfile)data;
            Assert.Equal(isInMemory, lp.IsInMemoryObject());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [InlineData(null)]
        public void IsInMemoryProfile_NativeDebuggingIsEnabled(bool? nativeDebugging)
        {
            bool isUsingNativeDebugging = nativeDebugging == null ? false : nativeDebugging.Value;
            var data = new LaunchProfile()
            {
                OtherSettings = nativeDebugging == null ? null : ImmutableStringDictionary<object>.EmptyOrdinal.Add(LaunchProfileExtensions.NativeDebuggingProperty, nativeDebugging.Value)
            };

            Assert.Equal(isUsingNativeDebugging, data.NativeDebuggingIsEnabled());
        }
    }
}
