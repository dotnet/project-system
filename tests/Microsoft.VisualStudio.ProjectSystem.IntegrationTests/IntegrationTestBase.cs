// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Test.Apex;
using Microsoft.Test.Apex.VisualStudio;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    public abstract class IntegrationTestBase : VisualStudioHostTest
    {
        protected IntegrationTestBase()
        {
            // TestCleanup will fire up another instance of Visual Studio to reset 
            // the AutoloadExternalChanges if it thinks the default changed even if
            // that was just caused by settings to be sync'd. Just turn this feature off.
            SuppressReloadPrompt = false;
        }

        protected override bool IncludeReferencedAssembliesInHostComposition => false; // Do not add things we reference to the MEF Container

        protected override VisualStudioHostConfiguration GetHostConfiguration()
        {
            return new ProjectSystemHostConfiguration();
        }

        protected override OperationsConfiguration GetOperationsConfiguration()
        {
            return new ProjectSystemOperationsConfiguration(TestContext);
        }

        public new void TryShutdownVisualStudioInstance()
        {
            base.TryShutdownVisualStudioInstance();
        }
    }
}
