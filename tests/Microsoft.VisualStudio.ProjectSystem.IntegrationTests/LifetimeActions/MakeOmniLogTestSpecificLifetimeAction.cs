// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.Test.Apex;
using Microsoft.Test.Apex.Services;
using Omni.Logging;

namespace Microsoft.VisualStudio.LifetimeActions
{
    /// <summary>
    ///     Responsible for making the OmniLog test-specific and prevent incremental logging.
    /// </summary>
    [ProvidesOperationsExtension]
    [Export(typeof(ITestLifetimeAction))]
    public class MakeOmniLogTestSpecificLifetimeAction : ITestLifetimeAction
    {
        public void OnTestLifeTimeAction(ApexTest testClass, Type classType, TestLifeTimeAction action)
        {
            if (action == TestLifeTimeAction.PreTestInitialize)
            {
                Log.ResetLog($"{testClass.GetType().Name}.{testClass.TestContext.TestName}");
            }
        }
    }
}
