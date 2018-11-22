// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.Packaging
{
    public class VisualBasicProjectSystemPackageTests
    {
        [Fact(Skip = "classes inheriting from AsyncPackage cannot be instantiated outside of VS see https://devdiv.visualstudio.com/DevDiv/_git/VS/commit/6ca4bf88f1d78a6eca95e198642e3cf81c1fbcfd")]
        public void Constructor_DoesNotThrow()
        {
            new VisualBasicProjectSystemPackage();
        }
    }
}
