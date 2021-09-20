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

        public ImmutableArray<(bool IsAdd, string ItemType, string Path, string? TargetPath, BuildUpToDateCheck.CopyType CopyType)> LastItemChanges { get; }

        public int? ItemHash { get; }

        public bool WasStateRestored { get; }

        public ImmutableArray<string> ItemTypes { get; }

        public ImmutableDictionary<string, ImmutableArray<(string Path, string? TargetPath, BuildUpToDateCheck.CopyType CopyType)>> ItemsByItemType { get; }

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
            var emptyItemBySetName = ImmutableDictionary.Create<string, ImmutableDictionary<string, ImmutableArray<string>>>(BuildUpToDateCheck.SetNameComparer);

            LastItemsChangedAtUtc = DateTime.MinValue;
            ItemTypes = ImmutableArray<string>.Empty;
            ItemsByItemType = ImmutableDictionary.Create<string, ImmutableArray<(string Path, string? TargetPath, BuildUpToDateCheck.CopyType CopyType)>>(StringComparers.ItemTypes);
            SetNames = ImmutableArray<string>.Empty;
            UpToDateCheckInputItemsByKindBySetName = emptyItemBySetName;
            UpToDateCheckOutputItemsByKindBySetName = emptyItemBySetName;
            UpToDateCheckBuiltItemsByKindBySetName = emptyItemBySetName;
            CopiedOutputFiles = ImmutableArray<(string DestinationRelative, string SourceRelative)>.Empty;
            ResolvedAnalyzerReferencePaths = ImmutableArray<string>.Empty;
            ResolvedCompilationReferencePaths = ImmutableArray<string>.Empty;
            CopyReferenceInputs = ImmutableArray<string>.Empty;
            AdditionalDependentFileTimes = ImmutableDictionary.Create<string, DateTime>(StringComparers.Paths);
            LastAdditionalDependentFileTimesChangedAtUtc = DateTime.MinValue;
            WasStateRestored = false;
        }

        private UpToDateCheckImplicitConfiguredInput(
            string? msBuildProjectFullPath,
            string? msBuildProjectDirectory,
            string? copyUpToDateMarkerItem,
            string? outputRelativeOrFullPath,
            string? newestImportInput,
            bool isDisabled,
            ImmutableArray<string> itemTypes,
            ImmutableDictionary<string, ImmutableArray<(string, string?, BuildUpToDateCheck.CopyType)>> itemsByItemType,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckInputItemsByKindBySetName,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckOutputItemsByKindBySetName,
            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckBuiltItemsByKindBySetName,
            ImmutableArray<(string DestinationRelative, string SourceRelative)> copiedOutputFiles,
            ImmutableArray<string> resolvedAnalyzerReferencePaths,
            ImmutableArray<string> resolvedCompilationReferencePaths,
            ImmutableArray<string> copyReferenceInputs,
            IImmutableDictionary<string, DateTime> additionalDependentFileTimes,
            DateTime lastAdditionalDependentFileTimesChangedAtUtc,
            DateTime lastItemsChangedAtUtc,
            ImmutableArray<(bool IsAdd, string ItemType, string Path, string? TargetPath, BuildUpToDateCheck.CopyType CopyType)> lastItemChanges,
            int? itemHash,
            bool wasStateRestored)
        {
            MSBuildProjectFullPath = msBuildProjectFullPath;
            MSBuildProjectDirectory = msBuildProjectDirectory;
            CopyUpToDateMarkerItem = copyUpToDateMarkerItem;
            OutputRelativeOrFullPath = outputRelativeOrFullPath;
            NewestImportInput = newestImportInput;
            IsDisabled = isDisabled;
            ItemTypes = itemTypes;
            ItemsByItemType = itemsByItemType;
            UpToDateCheckInputItemsByKindBySetName = upToDateCheckInputItemsByKindBySetName;
            UpToDateCheckOutputItemsByKindBySetName = upToDateCheckOutputItemsByKindBySetName;
            UpToDateCheckBuiltItemsByKindBySetName = upToDateCheckBuiltItemsByKindBySetName;
            CopiedOutputFiles = copiedOutputFiles;
            ResolvedAnalyzerReferencePaths = resolvedAnalyzerReferencePaths;
            ResolvedCompilationReferencePaths = resolvedCompilationReferencePaths;
            CopyReferenceInputs = copyReferenceInputs;
            LastItemsChangedAtUtc = lastItemsChangedAtUtc;
            AdditionalDependentFileTimes = additionalDependentFileTimes;
            LastAdditionalDependentFileTimesChangedAtUtc = lastAdditionalDependentFileTimesChangedAtUtc;
            LastItemChanges = lastItemChanges;
            ItemHash = itemHash;
            WasStateRestored = wasStateRestored;

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
            IProjectSnapshot2 projectSnapshot,
            IProjectItemSchema projectItemSchema,
            IProjectCatalogSnapshot projectCatalogSnapshot)
        {
            bool isDisabled = jointRuleUpdate.CurrentState.IsPropertyTrue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, defaultValue: false);

            string? msBuildProjectFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, MSBuildProjectFullPath);
            string? msBuildProjectDirectory = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectDirectoryProperty, MSBuildProjectDirectory);
            string? msBuildProjectOutputPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputPathProperty, OutputRelativeOrFullPath);
            string? outputRelativeOrFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutDirProperty, msBuildProjectOutputPath);
            string nuGetPackageFolders = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.NuGetPackageFoldersProperty, "");
            string msBuildAllProjects = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildAllProjectsProperty, "");

            // We identify non-modifiable inputs (i.e. anything in Program Files, the VS install dir, or NuGet cache folders)
            // and exclude them from the set of inputs we scan when an up-to-date query is made.
            //
            // For a .NET 5 xUnit project, this cuts the number of file timestamps checked from 187 to 17. Most of those are
            // reference assemblies for the framework, which clearly aren't expected to change over time.
            var projectFileClassifier = new ProjectFileClassifier
            {
                NuGetPackageFolders = nuGetPackageFolders
            };

            // The first item in this semicolon-separated list of project files will always be the one
            // with the newest timestamp. As we are only interested in timestamps on these files, we can
            // save memory and time by only considering this first path (dotnet/project-system#4333).
            string? newestImportInput = new LazyStringSplit(msBuildAllProjects, ';').FirstOrDefault();

            ImmutableArray<string> resolvedAnalyzerReferencePaths;
            if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedAnalyzerReference.SchemaName, out IProjectChangeDescription change) && change.Difference.AnyChanges)
            {
                resolvedAnalyzerReferencePaths = change.After.Items
                    .Select(item => item.Value[ResolvedAnalyzerReference.ResolvedPathProperty])
                    .Where(path => !projectFileClassifier.IsNonModifiable(path))
                    .Distinct(StringComparers.Paths)
                    .ToImmutableArray();
            }
            else
            {
                resolvedAnalyzerReferencePaths = ResolvedAnalyzerReferencePaths;
            }

            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckInputItemsByKindBySetName;
            if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckInput.SchemaName, out change) && change.Difference.AnyChanges)
            {
                upToDateCheckInputItemsByKindBySetName = BuildItemsByKindBySetName(change, UpToDateCheckInput.KindProperty, UpToDateCheckInput.SetProperty);
            }
            else
            {
                upToDateCheckInputItemsByKindBySetName = UpToDateCheckInputItemsByKindBySetName;
            }

            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckOutputItemsByKindBySetName;
            if (sourceItemsUpdate.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out change) && change.Difference.AnyChanges)
            {
                upToDateCheckOutputItemsByKindBySetName = BuildItemsByKindBySetName(change, UpToDateCheckOutput.KindProperty, UpToDateCheckOutput.SetProperty);
            }
            else if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out change) && change.Difference.AnyChanges)
            {
                upToDateCheckOutputItemsByKindBySetName = BuildItemsByKindBySetName(change, UpToDateCheckOutput.KindProperty, UpToDateCheckOutput.SetProperty);
            }
            else
            {
                upToDateCheckOutputItemsByKindBySetName = UpToDateCheckOutputItemsByKindBySetName;
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

                resolvedCompilationReferencePaths = resolvedCompilationReferencePathsBuilder.ToImmutableArray();
                copyReferenceInputs = copyReferenceInputsBuilder.ToImmutableArray();
            }
            else
            {
                resolvedCompilationReferencePaths = ResolvedCompilationReferencePaths;
                copyReferenceInputs = CopyReferenceInputs;
            }

            ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> upToDateCheckBuiltItemsByKindBySetName;
            ImmutableArray<(string DestinationRelative, string SourceRelative)> copiedOutputFiles;
            if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckBuilt.SchemaName, out change) && change.Difference.AnyChanges)
            {
                var itemsByKindBySet = new Dictionary<string, Dictionary<string, HashSet<string>>>(BuildUpToDateCheck.SetNameComparer);
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
                        string kindName = metadata.GetStringProperty(UpToDateCheckBuilt.KindProperty) ?? BuildUpToDateCheck.DefaultKindName;

                        if (!itemsByKindBySet.TryGetValue(setName, out Dictionary<string, HashSet<string>> itemsByKind))
                        {
                            itemsByKindBySet[setName] = itemsByKind = new Dictionary<string, HashSet<string>>(BuildUpToDateCheck.KindNameComparer);
                        }

                        if (!itemsByKind.TryGetValue(kindName, out HashSet<string> items))
                        {
                            itemsByKind[kindName] = items = new HashSet<string>(StringComparers.ItemNames);
                        }

                        items.Add(destination);
                    }
                }

                upToDateCheckBuiltItemsByKindBySetName = itemsByKindBySet.ToImmutableDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToImmutableDictionary(
                        pair => pair.Key,
                        pair => pair.Value.ToImmutableArray()),
                    BuildUpToDateCheck.SetNameComparer);
                copiedOutputFiles = copiedOutputFilesBuilder.Select(kvp => (kvp.Key, kvp.Value)).ToImmutableArray();
            }
            else
            {
                upToDateCheckBuiltItemsByKindBySetName = UpToDateCheckBuiltItemsByKindBySetName;
                copiedOutputFiles = CopiedOutputFiles;
            }

            var itemTypes = projectItemSchema
                .GetKnownItemTypes()
                .Where(itemType => projectItemSchema.GetItemType(itemType).UpToDateCheckInput)
                .ToHashSet(StringComparers.ItemTypes);

            var itemTypeDiff = new SetDiff<string>(ItemTypes, itemTypes, StringComparers.ItemTypes);

            var itemsByItemTypeBuilder = ItemsByItemType.ToBuilder();

            bool itemTypesChanged = false;

            List<(bool IsAdd, string ItemType, string Path, string? TargetPath, BuildUpToDateCheck.CopyType)> changes = new();

            // If an item type was removed, remove all items of that type
            foreach (string removedItemType in itemTypeDiff.Removed)
            {
                itemTypesChanged = true;

                if (itemsByItemTypeBuilder.TryGetValue(removedItemType, out var removedItems))
                {
                    foreach ((string path, string? targetPath, BuildUpToDateCheck.CopyType copyType) in removedItems)
                    {
                        changes.Add((false, removedItemType, path, targetPath, copyType));
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

                ImmutableArray<(string Path, string? TargetPath, BuildUpToDateCheck.CopyType)> before = ImmutableArray<(string Path, string? TargetPath, BuildUpToDateCheck.CopyType)>.Empty;
                if (itemsByItemTypeBuilder.TryGetValue(itemType, out ImmutableArray<(string Path, string? TargetPath, BuildUpToDateCheck.CopyType CopyType)> beforeItems))
                    before = beforeItems;

                var after = projectChange.After.Items
                    .Select(item => (Path: item.Key, TargetPath: GetTargetPath(item.Value), CopyType: GetCopyType(item.Value)))
                    .ToHashSet(BuildUpToDateCheck.ItemComparer.Instance);

                var diff = new SetDiff<(string, string?, BuildUpToDateCheck.CopyType)>(before, after, BuildUpToDateCheck.ItemComparer.Instance);

                foreach ((string path, string? targetPath, BuildUpToDateCheck.CopyType copyType) in diff.Added)
                {
                    changes.Add((true, itemType, path, targetPath, copyType));
                }

                foreach ((string path, string? targetPath, BuildUpToDateCheck.CopyType copyType) in diff.Removed)
                {
                    changes.Add((false, itemType, path, targetPath, copyType));
                }

                itemsByItemTypeBuilder[itemType] = after.ToImmutableArray();
                itemsChanged = true;
            }

            ImmutableDictionary<string, ImmutableArray<(string Path, string? TargetPath, BuildUpToDateCheck.CopyType CopyType)>> itemsByItemType = itemsByItemTypeBuilder.ToImmutable();

            int itemHash = BuildUpToDateCheck.ComputeItemHash(itemsByItemType);

            DateTime lastItemsChangedAtUtc = LastItemsChangedAtUtc;

            if (itemHash != ItemHash && ItemHash != null)
            {
                // The set of items has changed.
                // For the case that the project loaded and no hash was available, do not touch lastItemsChangedAtUtc.
                // Doing so when the project is actually up-to-date would cause builds to be scheduled until something
                // actually changes.
                lastItemsChangedAtUtc = DateTime.UtcNow;
            }
            else if (itemsChanged && !ItemTypes.IsEmpty)
            {
                // When we previously had zero item types, we can surmise that the project has just been loaded. In such
                // a case it is not correct to assume that the items changed, and so we do not update the timestamp.
                // If we did, and the project was up-to-date when loaded, it would remain out-of-date until something changed,
                // causing redundant builds until that time. See https://github.com/dotnet/project-system/issues/5386.
                lastItemsChangedAtUtc = DateTime.UtcNow;
            }

            DateTime lastAdditionalDependentFileTimesChangedAtUtc = GetLastTimeAdditionalDependentFilesAddedOrRemoved();

            return new(
                msBuildProjectFullPath,
                msBuildProjectDirectory,
                copyUpToDateMarkerItem,
                outputRelativeOrFullPath,
                newestImportInput,
                isDisabled: isDisabled,
                itemTypes: itemTypes.ToImmutableArray(),
                itemsByItemType: itemsByItemType,
                upToDateCheckInputItemsByKindBySetName: upToDateCheckInputItemsByKindBySetName,
                upToDateCheckOutputItemsByKindBySetName: upToDateCheckOutputItemsByKindBySetName,
                upToDateCheckBuiltItemsByKindBySetName: upToDateCheckBuiltItemsByKindBySetName,
                copiedOutputFiles: copiedOutputFiles,
                resolvedAnalyzerReferencePaths: resolvedAnalyzerReferencePaths,
                resolvedCompilationReferencePaths: resolvedCompilationReferencePaths,
                copyReferenceInputs: copyReferenceInputs,
                additionalDependentFileTimes: projectSnapshot.AdditionalDependentFileTimes,
                lastAdditionalDependentFileTimesChangedAtUtc: lastAdditionalDependentFileTimesChangedAtUtc,
                lastItemsChangedAtUtc: lastItemsChangedAtUtc,
                changes.ToImmutableArray(),
                itemHash,
                WasStateRestored);

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

            static string? GetTargetPath(IImmutableDictionary<string, string> itemMetadata)
            {
                // "Link" is an optional path and file name under which the item should be copied.
                // It allows a source file to be moved to a different relative path, or to be renamed.
                //
                // From the perspective of the FUTD check, it is only relevant on CopyToOutputDirectory items.
                //
                // Two properties can provide this feature: "Link" and "TargetPath".
                //
                // If specified, "TargetPath" metadata controls the path of the target file, relative to the output
                // folder.
                //
                // "Link" controls the location under the project in Solution Explorer where the item appears.
                // If "TargetPath" is not specified, then "Link" can also serve the role of "TargetPath".
                //
                // If both are specified, we only use "TargetPath". The use case for specifying both is wanting
                // to control the location of the item in Solution Explorer, as well as in the output directory.
                // The former is not relevant to us here.

                if (itemMetadata.TryGetValue(None.TargetPathProperty, out string? targetPath) && !string.IsNullOrWhiteSpace(targetPath))
                {
                    return targetPath;
                }

                if (itemMetadata.TryGetValue(None.LinkProperty, out string link) && !string.IsNullOrWhiteSpace(link))
                {
                    return link;
                }

                return null;
            }

            static ImmutableDictionary<string, ImmutableDictionary<string, ImmutableArray<string>>> BuildItemsByKindBySetName(IProjectChangeDescription projectChangeDescription, string kindPropertyName, string setPropertyName)
            {
                var itemsByKindBySet = new Dictionary<string, Dictionary<string, HashSet<string>>>(BuildUpToDateCheck.SetNameComparer);

                foreach ((string item, IImmutableDictionary<string, string> metadata) in projectChangeDescription.After.Items)
                {
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
                IsDisabled,
                ItemTypes,
                ItemsByItemType,
                UpToDateCheckInputItemsByKindBySetName,
                UpToDateCheckOutputItemsByKindBySetName,
                UpToDateCheckBuiltItemsByKindBySetName,
                CopiedOutputFiles,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                AdditionalDependentFileTimes,
                LastAdditionalDependentFileTimesChangedAtUtc,
                lastItemsChangedAtUtc,
                LastItemChanges,
                ItemHash,
                WasStateRestored);
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
                IsDisabled,
                ItemTypes,
                ItemsByItemType,
                UpToDateCheckInputItemsByKindBySetName,
                UpToDateCheckOutputItemsByKindBySetName,
                UpToDateCheckBuiltItemsByKindBySetName,
                CopiedOutputFiles,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                AdditionalDependentFileTimes,
                lastAdditionalDependentFileTimesChangedAtUtc,
                LastItemsChangedAtUtc,
                LastItemChanges,
                ItemHash,
                WasStateRestored);
        }

        public UpToDateCheckImplicitConfiguredInput WithRestoredState(int itemHash, DateTime lastInputsChangedAtUtc)
        {
            return new(
                MSBuildProjectFullPath,
                MSBuildProjectDirectory,
                CopyUpToDateMarkerItem,
                OutputRelativeOrFullPath,
                NewestImportInput,
                IsDisabled,
                ItemTypes,
                ItemsByItemType,
                UpToDateCheckInputItemsByKindBySetName,
                UpToDateCheckOutputItemsByKindBySetName,
                UpToDateCheckBuiltItemsByKindBySetName,
                CopiedOutputFiles,
                ResolvedAnalyzerReferencePaths,
                ResolvedCompilationReferencePaths,
                CopyReferenceInputs,
                AdditionalDependentFileTimes,
                LastAdditionalDependentFileTimesChangedAtUtc,
                lastInputsChangedAtUtc,
                LastItemChanges,
                itemHash,
                wasStateRestored: true);
        }
    }
}
