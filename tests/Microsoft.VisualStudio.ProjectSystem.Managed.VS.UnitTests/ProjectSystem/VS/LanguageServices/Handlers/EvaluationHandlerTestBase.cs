// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public abstract class EvaluationHandlerTestBase
    {
        [Fact]
        public void EvaluationRuleName_ReturnsValue()
        {
            var handler = CreateInstance();

            Assert.NotEmpty(handler.ProjectEvaluationRule);
        }

        internal static void Handle(IWorkspaceProjectContext context, IProjectEvaluationHandler handler, IProjectChangeDescription projectChange, ProjectConfiguration? projectConfiguration = null)
        {
            projectConfiguration ??= ProjectConfigurationFactory.Create("Debug|AnyCPU");

            handler.Handle(context, projectConfiguration, 1, projectChange, new ContextState(), IManagedProjectDiagnosticOutputServiceFactory.Create());
        }

        internal abstract IProjectEvaluationHandler CreateInstance();
    }
}
