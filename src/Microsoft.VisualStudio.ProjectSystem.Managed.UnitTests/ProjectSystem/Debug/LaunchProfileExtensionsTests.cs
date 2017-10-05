// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{
    [ProjectSystemTrait]
    public class LaunchProfileExtensionsTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsInMemoryProfile_ILaunchProfile(bool isInMemory)
        {
            LaunchProfile data =  new LaunchProfile()
            {
                DoNotPersist = isInMemory
            };
            
            ILaunchProfile lp = (ILaunchProfile)data;
            Assert.Equal(isInMemory, lp.IsInMemoryObject());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsInMemoryProfile_IWritableLaunchProfile(bool isInMemory)
        {
            WritableLaunchProfile data =  new WritableLaunchProfile()
            {
                DoNotPersist = isInMemory
            };

            IWritableLaunchProfile lp = (IWritableLaunchProfile)data;
            Assert.Equal(isInMemory, lp.IsInMemoryObject());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [InlineData(null)]
        public void IsInMemoryProfile_NativeDebuggingIsEnabled(bool? nativeDebugging)
        {
            bool isUsingNativeDebugging = nativeDebugging == null? false : nativeDebugging.Value;
            LaunchProfile data =  new LaunchProfile()
            {
                OtherSettings = nativeDebugging == null? null : ImmutableDictionary<string, object>.Empty.Add(LaunchProfileExtensions.NativeDebuggingProperty, nativeDebugging.Value)
            };

            Assert.Equal(isUsingNativeDebugging, data.NativeDebuggingIsEnabled());
        }
    }
}
