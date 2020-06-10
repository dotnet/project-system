// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Buffers.PooledObjects;
using Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Models;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.ProjectSystem.Tree.Dependencies.Snapshot
{
    internal sealed class Dependency : IDependency
    {
        public Dependency(IDependencyModel dependencyModel)
        {
            Requires.NotNull(dependencyModel, nameof(dependencyModel));
            Requires.NotNullOrEmpty(dependencyModel.ProviderType, nameof(dependencyModel.ProviderType));
            Requires.NotNullOrEmpty(dependencyModel.Id, nameof(dependencyModel.Id));

            _modelId = dependencyModel.Id;

            ProviderType = dependencyModel.ProviderType;
            Name = dependencyModel.Name ?? string.Empty;
            Caption = dependencyModel.Caption ?? string.Empty;
            OriginalItemSpec = dependencyModel.OriginalItemSpec ?? string.Empty;
            Path = dependencyModel.Path ?? string.Empty;
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
            }
            else
            {
                IconSet = DependencyIconSetCache.Instance.GetOrAddIconSet(dependencyModel.Icon, dependencyModel.ExpandedIcon, dependencyModel.UnresolvedIcon, dependencyModel.UnresolvedExpandedIcon);
            }

            BrowseObjectProperties = dependencyModel.Properties
                ?? ImmutableStringDictionary<string>.EmptyOrdinal
                     .Add(Folder.IdentityProperty, Caption)
                     .Add(Folder.FullPathProperty, Path);
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
            bool? isImplicit)
        {
            // Copy values as necessary to create a clone with any properties overridden

            _id = dependency._id;
            _modelId = dependency._modelId;
            ProviderType = dependency.ProviderType;
            Name = dependency.Name;
            OriginalItemSpec = dependency.OriginalItemSpec;
            Path = dependency.Path;
            _schemaItemType = dependency.SchemaItemType;
            Visible = dependency.Visible;
            BrowseObjectProperties = dependency.BrowseObjectProperties;
            Caption = caption ?? dependency.Caption; // TODO if Properties contains "Folder.IdentityProperty" should we update it? (see public ctor)
            Resolved = resolved ?? dependency.Resolved;
            Flags = flags ?? dependency.Flags;
            SchemaName = schemaName ?? dependency.SchemaName;
            IconSet = iconSet != null ? DependencyIconSetCache.Instance.GetOrAddIconSet(iconSet) : dependency.IconSet;
            Implicit = isImplicit ?? dependency.Implicit;
        }

        #region IDependency

        /// <summary>
        /// Id unique for a particular provider. We append target framework and provider type to it,
        /// to get a unique id for the whole snapshot.
        /// </summary>
        private readonly string _modelId;
        private string? _id;

        public string Id => _id ??= GetID(ProviderType, _modelId);

        public string ProviderType { get; }
        public string Name { get; }
        public string OriginalItemSpec { get; }
        public string Path { get; }

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

        public IDependency SetProperties(
            string? caption = null,
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            string? schemaName = null,
            DependencyIconSet? iconSet = null,
            bool? isImplicit = null)
        {
            return new Dependency(this, caption, resolved, flags, schemaName, iconSet, isImplicit);
        }

        public override string ToString()
        {
            // Used for debugging only
            var sb = PooledStringBuilder.GetInstance();
            sb.Append("Id=\"");
            sb.Append(Id);
            sb.Append('"');

            if (Resolved) sb.Append(" Resolved");
            if (Implicit) sb.Append(" Implicit");
            if (Visible) sb.Append(" Visible");

            return sb.ToStringAndFree();
        }

        /// <summary>
        /// Determines whether <paramref name="id"/> is equal to the result of <see cref="GetID"/> when passed
        /// <paramref name="providerType"/> and <paramref name="modelId"/>.
        /// </summary>
        /// <remarks>
        /// This method performs no heap allocations unless <paramref name="modelId"/> must be escaped.
        /// </remarks>
        public static bool IdEquals(string id, string providerType, string modelId)
        {
            Requires.NotNullOrEmpty(providerType, nameof(providerType));
            Requires.NotNullOrEmpty(modelId, nameof(modelId));

            if (id == null)
                return false;

            int modelSlashCount = 0;
            for (int i = modelId.Length - 1; i >= 0 && (modelId[i] == '\\' || modelId[i] == '/'); i--)
                modelSlashCount++;
            int length = providerType.Length + modelId.Length - modelSlashCount + 1;

            if (id.Length != length)
                return false;
            if (string.Compare(id, 0, providerType, 0, providerType.Length, StringComparisons.DependencyProviderTypes) != 0)
                return false;
            int index = providerType.Length;
            if (id[index++] != '\\')
                return false;

            // Escape model ID
            // NOTE It doesn't seem possible to avoid the potential string allocation here without
            // reimplementing OrdinalIgnoreCase comparison.
            modelId = modelId.Replace('/', '\\').Replace("..", "__");

            if (string.Compare(id, index, modelId, 0, modelId.Length - modelSlashCount, StringComparisons.DependencyTreeIds) != 0)
                return false;

            return true;
        }

        /// <summary>
        /// Constructs the string identifier for a dependency from its provider type and dependency model ID.
        /// </summary>
        /// <remarks>
        /// This string has form <c>"provider-type\model-id"</c>.
        /// <list type="bullet">
        ///   <item>All characters are lower-case.</item>
        ///   <item><c>".."</c> is replaced with <c>"__"</c>.</item>
        ///   <item><c>"/"</c> is replaced with <c>"\"</c>.</item>
        ///   <item>Any trailing <c>"\"</c> characters are trimmed.</item>
        /// </list>
        /// </remarks>
        /// <param name="providerType"></param>
        /// <param name="modelId"></param>
        public static string GetID(string providerType, string modelId)
        {
            Requires.NotNullOrEmpty(providerType, nameof(providerType));
            Requires.NotNullOrEmpty(modelId, nameof(modelId));

            var sb = PooledStringBuilder.GetInstance();
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
