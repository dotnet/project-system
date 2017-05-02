// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Debug
{
    [ProjectSystemTrait]
    public class DebugPageGuidProviderTests
    {
        [Fact]
        public async Task ProjectDebuggerProvider_GetDebugEngineForFrameworkTests()
        {
            var guid = new Guid("{0273C280-1882-4ED0-9308-52914672E3AA}");
            Assert.True(await new DebugPageGuidProvider().GetDebugPropertyPageGuidAsync() == guid);
        }
    }
}
