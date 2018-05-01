// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions
{
    internal abstract class DependenciesRuleHandlerBase :
        ICrossTargetRuleHandler<DependenciesRuleChangeContext>,
        IProjectDependenciesSubTreeProviderInternal
    {
        #region ICrossTargetRuleHandler

        public ImmutableHashSet<string> GetRuleNames(RuleHandlerType handlerType)
        {
            ImmutableHashSet<string> resultRules = ImmutableStringHashSet.EmptyOrdinal;
            if (handlerType == RuleHandlerType.Evaluation)
            {
                resultRules = resultRules.Add(UnresolvedRuleName);
            }
            else if (handlerType == RuleHandlerType.DesignTimeBuild)
            {
                resultRules = resultRules.Add(UnresolvedRuleName)
                                         .Add(ResolvedRuleName);
            }

            return resultRules;
        }

        protected abstract string UnresolvedRuleName { get; }
        protected abstract string ResolvedRuleName { get; }
        public abstract ImageMoniker GetImplicitIcon();

        /// <summary>
        /// If any standard provider has different OriginalItemSpec property name, 
        /// it could override this property, however currently all of them are the same.
        /// </summary>
        protected virtual string OriginalItemSpecPropertyName
        {
            get
            {
                return ResolvedAssemblyReference.OriginalItemSpecProperty;
            }
        }

        public virtual bool SupportsHandlerType(RuleHandlerType handlerType)
        {
            return true;
        }

        public virtual bool ReceiveUpdatesWithEmptyProjectChange
        {
            get
            {
                return false;
            }
        }

        public virtual Task HandleAsync(
            IProjectVersionedValue<Tuple<IProjectSubscriptionUpdate, IProjectCatalogSnapshot, IProjectCapabilitiesSnapshot>> e,
            IImmutableDictionary<string, IProjectChangeDescription> projectChanges,
            ITargetedProjectContext context,
            bool isActiveContext,
            DependenciesRuleChangeContext ruleChangeContext)
        {
            if (projectChanges.TryGetValue(UnresolvedRuleName, out IProjectChangeDescription unresolvedChanges))
            {
                HandleChangesForRule(false /*unresolved*/,
                    unresolvedChanges, context, isActiveContext, ruleChangeContext,
                        (itemSpec) => { return true; });
            }

            if (projectChanges.TryGetValue(ResolvedRuleName, out IProjectChangeDescription resolvedChanges))
            {
                HandleChangesForRule(true /*resolved*/,
                    resolvedChanges, context, isActiveContext, ruleChangeContext,
                    (metadata) => { return DoesUnresolvedProjectItemExist(metadata.OriginalItemSpec, unresolvedChanges); });
            }

            return Task.CompletedTask;
        }

        private void HandleChangesForRule(
            bool resolved,
            IProjectChangeDescription projectChange,
            ITargetedProjectContext context,
            bool isActiveContext,
            DependenciesRuleChangeContext ruleChangeContext,
            Func<IDependencyModel, bool> shouldProcess)
        {
            foreach (string removedItem in projectChange.Difference.RemovedItems)
            {
                IDependencyModel model = CreateDependencyModelForRule(removedItem, resolved, projectChange.Before, context.TargetFramework);
                if (shouldProcess(model))
                {
                    ruleChangeContext.IncludeRemovedChange(context.TargetFramework, model);
                }
            }

            foreach (string changedItem in projectChange.Difference.ChangedItems)
            {
                IDependencyModel model = CreateDependencyModelForRule(changedItem, resolved, projectChange.After, context.TargetFramework);
                if (shouldProcess(model))
                {
                    // For changes we try to add new dependency. If it is a resolved dependency, it would just override
                    // old one with new properties. If it is unresolved dependency, it would be added only when there no
                    // resolved version in the snapshot.
                    ruleChangeContext.IncludeAddedChange(context.TargetFramework, model);
                }
            }

            foreach (string addedItem in projectChange.Difference.AddedItems)
            {
                IDependencyModel model = CreateDependencyModelForRule(addedItem, resolved, projectChange.After, context.TargetFramework);
                if (shouldProcess(model))
                {
                    ruleChangeContext.IncludeAddedChange(context.TargetFramework, model);
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
            IProjectRuleSnapshot projectRuleSnapshot,
            ITargetFramework targetFramework)
        {
            IImmutableDictionary<string, string> properties = GetProjectItemProperties(projectRuleSnapshot, itemSpec);
            string originalItemSpec = itemSpec;
            if (resolved)
            {
                originalItemSpec = GetOriginalItemSpec(properties);
            }

            bool isImplicit = false;
            if (properties != null
                && properties.TryGetValue(ProjectItemMetadata.IsImplicitlyDefined, out string isImplicitlyDefinedString)
                && bool.TryParse(isImplicitlyDefinedString, out bool isImplicitlyDefined))
            {
                isImplicit = isImplicitlyDefined;
            }

            return CreateDependencyModel(
                ProviderType,
                itemSpec,
                originalItemSpec,
                resolved,
                isImplicit,
                properties);
        }

        protected virtual IDependencyModel CreateDependencyModel(
            string providerType,
            string path,
            string originalItemSpec,
            bool resolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            return null;
        }

        public Task OnContextReleasedAsync(ITargetedProjectContext context)
        {
            return Task.CompletedTask;
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

        protected virtual string GetOriginalItemSpec(IImmutableDictionary<string, string> properties)
        {
            if (properties != null && properties.TryGetValue(OriginalItemSpecPropertyName, out string originalItemSpec)
                && !string.IsNullOrEmpty(originalItemSpec))
            {
                return originalItemSpec;
            }

            return null;
        }

        /// <summary>
        /// Finds the resolved reference item for a given unresolved reference.
        /// </summary>
        /// <param name="projectRuleSnapshot">Resolved reference project items snapshot to search.</param>
        /// <param name="itemSpec">The unresolved reference item name.</param>
        /// <returns>The key is item name and the value is the metadata dictionary.</returns>
        protected static IImmutableDictionary<string, string> GetProjectItemProperties(
            IProjectRuleSnapshot projectRuleSnapshot,
            string itemSpec)
        {
            Requires.NotNull(projectRuleSnapshot, nameof(projectRuleSnapshot));
            Requires.NotNullOrEmpty(itemSpec, nameof(itemSpec));

            projectRuleSnapshot.Items.TryGetValue(itemSpec, out IImmutableDictionary<string, string> properties);

            return properties;
        }
    }
}
