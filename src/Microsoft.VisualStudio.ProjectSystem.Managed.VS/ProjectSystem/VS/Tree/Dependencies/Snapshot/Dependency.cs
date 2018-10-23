// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    [DebuggerDisplay("{" + nameof(Id) + ",nq}")]
    internal class Dependency : IDependency
    {
        private static readonly ConcurrentBag<StringBuilder> s_builderPool = new ConcurrentBag<StringBuilder>();
        private static readonly DependencyIconSetCache s_iconSetCache = new DependencyIconSetCache();

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
            if (Resolved)
            {
                Flags = Flags.Union(DependencyTreeFlags.ResolvedFlags);
            }
            else
            {
                Flags = Flags.Union(DependencyTreeFlags.UnresolvedFlags);
            }

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
                IconSet = s_iconSetCache.GetOrAddIconSet(dependencyModel.Icon, dependencyModel.ExpandedIcon, dependencyModel.UnresolvedIcon, dependencyModel.UnresolvedExpandedIcon);
            }

            Properties = dependencyModel.Properties ??
                            ImmutableStringDictionary<string>.EmptyOrdinal
                                                             .Add(Folder.IdentityProperty, Caption)
                                                             .Add(Folder.FullPathProperty, Path);
            if (dependencyModel.DependencyIDs == null)
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
        private Dependency(Dependency model, string modelId)
            : this(model, model.TargetFramework, model._containingProjectPath)
        {
            // since this is a clone make the modelId and dependencyIds match the original model
            _modelId = modelId;
            _fullPath = model._fullPath; // Grab the cached value if we've already created it

            if (model.DependencyIDs != null && model.DependencyIDs.Any())
            {
                DependencyIDs = model.DependencyIDs;
            }
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
        public string Name { get; protected set; }
        public string OriginalItemSpec { get; protected set; }
        public string Path { get; protected set; }
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
            }
        }
        public string SchemaName { get; protected set; }
        private string _schemaItemType;
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
            protected set
            {
                _schemaItemType = value;
            }
        }

        public string Caption { get; private set; }
        public string Version { get; }
        public bool Resolved { get; private set; }
        public bool TopLevel { get; }
        public bool Implicit { get; private set; }
        public bool Visible { get; }


        public ImageMoniker Icon => IconSet.Icon;
        public ImageMoniker ExpandedIcon => IconSet.ExpandedIcon;
        public ImageMoniker UnresolvedIcon => IconSet.UnresolvedIcon;
        public ImageMoniker UnresolvedExpandedIcon => IconSet.UnresolvedExpandedIcon;

        public DependencyIconSet IconSet { get; private set; }

        public int Priority { get; }
        public ProjectTreeFlags Flags { get; set; }

        public IImmutableDictionary<string, string> Properties { get; }

        public IImmutableList<string> DependencyIDs { get; private set; }

        #endregion

        public ITargetFramework TargetFramework { get; }

        public string Alias => GetAlias(this);

        public IDependency SetProperties(
            string caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string schemaName = null,
            IImmutableList<string> dependencyIDs = null,
            DependencyIconSet iconSet = null,
            bool? isImplicit = null)
        {
            var clone = new Dependency(this, _modelId);

            if (caption != null)
            {
                clone.Caption = caption;
            }

            if (resolved != null)
            {
                clone.Resolved = resolved.Value;
            }

            if (flags != null)
            {
                clone.Flags = flags.Value;
            }

            if (schemaName != null)
            {
                clone.SchemaName = schemaName;
            }

            if (dependencyIDs != null)
            {
                clone.DependencyIDs = dependencyIDs;
            }

            if (iconSet != null)
            {
                clone.IconSet = s_iconSetCache.GetOrAddIconSet(iconSet);
            }

            if (isImplicit != null)
            {
                clone.Implicit = isImplicit.Value;
            }

            return clone;
        }

        public override int GetHashCode() 
            => StringComparer.OrdinalIgnoreCase.GetHashCode(Id);

        public override bool Equals(object obj) 
            => obj is IDependency other && Equals(other);

        public bool Equals(IDependency other) 
            => other != null && other.Id.Equals(Id, StringComparison.OrdinalIgnoreCase);

        public static bool operator ==(Dependency left, Dependency right)
            => left is null ? right is null : left.Equals(right);

        public static bool operator !=(Dependency left, Dependency right)
            => !(left == right);

        public static bool operator <(Dependency left, Dependency right)
            => left is null ? !(right is null) : left.CompareTo(right) < 0;

        public static bool operator <=(Dependency left, Dependency right)
            => left is null || left.CompareTo(right) <= 0;

        public static bool operator >(Dependency left, Dependency right)
            => !(left is null) && left.CompareTo(right) > 0;

        public static bool operator >=(Dependency left, Dependency right)
            => left is null ? right is null : left.CompareTo(right) >= 0;

        public int CompareTo(IDependency other)
        {
            if (other == null)
            {
                return 1;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(Id, other.Id);
        }

        public override string ToString()
        {
            return Id;
        }

        private static string GetAlias(IDependency dependency)
        {
            string path = dependency.OriginalItemSpec ?? dependency.Path;
            if (string.IsNullOrEmpty(path) || path.Equals(dependency.Caption, StringComparison.OrdinalIgnoreCase))
            {
                return dependency.Caption;
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", dependency.Caption, path);
            }
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
            if (string.Compare(id, index, providerType, 0, providerType.Length, StringComparison.OrdinalIgnoreCase) != 0)
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

            StringBuilder sb = null;
            try
            {
                int length = targetFramework.ShortName.Length + providerType.Length + modelId.Length + 2;

                if (!s_builderPool.TryTake(out sb))
                {
                    sb = new StringBuilder(length);
                }
                else
                {
                    sb.EnsureCapacity(length);
                }

                sb.Append(targetFramework.ShortName).Append('\\');
                sb.Append(providerType).Append('\\');
                int offset = sb.Length;
                sb.Append(modelId);
                // normalize modelId (without allocating)
                sb.Replace('/', '\\', offset, modelId.Length)
                  .Replace("..", "__", offset, modelId.Length);
                sb.TrimEnd(Delimiter.BackSlash);
                return sb.ToString();
            }
            finally
            {
                // Prevent holding on to large builders
                if (sb?.Capacity < 1000)
                {
                    sb.Clear();
                    s_builderPool.Add(sb);
                }
            }
        }

        private static string GetFullPath(string originalItemSpec, string containingProjectPath)
        {
            if (string.IsNullOrEmpty(originalItemSpec) || ManagedPathHelper.IsRooted(originalItemSpec))
                return originalItemSpec ?? string.Empty;

            return ManagedPathHelper.TryMakeRooted(containingProjectPath, originalItemSpec);
        }
    }
}
