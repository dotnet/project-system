// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions.RuleHandlers
{
    internal abstract class DependenciesRuleHandlerBase
        : IDependenciesRuleHandler,
          IProjectDependenciesSubTreeProviderInternal
    {
        public string EvaluatedRuleName { get; }
        public string ResolvedRuleName { get; }

        protected DependenciesRuleHandlerBase(
            string evaluatedRuleName,
            string resolvedRuleName)
        {
            Requires.NotNullOrWhiteSpace(evaluatedRuleName, nameof(evaluatedRuleName));
            Requires.NotNullOrWhiteSpace(resolvedRuleName, nameof(resolvedRuleName));

            EvaluatedRuleName = evaluatedRuleName;
            ResolvedRuleName = resolvedRuleName;
        }

        #region IDependenciesRuleHandler

        public abstract ImageMoniker ImplicitIcon { get; }

        public virtual void Handle(
            IImmutableDictionary<NamedIdentity, IComparable> versions,
            IImmutableDictionary<string, IProjectChangeDescription> changesByRuleName,
            RuleSource source,
            ITargetFramework targetFramework,
            CrossTargetDependenciesChangesBuilder changesBuilder)
        {
            // We receive evaluated and resolved project data separately, each as its own rule.

            // We always have evaluated data.
            IProjectChangeDescription evaluatedChanges = changesByRuleName[EvaluatedRuleName];

            HandleChangesForRule(
                resolved: false,
                projectChange: evaluatedChanges,
                shouldProcess: dependencyId => true);

            // We only have resolved data if the update came via the JointRule data source.
            if (changesByRuleName.TryGetValue(ResolvedRuleName, out IProjectChangeDescription resolvedChanges))
            {
                HandleChangesForRule(
                    resolved: true,
                    projectChange: resolvedChanges,
                    shouldProcess: evaluatedChanges.After.Items.ContainsKey);
            }

            return;

            void HandleChangesForRule(bool resolved, IProjectChangeDescription projectChange, Func<string, bool> shouldProcess)
            {
                if (projectChange.Difference.RemovedItems.Count != 0)
                {
                    foreach (string removedItem in projectChange.Difference.RemovedItems)
                    {
                        string dependencyId = resolved
                            ? projectChange.Before.GetProjectItemProperties(removedItem)!.GetStringProperty(ProjectItemMetadata.OriginalItemSpec) ?? removedItem
                            : removedItem;

                        if (shouldProcess(dependencyId))
                        {
                            changesBuilder.Removed(targetFramework, ProviderType, removedItem);
                        }
                    }
                }

                if (projectChange.Difference.ChangedItems.Count != 0)
                {
                    foreach (string changedItem in projectChange.Difference.ChangedItems)
                    {
                        IDependencyModel model = CreateDependencyModelForRule(changedItem, projectChange.After, resolved);
                        if (shouldProcess(model.Id))
                        {
                            // For changes we try to add new dependency. If it is a resolved dependency, it would just override
                            // old one with new properties. If it is unresolved dependency, it would be added only when there no
                            // resolved version in the snapshot.
                            changesBuilder.Added(targetFramework, model);
                        }
                    }
                }

                if (projectChange.Difference.AddedItems.Count != 0)
                {
                    foreach (string addedItem in projectChange.Difference.AddedItems)
                    {
                        IDependencyModel model = CreateDependencyModelForRule(addedItem, projectChange.After, resolved);
                        if (shouldProcess(model.Id))
                        {
                            changesBuilder.Added(targetFramework, model);
                        }
                    }
                }

                System.Diagnostics.Debug.Assert(evaluatedChanges.Difference.RenamedItems.Count == 0, "Project rule diff should not contain renamed items");
            }
        }

        private IDependencyModel CreateDependencyModelForRule(string itemSpec, IProjectRuleSnapshot projectRuleSnapshot, bool isResolved)
        {
            IImmutableDictionary<string, string> properties = projectRuleSnapshot.GetProjectItemProperties(itemSpec)!;

            string originalItemSpec = isResolved
                ? properties.GetStringProperty(ProjectItemMetadata.OriginalItemSpec) ?? itemSpec
                : itemSpec;

            bool isImplicit = properties.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined) ?? false;

            return CreateDependencyModel(
                itemSpec,
                originalItemSpec,
                isResolved,
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
            // Should be overridden by subclasses, unless they override and replace 'Handle'.
            // Not 'abstract' because a subclass could replace 'Handle', in which case they don't need this method.
            throw new NotImplementedException();
        }

        #endregion

        #region IProjectDependenciesSubTreeProvider

        public abstract string ProviderType { get; }

        public abstract IDependencyModel CreateRootDependencyNode();

        public event EventHandler<DependenciesChangedEventArgs>? DependenciesChanged;

        protected void FireDependenciesChanged(DependenciesChangedEventArgs args)
        {
            DependenciesChanged?.Invoke(this, args);
        }

        #endregion
    }
}
