// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal abstract class DependenciesRuleHandlerBase
        : ICrossTargetRuleHandler<DependenciesRuleChangeContext>,
          IProjectDependenciesSubTreeProviderInternal
    {
        private readonly ImmutableHashSet<string> _evaluationRuleNames;
        private readonly ImmutableHashSet<string> _designTimeBuildRuleNames;

        protected string UnresolvedRuleName { get; }
        protected string ResolvedRuleName { get; }

        protected DependenciesRuleHandlerBase(
            string unresolvedRuleName,
            string resolvedRuleName)
        {
            UnresolvedRuleName = unresolvedRuleName;
            ResolvedRuleName = resolvedRuleName;

            _evaluationRuleNames = ImmutableStringHashSet.EmptyOrdinal.Add(unresolvedRuleName);
            _designTimeBuildRuleNames = _evaluationRuleNames.Add(resolvedRuleName);
        }

        #region ICrossTargetRuleHandler

        public ImmutableHashSet<string> GetRuleNames(RuleHandlerType handlerType)
        {
            switch (handlerType)
            {
                case RuleHandlerType.Evaluation:
                    return _evaluationRuleNames;
                case RuleHandlerType.DesignTimeBuild:
                    return _designTimeBuildRuleNames;
                default:
                    return ImmutableStringHashSet.EmptyOrdinal;
            }
        }

        public abstract ImageMoniker GetImplicitIcon();

        /// <summary>
        /// If any standard provider has different OriginalItemSpec property name, 
        /// it could override this property, however currently all of them are the same.
        /// </summary>
        protected virtual string OriginalItemSpecPropertyName => ResolvedAssemblyReference.OriginalItemSpecProperty;

        public virtual bool SupportsHandlerType(RuleHandlerType handlerType)
        {
            return true;
        }

        public virtual bool ReceiveUpdatesWithEmptyProjectChange => false;

        public virtual void Handle(
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            ITargetFramework targetFramework,
            DependenciesRuleChangeContext ruleChangeContext)
        {
            if (changesByRuleName.TryGetValue(UnresolvedRuleName, out IProjectChangeDescription unresolvedChanges))
            {
                HandleChangesForRule(
                    resolved: false, 
                    projectChange: unresolvedChanges, 
                    targetFramework, 
                    ruleChangeContext, 
                    shouldProcess: itemSpec => true);
            }

            if (changesByRuleName.TryGetValue(ResolvedRuleName, out IProjectChangeDescription resolvedChanges))
            {
                HandleChangesForRule(
                    resolved: true, 
                    projectChange: resolvedChanges, 
                    targetFramework, 
                    ruleChangeContext, 
                    shouldProcess: metadata => DoesUnresolvedProjectItemExist(metadata.OriginalItemSpec, unresolvedChanges));
            }
        }

        private void HandleChangesForRule(
            bool resolved,
            IProjectChangeDescription projectChange,
            ITargetFramework targetFramework,
            DependenciesRuleChangeContext ruleChangeContext,
            Func<IDependencyModel, bool> shouldProcess)
        {
            foreach (string removedItem in projectChange.Difference.RemovedItems)
            {
                IDependencyModel model = CreateDependencyModelForRule(removedItem, resolved, projectChange.Before);
                if (shouldProcess(model))
                {
                    ruleChangeContext.IncludeRemovedChange(targetFramework, model);
                }
            }

            foreach (string changedItem in projectChange.Difference.ChangedItems)
            {
                IDependencyModel model = CreateDependencyModelForRule(changedItem, resolved, projectChange.After);
                if (shouldProcess(model))
                {
                    // For changes we try to add new dependency. If it is a resolved dependency, it would just override
                    // old one with new properties. If it is unresolved dependency, it would be added only when there no
                    // resolved version in the snapshot.
                    ruleChangeContext.IncludeAddedChange(targetFramework, model);
                }
            }

            foreach (string addedItem in projectChange.Difference.AddedItems)
            {
                IDependencyModel model = CreateDependencyModelForRule(addedItem, resolved, projectChange.After);
                if (shouldProcess(model))
                {
                    ruleChangeContext.IncludeAddedChange(targetFramework, model);
                }
            }
        }

        protected static bool DoesUnresolvedProjectItemExist(string itemSpec, IProjectChangeDescription unresolvedChanges)
        {
            return unresolvedChanges != null && unresolvedChanges.After.Items.ContainsKey(itemSpec);
        }

        private IDependencyModel CreateDependencyModelForRule(
            string itemSpec,
            bool resolved,
            IProjectRuleSnapshot projectRuleSnapshot)
        {
            IImmutableDictionary<string, string> properties = projectRuleSnapshot.GetProjectItemProperties(itemSpec);

            string originalItemSpec = resolved
                ? properties.GetStringProperty(OriginalItemSpecPropertyName)
                : itemSpec;

            bool isImplicit = properties.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined) ?? false;

            return CreateDependencyModel(
                itemSpec,
                originalItemSpec,
                resolved,
                isImplicit,
                properties);
        }

        protected virtual IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return null;
        }

        #endregion

        #region  IProjectDependenciesSubTreeProvider

        public abstract string ProviderType { get; }

        public abstract IDependencyModel CreateRootDependencyNode();

        public event EventHandler<DependenciesChangedEventArgs> DependenciesChanged;

        protected void FireDependenciesChanged(DependenciesChangedEventArgs args)
        {
            DependenciesChanged?.Invoke(this, args);
        }

        #endregion
    }
}
