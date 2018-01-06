// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.Packaging
{
    [Trait("UnitTest", "ProjectSystem")]
    public class CSharpProjectSystemPackageTests
    {
        [Fact]
        public void Constructor_DoesNotThrow()
        {
            new CSharpProjectSystemPackage();            
        }
    }
}
