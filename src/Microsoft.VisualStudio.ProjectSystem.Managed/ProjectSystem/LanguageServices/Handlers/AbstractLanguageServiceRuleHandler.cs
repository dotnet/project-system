// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.ProjectSystem.LanguageServices.Handlers
{
    internal abstract class AbstractLanguageServiceRuleHandler : IEvaluationHandler
    {
        public abstract RuleHandlerType HandlerType { get; }

        public abstract string EvaluationRuleName { get; }

        public virtual bool ReceiveUpdatesWithEmptyProjectChange => false;

        public virtual void Handle(IProjectChangeDescription projectChange, IWorkspaceProjectContext context, bool isActiveContext)
        {
        }

        public virtual Task OnContextReleasedAsync(IWorkspaceProjectContext context)
        {
            return Task.CompletedTask;
        }
    }
}
