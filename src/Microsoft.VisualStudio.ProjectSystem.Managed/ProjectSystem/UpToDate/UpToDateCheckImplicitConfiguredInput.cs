// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    /// <summary>
    /// Models all up-to-date check state for a single configuration.
    /// </summary>
    /// <remarks>
    /// Produced by <see cref="IUpToDateCheckImplicitConfiguredInputDataSource" />.
    /// </remarks>
    internal sealed class UpToDateCheckImplicitConfiguredInput
    {
        public static UpToDateCheckImplicitConfiguredInput CreateEmpty(ProjectConfiguration projectConfiguration)
        {
            return new UpToDateCheckImplicitConfiguredInput(projectConfiguration);
        }

        public static UpToDateCheckImplicitConfiguredInput CreateDisabled(ProjectConfiguration projectConfiguration)
        {
            return new UpToDateCheckImplicitConfiguredInput(
                projectConfiguration: projectConfiguration,
                msBuildProjectFullPath: null,
                msBuildProjectDirectory: null,
                copyUpToDateMarkerItem: null,
                outputRelativeOrFullPath: null,
                newestImportInput: null,
                isDisabled: true,
                isCopyAlwaysOptimizationDisabled: false,
                inputSourceItemTypes: ImmutableArray<string>.Empty,
                inputSourceItemsByItemType: ImmutableDictionary<string, ImmutableArray<UpToDateCheckInputItem>>.Empty,
                upToDateCheckInputItemsByKindBySetName: ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>>.Empty,
                upToDateCheckOutputItemsByKindBySetName: ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>>.Empty,
                upToDateCheckBuiltItemsByKindBySetName: ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>>.Empty,
                copiedOutputFiles: ImmutableArray<(string DestinationRelative, string SourceRelative)>.Empty,
                resolvedAnalyzerReferencePaths: ImmutableArray<string>.Empty,
                resolvedCompilationReferencePaths: ImmutableArray<string>.Empty,
                copyReferenceInputs: ImmutableArray<string>.Empty,
                lastItemsChangedAtUtc: DateTime.MinValue,
                lastItemChanges: ImmutableArray<(bool IsAdd, string ItemType, UpToDateCheckInputItem)>.Empty,
                itemHash: null);
        }

        /// <summary>
        /// Gets the project configuration for this configured data snapshot.
        /// </summary>
        /// <remarks>
        /// Useful when a project multi-targets and we want to differentiate targets in log output.
        /// <see langword="null"/> when the up-to-date check is disabled.
        /// </remarks>
        public ProjectConfiguration ProjectConfiguration { get; }

        public string? MSBuildProjectFullPath { get; }

        public string? MSBuildProjectDirectory { get; }

        public string? CopyUpToDateMarkerItem { get; }

        public string? OutputRelativeOrFullPath { get; }

        /// <summary>
        /// Contains the first path from the <see cref="ConfigurationGeneral.MSBuildAllProjectsProperty"/>,
        /// which MSBuild guarantees to be the newest import from all properties (since 16.0). As we
        /// are only interested in the newest import, we need not retain the remaining paths.
        /// </summary>
        public string? NewestImportInput { get; }

        /// <summary>
        /// Gets whether the fast up-to-date check has been disabled for this project.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Customers disable the fast up-to-date check by setting the <c>DisableFastUpToDateCheck</c>
        /// project property to <c>true</c>.
        /// </para>
        /// <para>
        /// When <see langword="true"/>, other properties on this snapshot will not be used, and so
        /// are not populated.
        /// </para>
        /// </remarks>
        public bool IsDisabled { get; }

        /// <summary>
        /// Gets whether the fast up-to-date check should not optimize copy-always items.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In VS17.2 we introduced an optimization for <c>CopyToOutputDirectory="Always"</c> items.
        /// Previously, the presence of such an item in the project would cause the FUTD check to immediately
        /// fail. With this optimization, we only fail if the timestamps or file sizes differ.
        /// </para>
        /// <para>
        /// Customers can restore the old behaviour by setting the <c>DisableFastUpToDateCopyAlwaysOptimization</c>
        /// project property to <c>true</c>.
        /// </para>
        /// </remarks>
        public bool IsCopyAlwaysOptimizationDisabled { get; }

        /// <summary>
        /// Gets the time at which the set of items changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is not the last timestamp of the items themselves. It is time at which items were
        /// last added or removed from the project.
        /// </para>
        /// <para>
        /// This property is not set until the set of items is observed changing. Until that time it will
        /// be <see langword="null"/>, representing the fact that the value is unknown and we cannot base
        /// any decisions on this data property.
        /// </para>
        /// </remarks>
        public DateTime? LastItemsChangedAtUtc { get; }

        public ImmutableArray<(bool IsAdd, string ItemType, UpToDateCheckInputItem Item)> LastItemChanges { get; }

        public int? ItemHash { get; }

        /// <summary>
        /// Gets the set of source item types known to be up-to-date check inputs.
        /// </summary>
        /// <remarks>
        /// Items of these types are available in <see cref="InputSourceItemsByItemType"/>.
        /// </remarks>
        public ImmutableArray<string> InputSourceItemTypes { get; }

        public ImmutableDictionary<string, ImmutableArray<UpToDateCheckInputItem>> InputSourceItemsByItemType { get; }

        public ImmutableArray<string> SetNames { get; }

        public ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> UpToDateCheckInputItemsByKindBySetName { get; }

        public ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> UpToDateCheckOutputItemsByKindBySetName { get; }

        public ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> UpToDateCheckBuiltItemsByKindBySetName { get; }

        /// <summary>
        /// Holds <see cref="UpToDateCheckBuilt"/> items which are copied, not built.
        /// </summary>
        /// <remarks>
        /// Projects add to this collection by specifying the <see cref="UpToDateCheckBuilt.OriginalProperty"/>
        /// on <see cref="UpToDateCheckBuilt"/> items.
        /// </remarks>
        public ImmutableArray<(string DestinationRelative, string SourceRelative)> CopiedOutputFiles { get; }

        public ImmutableArray<string> ResolvedAnalyzerReferencePaths { get; }

        /// <summary>
        /// Absolute (rooted) paths to <see cref="ResolvedCompilationReference"/> items in the project.
        /// </summary>
        public ImmutableArray<string> ResolvedCompilationReferencePaths { get; }

        /// <summary>
        /// Holds the set of observed <see cref="CopyUpToDateMarker"/> metadata values from all
        /// <see cref="ResolvedCompilationReference"/> items in the project.
        /// </summary>
        public ImmutableArray<string> CopyReferenceInputs { get; }

        private UpToDateCheckImplicitConfiguredInput(ProjectConfiguration projectConfiguration)
        {
            var emptyItemBySetName = ImmutableDictionary.Create<string, ImmutableDictionary<string, ImmutableArray<string>>>(BuildUpToDateCheck.SetNameComparer);

            ProjectConfiguration = projectConfiguration;
            LastItemsChangedAtUtc = null;
            InputSourceItemTypes = ImmutableArray<string>.Empty;
            InputSourceItemsByItemType = ImmutableDictionary.Create<string, ImmutableArray<UpToDateCheckInputItem>>(StringComparers.ItemTypes);
            SetNames = ImmutableArray<string>.Empty;
            UpToDateCheckInputItemsByKindBySetName = emptyItemBySetName;
            UpToDateCheckOutputItemsByKindBySetName = emptyItemBySetName;
            UpToDateCheckBuiltItemsByKindBySetName = emptyItemBySetName;
            CopiedOutputFiles = ImmutableArray<(string DestinationRelative, string SourceRelative)>.Empty;
            ResolvedAnalyzerReferencePaths = ImmutableArray<string>.Empty;
            ResolvedCompilationReferencePaths = ImmutableArray<string>.Empty;
            CopyReferenceInputs = ImmutableArray<string>.Empty;
        }

        private UpToDateCheckImplicitConfiguredInput(
            ProjectConfiguration projectConfiguration,
            string? msBuildProjectFullPath,
            string? msBuildProjectDirectory,
            string? copyUpToDateMarkerItem,
            string? outputRelativeOrFullPath,
            string? newestImportInput,
            bool isDisabled,
            bool isCopyAlwaysOptimizationDisabled,
            ImmutableArray<string> inputSourceItemTypes,
            ImmutableDictionary<string, ImmutableArray<UpToDateCheckInputItem>> inputSourceItemsByItemType,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckInputItemsByKindBySetName,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckOutputItemsByKindBySetName,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckBuiltItemsByKindBySetName,
            ImmutableArray<(string DestinationRelative, string SourceRelative)> copiedOutputFiles,
            ImmutableArray<string> resolvedAnalyzerReferencePaths,
            ImmutableArray<string> resolvedCompilationReferencePaths,
            ImmutableArray<string> copyReferenceInputs,
            DateTime? lastItemsChangedAtUtc,
            ImmutableArray<(bool IsAdd, string ItemType, UpToDateCheckInputItem)> lastItemChanges,
            int? itemHash)
        {
            ProjectConfiguration = projectConfiguration;
            MSBuildProjectFullPath = msBuildProjectFullPath;
            MSBuildProjectDirectory = msBuildProjectDirectory;
            CopyUpToDateMarkerItem = copyUpToDateMarkerItem;
            OutputRelativeOrFullPath = outputRelativeOrFullPath;
            NewestImportInput = newestImportInput;
            IsDisabled = isDisabled;
            IsCopyAlwaysOptimizationDisabled = isCopyAlwaysOptimizationDisabled;
            InputSourceItemTypes = inputSourceItemTypes;
            InputSourceItemsByItemType = inputSourceItemsByItemType;
            UpToDateCheckInputItemsByKindBySetName = upToDateCheckInputItemsByKindBySetName;
            UpToDateCheckOutputItemsByKindBySetName = upToDateCheckOutputItemsByKindBySetName;
            UpToDateCheckBuiltItemsByKindBySetName = upToDateCheckBuiltItemsByKindBySetName;
            CopiedOutputFiles = copiedOutputFiles;
            ResolvedAnalyzerReferencePaths = resolvedAnalyzerReferencePaths;
            ResolvedCompilationReferencePaths = resolvedCompilationReferencePaths;
            CopyReferenceInputs = copyReferenceInputs;
            LastItemsChangedAtUtc = lastItemsChangedAtUtc;
            LastItemChanges = lastItemChanges;
            ItemHash = itemHash;

            var setNames = new HashSet<string>(BuildUpToDateCheck.SetNameComparer);
            AddKeys(upToDateCheckInputItemsByKindBySetName);
            AddKeys(upToDateCheckOutputItemsByKindBySetName);
            AddKeys(upToDateCheckBuiltItemsByKindBySetName);
            setNames.Remove(BuildUpToDateCheck.DefaultSetName);
            SetNames = setNames.OrderBy(n => n, BuildUpToDateCheck.SetNameComparer).ToImmutableArray();

            void AddKeys(ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> dictionary)
            {
                foreach ((string key, _) in dictionary)
                {
                    setNames.Add(key);
                }
            }
        }

        public UpToDateCheckImplicitConfiguredInput Update(
            IProjectSubscriptionUpdate jointRuleUpdate,
            IProjectSubscriptionUpdate sourceItemsUpdate,
            IProjectItemSchema projectItemSchema,
            IProjectCatalogSnapshot projectCatalogSnapshot)
        {
            bool isDisabled = jointRuleUpdate.CurrentState.IsPropertyTrue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, defaultValue: false);

            if (isDisabled)
            {
                return CreateDisabled(ProjectConfiguration);
            }

            string? msBuildProjectFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, MSBuildProjectFullPath);
            string? msBuildProjectDirectory = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectDirectoryProperty, MSBuildProjectDirectory);
            string? msBuildProjectOutputPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputPathProperty, OutputRelativeOrFullPath);
            string? outputRelativeOrFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutDirProperty, msBuildProjectOutputPath);
            string msBuildAllProjects = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildAllProjectsProperty, "");
            bool isCopyAlwaysOptimizationDisabled = jointRuleUpdate.CurrentState.IsPropertyTrue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCopyAlwaysOptimizationProperty, defaultValue: false);

            // The first item in this semicolon-separated list of project files will always be the one
            // with the newest timestamp. As we are only interested in timestamps on these files, we can
            // save memory and time by only considering this first path (dotnet/project-system#4333).
            string? newestImportInput = new LazyStringSplit(msBuildAllProjects, ';').FirstOrDefault();

            ProjectFileClassifier? projectFileClassifier = null;

            // Not all item types are up-to-date check inputs. Filter to the set that are.
            var inputSourceItemTypes = projectItemSchema
                .GetKnownItemTypes()
                .Where(itemType => projectItemSchema.GetItemType(itemType).UpToDateCheckInput)
                .ToHashSet(StringComparers.ItemTypes);

            var itemTypeDiff = new SetDiff<string>(InputSourceItemTypes, inputSourceItemTypes, StringComparers.ItemTypes);

            var inputSourceItemsByItemTypeBuilder = InputSourceItemsByItemType.ToBuilder();

            bool itemTypesChanged = false;

            List<(bool IsAdd, string ItemType, UpToDateCheckInputItem)> lastItemChanges = new();

            // If an item type was removed, remove all items of that type
            foreach (string removedItemType in itemTypeDiff.Removed)
            {
                itemTypesChanged = true;

                if (inputSourceItemsByItemTypeBuilder.TryGetValue(removedItemType, out ImmutableArray<UpToDateCheckInputItem> removedItems))
                {
                    foreach (UpToDateCheckInputItem item in removedItems)
                    {
                        lastItemChanges.Add((IsAdd: false, removedItemType, item));
                    }

                    inputSourceItemsByItemTypeBuilder.Remove(removedItemType);
                }
            }

            itemTypesChanged |= itemTypeDiff.Added.GetEnumerator().MoveNext();

            bool itemsChanged = false;

            foreach ((string schemaName, IProjectChangeDescription projectChange) in sourceItemsUpdate.ProjectChanges)
            {
                if ((!projectChange.Difference.AnyChanges && !itemTypesChanged) ||
                   (projectChange.After.Items.Count == 0 && projectChange.Difference.RemovedItems.Count == 0))
                {
                    continue;
                }

                // Rule name (schema name) is usually the same as its item type, but not always (eg: auto-generated rules)
                string? itemType = null;
                if (projectCatalogSnapshot.NamedCatalogs.TryGetValue(PropertyPageContexts.File, out IPropertyPagesCatalog? fileCatalog))
                    itemType = fileCatalog.GetSchema(schemaName)?.DataSource.ItemType;

                if (itemType is null || !inputSourceItemTypes.Contains(itemType))
                {
                    continue;
                }

                ImmutableArray<UpToDateCheckInputItem> before = ImmutableArray<UpToDateCheckInputItem>.Empty;
                if (inputSourceItemsByItemTypeBuilder.TryGetValue(itemType, out ImmutableArray<UpToDateCheckInputItem> beforeItems))
                    before = beforeItems;

                var after = projectChange.After.Items
                    .Select(item => new UpToDateCheckInputItem(path: item.Key, itemType, metadata: item.Value))
                    .ToHashSet(UpToDateCheckInputItem.PathComparer);

                var diff = new SetDiff<UpToDateCheckInputItem>(before, after, UpToDateCheckInputItem.PathComparer);

                foreach (UpToDateCheckInputItem item in diff.Added)
                {
                    lastItemChanges.Add((IsAdd: true, itemType, item));
                }

                foreach (UpToDateCheckInputItem item in diff.Removed)
                {
                    lastItemChanges.Add((IsAdd: false, itemType, item));
                }

                inputSourceItemsByItemTypeBuilder[itemType] = after.ToImmutableArray();
                itemsChanged = true;
            }

            ImmutableDictionary<string, ImmutableArray<UpToDateCheckInputItem>> inputSourceItemsByItemType = inputSourceItemsByItemTypeBuilder.ToImmutable();

            int itemHash = BuildUpToDateCheck.ComputeItemHash(inputSourceItemsByItemType);

            DateTime? lastItemsChangedAtUtc = LastItemsChangedAtUtc;

            if (itemHash != ItemHash && ItemHash != null)
            {
                // The set of items has changed.
                // For the case that the project loaded and no hash was available, do not touch lastItemsChangedAtUtc.
                // Doing so when the project is actually up-to-date would cause builds to be scheduled until something
                // actually changes.
                lastItemsChangedAtUtc = DateTime.UtcNow;
            }
            else if (itemsChanged && !InputSourceItemTypes.IsEmpty)
            {
                // When we previously had zero item types, we can surmise that the project has just been loaded. In such
                // a case it is not correct to assume that the items changed, and so we do not update the timestamp.
                // If we did, and the project was up-to-date when loaded, it would remain out-of-date until something changed,
                // causing redundant builds until that time. See https://github.com/dotnet/project-system/issues/5386.
                lastItemsChangedAtUtc = DateTime.UtcNow;
            }

            (ImmutableArray<string> resolvedCompilationReferencePaths, ImmutableArray<string> copyReferenceInputs) = UpdateResolvedCompilationReferences();

            return new(
                ProjectConfiguration,
                msBuildProjectFullPath,
                msBuildProjectDirectory,
                copyUpToDateMarkerItem: UpdateCopyUpToDateMarkerItem(),
                outputRelativeOrFullPath,
                newestImportInput,
                isDisabled: isDisabled,
                isCopyAlwaysOptimizationDisabled: isCopyAlwaysOptimizationDisabled,
                inputSourceItemTypes: inputSourceItemTypes.ToImmutableArray(),
                inputSourceItemsByItemType: inputSourceItemsByItemType,
                upToDateCheckInputItemsByKindBySetName:  UpdateItemsByKindBySetName(UpToDateCheckInputItemsByKindBySetName,  jointRuleUpdate, UpToDateCheckInput.SchemaName,  UpToDateCheckInput.KindProperty,  UpToDateCheckInput.SetProperty),
                upToDateCheckOutputItemsByKindBySetName: UpdateItemsByKindBySetName(UpToDateCheckOutputItemsByKindBySetName, jointRuleUpdate, UpToDateCheckOutput.SchemaName, UpToDateCheckOutput.KindProperty, UpToDateCheckOutput.SetProperty),
                upToDateCheckBuiltItemsByKindBySetName:  UpdateItemsByKindBySetName(UpToDateCheckBuiltItemsByKindBySetName,  jointRuleUpdate, UpToDateCheckBuilt.SchemaName,  UpToDateCheckBuilt.KindProperty,  UpToDateCheckBuilt.SetProperty, metadata => !metadata.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) || string.IsNullOrEmpty(source)),
                copiedOutputFiles: UpdateCopiedItems(),
                resolvedAnalyzerReferencePaths: UpdateResolvedAnalyzerReferencePaths(),
                resolvedCompilationReferencePaths: resolvedCompilationReferencePaths,
                copyReferenceInputs: copyReferenceInputs,
                lastItemsChangedAtUtc: lastItemsChangedAtUtc,
                lastItemChanges.ToImmutableArray(),
                itemHash);

            string? UpdateCopyUpToDateMarkerItem()
            {
                if (jointRuleUpdate.ProjectChanges.TryGetValue(CopyUpToDateMarker.SchemaName, out IProjectChangeDescription? change) && change.Difference.AnyChanges)
                {
                    return change.After.Items.Count == 1 ? change.After.Items.Single().Key : null;
                }

                return CopyUpToDateMarkerItem;
            }

            static ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> UpdateItemsByKindBySetName(
                ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> prior,
                IProjectSubscriptionUpdate update,
                string itemSchemaName,
                string kindPropertyName,
                string setPropertyName,
                Predicate<IImmutableDictionary<string, string>>? metadataPredicate = null)
            {
                if (!update.ProjectChanges.TryGetValue(itemSchemaName, out IProjectChangeDescription projectChangeDescription) || !projectChangeDescription.Difference.AnyChanges)
                {
                    // No change in state for this collection. Return the prior data unchanged.
                    return prior;
                }

                var itemsByKindBySet = new Dictionary<string, Dictionary<string, HashSet<string>>>(BuildUpToDateCheck.SetNameComparer);

                foreach ((string item, IImmutableDictionary<string, string> metadata) in projectChangeDescription.After.Items)
                {
                    if (metadataPredicate != null && !metadataPredicate(metadata))
                    {
                        continue;
                    }

                    string? setNames = metadata.GetStringProperty(setPropertyName);
                    string kindName = metadata.GetStringProperty(kindPropertyName) ?? BuildUpToDateCheck.DefaultKindName;

                    if (setNames != null)
                    {
                        foreach (string setName in new LazyStringSplit(setNames, ';'))
                        {
                            AddItem(setName, kindName, item);
                        }
                    }
                    else
                    {
                        AddItem(BuildUpToDateCheck.DefaultSetName, kindName, item);
                    }
                }

                return itemsByKindBySet.ToImmutableDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToImmutableDictionary(
                        pair => pair.Key,
                        pair => pair.Value.ToImmutableArray()),
                    BuildUpToDateCheck.SetNameComparer);

                void AddItem(string setName, string kindName, string item)
                {
                    if (!itemsByKindBySet.TryGetValue(setName, out Dictionary<string, HashSet<string>>? itemsByKind))
                    {
                        itemsByKindBySet[setName] = itemsByKind = new Dictionary<string, HashSet<string>>(BuildUpToDateCheck.SetNameComparer);
                    }

                    if (!itemsByKind.TryGetValue(kindName, out HashSet<string>? items))
                    {
                        itemsByKind[kindName] = items = new HashSet<string>(StringComparers.Paths);
                    }

                    items.Add(item);
                }
            }

            ImmutableArray<(string DestinationRelative, string SourceRelative)> UpdateCopiedItems()
            {
                if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckBuilt.SchemaName, out IProjectChangeDescription? change) && change.Difference.AnyChanges)
                {
                    var copiedOutputFilesBuilder = new Dictionary<string, string>(StringComparers.Paths);

                    foreach ((string destination, IImmutableDictionary<string, string> metadata) in change.After.Items)
                    {
                        if (metadata.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) && !string.IsNullOrEmpty(source))
                        {
                            // This file is copied, not built
                            // Remember the `Original` source for later
                            copiedOutputFilesBuilder[destination] = source;
                        }
                    }

                    return copiedOutputFilesBuilder.Select(kvp => (kvp.Key, kvp.Value)).ToImmutableArray();
                }
                else
                {
                    return CopiedOutputFiles;
                }
            }

            ImmutableArray<string> UpdateResolvedAnalyzerReferencePaths()
            {
                if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedAnalyzerReference.SchemaName, out IProjectChangeDescription? change) && change.Difference.AnyChanges)
                {
                    projectFileClassifier ??= BuildClassifier();

                    return change.After.Items
                        .Select(item => item.Value[ResolvedAnalyzerReference.ResolvedPathProperty])
                        .Where(path => !projectFileClassifier.IsNonModifiable(path))
                        .Distinct(StringComparers.Paths)
                        .ToImmutableArray();
                }

                return ResolvedAnalyzerReferencePaths;
            }

            (ImmutableArray<string> ResolvedCompilationReferencePaths, ImmutableArray<string> CopyReferenceInputs) UpdateResolvedCompilationReferences()
            {
                if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedCompilationReference.SchemaName, out IProjectChangeDescription? change) && change.Difference.AnyChanges)
                {
                    // We identify non-modifiable inputs (i.e. anything in Program Files, the VS install dir, or NuGet cache folders)
                    // and exclude them from the set of inputs we scan when an up-to-date query is made.
                    //
                    // For a .NET 5 xUnit project, this cuts the number of file timestamps checked from 187 to 17. Most of those are
                    // reference assemblies for the framework, which clearly aren't expected to change over time.
                    projectFileClassifier ??= BuildClassifier();

                    HashSet<string> resolvedCompilationReferencePathsBuilder = new(StringComparers.Paths);
                    HashSet<string> copyReferenceInputsBuilder = new(StringComparers.Paths);

                    foreach (IImmutableDictionary<string, string> itemMetadata in change.After.Items.Values)
                    {
                        string originalPath = itemMetadata[ResolvedCompilationReference.OriginalPathProperty];
                        string resolvedPath = itemMetadata[ResolvedCompilationReference.ResolvedPathProperty];
                        string copyReferenceInput = itemMetadata[CopyUpToDateMarker.SchemaName];

                        if (!projectFileClassifier.IsNonModifiable(resolvedPath))
                        {
                            resolvedCompilationReferencePathsBuilder.Add(resolvedPath);
                        }

                        if (!string.IsNullOrWhiteSpace(originalPath))
                        {
                            copyReferenceInputsBuilder.Add(originalPath);
                        }

                        if (!string.IsNullOrWhiteSpace(copyReferenceInput))
                        {
                            copyReferenceInputsBuilder.Add(copyReferenceInput);
                        }
                    }

                    return (resolvedCompilationReferencePathsBuilder.ToImmutableArray(), copyReferenceInputsBuilder.ToImmutableArray());
                }
                else
                {
                    return (ResolvedCompilationReferencePaths, CopyReferenceInputs);
                }
            }

            ProjectFileClassifier BuildClassifier()
            {
                return new ProjectFileClassifier
                {
                    NuGetPackageFolders = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.NuGetPackageFoldersProperty, "")
                };
            }
        }

        /// <summary>
        /// For unit tests only.
        /// </summary>
        internal UpToDateCheckImplicitConfiguredInput WithLastItemsChangedAtUtc(DateTime? lastItemsChangedAtUtc)
        {
            return new(
                ProjectConfiguration,
                MSBuildProjectFullPath,
                MSBuildProjectDirectory,
                CopyUpToDateMarkerItem,
                OutputRelativeOrFullPath,
                NewestImportInput,
                IsDisabled,
                IsCopyAlwaysOptimizationDisabled,
                InputSourceItemTypes,
                InputSourceItemsByItemType,
                UpToDateCheckInputItemsByKindBySetName,
                UpToDateCheckOutputItemsByKindBySetName,
                UpToDateCheckBuiltItemsByKindBySetName,
                CopiedOutputFiles,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                lastItemsChangedAtUtc,
                LastItemChanges,
                ItemHash);
        }

        public UpToDateCheckImplicitConfiguredInput WithRestoredState(int itemHash, DateTime? lastItemsChangedAtUtc)
        {
            return new(
                ProjectConfiguration,
                MSBuildProjectFullPath,
                MSBuildProjectDirectory,
                CopyUpToDateMarkerItem,
                OutputRelativeOrFullPath,
                NewestImportInput,
                IsDisabled,
                IsCopyAlwaysOptimizationDisabled,
                InputSourceItemTypes,
                InputSourceItemsByItemType,
                UpToDateCheckInputItemsByKindBySetName,
                UpToDateCheckOutputItemsByKindBySetName,
                UpToDateCheckBuiltItemsByKindBySetName,
                CopiedOutputFiles,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                lastItemsChangedAtUtc,
                LastItemChanges,
                itemHash);
        }
    }
}
