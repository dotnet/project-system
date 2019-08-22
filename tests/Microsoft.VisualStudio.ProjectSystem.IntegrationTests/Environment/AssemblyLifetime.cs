// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LifetimeActions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio
{
    [TestClass] // Just to make sure AssemblyInitialize/Cleanup get called
    public static class AssemblyLifetime
    {
        [AssemblyInitialize]
        public static void OnAssemblyInitialize(TestContext context)
        {
            TestEnvironment.OnAssemblyInitialize(context);
        }

        [AssemblyCleanup]
        public static void OnAssemblyCleanup()
        {
            ShutdownVisualStudioAfterLastTestLifetimeAction.OnAssemblyCleanup();
        }
    }
}
