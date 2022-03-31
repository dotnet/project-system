// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
