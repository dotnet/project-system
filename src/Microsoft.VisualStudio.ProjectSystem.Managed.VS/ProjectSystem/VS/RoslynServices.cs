// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;

using Workspace = Microsoft.CodeAnalysis.Workspace;

namespace Microsoft.VisualStudio.ProjectSystem.VS
{
    [Export(typeof(IRoslynServices))]
    internal class RoslynServices : IRoslynServices
    {
        private readonly IProjectThreadingService _threadingService;

        [ImportingConstructor]
        public RoslynServices(
            IProjectThreadingService threadingService,
            UnconfiguredProject project)
        {
            _threadingService = threadingService;
            SyntaxFactsServicesImpl = new OrderPrecedenceImportCollection<ISyntaxFactsService>(projectCapabilityCheckProvider: project);
        }

        [ImportMany]
        protected OrderPrecedenceImportCollection<ISyntaxFactsService> SyntaxFactsServicesImpl { get; }

        private ISyntaxFactsService? SyntaxFactsService => SyntaxFactsServicesImpl.FirstOrDefault()?.Value;

        public bool ApplyChangesToSolution(Workspace ws, Solution renamedSolution)
        {
            _threadingService.VerifyOnUIThread();

            // Always make sure TryApplyChanges is called from an UI thread.
            return ws.TryApplyChanges(renamedSolution);
        }

        public bool IsValidIdentifier(string identifierName)
        {
            return SyntaxFactsService?.IsValidIdentifier(identifierName) ?? false;
        }
    }
}
