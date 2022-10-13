// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    public partial class AbstractEvaluationCommandLineHandlerTests
    {
        private class EvaluationCommandLineHandler : AbstractEvaluationCommandLineHandler
        {
            public EvaluationCommandLineHandler(UnconfiguredProject project)
                : base(project)
            {
                Files = new Dictionary<string, IImmutableDictionary<string, string>>();
            }

            public ICollection<string> FileNames
            {
                get { return Files.Keys; }
            }

            public Dictionary<string, IImmutableDictionary<string, string>> Files { get; }

            protected override void AddToContext(IWorkspaceProjectContext context, string fullPath, IImmutableDictionary<string, string> metadata, bool isActiveContext, IManagedProjectDiagnosticOutputService logger)
            {
                Files.Add(fullPath, metadata);
            }

            protected override void RemoveFromContext(IWorkspaceProjectContext context, string fullPath, IManagedProjectDiagnosticOutputService logger)
            {
                Files.Remove(fullPath);
            }

            protected override void UpdateInContext(IWorkspaceProjectContext context, string fullPath, IImmutableDictionary<string, string> previousMetadata, IImmutableDictionary<string, string> currentMetadata, bool isActiveContext, IManagedProjectDiagnosticOutputService logger)
            {
                RemoveFromContext(context, fullPath, logger);
                AddToContext(context, fullPath, currentMetadata, isActiveContext, logger);
            }
        }
    }
}
