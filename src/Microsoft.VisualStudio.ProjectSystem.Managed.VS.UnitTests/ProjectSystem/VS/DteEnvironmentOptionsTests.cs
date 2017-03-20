// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [ProjectSystemTrait]
    public class DteEnvironmentOptionsTests
    {
        [Fact]
        public void Constructor_NullAsDteServices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("dteServices", () => {

                new DteEnvironmentOptions((IDteServices)null);
            });
        }

        [Fact]
        public void Constructor_Success()
        {
            var optionnsSettings = new DteEnvironmentOptions(IDteServicesFactory.Create());

            Assert.True(optionnsSettings != null);
        }
    }
}
