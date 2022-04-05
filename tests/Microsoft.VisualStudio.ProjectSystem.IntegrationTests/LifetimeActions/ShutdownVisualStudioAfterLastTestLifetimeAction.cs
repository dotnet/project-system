// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Test.Apex;
using Microsoft.Test.Apex.Services;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.LifetimeActions
{
    /// <summary>
    ///     Responsible for shutting down Visual Studio after last test is done with it.
    /// </summary>
    [ProvidesOperationsExtension]
    [Export(typeof(ITestLifetimeAction))]
    internal class ShutdownVisualStudioAfterLastTestLifetimeAction : ITestLifetimeAction
    {
        private static IntegrationTestBase? _lastTest;

        public void OnTestLifeTimeAction(ApexTest testClass, Type classType, TestLifeTimeAction action)
        {
            if (action == TestLifeTimeAction.PostTestCleanup)
            {
                _lastTest = testClass as IntegrationTestBase;
            }
        }

        public static void OnAssemblyCleanup()
        {
            // To reduce integration test time, we want to reuse Visual Studio instances where possible.
            // Apex will automatically close VS only if the previous test failed, this shuts down Visual Studio
            // after all the tests have finished.            
            _lastTest?.TryShutdownVisualStudioInstance();
        }
    }
}
