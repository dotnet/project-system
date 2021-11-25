// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Test.Apex;
using Microsoft.Test.Apex.Providers;
using Microsoft.Test.Apex.Services.Logging;
using Microsoft.VisualStudio.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio
{
    // Heavily based on MsTestOperationsConfiguration, which is only needed so that we can control the CompositionAssemblies
    // to avoid MEF composition errors being output into the test output and making it harder to understand the build log.
    internal class ProjectSystemOperationsConfiguration : OperationsConfiguration
    {
        internal ProjectSystemOperationsConfiguration(TestContext testContext)
        {
            TestContext = testContext;
        }

        public override IEnumerable<string> CompositionAssemblies => ProjectSystemHostConfiguration.CompositionAssemblyPaths;

        public TestContext TestContext { get; }

        protected override Type Verifier => typeof(IAssertionVerifier);

        protected override Type Logger => typeof(WarningIgnorerLogger);

        protected override void OnOperationsCreated(Operations operations)
        {
            base.OnOperationsCreated(operations);

            IAssertionVerifier verifier = operations.Get<IAssertionVerifier>();
            verifier.AssertionDelegate = Assert.Fail;
            verifier.FinalFailure += WriteVerificationFailureTree;

            var logger = operations.Get<WarningIgnorerLogger>();
            logger.SetTestContext(TestContext);
        }

        protected override void OnProbingDirectoriesProviderCreated(IProbingDirectoriesProvider provider)
        {
        }

        private static void WriteVerificationFailureTree(object sender, FailureEventArgs e)
        {
            e.Logger.WriteEntry(SinkEntryType.Message, "Full verification failure tree:" + Environment.NewLine + Environment.NewLine + ResultMessageTreeController.Instance.FormatTreeAsText());
        }
    }
}
