// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    [Export(typeof(IDependencyCrossTargetSubscriber))]
    [AppliesTo(ProjectCapability.DependenciesTree)]
    internal class DependencyRulesSubscriber : CrossTargetRuleSubscriberBase<DependenciesRuleChangeContext>, IDependencyCrossTargetSubscriber
    {
        public const string DependencyRulesSubscriberContract = "DependencyRulesSubscriberContract";

        [ImportingConstructor]
        public DependencyRulesSubscriber(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(ExportContractNames.Scopes.UnconfiguredProject)]IProjectAsynchronousTasksService tasksService,
            IDependencyTreeTelemetryService treeTelemetryService)
            : base(commonServices, tasksService, treeTelemetryService)
        {
            Handlers = new OrderPrecedenceImportCollection<ICrossTargetRuleHandler<DependenciesRuleChangeContext>>(
                projectCapabilityCheckProvider: commonServices.Project);
        }

        [ImportMany(DependencyRulesSubscriberContract)]
        protected override OrderPrecedenceImportCollection<ICrossTargetRuleHandler<DependenciesRuleChangeContext>> Handlers { get; }

        protected override DependenciesRuleChangeContext CreateRuleChangeContext(
            ITargetFramework target,
            IProjectCatalogSnapshot catalogs)
        {
            return new DependenciesRuleChangeContext(target, catalogs);
        }

        public event EventHandler<DependencySubscriptionChangedEventArgs> DependenciesChanged;

        protected override void CompleteHandle(DependenciesRuleChangeContext ruleChangeContext)
        {
            DependenciesChanged?.Invoke(this, new DependencySubscriptionChangedEventArgs(ruleChangeContext));
        }
    }
}
