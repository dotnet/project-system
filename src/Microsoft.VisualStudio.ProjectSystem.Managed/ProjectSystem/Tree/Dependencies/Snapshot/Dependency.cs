// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
            SchemaItemType = dependencyModel.SchemaItemType ?? Folder.PrimaryDataSourceItemType;
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
                if (!Flags.Contains(ProjectTreeFlags.ResolvedReference))
                {
                    Flags += ProjectTreeFlags.ResolvedReference;
                }
            }
            else
            {
                if (!Flags.Contains(ProjectTreeFlags.BrokenReference))
                {
                    Flags += ProjectTreeFlags.BrokenReference;
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
                IconSet = DependencyIconSetCache.Instance.GetOrAddIconSet(dependencyModel.Icon, dependencyModel.ExpandedIcon, dependencyModel.UnresolvedIcon, dependencyModel.UnresolvedExpandedIcon, dependencyModel.Icon, dependencyModel.ExpandedIcon);

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
            string? caption)
        {
            // Copy values as necessary to create a clone with any properties overridden

            Id = dependency.Id;
            ProviderType = dependency.ProviderType;
            OriginalItemSpec = dependency.OriginalItemSpec;
            FilePath = dependency.FilePath;
            SchemaItemType = dependency.SchemaItemType;
            Visible = dependency.Visible;
            BrowseObjectProperties = dependency.BrowseObjectProperties; // NOTE we explicitly do not update Identity in these properties if caption changes
            Caption = caption ?? dependency.Caption; // TODO if Properties contains "Folder.IdentityProperty" should we update it? (see public ctor)
            Resolved = dependency.Resolved;
            Flags = dependency.Flags;
            SchemaName = dependency.SchemaName;
            IconSet =dependency.IconSet;
            Implicit = dependency.Implicit;
            DiagnosticLevel = dependency.DiagnosticLevel;
        }

        public DiagnosticLevel DiagnosticLevel { get; }

        #region IDependency

        public string Id { get; }

        public string ProviderType { get; }
        public string? OriginalItemSpec { get; }

        public string SchemaName { get; }
        public string SchemaItemType { get; }

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

        public ImageMoniker Icon         => DiagnosticLevel == DiagnosticLevel.None ? Implicit ? IconSet.ImplicitIcon         : IconSet.Icon         : IconSet.UnresolvedIcon;
        public ImageMoniker ExpandedIcon => DiagnosticLevel == DiagnosticLevel.None ? Implicit ? IconSet.ImplicitExpandedIcon : IconSet.ExpandedIcon : IconSet.UnresolvedExpandedIcon;

        #endregion

        public IDependency WithCaption(string caption)
        {
            return new Dependency(this, caption);
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

            if (Resolved)
                sb.Append(" Resolved");
            if (Implicit)
                sb.Append(" Implicit");
            if (Visible)
                sb.Append(" Visible");

            return sb.ToStringAndFree();
        }
    }
}
