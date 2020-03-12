// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
