// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal sealed class Dependency : IDependency
    {
        // These priorities are for graph nodes only and are used to group graph nodes 
        // appropriately in order groups predefined order instead of alphabetically.
        // Order is not changed for top dependency nodes only for graph hierarchies.
        public const int DiagnosticsErrorNodePriority = 100;
        public const int DiagnosticsWarningNodePriority = 101;
        public const int UnresolvedReferenceNodePriority = 110;
        public const int ProjectNodePriority = 120;
        public const int PackageNodePriority = 130;
        public const int FrameworkAssemblyNodePriority = 140;
        public const int PackageAssemblyNodePriority = 150;
        public const int AnalyzerNodePriority = 160;
        public const int ComNodePriority = 170;
        public const int SdkNodePriority = 180;

        public Dependency(IDependencyModel dependencyModel, ITargetFramework targetFramework, string containingProjectPath)
        {
            Requires.NotNull(dependencyModel, nameof(dependencyModel));
            Requires.NotNullOrEmpty(dependencyModel.ProviderType, nameof(dependencyModel.ProviderType));
            Requires.NotNullOrEmpty(dependencyModel.Id, nameof(dependencyModel.Id));
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNullOrEmpty(containingProjectPath, nameof(containingProjectPath));

            TargetFramework = targetFramework;

            _modelId = dependencyModel.Id;
            _containingProjectPath = containingProjectPath;

            ProviderType = dependencyModel.ProviderType;
            Name = dependencyModel.Name ?? string.Empty;
            Version = dependencyModel.Version ?? string.Empty;
            Caption = dependencyModel.Caption ?? string.Empty;
            OriginalItemSpec = dependencyModel.OriginalItemSpec ?? string.Empty;
            Path = dependencyModel.Path ?? string.Empty;
            SchemaName = dependencyModel.SchemaName ?? Folder.SchemaName;
            _schemaItemType = dependencyModel.SchemaItemType ?? Folder.PrimaryDataSourceItemType;
            Resolved = dependencyModel.Resolved;
            TopLevel = dependencyModel.TopLevel;
            Implicit = dependencyModel.Implicit;
            Visible = dependencyModel.Visible;
            Priority = dependencyModel.Priority;
            Flags = dependencyModel.Flags;

            // Just in case custom providers don't do it, add corresponding flags for Resolved state.
            // This is needed for tree update logic to track if tree node changing state from unresolved 
            // to resolved or vice-versa (it helps to decide if we need to remove it or update in-place
            // in the tree to avoid flicks).
            Flags += Resolved
                ? DependencyTreeFlags.ResolvedFlags
                : DependencyTreeFlags.UnresolvedFlags;

            // If this is one of our implementations of IDependencyModel then we can just reuse the icon
            // set rather than creating a new one.
            if (dependencyModel is Dependency dependency)
            {
                IconSet = dependency.IconSet;
            }
            else if (dependencyModel is DependencyModel model)
            {
                IconSet = model.IconSet;
            }
            else
            {
                IconSet = DependencyIconSetCache.Instance.GetOrAddIconSet(dependencyModel.Icon, dependencyModel.ExpandedIcon, dependencyModel.UnresolvedIcon, dependencyModel.UnresolvedExpandedIcon);
            }

            Properties = dependencyModel.Properties
                ?? ImmutableStringDictionary<string>.EmptyOrdinal
                     .Add(Folder.IdentityProperty, Caption)
                     .Add(Folder.FullPathProperty, Path);

            if (dependencyModel.DependencyIDs == null || dependencyModel.DependencyIDs.Count == 0)
            {
                DependencyIDs = ImmutableList<string>.Empty;
            }
            else
            {
                int count = dependencyModel.DependencyIDs.Count;
                ImmutableArray<string>.Builder ids = ImmutableArray.CreateBuilder<string>(count);
                for (int i = 0; i < count; i++)
                    ids.Add(GetID(TargetFramework, ProviderType, dependencyModel.DependencyIDs[i]));
                DependencyIDs = ids.MoveToImmutable();
            }
        }

        /// <summary>
        /// Private constructor used to clone Dependency
        /// </summary>
        private Dependency(
            Dependency dependency,
            string caption,
            bool? resolved,
            ProjectTreeFlags? flags,
            string schemaName,
            IImmutableList<string> dependencyIDs,
            DependencyIconSet iconSet,
            bool? isImplicit)
        {
            // Copy values as necessary to create a clone with any properties overridden

            _modelId = dependency._modelId;
            _fullPath = dependency._fullPath;
            TargetFramework = dependency.TargetFramework;
            _containingProjectPath = dependency._containingProjectPath;
            ProviderType = dependency.ProviderType;
            Name = dependency.Name;
            Version = dependency.Version;
            OriginalItemSpec = dependency.OriginalItemSpec;
            Path = dependency.Path;
            _schemaItemType = dependency.SchemaItemType;
            TopLevel = dependency.TopLevel;
            Visible = dependency.Visible;
            Priority = dependency.Priority;
            Properties = dependency.Properties;
            Caption = caption ?? dependency.Caption; // TODO if Properties contains "Folder.IdentityProperty" should we update it? (see public ctor)
            Resolved = resolved ?? dependency.Resolved;
            Flags = flags ?? dependency.Flags;
            SchemaName = schemaName ?? dependency.SchemaName;
            DependencyIDs = dependencyIDs ?? dependency.DependencyIDs;
            IconSet = iconSet != null ? DependencyIconSetCache.Instance.GetOrAddIconSet(iconSet) : dependency.IconSet;
            Implicit = isImplicit ?? dependency.Implicit;
        }

        #region IDependencyModel

        /// <summary>
        /// Id unique for a particular provider. We append target framework and provider type to it, 
        /// to get a unique id for the whole snapshot.
        /// </summary>
        private readonly string _modelId;
        private string _id;
        private readonly string _containingProjectPath;
        private string _fullPath;

        public string Id
        {
            get
            {
                if (_id == null)
                {
                    _id = GetID(TargetFramework, ProviderType, _modelId);
                }

                return _id;
            }
        }

        public string ProviderType { get; }
        public string Name { get; }
        public string OriginalItemSpec { get; }
        public string Path { get; }
        public string FullPath
        {
            get
            {
                // Avoid calculating this unless absolutely needed as 
                // we have a lot of Dependency instances floating around
                if (_fullPath == null)
                {
                    _fullPath = GetFullPath(OriginalItemSpec, _containingProjectPath);
                }

                return _fullPath;

                string GetFullPath(string originalItemSpec, string containingProjectPath)
                {
                    if (string.IsNullOrEmpty(originalItemSpec) || ManagedPathHelper.IsRooted(originalItemSpec))
                        return originalItemSpec ?? string.Empty;

                    return ManagedPathHelper.TryMakeRooted(containingProjectPath, originalItemSpec);
                }
            }
        }

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
                // tracking issue: https://github.com/dotnet/roslyn-project-system/issues/1102
                bool isGenericNodeType = Flags.Contains(DependencyTreeFlags.GenericDependencyFlags);
                return isGenericNodeType ? _schemaItemType : Folder.PrimaryDataSourceItemType;
            }
        }

        public string Caption { get; }
        public string Version { get; }
        public bool Resolved { get; }
        public bool TopLevel { get; }
        public bool Implicit { get; }
        public bool Visible { get; }

        public ImageMoniker Icon => IconSet.Icon;
        public ImageMoniker ExpandedIcon => IconSet.ExpandedIcon;
        public ImageMoniker UnresolvedIcon => IconSet.UnresolvedIcon;
        public ImageMoniker UnresolvedExpandedIcon => IconSet.UnresolvedExpandedIcon;

        public DependencyIconSet IconSet { get; }

        public int Priority { get; }
        public ProjectTreeFlags Flags { get; }

        public IImmutableDictionary<string, string> Properties { get; }

        public IImmutableList<string> DependencyIDs { get; }

        #endregion

        public ITargetFramework TargetFramework { get; }

        public string Alias
        {
            get
            {
                string path = OriginalItemSpec ?? Path;

                return string.IsNullOrEmpty(path) || path.Equals(Caption, StringComparison.OrdinalIgnoreCase)
                    ? Caption
                    : string.Concat(Caption, " (", path, ")");
            }
        }

        public IDependency SetProperties(
            string caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string schemaName = null,
            IImmutableList<string> dependencyIDs = null,
            DependencyIconSet iconSet = null,
            bool? isImplicit = null)
        {
            return new Dependency(this, caption, resolved, flags, schemaName, dependencyIDs, iconSet, isImplicit);
        }

        public override int GetHashCode() 
            => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);

        public override bool Equals(object obj) 
            => obj is IDependency other && Equals(other);

        public bool Equals(IDependency other) 
            => StringComparer.OrdinalIgnoreCase.Equals(Id, other?.Id);

        public override string ToString()
        {
            // Used for debugging only
            var sb = PooledStringBuilder.GetInstance();
            sb.Append("Id=\"");
            sb.Append(Id);
            sb.Append('"');

            if (Resolved) sb.Append(" Resolved");
            if (TopLevel) sb.Append(" TopLevel");
            if (Implicit) sb.Append(" Implicit");
            if (Visible)  sb.Append(" Visible");

            return sb.ToStringAndFree();
        }

        /// <summary>
        /// Determines whether <paramref name="id"/> is equal to the result of <see cref="GetID"/> when passed
        /// <paramref name="targetFramework"/>, <paramref name="providerType"/> and <paramref name="modelId"/>.
        /// </summary>
        /// <remarks>
        /// This method performs no heap allocations unless <paramref name="modelId"/> must be escaped.
        /// </remarks>
        public static bool IdEquals(string id, ITargetFramework targetFramework, string providerType, string modelId)
        {
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNullOrEmpty(providerType, nameof(providerType));
            Requires.NotNullOrEmpty(modelId, nameof(modelId));

            if (id == null)
                return false;

            int modelSlashCount = 0;
            for (int i = modelId.Length - 1; i >= 0 && (modelId[i] == '\\' || modelId[i] == '/'); i--)
                modelSlashCount++;
            int length = targetFramework.ShortName.Length + providerType.Length + modelId.Length - modelSlashCount + 2;

            if (id.Length != length)
                return false;
            if (!id.StartsWith(targetFramework.ShortName, StringComparison.OrdinalIgnoreCase))
                return false;
            int index = targetFramework.ShortName.Length;
            if (id[index++] != '\\')
                return false;
            if (string.Compare(id, index, providerType, 0, providerType.Length, StringComparisons.DependencyProviderTypes) != 0)
                return false;
            index += providerType.Length;
            if (id[index++] != '\\')
                return false;

            // Escape model ID
            // NOTE It doesn't seem possible to avoid the potential string allocation here without
            // reimplementing OrdinalIgnoreCase comparison.
            modelId = modelId.Replace('/', '\\').Replace("..", "__");

            if (string.Compare(id, index, modelId, 0, modelId.Length - modelSlashCount, StringComparison.OrdinalIgnoreCase) != 0)
                return false;

            return true;
        }

        public static string GetID(ITargetFramework targetFramework, string providerType, string modelId)
        {
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNullOrEmpty(providerType, nameof(providerType));
            Requires.NotNullOrEmpty(modelId, nameof(modelId));

            var sb = PooledStringBuilder.GetInstance();
            sb.Append(targetFramework.ShortName);
            sb.Append('\\');
            sb.Append(providerType);
            sb.Append('\\');
            int offset = sb.Length;
            sb.Append(modelId);
            // normalize modelId (without allocating)
            sb.Replace('/', '\\', offset, modelId.Length);
            sb.Replace("..", "__", offset, modelId.Length);
            sb.TrimEnd(Delimiter.BackSlash);
            return sb.ToStringAndFree();
        }
    }
}
