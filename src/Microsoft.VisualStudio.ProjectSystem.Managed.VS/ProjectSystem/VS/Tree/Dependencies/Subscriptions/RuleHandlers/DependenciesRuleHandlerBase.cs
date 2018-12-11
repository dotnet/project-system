// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    internal abstract class DependenciesRuleHandlerBase
        : IDependenciesRuleHandler,
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

        #region IDependenciesRuleHandler

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

        public virtual void Handle(
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            ITargetFramework targetFramework,
            CrossTargetDependenciesChangesBuilder changesBuilder)
        {
            // We receive unresolved and resolved changes separately.

            // Process all unresolved changes.
            if (changesByRuleName.TryGetValue(UnresolvedRuleName, out IProjectChangeDescription unresolvedChanges))
            {
                HandleChangesForRule(
                    resolved: false, 
                    projectChange: unresolvedChanges, 
                    shouldProcess: dependencyId => true);
            }

            // Process only resolved changes that have a corresponding unresolved item.
            if (unresolvedChanges != null &&
                changesByRuleName.TryGetValue(ResolvedRuleName, out IProjectChangeDescription resolvedChanges))
            {
                HandleChangesForRule(
                    resolved: true, 
                    projectChange: resolvedChanges, 
                    shouldProcess: unresolvedChanges.After.Items.ContainsKey);
            }

            return;

            void HandleChangesForRule(bool resolved, IProjectChangeDescription projectChange, Func<string, bool> shouldProcess)
            {
                foreach (string removedItem in projectChange.Difference.RemovedItems)
                {
                    string dependencyId = resolved
                        ? projectChange.Before.GetProjectItemProperties(removedItem).GetStringProperty(ResolvedAssemblyReference.OriginalItemSpecProperty)
                        : removedItem;

                    if (shouldProcess(dependencyId))
                    {
                        changesBuilder.Removed(targetFramework, ProviderType, removedItem);
                    }
                }

                foreach (string changedItem in projectChange.Difference.ChangedItems)
                {
                    IDependencyModel model = CreateDependencyModelForRule(changedItem, projectChange.After);
                    if (shouldProcess(model.Id))
                    {
                        // For changes we try to add new dependency. If it is a resolved dependency, it would just override
                        // old one with new properties. If it is unresolved dependency, it would be added only when there no
                        // resolved version in the snapshot.
                        changesBuilder.Added(targetFramework, model);
                    }
                }

                foreach (string addedItem in projectChange.Difference.AddedItems)
                {
                    IDependencyModel model = CreateDependencyModelForRule(addedItem, projectChange.After);
                    if (shouldProcess(model.Id))
                    {
                        changesBuilder.Added(targetFramework, model);
                    }
                }

                return;

                IDependencyModel CreateDependencyModelForRule(string itemSpec, IProjectRuleSnapshot projectRuleSnapshot)
                {
                    IImmutableDictionary<string, string> properties = projectRuleSnapshot.GetProjectItemProperties(itemSpec);

                    string originalItemSpec = resolved
                        ? properties.GetStringProperty(ResolvedAssemblyReference.OriginalItemSpecProperty)
                        : itemSpec;

                    bool isImplicit = properties.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined) ?? false;

                    return CreateDependencyModel(
                        itemSpec,
                        originalItemSpec,
                        resolved,
                        isImplicit,
                        properties);
                }
            }
        }

        protected virtual IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            // Should be overridden by subclasses, unless they override and replace 'Handle'.
            // Not 'abstract' because a subclass could replace 'Handle', in which case they don't need this method.
            throw new NotImplementedException();
        }

        #endregion

        #region IProjectDependenciesSubTreeProvider

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
