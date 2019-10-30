// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    [DebuggerDisplay("{" + nameof(Id) + ",nq}")]
    internal sealed class TestDependency : IDependency
    {
        private static readonly DependencyIconSet s_defaultIconSet = new DependencyIconSet(
            KnownMonikers.Accordian,
            KnownMonikers.Bug,
            KnownMonikers.CrashDumpFile,
            KnownMonikers.DataCenter);

        public IDependency ClonePropertiesFrom
        {
            set
            {
                ProviderType = value.ProviderType;
                Name = value.Name;
                Caption = value.Caption;
                OriginalItemSpec = value.OriginalItemSpec;
                Path = value.Path;
                FullPath = value.FullPath;
                SchemaName = value.SchemaName;
                SchemaItemType = value.SchemaItemType;
                Resolved = value.Resolved;
                TopLevel = value.TopLevel;
                Implicit = value.Implicit;
                Visible = value.Visible;
                Priority = value.Priority;
                IconSet = value.IconSet;
                BrowseObjectProperties = value.BrowseObjectProperties;
                DependencyIDs = value.DependencyIDs;
                Flags = value.Flags;
                Id = value.Id;
                TargetFramework = value.TargetFramework;
            }
        }

#pragma warning disable CS8618 // Non-nullable property is uninitialized
        public string ProviderType { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public string OriginalItemSpec { get; set; }
        public string Path { get; set; }
        public string FullPath { get; set; }
        public string SchemaName { get; set; }
        public string SchemaItemType { get; set; }
        public bool Resolved { get; set; } = false;
        public bool TopLevel { get; set; } = true;
        public bool Implicit { get; set; } = false;
        public bool Visible { get; set; } = true;
        public int Priority { get; set; } = 0;
        public IImmutableDictionary<string, string> BrowseObjectProperties { get; set; }
        public ImmutableArray<string> DependencyIDs { get; set; } = ImmutableArray<string>.Empty;
        public ProjectTreeFlags Flags { get; set; } = ProjectTreeFlags.Empty;
        public string Id { get; set; }
        public ITargetFramework TargetFramework { get; set; }
        public DependencyIconSet IconSet { get; set; } = s_defaultIconSet;
#pragma warning restore CS8618 // Non-nullable property is uninitialized

        public IDependency SetProperties(
            string? caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string? schemaName = null,
            ImmutableArray<string> dependencyIDs = default,
            DependencyIconSet? iconSet = null,
            bool? isImplicit = null)
        {
            return new TestDependency
            {
                // Copy all properties from this instance
                ClonePropertiesFrom = this,

                // Override specific properties as needed
                Caption = caption ?? Caption,
                Resolved = resolved ?? Resolved,
                Flags = flags ?? Flags,
                SchemaName = schemaName ?? SchemaName,
                DependencyIDs = dependencyIDs.IsDefault ? DependencyIDs : dependencyIDs,
                IconSet = iconSet ?? IconSet,
                Implicit = isImplicit ?? Implicit
            };
        }
    }
}
