// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Subscriptions.MSBuildDependencies;

/// <summary>
/// Maintains a set of <see cref="MSBuildDependency"/> objects in response to project data updates.
/// Scoped to a particular dependency type, and a particular project configuration slice.
/// </summary>
internal sealed class MSBuildDependencyCollection
{
    private readonly Dictionary<string, MSBuildDependency> _dependencyById = new(StringComparers.DependencyIds);
    private readonly MSBuildDependencyFactoryBase _factory;

    public DependencyGroupType DependencyGroupType => _factory.DependencyGroupType;

    public MSBuildDependencyCollection(MSBuildDependencyFactoryBase factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Integrates an update of project data for this handler's rule.
    /// May receive either evaluation data, or joint (evaluation and build) data.
    /// If the data produces updated dependencies, they are returned via <paramref name="dependencies"/>.
    /// </summary>
    /// <remarks>
    /// When returning <see langword="true"/>, <paramref name="dependencies"/> may still be <see langword="null"/>.
    /// That would indicate that the entire dependency group should be removed from the snapshot.
    /// Alternatively, and empty collection can be returned which indicates that the group should
    /// still be displayed, just with no children.
    /// </remarks>
    /// <param name="evaluationProjectChange"></param>
    /// <param name="buildProjectChange"></param>
    /// <param name="projectFullPath"></param>
    /// <param name="dependencies"></param>
    /// <returns><see langword="true"/> if dependencies are changed, otherwise <see langword="false"/>.</returns>
    public bool TryUpdate(
        IProjectChangeDescription evaluationProjectChange,
        IProjectChangeDescription? buildProjectChange,
        string projectFullPath,
        out ImmutableArray<IDependency>? dependencies)
    {
        if (evaluationProjectChange.Difference.AnyChanges is false &&
            buildProjectChange?.Difference.AnyChanges is false or null)
        {
            // No change to process. Return early.
            dependencies = null;
            return false;
        }

        bool hasBuildError = !evaluationProjectChange.After.IsEvaluationSucceeded() || buildProjectChange?.After.IsEvaluationSucceeded() == false;

        System.Diagnostics.Debug.Assert(evaluationProjectChange.Difference.RenamedItems.Count == 0, "Evaluation ProjectChange should not contain renamed items");
        System.Diagnostics.Debug.Assert(buildProjectChange?.Difference.RenamedItems.Count is null or 0, "Build ProjectChange should not contain renamed items");

        bool isJointUpdate = buildProjectChange is not null;
        bool hasChange = false;

        // TODO preprocess the differences so that items that were both removed and added are considered as "changed" and only processed once

        // We process removals first. This prevents showing dependencies as unresolved when the resolved item changes for some reason,
        // such as the active configuration changing (e.g. from Debug to Release). Such a scenario both removes and adds an item.
        // The removal will mark the dependency as unresolved, so we do it first which allows the update to return it to resolved
        // state if appropriate.

        ProcessRemovals();

        ProcessAddsAndUpdates();

        if (hasChange)
        {
            if (_dependencyById.Count == 0)
            {
                // No dependencies exist. They must have all been removed. Remove the group.
                dependencies = null;
            }
            else
            {
                dependencies = _dependencyById.Values.ToImmutableArray<IDependency>();
            }

            return true;
        }

        dependencies = null;
        return hasChange;

        void ProcessAddsAndUpdates()
        {
            if (evaluationProjectChange.Difference.AddedItems.Count is 0 &&
                evaluationProjectChange.Difference.ChangedItems.Count is 0 &&
                buildProjectChange?.Difference.AddedItems.Count is 0 or null &&
                buildProjectChange?.Difference.ChangedItems.Count is 0 or null)
            {
                // Nothing added or changed. Return early.
                return;
            }

            foreach ((string id, ItemUpdate update) in ComposeItemUpdates())
            {
                if (_dependencyById.TryGetValue(id, out MSBuildDependency? dependency))
                {
                    // Updating an existing dependency.
                    if (_factory.TryUpdate(dependency, update.Evaluation, update.Build, projectFullPath, isEvaluationOnlySnapshot: buildProjectChange is null, hasBuildError, out MSBuildDependency? updated))
                    {
                        if (updated is not null)
                        {
                            _dependencyById[id] = updated;
                        }
                        else
                        {
                            _dependencyById.Remove(id);
                        }

                        hasChange = true;
                    }
                }
                else
                {
                    // Creating a new dependency.
                    dependency = _factory.CreateDependency(id, update.Evaluation, update.Build, projectFullPath, hasBuildError, isEvaluationOnlySnapshot: buildProjectChange is null);

                    if (dependency is not null)
                    {
                        _dependencyById.Add(id, dependency);
                        hasChange = true;
                    }
                }
            }

            return;

            Dictionary<string, ItemUpdate> ComposeItemUpdates()
            {
                // When we have both evaluation and build data, we would ideally join them into one set of pairs, then process those pairs.
                // However the two data sources use different identifiers (resolved items commonly have a different item spec than their unresolved counterparts).
                // To address this we take the following approach to produce a joined view over the data for all items involved in add/update operations.

                Dictionary<string, ItemUpdate> updateById = new(StringComparers.DependencyIds);
                int missingBuildCount = 0;

                foreach (string id in evaluationProjectChange.Difference.AddedItems)
                {
                    AddEvaluationUpdate(id);
                }

                foreach (string id in evaluationProjectChange.Difference.ChangedItems)
                {
                    AddEvaluationUpdate(id);
                }

                if (buildProjectChange is not null)
                {
                    foreach (string resolvedItemSpec in buildProjectChange.Difference.AddedItems)
                    {
                        AddBuildUpdate(resolvedItemSpec);
                    }

                    foreach (string resolvedItemSpec in buildProjectChange.Difference.ChangedItems)
                    {
                        AddBuildUpdate(resolvedItemSpec);
                    }

                    PopulateMissingBuildData();
                }

                return updateById;

                void AddEvaluationUpdate(string id)
                {
                    updateById.Add(id, new() { Evaluation = (id, evaluationProjectChange.After.Items[id]) });
                    missingBuildCount++;
                }

                void AddBuildUpdate(string resolvedItemSpec)
                {
                    IImmutableDictionary<string, string> buildProperties = buildProjectChange.After.Items[resolvedItemSpec];

                    string? id = _factory.GetOriginalItemSpec(resolvedItemSpec, buildProperties);

                    if (id is null)
                    {
                        return;
                    }

                    if (!updateById.TryGetValue(id, out ItemUpdate? update))
                    {
                        update = new();
                        updateById.Add(id, update);
                    }
                    else
                    {
                        missingBuildCount--;
                    }

                    System.Diagnostics.Debug.Assert(update.Build is null, "Update's Build property should be null.");

                    update.Build = (resolvedItemSpec, buildProperties);

                    if (update.Evaluation is null && evaluationProjectChange.After.Items.TryGetValue(id, out IImmutableDictionary<string, string>? evaluationProperties))
                    {
                        update.Evaluation = (id, evaluationProperties);
                    }
                }

                void PopulateMissingBuildData()
                {
                    System.Diagnostics.Debug.Assert(missingBuildCount >= 0, "At least one missing build data is expected.");

                    if (missingBuildCount == 0)
                    {
                        return;
                    }

                    foreach ((string resolvedItemSpec, IImmutableDictionary<string, string> buildProperties) in buildProjectChange.After.Items)
                    {
                        string? id = _factory.GetOriginalItemSpec(resolvedItemSpec, buildProperties);

                        if (id is not null && updateById.TryGetValue(id, out ItemUpdate? update) && update.Build is null)
                        {
                            update.Build = new(resolvedItemSpec, buildProperties);
                        }
                    }
                }
            }
        }

        void ProcessRemovals()
        {
            // Process evaluation removals.
            foreach (string id in evaluationProjectChange.Difference.RemovedItems)
            {
                if (_dependencyById.TryGetValue(id, out MSBuildDependency? dependency))
                {
                    if (_factory.ResolvedItemRequiresEvaluatedItem)
                    {
                        Assumes.True(_dependencyById.Remove(id));
                        hasChange = true;
                    }
                    else
                    {
                        // This factory allows dependencies to exist as resolved items only. See if we have one.
                        if (buildProjectChange is not null && !buildProjectChange.Before.Items.Any(pair => StringComparers.DependencyIds.Equals(id, _factory.GetOriginalItemSpec(pair.Key, pair.Value))))
                        {
                            // No resolved data exists for this dependency either. Remove it.
                            Assumes.True(_dependencyById.Remove(id));
                            hasChange = true;
                        }
                    }
                }
            }

            // Process build removals.
            if (buildProjectChange is not null)
            {
                foreach (string resolvedItemSpec in buildProjectChange.Difference.RemovedItems)
                {
                    string? id = _factory.GetOriginalItemSpec(resolvedItemSpec, buildProjectChange.Before.Items[resolvedItemSpec]);

                    if (id is not null && _dependencyById.TryGetValue(id, out MSBuildDependency? dependency))
                    {
                        if (!_factory.ResolvedItemRequiresEvaluatedItem && !evaluationProjectChange.Before.Items.ContainsKey(id))
                        {
                            // The item is not present in evaluation, and this factory doesn't require an evaluated item.
                            // The removal of the build item means that the item must be removed altogether, as there's no
                            // evaluation item to keep it present.
                            Assumes.True(_dependencyById.Remove(id));
                            hasChange = true;
                        }
                        else
                        {
                            // The resolved item was removed, yet the dependency still exists. Mark it as unresolved.
                            if (_factory.TryMakeUnresolved(dependency, out MSBuildDependency? updated))
                            {
                                _dependencyById[id] = updated;
                                hasChange = true;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>Represents an item being added or updated.</summary>
    private sealed class ItemUpdate
    {
        public (string ItemSpec, IImmutableDictionary<string, string> Properties)? Evaluation { get; set; }
        public (string ItemSpec, IImmutableDictionary<string, string> Properties)? Build { get; set; }
    }
}
