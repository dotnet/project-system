// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    internal sealed class Dependency : IDependency
    {
        public Dependency(IDependencyModel dependencyModel)
        {
            Requires.NotNull(dependencyModel, nameof(dependencyModel));
            Requires.NotNullOrEmpty(dependencyModel.ProviderType, nameof(dependencyModel.ProviderType));
            Requires.NotNullOrEmpty(dependencyModel.Id, nameof(dependencyModel.Id));

            Id = dependencyModel.Id;

            ProviderType = dependencyModel.ProviderType;
            Caption = dependencyModel.Caption ?? string.Empty;
            OriginalItemSpec = dependencyModel.OriginalItemSpec;
            FilePath = dependencyModel.Path;
            SchemaName = dependencyModel.SchemaName ?? Folder.SchemaName;
            _schemaItemType = dependencyModel.SchemaItemType ?? Folder.PrimaryDataSourceItemType;
            Resolved = dependencyModel.Resolved;
            Implicit = dependencyModel.Implicit;
            Visible = dependencyModel.Visible;
            Flags = dependencyModel.Flags;

            // Just in case custom providers don't do it, add corresponding flags for Resolved state.
            // This is needed for tree update logic to track if tree node changing state from unresolved
            // to resolved or vice-versa (it helps to decide if we need to remove it or update in-place
            // in the tree to avoid flicks).
            if (Resolved)
            {
                if (!Flags.Contains(DependencyTreeFlags.Resolved))
                {
                    Flags += DependencyTreeFlags.Resolved;
                }
            }
            else
            {
                if (!Flags.Contains(DependencyTreeFlags.Unresolved))
                {
                    Flags += DependencyTreeFlags.Unresolved;
                }
            }

            // If this is one of our implementations of IDependencyModel then we can just reuse the icon
            // set rather than creating a new one.
            if (dependencyModel is DependencyModel model)
            {
                IconSet = model.IconSet;

                DiagnosticLevel = model.DiagnosticLevel;
            }
            else
            {
                IconSet = DependencyIconSetCache.Instance.GetOrAddIconSet(dependencyModel.Icon, dependencyModel.ExpandedIcon, dependencyModel.UnresolvedIcon, dependencyModel.UnresolvedExpandedIcon);

                DiagnosticLevel = dependencyModel.Resolved ? DiagnosticLevel.None : DiagnosticLevel.Warning;
            }

            BrowseObjectProperties = dependencyModel.Properties
                ?? ImmutableStringDictionary<string>.EmptyOrdinal
                     .Add(Folder.IdentityProperty, Caption)
                     .Add(Folder.FullPathProperty, FilePath ?? string.Empty);
        }

        /// <summary>
        /// Private constructor used to clone Dependency
        /// </summary>
        private Dependency(
            Dependency dependency,
            string? caption,
            bool? resolved,
            ProjectTreeFlags? flags,
            string? schemaName,
            DependencyIconSet? iconSet,
            bool? isImplicit,
            DiagnosticLevel? diagnosticLevel)
        {
            // Copy values as necessary to create a clone with any properties overridden

            Id = dependency.Id;
            ProviderType = dependency.ProviderType;
            OriginalItemSpec = dependency.OriginalItemSpec;
            FilePath = dependency.FilePath;
            _schemaItemType = dependency._schemaItemType;
            Visible = dependency.Visible;
            BrowseObjectProperties = dependency.BrowseObjectProperties; // NOTE we explicitly do not update Identity in these properties if caption changes
            Caption = caption ?? dependency.Caption; // TODO if Properties contains "Folder.IdentityProperty" should we update it? (see public ctor)
            Resolved = resolved ?? dependency.Resolved;
            Flags = flags ?? dependency.Flags;
            SchemaName = schemaName ?? dependency.SchemaName;
            IconSet = iconSet != null ? DependencyIconSetCache.Instance.GetOrAddIconSet(iconSet) : dependency.IconSet;
            Implicit = isImplicit ?? dependency.Implicit;
            DiagnosticLevel = diagnosticLevel ?? dependency.DiagnosticLevel;
        }

        public DiagnosticLevel DiagnosticLevel { get; }

        #region IDependency

        public string Id { get; }

        public string ProviderType { get; }
        public string? OriginalItemSpec { get; }

        public string SchemaName { get; }

        private readonly string _schemaItemType;

        public string SchemaItemType
        {
            get
            {
                // For generic node types we do set correct, known item types, however for custom nodes
                // provided by third party extensions we can not guarantee that item type will be known.
                // Thus always set predefined itemType for all custom nodes.
                // TODO: generate specific xaml rule for generic Dependency nodes
                // tracking issue: https://github.com/dotnet/project-system/issues/1102
                bool isGenericNodeType = Flags.Contains(DependencyTreeFlags.GenericDependency);
                return isGenericNodeType ? _schemaItemType : Folder.PrimaryDataSourceItemType;
            }
        }

        public string Caption { get; }
        public bool Resolved { get; }
        public bool Implicit { get; }
        public bool Visible { get; }

        public DependencyIconSet IconSet { get; }

        public ProjectTreeFlags Flags { get; }

        public IImmutableDictionary<string, string> BrowseObjectProperties { get; }

        #endregion

        #region IDependencyViewModel

        public string? FilePath { get; }

        public ImageMoniker Icon => DiagnosticLevel == DiagnosticLevel.None ? IconSet.Icon : IconSet.UnresolvedIcon;
        public ImageMoniker ExpandedIcon => DiagnosticLevel == DiagnosticLevel.None ? IconSet.ExpandedIcon : IconSet.UnresolvedExpandedIcon;

        #endregion

        public IDependency SetProperties(
            string? caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string? schemaName = null,
            DependencyIconSet? iconSet = null,
            bool? isImplicit = null,
            DiagnosticLevel? diagnosticLevel = null)
        {
            return new Dependency(this, caption, resolved, flags, schemaName, iconSet, isImplicit, diagnosticLevel);
        }

        public override string ToString()
        {
            // Used for debugging only
            var sb = PooledStringBuilder.GetInstance();
            sb.Append("Provider=\"");
            sb.Append(ProviderType);
            sb.Append("\" ModelId=\"");
            sb.Append(Id);
            sb.Append('"');

            if (Resolved) sb.Append(" Resolved");
            if (Implicit) sb.Append(" Implicit");
            if (Visible) sb.Append(" Visible");

            return sb.ToStringAndFree();
        }
    }
}
