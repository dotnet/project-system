// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.RuleHandlers
{
    internal abstract class DependenciesRuleHandlerBase
        : IDependenciesRuleHandler,
          IProjectDependenciesSubTreeProvider2
    {
        public string EvaluatedRuleName { get; }
        public string ResolvedRuleName { get; }

        public abstract ProjectTreeFlags GroupNodeFlag { get; }

        protected DependenciesRuleHandlerBase(
            string evaluatedRuleName,
            string resolvedRuleName)
        {
            Requires.NotNullOrWhiteSpace(evaluatedRuleName, nameof(evaluatedRuleName));
            Requires.NotNullOrWhiteSpace(resolvedRuleName, nameof(resolvedRuleName));

            EvaluatedRuleName = evaluatedRuleName;
            ResolvedRuleName = resolvedRuleName;
        }

        /// <summary>
        /// Controls whether a resolved item must have a corresponding evaluated item
        /// in order to be considered.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For most rules we require the item to be present in evaluation data as well
        /// as design-time data to be considered resolved. In general, all items should
        /// be provided to the tree by evaluation. However currently Analyzers are only
        /// available when resolved during design-time builds.
        /// </para>
        /// <para>
        /// https://github.com/dotnet/project-system/issues/4782 tracks making these
        /// items available during evaluation.
        /// </para>
        /// </remarks>
        protected virtual bool ResolvedItemRequiresEvaluatedItem => true;

        #region IDependenciesRuleHandler

        public void Handle(
            string projectFullPath,
            IProjectChangeDescription evaluationProjectChange,
            IProjectChangeDescription? buildProjectChange,
            TargetFramework targetFramework,
            DependenciesChangesBuilder changesBuilder)
        {
            bool hasResolvedData = buildProjectChange is not null;

            HandleChangesForRule(evaluationProjectChange, isResolvedItem: false);

            // We only have resolved data if the update came via the JointRule data source.
            if (hasResolvedData)
            {
                Func<string, bool>? isEvaluatedItemSpec = ResolvedItemRequiresEvaluatedItem ? evaluationProjectChange.After.Items.ContainsKey : null;

                HandleChangesForRule(evaluationProjectChange, isResolvedItem: true, buildProjectChange, isEvaluatedItemSpec);
            }

            return;

            void HandleChangesForRule(IProjectChangeDescription evaluationProjectChange, bool isResolvedItem, IProjectChangeDescription? buildProjectChange = null, Func<string, bool>? isEvaluatedItemSpec = null)
            {
                IProjectChangeDescription projectChange = buildProjectChange ?? evaluationProjectChange;

                foreach (string removedItem in projectChange.Difference.RemovedItems)
                {
                    HandleRemovedItem(projectFullPath, removedItem, isResolvedItem, projectChange, evaluationProjectChange.After, changesBuilder, targetFramework, isEvaluatedItemSpec);
                }

                foreach (string changedItem in projectChange.Difference.ChangedItems)
                {
                    HandleAddedItem(projectFullPath, changedItem, isResolvedItem, projectChange, evaluationProjectChange.After, changesBuilder, targetFramework, isEvaluatedItemSpec);
                }

                foreach (string addedItem in projectChange.Difference.AddedItems)
                {
                    HandleAddedItem(projectFullPath, addedItem, isResolvedItem, projectChange, evaluationProjectChange.After, changesBuilder, targetFramework, isEvaluatedItemSpec);
                }

                System.Diagnostics.Debug.Assert(projectChange.Difference.RenamedItems.Count == 0, "Project rule diff should not contain renamed items");
            }
        }

        protected virtual void HandleAddedItem(
            string projectFullPath,
            string addedItem,
            bool isResolvedItem,
            IProjectChangeDescription projectChange,
            IProjectRuleSnapshot evaluationRuleSnapshot,
            DependenciesChangesBuilder changesBuilder,
            TargetFramework targetFramework,
            Func<string, bool>? isEvaluatedItemSpec)
        {
            IDependencyModel? model = CreateDependencyModelForRule(addedItem, evaluationRuleSnapshot, projectChange.After, isResolvedItem, changesBuilder, targetFramework, projectFullPath);

            if (model is not null && (isEvaluatedItemSpec is null || isEvaluatedItemSpec(model.Id)))
            {
                changesBuilder.Added(targetFramework, model);
            }
        }

        protected virtual void HandleRemovedItem(
            string projectFullPath,
            string removedItem,
            bool isResolvedItem,
            IProjectChangeDescription projectChange,
            IProjectRuleSnapshot evaluationRuleSnapshot,
            DependenciesChangesBuilder changesBuilder,
            TargetFramework targetFramework,
            Func<string, bool>? isEvaluatedItemSpec)
        {
            if (isEvaluatedItemSpec is not null)
            {
                // The item is resolved
                string dependencyId = projectChange.Before.GetProjectItemProperties(removedItem)!.GetStringProperty(ProjectItemMetadata.OriginalItemSpec) ?? removedItem;

                if (!isEvaluatedItemSpec(dependencyId))
                {
                    // Skip any items returned by build that were not present in evaluation
                    return;
                }
            }

            changesBuilder.Removed(targetFramework, ProviderType, removedItem);
        }

        private IDependencyModel? CreateDependencyModelForRule(
            string itemSpec,
            IProjectRuleSnapshot evaluationRuleSnapshot,
            IProjectRuleSnapshot updatedRuleSnapshot,
            bool isResolved,
            DependenciesChangesBuilder changesBuilder,
            TargetFramework targetFramework,
            string projectFullPath)
        {
            IImmutableDictionary<string, string>? properties = updatedRuleSnapshot.GetProjectItemProperties(itemSpec);

            Assumes.NotNull(properties);

            string originalItemSpec = isResolved
                ? properties.GetStringProperty(ProjectItemMetadata.OriginalItemSpec) ?? itemSpec
                : itemSpec;

            IImmutableDictionary<string, string>? evaluationProperties = evaluationRuleSnapshot.GetProjectItemProperties(originalItemSpec);

            if (evaluationProperties is null)
            {
                if (ResolvedItemRequiresEvaluatedItem)
                {
                    // This item is present in build results, but not in evaluation.
                    return null;
                }

                evaluationProperties = properties;
            }

            bool isImplicit = IsImplicit(projectFullPath, evaluationProperties);

            // When we only have evaluation data, mark the dependency as resolved if we currently have a corresponding resolved item
            isResolved = isResolved || changesBuilder.HasResolvedItem(targetFramework, ProviderType, originalItemSpec);

            return CreateDependencyModel(
                itemSpec,
                originalItemSpec,
                isResolved,
                isImplicit,
                properties);
        }

        protected static bool IsImplicit(
            string projectFullPath,
            IImmutableDictionary<string, string> properties)
        {
            Requires.NotNull(projectFullPath, nameof(projectFullPath));
            Requires.NotNull(properties, nameof(properties));
            
            // Check for "IsImplicitlyDefined" metadata, which is available on certain items.
            bool? isImplicitMetadata = properties.GetBoolProperty(ProjectItemMetadata.IsImplicitlyDefined);

            if (isImplicitMetadata != null)
            {
                return isImplicitMetadata.Value;
            }

            // Check for "DefiningProjectFullPath" metadata and compare with the project file path.
            string? definingProjectFullPath = properties.GetStringProperty("DefiningProjectFullPath");

            if (!string.IsNullOrEmpty(definingProjectFullPath))
            {
                return !StringComparers.Paths.Equals(definingProjectFullPath, projectFullPath);
            }

            return false;
        }

        protected virtual IDependencyModel CreateDependencyModel(
            string path,
            string originalItemSpec,
            bool isResolved,
            bool isImplicit,
            IImmutableDictionary<string, string> properties)
        {
            // Should be overridden by subclasses, unless they override 'HandleAddedItem', 'HandleRemovedItem'
            // and 'HandleChangedItem' to not use this method.
            throw new NotImplementedException();
        }

        #endregion

        #region IProjectDependenciesSubTreeProvider

        public abstract string ProviderType { get; }

        public abstract IDependencyModel CreateRootDependencyNode();

        // NOTE we have no subclasses that fire this event
        public event EventHandler<DependenciesChangedEventArgs>? DependenciesChanged { add { } remove { } }

        #endregion
    }
}
