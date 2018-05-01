// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Trait("UnitTest", "ProjectSystem")]
    public class DteEnvironmentOptionsTests
    {
        [Fact]
        public void Constructor_Success()
        {
            var optionnsSettings = new DteEnvironmentOptions(IDteServicesFactory.Create());

            Assert.True(optionnsSettings != null);
        }
    }
}
