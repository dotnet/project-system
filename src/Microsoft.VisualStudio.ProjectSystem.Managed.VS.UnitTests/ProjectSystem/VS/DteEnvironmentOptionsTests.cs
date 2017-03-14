// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class DteEnvironmentOptionsTests
    {
        [Fact]
        public void Constructor_AllNull_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => {
                new DteEnvironmentOptions(null, null);
            });
        }

        [Fact]
        public void Constructor_NullAsSVsServiceProvider_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("serviceProvider", () => {
                new DteEnvironmentOptions(null, IProjectThreadingServiceFactory.Create());
            });
        }

        [Fact]
        public void Constructor_NullAsProjectThreadingService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("threadingService", () => {
                new DteEnvironmentOptions(SVsServiceProviderFactory.Create(), null);
            });
        }

        [Fact]
        public void Constructor_Success()
        {
            var optionnsSettings = new DteEnvironmentOptions(SVsServiceProviderFactory.Create(), IProjectThreadingServiceFactory.Create());

            Assert.True(optionnsSettings != null);
        }

        [Fact]
        public void GetOption_UIThread_Failure()
        {
            InvalidOperationException exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var optionnsSettings = new DteEnvironmentOptions(SVsServiceProviderFactory.Create(), IProjectThreadingServiceFactory.Create());
                var task = Task.Run(() =>
                {
                    optionnsSettings.GetOption("foo", "foo", "foo", true);
                });
                await task;
            }).Result;
        }
    }
}
