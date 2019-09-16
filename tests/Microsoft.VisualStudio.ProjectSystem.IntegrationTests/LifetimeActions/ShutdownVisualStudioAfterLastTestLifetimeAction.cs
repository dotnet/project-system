// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
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
