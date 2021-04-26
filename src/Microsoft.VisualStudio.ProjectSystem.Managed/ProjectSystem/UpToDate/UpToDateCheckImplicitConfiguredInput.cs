// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        public static UpToDateCheckImplicitConfiguredInput Empty { get; } = new();

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

        public IComparable? LastVersionSeen { get; }

        public bool IsDisabled { get; }

        /// <summary>
        /// Gets the time at which the set of items changed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is not the last timestamp of the items themselves. It is time at which items were
        /// last added or removed from the project.
        /// </para>
        /// <para>
        /// This property is not updated until after the first query occurs. Until that time it will
        /// equal <see cref="DateTime.MinValue"/> which represents the fact that we do not know when
        /// the set of items was last changed, so we cannot base any decisions on this data property.
        /// </para>
        /// </remarks>
        public DateTime LastItemsChangedAtUtc { get; }

        public ImmutableArray<(bool IsAdd, string ItemType, string Path, string? Link, BuildUpToDateCheck.CopyType CopyType)> LastItemChanges { get; }

        public ImmutableArray<string> ItemTypes { get; }

        public ImmutableDictionary<string, ImmutableArray<(string Path, string? Link, BuildUpToDateCheck.CopyType CopyType)>> ItemsByItemType { get; }

        public ImmutableArray<string> SetNames { get; }

        public ImmutableDictionary<string, ImmutableArray<string>> UpToDateCheckInputItemsBySetName { get; }

        public ImmutableDictionary<string, ImmutableArray<string>> UpToDateCheckOutputItemsBySetName { get; }

        public ImmutableDictionary<string, ImmutableArray<string>> UpToDateCheckBuiltItemsBySetName { get; }

        /// <summary>
        /// Holds <see cref="UpToDateCheckBuilt"/> items which are copied, not built.
        /// </summary>
        /// <remarks>
        /// Projects add to this collection by specifying the <see cref="UpToDateCheckBuilt.OriginalProperty"/>
        /// on <see cref="UpToDateCheckBuilt"/> items.
        /// </remarks>
        public ImmutableArray<(string DestinationRelative, string SourceRelative)> CopiedOutputFiles { get; }

        public ImmutableArray<string> ResolvedAnalyzerReferencePaths { get; }

        public ImmutableArray<string> ResolvedCompilationReferencePaths { get; }

        /// <summary>
        /// Holds the set of observed <see cref="CopyUpToDateMarker"/> metadata values from all
        /// <see cref="ResolvedCompilationReference"/> items in the project.
        /// </summary>
        public ImmutableArray<string> CopyReferenceInputs { get; }

        /// <summary>
        /// Contains files such as:
        /// <list type="bullet">
        ///   <item>Potential <c>.editorconfig</c> paths</item>
        ///   <item>Potential <c>global.json</c> paths</item>
        ///   <item>Potential <c>Directory.Build.props</c> paths</item>
        ///   <item>Potential <c>Directory.Build.targets</c> paths</item>
        ///   <item><c>project.assets.json</c></item>
        /// </list>
        /// <para>
        /// We need all <em>potential</em> paths for files which may start affecting build if they were to be added.
        /// Any files not found on disk have MinValue dates.
        /// </para>
        /// </summary>
        /// <remarks>
        /// The <see cref="DateTime"/> values here do not update dynamically.
        /// </remarks>
        public IImmutableDictionary<string, DateTime> AdditionalDependentFileTimes { get; }

        /// <summary>
        /// Gets the time at which the set of items with non-<see cref="DateTime.MinValue"/> times
        /// in <see cref="AdditionalDependentFileTimes"/> changed (files added or removed).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is not updated until after the first query occurs. Until that time it will
        /// equal <see cref="DateTime.MinValue"/> which represents the fact that we do not know when
        /// the set of items was last added or removed, so we cannot base any decisions on this data
        /// property.
        /// </para>
        /// </remarks>
        public DateTime LastAdditionalDependentFileTimesChangedAtUtc { get; }

        private UpToDateCheckImplicitConfiguredInput()
        {
            var emptyItemBySetName = ImmutableDictionary.Create<string, ImmutableArray<string>>(BuildUpToDateCheck.SetNameComparer);

            LastItemsChangedAtUtc = DateTime.MinValue;
            ItemTypes = ImmutableArray<string>.Empty;
            ItemsByItemType = ImmutableDictionary.Create<string, ImmutableArray<(string Path, string? Link, BuildUpToDateCheck.CopyType CopyType)>>(StringComparers.ItemTypes);
            SetNames = ImmutableArray<string>.Empty;
            UpToDateCheckInputItemsBySetName = emptyItemBySetName;
            UpToDateCheckOutputItemsBySetName = emptyItemBySetName;
            UpToDateCheckBuiltItemsBySetName = emptyItemBySetName;
            CopiedOutputFiles = ImmutableArray<(string DestinationRelative, string SourceRelative)>.Empty;
            ResolvedAnalyzerReferencePaths = ImmutableArray<string>.Empty;
            ResolvedCompilationReferencePaths = ImmutableArray<string>.Empty;
            CopyReferenceInputs = ImmutableArray<string>.Empty;
            AdditionalDependentFileTimes = ImmutableDictionary.Create<string, DateTime>(StringComparers.Paths);
            LastAdditionalDependentFileTimesChangedAtUtc = DateTime.MinValue;
        }

        private UpToDateCheckImplicitConfiguredInput(
            string? msBuildProjectFullPath,
            string? msBuildProjectDirectory,
            string? copyUpToDateMarkerItem,
            string? outputRelativeOrFullPath,
            string? newestImportInput,
            IComparable? lastVersionSeen,
            bool isDisabled,
            ImmutableArray<string> itemTypes,
            ImmutableDictionary<string, ImmutableArray<(string, string?, BuildUpToDateCheck.CopyType)>> itemsByItemType,
            ImmutableDictionary<string, ImmutableArray<string>> upToDateCheckInputItemsBySetName,
            ImmutableDictionary<string, ImmutableArray<string>> upToDateCheckOutputItemsBySetName,
            ImmutableDictionary<string, ImmutableArray<string>> upToDateCheckBuiltItemsBySetName,
            ImmutableArray<(string DestinationRelative, string SourceRelative)> copiedOutputFiles,
            ImmutableArray<string> resolvedAnalyzerReferencePaths,
            ImmutableArray<string> resolvedCompilationReferencePaths,
            ImmutableArray<string> copyReferenceInputs,
            IImmutableDictionary<string, DateTime> additionalDependentFileTimes,
            DateTime lastAdditionalDependentFileTimesChangedAtUtc,
            DateTime lastItemsChangedAtUtc,
            ImmutableArray<(bool IsAdd, string ItemType, string Path, string? Link, BuildUpToDateCheck.CopyType CopyType)> lastItemChanges)
        {
            MSBuildProjectFullPath = msBuildProjectFullPath;
            MSBuildProjectDirectory = msBuildProjectDirectory;
            CopyUpToDateMarkerItem = copyUpToDateMarkerItem;
            OutputRelativeOrFullPath = outputRelativeOrFullPath;
            NewestImportInput = newestImportInput;
            LastVersionSeen = lastVersionSeen;
            IsDisabled = isDisabled;
            ItemTypes = itemTypes;
            ItemsByItemType = itemsByItemType;
            UpToDateCheckInputItemsBySetName = upToDateCheckInputItemsBySetName;
            UpToDateCheckOutputItemsBySetName = upToDateCheckOutputItemsBySetName;
            UpToDateCheckBuiltItemsBySetName = upToDateCheckBuiltItemsBySetName;
            CopiedOutputFiles = copiedOutputFiles;
            ResolvedAnalyzerReferencePaths = resolvedAnalyzerReferencePaths;
            ResolvedCompilationReferencePaths = resolvedCompilationReferencePaths;
            CopyReferenceInputs = copyReferenceInputs;
            LastItemsChangedAtUtc = lastItemsChangedAtUtc;
            AdditionalDependentFileTimes = additionalDependentFileTimes;
            LastAdditionalDependentFileTimesChangedAtUtc = lastAdditionalDependentFileTimesChangedAtUtc;
            LastItemChanges = lastItemChanges;

            var setNames = new HashSet<string>(BuildUpToDateCheck.SetNameComparer);
            AddKeys(upToDateCheckInputItemsBySetName);
            AddKeys(upToDateCheckOutputItemsBySetName);
            AddKeys(upToDateCheckBuiltItemsBySetName);
            setNames.Remove(BuildUpToDateCheck.DefaultSetName);
            SetNames = setNames.OrderBy(n => n, BuildUpToDateCheck.SetNameComparer).ToImmutableArray();

            void AddKeys(ImmutableDictionary<string, ImmutableArray<string>> dictionary)
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
            IProjectSnapshot2 projectSnapshot,
            IProjectItemSchema projectItemSchema,
            IProjectCatalogSnapshot projectCatalogSnapshot,
            IComparable configuredProjectVersion)
        {
            bool isDisabled = jointRuleUpdate.CurrentState.IsPropertyTrue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, defaultValue: false);

            string? msBuildProjectFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, MSBuildProjectFullPath);
            string? msBuildProjectDirectory = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectDirectoryProperty, MSBuildProjectDirectory);
            string? msBuildProjectOutputPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputPathProperty, OutputRelativeOrFullPath);
            string? outputRelativeOrFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutDirProperty, msBuildProjectOutputPath);
            string msBuildAllProjects = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildAllProjectsProperty, "");

            // The first item in this semicolon-separated list of project files will always be the one
            // with the newest timestamp. As we are only interested in timestamps on these files, we can
            // save memory and time by only considering this first path (dotnet/project-system#4333).
            string? newestImportInput = new LazyStringSplit(msBuildAllProjects, ';').FirstOrDefault();

            ImmutableArray<string> resolvedAnalyzerReferencePaths;
            if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedAnalyzerReference.SchemaName, out IProjectChangeDescription change) && change.Difference.AnyChanges)
            {
                resolvedAnalyzerReferencePaths = change.After.Items.Select(item => item.Value[ResolvedAnalyzerReference.ResolvedPathProperty]).Distinct(StringComparers.Paths).ToImmutableArray();
            }
            else
            {
                resolvedAnalyzerReferencePaths = ResolvedAnalyzerReferencePaths;
            }

            ImmutableDictionary<string, ImmutableArray<string>> upToDateCheckInputItems;
            if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckInput.SchemaName, out change) && change.Difference.AnyChanges)
            {
                upToDateCheckInputItems = BuildItemsBySetName(change, UpToDateCheckInput.SetProperty);
            }
            else
            {
                upToDateCheckInputItems = UpToDateCheckInputItemsBySetName;
            }

            ImmutableDictionary<string, ImmutableArray<string>> upToDateCheckOutputItems;
            if (sourceItemsUpdate.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out change) && change.Difference.AnyChanges)
            {
                upToDateCheckOutputItems = BuildItemsBySetName(change, UpToDateCheckOutput.SetProperty);
            }
            else if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out change) && change.Difference.AnyChanges)
            {
                upToDateCheckOutputItems = BuildItemsBySetName(change, UpToDateCheckOutput.SetProperty);
            }
            else
            {
                upToDateCheckOutputItems = UpToDateCheckOutputItemsBySetName;
            }

            string? copyUpToDateMarkerItem;
            if (jointRuleUpdate.ProjectChanges.TryGetValue(CopyUpToDateMarker.SchemaName, out change) && change.Difference.AnyChanges)
            {
                copyUpToDateMarkerItem = change.After.Items.Count == 1 ? change.After.Items.Single().Key : null;
            }
            else
            {
                copyUpToDateMarkerItem = CopyUpToDateMarkerItem;
            }

            ImmutableArray<string> resolvedCompilationReferencePaths;
            ImmutableArray<string> copyReferenceInputs;
            if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedCompilationReference.SchemaName, out change) && change.Difference.AnyChanges)
            {
                HashSet<string> resolvedCompilationReferencePathsBuilder = new(StringComparers.Paths);
                HashSet<string> copyReferenceInputsBuilder = new(StringComparers.Paths);

                foreach (IImmutableDictionary<string, string> itemMetadata in change.After.Items.Values)
                {
                    string originalPath = itemMetadata[ResolvedCompilationReference.OriginalPathProperty];
                    string resolvedPath = itemMetadata[ResolvedCompilationReference.ResolvedPathProperty];
                    string copyReferenceInput = itemMetadata[CopyUpToDateMarker.SchemaName];

                    resolvedCompilationReferencePathsBuilder.Add(resolvedPath);

                    if (!string.IsNullOrWhiteSpace(originalPath))
                    {
                        copyReferenceInputsBuilder.Add(originalPath);
                    }

                    if (!string.IsNullOrWhiteSpace(copyReferenceInput))
                    {
                        copyReferenceInputsBuilder.Add(copyReferenceInput);
                    }
                }

                resolvedCompilationReferencePaths = resolvedCompilationReferencePathsBuilder.ToImmutableArray();
                copyReferenceInputs = copyReferenceInputsBuilder.ToImmutableArray();
            }
            else
            {
                resolvedCompilationReferencePaths = ResolvedCompilationReferencePaths;
                copyReferenceInputs = CopyReferenceInputs;
            }

            ImmutableDictionary<string, ImmutableArray<string>> upToDateCheckBuiltItems;
            ImmutableArray<(string DestinationRelative, string SourceRelative)> copiedOutputFiles;
            if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckBuilt.SchemaName, out change) && change.Difference.AnyChanges)
            {
                var itemsBySet = new Dictionary<string, HashSet<string>>(BuildUpToDateCheck.SetNameComparer);
                var copiedOutputFilesBuilder = new Dictionary<string, string>(StringComparers.Paths);

                foreach ((string destination, IImmutableDictionary<string, string> metadata) in change.After.Items)
                {
                    if (metadata.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) && !string.IsNullOrEmpty(source))
                    {
                        // This file is copied, not built
                        // Remember the `Original` source for later
                        copiedOutputFilesBuilder[destination] = source;
                    }
                    else
                    {
                        // This file is built, not copied
                        string setName = metadata.GetStringProperty(UpToDateCheckBuilt.SetProperty) ?? BuildUpToDateCheck.DefaultSetName;

                        if (!itemsBySet.TryGetValue(setName, out HashSet<string> builder))
                        {
                            itemsBySet[setName] = builder = new HashSet<string>(BuildUpToDateCheck.SetNameComparer);
                        }

                        builder.Add(destination);
                    }
                }

                upToDateCheckBuiltItems = itemsBySet.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray(), BuildUpToDateCheck.SetNameComparer);
                copiedOutputFiles = copiedOutputFilesBuilder.Select(kvp => (kvp.Key, kvp.Value)).ToImmutableArray();
            }
            else
            {
                upToDateCheckBuiltItems = UpToDateCheckBuiltItemsBySetName;
                copiedOutputFiles = CopiedOutputFiles;
            }

            var itemTypes = projectItemSchema
                .GetKnownItemTypes()
                .Where(itemType => projectItemSchema.GetItemType(itemType).UpToDateCheckInput)
                .ToHashSet(StringComparers.ItemTypes);

            var itemTypeDiff = new SetDiff<string>(ItemTypes, itemTypes, StringComparers.ItemTypes);

            var itemsByItemTypeBuilder = ItemsByItemType.ToBuilder();

            bool itemTypesChanged = false;

            List<(bool IsAdd, string ItemType, string Path, string? Link, BuildUpToDateCheck.CopyType)> changes = new();

            // If an item type was removed, remove all items of that type
            foreach (string removedItemType in itemTypeDiff.Removed)
            {
                itemTypesChanged = true;

                if (itemsByItemTypeBuilder.TryGetValue(removedItemType, out var removedItems))
                {
                    foreach ((string path, string? link, BuildUpToDateCheck.CopyType copyType) in removedItems)
                    {
                        changes.Add((false, removedItemType, path, link, copyType));
                    }

                    itemsByItemTypeBuilder.Remove(removedItemType);
                }
            }

            itemTypesChanged |= itemTypeDiff.Added.GetEnumerator().MoveNext();

            bool itemsChanged = false;

            foreach ((string schemaName, IProjectChangeDescription projectChange) in sourceItemsUpdate.ProjectChanges)
            {
                // ProjectChanges is keyed by the rule name which is usually the same as the item type, but not always (eg, in auto-generated rules)
                string? itemType = null;
                if (projectCatalogSnapshot.NamedCatalogs.TryGetValue(PropertyPageContexts.File, out IPropertyPagesCatalog fileCatalog))
                    itemType = fileCatalog.GetSchema(schemaName)?.DataSource.ItemType;

                if (itemType == null)
                    continue;
                if (!itemTypes.Contains(itemType))
                    continue;
                if (!itemTypesChanged && !projectChange.Difference.AnyChanges)
                    continue;
                if (projectChange.After.Items.Count == 0)
                    continue;

                ImmutableArray<(string Path, string? Link, BuildUpToDateCheck.CopyType)> before = ImmutableArray<(string Path, string? Link, BuildUpToDateCheck.CopyType)>.Empty;
                if (itemsByItemTypeBuilder.TryGetValue(itemType, out ImmutableArray<(string Path, string? Link, BuildUpToDateCheck.CopyType CopyType)> beforeItems))
                    before = beforeItems;

                var after = projectChange.After.Items.Select(item => (Path: item.Key, GetLink(item.Value), GetCopyType(item.Value))).ToHashSet(BuildUpToDateCheck.ItemComparer.Instance);

                var diff = new SetDiff<(string, string?, BuildUpToDateCheck.CopyType)>(before, after, BuildUpToDateCheck.ItemComparer.Instance);

                foreach ((string path, string? link, BuildUpToDateCheck.CopyType copyType) in diff.Added)
                {
                    changes.Add((true, itemType, path, link, copyType));
                }

                foreach ((string path, string? link, BuildUpToDateCheck.CopyType copyType) in diff.Removed)
                {
                    changes.Add((false, itemType, path, link, copyType));
                }

                itemsByItemTypeBuilder[itemType] = after.ToImmutableArray();
                itemsChanged = true;
            }

            // NOTE when we previously had zero item types, we can surmise that the project has just been loaded. In such
            // a case it is not correct to assume that the items changed, and so we do not update the timestamp.
            // See https://github.com/dotnet/project-system/issues/5386
            DateTime lastItemsChangedAtUtc = itemsChanged && !ItemTypes.IsEmpty ? DateTime.UtcNow : LastItemsChangedAtUtc;

            DateTime lastAdditionalDependentFileTimesChangedAtUtc = GetLastTimeAdditionalDependentFilesAddedOrRemoved();

            return new(
                msBuildProjectFullPath,
                msBuildProjectDirectory,
                copyUpToDateMarkerItem,
                outputRelativeOrFullPath,
                newestImportInput,
                lastVersionSeen: configuredProjectVersion,
                isDisabled: isDisabled,
                itemTypes: itemTypes.ToImmutableArray(),
                itemsByItemType: itemsByItemTypeBuilder.ToImmutable(),
                upToDateCheckInputItemsBySetName: upToDateCheckInputItems,
                upToDateCheckOutputItemsBySetName: upToDateCheckOutputItems,
                upToDateCheckBuiltItemsBySetName: upToDateCheckBuiltItems,
                copiedOutputFiles: copiedOutputFiles,
                resolvedAnalyzerReferencePaths: resolvedAnalyzerReferencePaths,
                resolvedCompilationReferencePaths: resolvedCompilationReferencePaths,
                copyReferenceInputs: copyReferenceInputs,
                additionalDependentFileTimes: projectSnapshot.AdditionalDependentFileTimes,
                lastAdditionalDependentFileTimesChangedAtUtc: lastAdditionalDependentFileTimesChangedAtUtc,
                lastItemsChangedAtUtc: lastItemsChangedAtUtc,
                changes.ToImmutableArray());

            DateTime GetLastTimeAdditionalDependentFilesAddedOrRemoved()
            {
                var lastExistingAdditionalDependentFiles = AdditionalDependentFileTimes.Where(pair => pair.Value != DateTime.MinValue)
                    .Select(pair => pair.Key)
                    .ToImmutableHashSet();

                IEnumerable<string> currentExistingAdditionalDependentFiles = projectSnapshot.AdditionalDependentFileTimes
                    .Where(pair => pair.Value != DateTime.MinValue)
                    .Select(pair => pair.Key);

                bool additionalDependentFilesChanged = !lastExistingAdditionalDependentFiles.SetEquals(currentExistingAdditionalDependentFiles);

                return additionalDependentFilesChanged && !lastExistingAdditionalDependentFiles.IsEmpty ? DateTime.UtcNow : LastAdditionalDependentFileTimesChangedAtUtc;
            }

            static BuildUpToDateCheck.CopyType GetCopyType(IImmutableDictionary<string, string> itemMetadata)
            {
                if (itemMetadata.TryGetValue(Compile.CopyToOutputDirectoryProperty, out string value))
                {
                    if (string.Equals(value, Compile.CopyToOutputDirectoryValues.Always, StringComparisons.PropertyLiteralValues))
                    {
                        return BuildUpToDateCheck.CopyType.CopyAlways;
                    }

                    if (string.Equals(value, Compile.CopyToOutputDirectoryValues.PreserveNewest, StringComparisons.PropertyLiteralValues))
                    {
                        return BuildUpToDateCheck.CopyType.CopyIfNewer;
                    }
                }

                return BuildUpToDateCheck.CopyType.CopyNever;
            }

            static string? GetLink(IImmutableDictionary<string, string> itemMetadata)
            {
                return itemMetadata.TryGetValue(BuildUpToDateCheck.Link, out string link) ? link : null;
            }

            static ImmutableDictionary<string, ImmutableArray<string>> BuildItemsBySetName(IProjectChangeDescription projectChangeDescription, string setPropertyName)
            {
                var itemsBySet = new Dictionary<string, HashSet<string>>(BuildUpToDateCheck.SetNameComparer);

                foreach ((string item, IImmutableDictionary<string, string> metadata) in projectChangeDescription.After.Items)
                {
                    string? setNames = metadata.GetStringProperty(setPropertyName);

                    if (setNames != null)
                    {
                        foreach (string setName in new LazyStringSplit(setNames, ';'))
                        {
                            AddItem(setName, item);
                        }
                    }
                    else
                    {
                        AddItem(BuildUpToDateCheck.DefaultSetName, item);
                    }
                }

                return itemsBySet.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray(), BuildUpToDateCheck.SetNameComparer);

                void AddItem(string setName, string item)
                {
                    if (!itemsBySet.TryGetValue(setName, out HashSet<string> builder))
                    {
                        itemsBySet[setName] = builder = new HashSet<string>(StringComparers.Paths);
                    }

                    builder.Add(item);
                }
            }
        }

        /// <summary>
        /// For unit tests only.
        /// </summary>
        internal UpToDateCheckImplicitConfiguredInput WithLastItemsChangedAtUtc(DateTime lastItemsChangedAtUtc)
        {
            return new(
                MSBuildProjectFullPath,
                MSBuildProjectDirectory,
                CopyUpToDateMarkerItem,
                OutputRelativeOrFullPath,
                NewestImportInput,
                LastVersionSeen,
                IsDisabled,
                ItemTypes,
                ItemsByItemType,
                UpToDateCheckInputItemsBySetName,
                UpToDateCheckOutputItemsBySetName,
                UpToDateCheckBuiltItemsBySetName,
                CopiedOutputFiles,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                AdditionalDependentFileTimes,
                LastAdditionalDependentFileTimesChangedAtUtc,
                lastItemsChangedAtUtc,
                LastItemChanges);
        }

        /// <summary>
        /// For unit tests only.
        /// </summary>
        internal UpToDateCheckImplicitConfiguredInput WithLastAdditionalDependentFilesChangedAtUtc(DateTime lastAdditionalDependentFileTimesChangedAtUtc)
        {
            return new(
                MSBuildProjectFullPath,
                MSBuildProjectDirectory,
                CopyUpToDateMarkerItem,
                OutputRelativeOrFullPath,
                NewestImportInput,
                LastVersionSeen,
                IsDisabled,
                ItemTypes,
                ItemsByItemType,
                UpToDateCheckInputItemsBySetName,
                UpToDateCheckOutputItemsBySetName,
                UpToDateCheckBuiltItemsBySetName,
                CopiedOutputFiles,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                AdditionalDependentFileTimes,
                lastAdditionalDependentFileTimesChangedAtUtc,
                LastItemsChangedAtUtc,
                LastItemChanges);
        }
    }
}
