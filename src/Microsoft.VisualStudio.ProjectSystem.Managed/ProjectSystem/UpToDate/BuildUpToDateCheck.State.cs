// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.UpToDate
{
    internal sealed partial class BuildUpToDateCheck
    {
        private sealed class State
        {
            public static State Empty { get; } = new State();

            public string? MSBuildProjectFullPath { get; }
            public string? MSBuildProjectDirectory { get; }
            public string? MarkerFile { get; }
            public string? OutputRelativeOrFullPath { get; }
            public string? NewestImportInput { get; }
            public IComparable? LastVersionSeen { get; }
            public bool IsDisabled { get; }

            public ImmutableHashSet<string> ItemTypes { get; }
            public ImmutableDictionary<string, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)>> Items { get; }
            public ImmutableHashSet<string> CustomInputs { get; }
            public ImmutableHashSet<string> CustomOutputs { get; }
            public ImmutableHashSet<string> BuiltOutputs { get; }

            /// <summary>Key is destination, value is source.</summary>
            public ImmutableDictionary<string, string> CopiedOutputFiles { get; }

            public ImmutableHashSet<string> AnalyzerReferences { get; }
            public ImmutableHashSet<string> CompilationReferences { get; }
            public ImmutableHashSet<string> CopyReferenceInputs { get; }

            private State()
            {
                ImmutableHashSet<string> emptyPathSet = ImmutableHashSet.Create(StringComparers.Paths);

                ItemTypes = ImmutableHashSet.Create(StringComparers.ItemTypes);
                Items = ImmutableDictionary.Create<string, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)>>(StringComparers.ItemTypes);
                CustomInputs = emptyPathSet;
                CustomOutputs = emptyPathSet;
                BuiltOutputs = emptyPathSet;
                CopiedOutputFiles = ImmutableDictionary.Create<string, string>(StringComparers.Paths);
                AnalyzerReferences = emptyPathSet;
                CompilationReferences = emptyPathSet;
                CopyReferenceInputs = emptyPathSet;
            }

            private State(
                string msBuildProjectFullPath,
                string msBuildProjectDirectory,
                string? markerFile,
                string outputRelativeOrFullPath,
                string? newestImportInput,
                IComparable lastVersionSeen,
                bool isDisabled,
                ImmutableHashSet<string> itemTypes,
                ImmutableDictionary<string, ImmutableHashSet<(string, string?, CopyToOutputDirectoryType)>> items,
                ImmutableHashSet<string> customInputs,
                ImmutableHashSet<string> customOutputs,
                ImmutableHashSet<string> builtOutputs,
                ImmutableDictionary<string, string> copiedOutputFiles,
                ImmutableHashSet<string> analyzerReferences,
                ImmutableHashSet<string> compilationReferences,
                ImmutableHashSet<string> copyReferenceInputs)
            {
                MSBuildProjectFullPath = msBuildProjectFullPath;
                MSBuildProjectDirectory = msBuildProjectDirectory;
                MarkerFile = markerFile;
                OutputRelativeOrFullPath = outputRelativeOrFullPath;
                NewestImportInput = newestImportInput;
                LastVersionSeen = lastVersionSeen;
                IsDisabled = isDisabled;
                ItemTypes = itemTypes;
                Items = items;
                CustomInputs = customInputs;
                CustomOutputs = customOutputs;
                BuiltOutputs = builtOutputs;
                CopiedOutputFiles = copiedOutputFiles;
                AnalyzerReferences = analyzerReferences;
                CompilationReferences = compilationReferences;
                CopyReferenceInputs = copyReferenceInputs;
            }

            public State Update(
                IProjectSubscriptionUpdate jointRuleUpdate,
                IProjectSubscriptionUpdate sourceItemsUpdate,
                IProjectItemSchema projectItemSchema,
                IComparable configuredProjectVersion,
                out bool itemsChanged)
            {
                bool isDisabled = jointRuleUpdate.CurrentState.IsPropertyTrue(ConfigurationGeneral.SchemaName, ConfigurationGeneral.DisableFastUpToDateCheckProperty, defaultValue: false);

                string msBuildProjectFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectFullPathProperty, MSBuildProjectFullPath);
                string msBuildProjectDirectory = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildProjectDirectoryProperty, MSBuildProjectDirectory);
                string outputRelativeOrFullPath = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.OutputPathProperty, OutputRelativeOrFullPath);
                string msBuildAllProjects = jointRuleUpdate.CurrentState.GetPropertyOrDefault(ConfigurationGeneral.SchemaName, ConfigurationGeneral.MSBuildAllProjectsProperty, "");

                // The first item in this semicolon-separated list of project files will always be the one
                // with the newest timestamp. As we are only interested in timestamps on these files, we can
                // save memory and time by only considering this first path (dotnet/project-system#4333).
                string? newestImportInput = new LazyStringSplit(msBuildAllProjects, ';').FirstOrDefault();

                ImmutableHashSet<string> analyzerReferences;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedAnalyzerReference.SchemaName, out IProjectChangeDescription change) && change.Difference.AnyChanges)
                {
                    analyzerReferences = change.After.Items.Select(item => item.Value[ResolvedAnalyzerReference.ResolvedPathProperty]).ToImmutableHashSet(StringComparers.Paths);
                }
                else
                {
                    analyzerReferences = AnalyzerReferences;
                }

                ImmutableHashSet<string> customInputs;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckInput.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    customInputs = change.After.Items.Keys.ToImmutableHashSet(StringComparers.Paths);
                }
                else
                {
                    customInputs = CustomInputs;
                }

                ImmutableHashSet<string> customOutputs;
                if (sourceItemsUpdate.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    customOutputs = change.After.Items.Keys.ToImmutableHashSet(StringComparers.Paths);
                }
                else if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckOutput.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    customOutputs = change.After.Items.Keys.ToImmutableHashSet(StringComparers.Paths);
                }
                else
                {
                    customOutputs = CustomOutputs;
                }

                string? markerFile;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(CopyUpToDateMarker.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    markerFile = change.After.Items.Count == 1 ? change.After.Items.Single().Key : null;
                }
                else
                {
                    markerFile = MarkerFile;
                }

                ImmutableHashSet<string> compilationReferences;
                ImmutableHashSet<string> copyReferenceInputs;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(ResolvedCompilationReference.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    ImmutableHashSet<string>.Builder compilationReferencesBuilder = ImmutableHashSet.CreateBuilder(StringComparers.Paths);
                    ImmutableHashSet<string>.Builder copyReferenceInputsBuilder = ImmutableHashSet.CreateBuilder(StringComparers.Paths);

                    foreach (IImmutableDictionary<string, string> item in change.After.Items.Values)
                    {
                        compilationReferencesBuilder.Add(item[ResolvedCompilationReference.ResolvedPathProperty]);
                        if (!string.IsNullOrWhiteSpace(item[CopyUpToDateMarker.SchemaName]))
                        {
                            copyReferenceInputsBuilder.Add(item[CopyUpToDateMarker.SchemaName]);
                        }

                        if (!string.IsNullOrWhiteSpace(item[ResolvedCompilationReference.OriginalPathProperty]))
                        {
                            copyReferenceInputsBuilder.Add(item[ResolvedCompilationReference.OriginalPathProperty]);
                        }
                    }

                    compilationReferences = compilationReferencesBuilder.ToImmutable();
                    copyReferenceInputs = copyReferenceInputsBuilder.ToImmutable();
                }
                else
                {
                    compilationReferences = CompilationReferences;
                    copyReferenceInputs = CopyReferenceInputs;
                }

                ImmutableHashSet<string> builtOutputs;
                ImmutableDictionary<string, string> copiedOutputFiles;
                if (jointRuleUpdate.ProjectChanges.TryGetValue(UpToDateCheckBuilt.SchemaName, out change) && change.Difference.AnyChanges)
                {
                    ImmutableHashSet<string>.Builder builtOutputsBuilder = ImmutableHashSet.CreateBuilder(StringComparers.Paths);
                    ImmutableDictionary<string, string>.Builder copiedOutputFilesBuilder = ImmutableDictionary.CreateBuilder<string, string>(StringComparers.Paths);

                    foreach ((string destination, IImmutableDictionary<string, string> properties) in change.After.Items)
                    {
                        if (properties.TryGetValue(UpToDateCheckBuilt.OriginalProperty, out string source) && !string.IsNullOrEmpty(source))
                        {
                            // This file is copied, not built
                            // Remember the `Original` source for later
                            copiedOutputFilesBuilder[destination] = source;
                        }
                        else
                        {
                            // This file is built, not copied
                            builtOutputsBuilder.Add(destination);
                        }
                    }

                    builtOutputs = builtOutputsBuilder.ToImmutable();
                    copiedOutputFiles = copiedOutputFilesBuilder.ToImmutable();
                }
                else
                {
                    builtOutputs = BuiltOutputs;
                    copiedOutputFiles = CopiedOutputFiles;
                }

                var itemTypes = projectItemSchema.GetKnownItemTypes()
                    .Where(itemType => projectItemSchema.GetItemType(itemType).UpToDateCheckInput)
                    .ToImmutableHashSet(StringComparers.ItemTypes);

                ImmutableDictionary<string, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)>>.Builder itemsBuilder;
                bool itemTypesChanged = !ItemTypes.SetEquals(itemTypes);

                if (itemTypesChanged)
                {
                    itemsBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<(string path, string? link, CopyToOutputDirectoryType copyType)>>(StringComparers.ItemTypes);
                }
                else
                {
                    itemTypes = ItemTypes;
                    itemsBuilder = Items.ToBuilder();
                }

                itemsChanged = false;

                foreach ((string itemType, IProjectChangeDescription projectChange) in sourceItemsUpdate.ProjectChanges)
                {
                    if (!itemTypes.Contains(itemType))
                        continue;
                    if (!itemTypesChanged && !projectChange.Difference.AnyChanges)
                        continue;
                    if (projectChange.After.Items.Count == 0)
                        continue;

                    itemsBuilder[itemType] = projectChange.After.Items.Select(item => (item.Key, GetLink(item.Value), GetCopyType(item.Value))).ToImmutableHashSet(UpToDateCheckItemComparer.Instance);
                    itemsChanged = true;
                }

                return new State(
                    msBuildProjectFullPath,
                    msBuildProjectDirectory,
                    markerFile,
                    outputRelativeOrFullPath,
                    newestImportInput,
                    lastVersionSeen: configuredProjectVersion,
                    isDisabled,
                    itemTypes,
                    itemsBuilder.ToImmutable(),
                    customInputs,
                    customOutputs,
                    builtOutputs,
                    copiedOutputFiles,
                    analyzerReferences,
                    compilationReferences,
                    copyReferenceInputs);
            }
        }
    }
}
