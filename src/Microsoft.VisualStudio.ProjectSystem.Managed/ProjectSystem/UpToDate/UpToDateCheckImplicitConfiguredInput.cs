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
                projectTargetPath: null,
                copyUpToDateMarkerItem: null,
                outputRelativeOrFullPath: null,
                newestImportInput: null,
                isDisabled: true,
                isBuildAccelerationEnabled: false,
                inputSourceItemTypes: ImmutableArray<string>.Empty,
                inputSourceItemsByItemType: ImmutableDictionary<string, ImmutableArray<string>>.Empty,
                upToDateCheckInputItemsByKindBySetName: ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>>.Empty,
                upToDateCheckOutputItemsByKindBySetName: ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>>.Empty,
                upToDateCheckBuiltItemsByKindBySetName: ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>>.Empty,
                buildFromInputFileItems: ImmutableArray<(string DestinationRelative, string SourceRelative)>.Empty,
                resolvedAnalyzerReferencePaths: ImmutableArray<string>.Empty,
                resolvedCompilationReferencePaths: ImmutableArray<string>.Empty,
                copyReferenceInputs: ImmutableArray<string>.Empty,
                lastItemsChangedAtUtc: null,
                lastItemChanges: ImmutableArray<(bool IsAdd, string ItemType, string)>.Empty,
                itemHash: null,
                projectCopyData: default);
        }

        /// <summary>
        /// Gets the project configuration for this configured data snapshot.
        /// </summary>
        /// <remarks>
        /// Useful when a project multi-targets and we want to differentiate targets in log output.
        /// <see langword="null"/> when the up-to-date check is disabled.
        /// </remarks>
        public ProjectConfiguration ProjectConfiguration { get; }

        /// <summary>
        /// Gets the full path to the project file.
        /// </summary>
        /// <remarks>
        /// Comes from the <c>MSBuildProjectFullPath</c> MSBuild property.
        /// For example, <c>C:\repos\MySolution\MyProject\MyProject.csproj</c>.
        /// </remarks>
        public string? MSBuildProjectFullPath { get; }

        /// <summary>
        /// Gets the full path to the project directory.
        /// </summary>
        /// <remarks>
        /// Comes from the <c>MSBuildProjectDirectory</c> MSBuild property.
        /// For example, <c>C:\repos\MySolution\MyProject</c>.
        /// </remarks>
        public string? MSBuildProjectDirectory { get; }

        /// <summary>
        /// Gets the full path to the project's output file.
        /// </summary>
        /// <remarks>
        /// Comes from the <c>TargetPath</c> MSBuild property.
        /// For example, <c>C:\repos\MySolution\MyProject\bin\Debug\net6.0\MyProject.dll</c>.
        /// </remarks>
        public string? ProjectTargetPath { get; }

        /// <summary>
        /// Gets the output path of the project, relative to the project directory.
        /// </summary>
        /// <remarks>
        /// Comes from the <c>OutDir</c> MSBuild property, if available, otherwise from the <c>OutputPath</c> MSBuild property.
        /// For example, <c>bin\Debug\net6.0\</c>.
        /// </remarks>
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
        /// Gets whether the fast up-to-date check is prohibited from copying files in the case
        /// that the only reason a project is not up-to-date is due to the need to copy files.
        /// </summary>
        /// <remarks>
        /// Controlled by the <c>AccelerateBuildsInVisualStudio</c> project property.
        /// <see langword="null"/> when the property is undefined or cannot be parsed as a bool.
        /// </remarks>
        public bool? IsBuildAccelerationEnabled { get; }

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

        public ImmutableArray<(bool IsAdd, string ItemType, string Item)> LastItemChanges { get; }

        public int? ItemHash { get; }

        /// <summary>
        /// Gets the set of source item types known to be up-to-date check inputs.
        /// </summary>
        /// <remarks>
        /// Items of these types are available in <see cref="InputSourceItemsByItemType"/>.
        /// </remarks>
        public ImmutableArray<string> InputSourceItemTypes { get; }

        public ImmutableDictionary<string, ImmutableArray<string>> InputSourceItemsByItemType { get; }

        /// <summary>
        /// An alphabetically ordered list of the set names present in this project.
        /// </summary>
        /// <remarks>
        /// Does NOT contain <see cref="BuildUpToDateCheck.DefaultSetName"/>.
        /// </remarks>
        public ImmutableArray<string> SetNames { get; }

        /// <summary>
        /// An alphabetically ordered list of the kind names present in this project.
        /// </summary>
        /// <remarks>
        /// Contains <see cref="BuildUpToDateCheck.DefaultKindName"/>.
        /// </remarks>
        public ImmutableArray<string> KindNames { get; }

        public ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> UpToDateCheckInputItemsByKindBySetName { get; }

        public ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> UpToDateCheckOutputItemsByKindBySetName { get; }

        public ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> UpToDateCheckBuiltItemsByKindBySetName { get; }

        /// <summary>
        /// Holds <see cref="UpToDateCheckBuilt"/> items which are each built from a single input file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Projects add to this collection by specifying the <see cref="UpToDateCheckBuilt.OriginalProperty"/>
        /// on <see cref="UpToDateCheckBuilt"/> items.
        /// </para>
        /// <para>
        /// <see cref="UpToDateCheckBuilt"/> items without <see cref="UpToDateCheckBuilt.OriginalProperty"/> metadata
        /// are treated as regular output items, and are modelled in <see cref="UpToDateCheckBuiltItemsByKindBySetName"/>
        /// rather than here.
        /// </para>
        /// </remarks>
        public ImmutableArray<(string DestinationRelative, string SourceRelative)> BuiltFromInputFileItems { get; }

        public ImmutableArray<string> ResolvedAnalyzerReferencePaths { get; }

        /// <summary>
        /// Absolute (rooted) paths to <see cref="ResolvedCompilationReference"/> items in the project.
        /// </summary>
        public ImmutableArray<string> ResolvedCompilationReferencePaths { get; }

        /// <summary>
        /// Holds the set of non-empty <see cref="ResolvedCompilationReference.CopyUpToDateMarkerProperty"/> and
        /// <see cref="ResolvedCompilationReference.ResolvedPathProperty"/> metadata values from all
        /// <see cref="ResolvedCompilationReference"/> items in the project.
        /// </summary>
        public ImmutableArray<string> CopyReferenceInputs { get; }

        /// <summary>
        /// Gets the relative path to the output marker file.
        /// </summary>
        public string? CopyUpToDateMarkerItem { get; }

        /// <summary>
        /// Gets the set of items this project contributes to the output directory when built.
        /// These items are inherited by referencing projects too, transitively.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Includes various kinds of files including assemblies content files from this project ONLY.
        /// </para>
        /// <para>
        /// When building, we must obtain the full set of output directory items from this and all transitively referenced projects.
        /// This collection does not provide that. Use <see cref="ICopyItemAggregator"/> instead. The intent is for the implementation
        /// of that interface to consume this property.
        /// </para>
        /// </remarks>
        public ProjectCopyData ProjectCopyData { get; }

        private UpToDateCheckImplicitConfiguredInput(ProjectConfiguration projectConfiguration)
        {
            var emptyItemBySetName = ImmutableDictionary.Create<string, ImmutableDictionary<string, ImmutableArray<string>>>(BuildUpToDateCheck.SetNameComparer);

            ProjectConfiguration = projectConfiguration;
            LastItemsChangedAtUtc = null;
            InputSourceItemTypes = ImmutableArray<string>.Empty;
            InputSourceItemsByItemType = ImmutableDictionary.Create<string, ImmutableArray<string>>(StringComparers.ItemTypes);
            SetNames = ImmutableArray<string>.Empty;
            KindNames = ImmutableArray<string>.Empty;
            UpToDateCheckInputItemsByKindBySetName = emptyItemBySetName;
            UpToDateCheckOutputItemsByKindBySetName = emptyItemBySetName;
            UpToDateCheckBuiltItemsByKindBySetName = emptyItemBySetName;
            BuiltFromInputFileItems = ImmutableArray<(string DestinationRelative, string SourceRelative)>.Empty;
            ResolvedAnalyzerReferencePaths = ImmutableArray<string>.Empty;
            ResolvedCompilationReferencePaths = ImmutableArray<string>.Empty;
            CopyReferenceInputs = ImmutableArray<string>.Empty;
            LastItemChanges = ImmutableArray<(bool IsAdd, string ItemType, string)>.Empty;
            ProjectCopyData = new(null, "", ImmutableArray<CopyItem>.Empty, ImmutableArray<string>.Empty);
        }

        private UpToDateCheckImplicitConfiguredInput(
            ProjectConfiguration projectConfiguration,
            string? msBuildProjectFullPath,
            string? msBuildProjectDirectory,
            string? projectTargetPath,
            string? copyUpToDateMarkerItem,
            string? outputRelativeOrFullPath,
            string? newestImportInput,
            bool isDisabled,
            bool? isBuildAccelerationEnabled,
            ImmutableArray<string> inputSourceItemTypes,
            ImmutableDictionary<string, ImmutableArray<string>> inputSourceItemsByItemType,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckInputItemsByKindBySetName,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckOutputItemsByKindBySetName,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckBuiltItemsByKindBySetName,
            ImmutableArray<(string DestinationRelative, string SourceRelative)> buildFromInputFileItems,
            ImmutableArray<string> resolvedAnalyzerReferencePaths,
            ImmutableArray<string> resolvedCompilationReferencePaths,
            ImmutableArray<string> copyReferenceInputs,
            DateTime? lastItemsChangedAtUtc,
            ImmutableArray<(bool IsAdd, string ItemType, string)> lastItemChanges,
            int? itemHash,
            ProjectCopyData projectCopyData)
        {
            ProjectConfiguration = projectConfiguration;
            MSBuildProjectFullPath = msBuildProjectFullPath;
            MSBuildProjectDirectory = msBuildProjectDirectory;
            ProjectTargetPath = projectTargetPath;
            CopyUpToDateMarkerItem = copyUpToDateMarkerItem;
            OutputRelativeOrFullPath = outputRelativeOrFullPath;
            NewestImportInput = newestImportInput;
            IsDisabled = isDisabled;
            IsBuildAccelerationEnabled = isBuildAccelerationEnabled;
            InputSourceItemTypes = inputSourceItemTypes;
            InputSourceItemsByItemType = inputSourceItemsByItemType;
            UpToDateCheckInputItemsByKindBySetName = upToDateCheckInputItemsByKindBySetName;
            UpToDateCheckOutputItemsByKindBySetName = upToDateCheckOutputItemsByKindBySetName;
            UpToDateCheckBuiltItemsByKindBySetName = upToDateCheckBuiltItemsByKindBySetName;
            BuiltFromInputFileItems = buildFromInputFileItems;
            ResolvedAnalyzerReferencePaths = resolvedAnalyzerReferencePaths;
            ResolvedCompilationReferencePaths = resolvedCompilationReferencePaths;
            CopyReferenceInputs = copyReferenceInputs;
            LastItemsChangedAtUtc = lastItemsChangedAtUtc;
            LastItemChanges = lastItemChanges;
            ItemHash = itemHash;
            ProjectCopyData = projectCopyData;

            var setNames = new HashSet<string>(BuildUpToDateCheck.SetNameComparer);
            AddSetNames(upToDateCheckInputItemsByKindBySetName);
            AddSetNames(upToDateCheckOutputItemsByKindBySetName);
            AddSetNames(upToDateCheckBuiltItemsByKindBySetName);
            setNames.Remove(BuildUpToDateCheck.DefaultSetName);
            SetNames = setNames.OrderBy(n => n, BuildUpToDateCheck.SetNameComparer).ToImmutableArray();

            var kindNames = new HashSet<string>(BuildUpToDateCheck.KindNameComparer);
            AddKindNames(upToDateCheckInputItemsByKindBySetName);
            AddKindNames(upToDateCheckOutputItemsByKindBySetName);
            AddKindNames(upToDateCheckBuiltItemsByKindBySetName);
            KindNames = kindNames.OrderBy(n => n, BuildUpToDateCheck.KindNameComparer).ToImmutableArray();

            void AddSetNames(ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> itemsByKindBySetName)
            {
                foreach ((string setName, _) in itemsByKindBySetName)
                {
                    setNames.Add(setName);
                }
            }

            void AddKindNames(ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> itemsByKindBySetName)
            {
                foreach ((_, ImmutableDictionary<string, ImmutableArray<string>> itemsByKind) in itemsByKindBySetName)
                {
                    foreach ((string kind, _) in itemsByKind)
                    {
                        kindNames.Add(kind);
                    }
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

            string? msBuildProjectFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, defaultValue: MSBuildProjectFullPath);
            string? msBuildProjectDirectory = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectDirectoryProperty, defaultValue: MSBuildProjectDirectory);
            string? projectTargetPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.TargetPathProperty, defaultValue: "");
            string? projectOutputPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputPathProperty, defaultValue: OutputRelativeOrFullPath);
            string? outputRelativeOrFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutDirProperty, defaultValue: projectOutputPath);
            string msBuildAllProjects = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildAllProjectsProperty, defaultValue: "");
            bool? isBuildAccelerationEnabled = jointRuleUpdate.CurrentState.GetBooleanPropertyValue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.AccelerateBuildsInVisualStudioProperty);

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

            List<(bool IsAdd, string ItemType, string)> lastItemChanges = new();

            // If an item type was removed, remove all items of that type
            foreach (string removedItemType in itemTypeDiff.Removed)
            {
                itemTypesChanged = true;

                if (inputSourceItemsByItemTypeBuilder.TryGetValue(removedItemType, out ImmutableArray<string> removedItems))
                {
                    foreach (string item in removedItems)
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

                if (!inputSourceItemsByItemTypeBuilder.TryGetValue(itemType, out ImmutableArray<string> before))
                {
                    before = ImmutableArray<string>.Empty;
                }

                var after = projectChange.After.Items
                    .Select(item => item.Key)
                    .ToHashSet(StringComparers.Paths);

                var diff = new SetDiff<string>(before, after, StringComparers.Paths);

                foreach (string item in diff.Added)
                {
                    lastItemChanges.Add((IsAdd: true, itemType, item));
                }

                foreach (string item in diff.Removed)
                {
                    lastItemChanges.Add((IsAdd: false, itemType, item));
                }

                inputSourceItemsByItemTypeBuilder[itemType] = after.ToImmutableArray();
                itemsChanged = true;
            }

            ImmutableDictionary<string, ImmutableArray<string>> inputSourceItemsByItemType = inputSourceItemsByItemTypeBuilder.ToImmutable();

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
                projectTargetPath,
                copyUpToDateMarkerItem: UpdateCopyUpToDateMarkerItem(),
                outputRelativeOrFullPath,
                newestImportInput,
                isDisabled: isDisabled,
                isBuildAccelerationEnabled: isBuildAccelerationEnabled,
                inputSourceItemTypes: inputSourceItemTypes.ToImmutableArray(),
                inputSourceItemsByItemType: inputSourceItemsByItemType,
                upToDateCheckInputItemsByKindBySetName:  UpdateItemsByKindBySetName(UpToDateCheckInputItemsByKindBySetName,  jointRuleUpdate, UpToDateCheckInput.SchemaName,  UpToDateCheckInput.KindProperty,  UpToDateCheckInput.SetProperty),
                upToDateCheckOutputItemsByKindBySetName: UpdateItemsByKindBySetName(UpToDateCheckOutputItemsByKindBySetName, jointRuleUpdate, UpToDateCheckOutput.SchemaName, UpToDateCheckOutput.KindProperty, UpToDateCheckOutput.SetProperty),
                upToDateCheckBuiltItemsByKindBySetName:  UpdateItemsByKindBySetName(UpToDateCheckBuiltItemsByKindBySetName,  jointRuleUpdate, UpToDateCheckBuilt.SchemaName,  UpToDateCheckBuilt.KindProperty,  UpToDateCheckBuilt.SetProperty, metadata => !metadata.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) || string.IsNullOrEmpty(source)),
                buildFromInputFileItems: UpdateBuildFromInputFileItems(),
                resolvedAnalyzerReferencePaths: UpdateResolvedAnalyzerReferencePaths(),
                resolvedCompilationReferencePaths: resolvedCompilationReferencePaths,
                copyReferenceInputs: copyReferenceInputs,
                lastItemsChangedAtUtc: lastItemsChangedAtUtc,
                lastItemChanges.ToImmutableArray(),
                itemHash,
                projectCopyData: UpdateCopyData());

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

                foreach ((string item, IImmutableDictionary<string, string> metadata) in TryGetOrderedData(projectChangeDescription.After.Items))
                {
                    if (metadataPredicate is not null && !metadataPredicate(metadata))
                    {
                        continue;
                    }

                    string? setNames = metadata.GetStringProperty(setPropertyName);
                    string kindName = metadata.GetStringProperty(kindPropertyName) ?? BuildUpToDateCheck.DefaultKindName;

                    if (setNames is not null)
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

                static IEnumerable<KeyValuePair<string, IImmutableDictionary<string, string>>> TryGetOrderedData(IImmutableDictionary<string, IImmutableDictionary<string, string>> items)
                {
                    if (items is IDataWithOriginalSource<KeyValuePair<string, IImmutableDictionary<string, string>>> dataWithOriginalSource)
                        return dataWithOriginalSource.SourceData;

                    // We couldn't obtain ordered items for some reason.
                    // This is not a big problem, so just return the items in whatever order
                    // the backing collection from CPS models them in.
                    return items;
                }

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

            ImmutableArray<(string DestinationRelative, string SourceRelative)> UpdateBuildFromInputFileItems()
            {
                if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckBuilt.SchemaName, out IProjectChangeDescription? change) && change.Difference.AnyChanges)
                {
                    var buildFromInputFileItemsBuilder = new Dictionary<string, string>(StringComparers.Paths);

                    foreach ((string destination, IImmutableDictionary<string, string> metadata) in change.After.Items)
                    {
                        if (metadata.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) && !string.IsNullOrEmpty(source))
                        {
                            // This file is copied, not built
                            // Remember the `Original` source for later
                            buildFromInputFileItemsBuilder[destination] = source;
                        }
                    }

                    return buildFromInputFileItemsBuilder.Select(kvp => (kvp.Key, kvp.Value)).ToImmutableArray();
                }
                else
                {
                    return BuiltFromInputFileItems;
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

            ProjectCopyData UpdateCopyData()
            {
                if (jointRuleUpdate.ProjectChanges.TryGetValue(CopyToOutputDirectoryItem.SchemaName, out IProjectChangeDescription? change1) &&
                    jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedProjectReference.SchemaName, out IProjectChangeDescription? change2))
                {
                    if (change1.Difference.AnyChanges || change2.Difference.AnyChanges)
                    {
                        // Register this project's data with the CopyToOutputDirectoryItem tracking service.

                        string targetPath = jointRuleUpdate.CurrentState[ConfigurationGeneral.SchemaName].Properties[ConfigurationGeneral.TargetPathProperty];

                        projectFileClassifier ??= BuildClassifier();

                        ImmutableArray<CopyItem> copyItems = change1.After.Items
                            .Where(pair => !projectFileClassifier.IsNonModifiable(pair.Key))
                            .Select(pair => new CopyItem(path: pair.Key, metadata: pair.Value))
                            .ToImmutableArray();

                        ImmutableArray<string> referenceItems = change2.After.Items.Where(pair => IncludeProjectReference(pair.Value)).Select(item => item.Key).ToImmutableArray();

                        return new ProjectCopyData(msBuildProjectFullPath, targetPath, copyItems, referenceItems);
                    }
                }

                return ProjectCopyData;

                static bool IncludeProjectReference(IImmutableDictionary<string, string> metadata)
                {
                    // TODO this filtering is overzealous. In each of these cases, there are subtleties to how
                    // builds handle the output assembly vs. CopyToOutputDirectory items both of the directly
                    // referenced project, and of transitively referenced projects. To improve this we need
                    // more information on the edges of our project reference graph.

                    // Exclude any project reference for which we do not reference the output assembly.
                    if (metadata.GetBoolProperty(ResolvedProjectReference.ReferenceOutputAssemblyProperty) == false)
                    {
                        return false;
                    }

                    // Exclude any project reference having Private="false" (aka CopyLocal="No").
                    if (metadata.GetBoolProperty(ResolvedProjectReference.PrivateProperty) == false)
                    {
                        return false;
                    }

                    // Exclude any project reference having EmbedInteropTypes="true".
                    if (metadata.GetBoolProperty(ResolvedProjectReference.EmbedInteropTypesProperty) == true)
                    {
                        return false;
                    }

                    return true;
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
                ProjectTargetPath,
                CopyUpToDateMarkerItem,
                OutputRelativeOrFullPath,
                NewestImportInput,
                IsDisabled,
                IsBuildAccelerationEnabled,
                InputSourceItemTypes,
                InputSourceItemsByItemType,
                UpToDateCheckInputItemsByKindBySetName,
                UpToDateCheckOutputItemsByKindBySetName,
                UpToDateCheckBuiltItemsByKindBySetName,
                BuiltFromInputFileItems,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                lastItemsChangedAtUtc,
                LastItemChanges,
                ItemHash,
                ProjectCopyData);
        }

        public UpToDateCheckImplicitConfiguredInput WithRestoredState(int itemHash, DateTime? lastItemsChangedAtUtc)
        {
            return new(
                ProjectConfiguration,
                MSBuildProjectFullPath,
                MSBuildProjectDirectory,
                ProjectTargetPath,
                CopyUpToDateMarkerItem,
                OutputRelativeOrFullPath,
                NewestImportInput,
                IsDisabled,
                IsBuildAccelerationEnabled,
                InputSourceItemTypes,
                InputSourceItemsByItemType,
                UpToDateCheckInputItemsByKindBySetName,
                UpToDateCheckOutputItemsByKindBySetName,
                UpToDateCheckBuiltItemsByKindBySetName,
                BuiltFromInputFileItems,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                lastItemsChangedAtUtc,
                LastItemChanges,
                itemHash,
                ProjectCopyData);
        }
    }
}
