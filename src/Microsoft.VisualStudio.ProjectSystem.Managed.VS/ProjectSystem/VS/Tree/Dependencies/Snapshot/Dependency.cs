// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.CrossTarget;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot
{
    internal class Dependency : IDependency
    {
        // These priorities are for graph nodes only and are used to group graph nodes 
        // appropriatelly in order groups predefined order instead of alphabetically.
        // Order is not changed for top dependency nodes only for grpah hierarchies.
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

        public Dependency(IDependencyModel dependencyModel, ITargetFramework targetFramework)
        {
            Requires.NotNull(dependencyModel, nameof(dependencyModel));
            Requires.NotNullOrEmpty(dependencyModel.ProviderType, nameof(dependencyModel.ProviderType));
            Requires.NotNullOrEmpty(dependencyModel.Id, nameof(dependencyModel.Id));
            Requires.NotNull(targetFramework, nameof(targetFramework));

            TargetFramework = targetFramework;

            _modelId = dependencyModel.Id;
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

            Icon = dependencyModel.Icon;
            ExpandedIcon = dependencyModel.ExpandedIcon;
            UnresolvedIcon = dependencyModel.UnresolvedIcon;
            UnresolvedExpandedIcon = dependencyModel.UnresolvedExpandedIcon;
            Properties = dependencyModel.Properties ??
                            ImmutableDictionary<string, string>.Empty
                                                               .Add(Folder.IdentityProperty, Caption)
                                                               .Add(Folder.FullPathProperty, Path);
            if (dependencyModel.DependencyIDs == null)
            {
                DependencyIDs = ImmutableList<string>.Empty;
            }
            else
            {
                var normalizedDependencyIDs = ImmutableList.CreateBuilder<string>();
                foreach (var id in dependencyModel.DependencyIDs)
                {
                    normalizedDependencyIDs.Add(GetID(TargetFramework, ProviderType, id));
                }

                DependencyIDs = normalizedDependencyIDs.ToImmutable();
            }
        }

        /// <summary>
        /// Private constructor used to clone Dependency
        /// </summary>
        private Dependency(IDependency model, string modelId)
            : this(model, model.TargetFramework)
        {
            // since this is a clone make the modelId and dependencyIds match the original model
            _modelId = modelId;

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
        private string _modelId;

        private string _id;
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
                var isGenericNodeType = Flags.Contains(DependencyTreeFlags.GenericDependencyFlags);
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
        public ImageMoniker Icon { get; private set; }
        public ImageMoniker ExpandedIcon { get; private set; }
        public ImageMoniker UnresolvedIcon { get; }
        public ImageMoniker UnresolvedExpandedIcon { get; }
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
            ImageMoniker icon = default(ImageMoniker),
            ImageMoniker expandedIcon = default(ImageMoniker),
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

            if (icon.Id != 0 && icon.Guid != Guid.Empty)
            {
                clone.Icon = icon;
            }

            if (expandedIcon.Id != 0 && expandedIcon.Guid != Guid.Empty)
            {
                clone.ExpandedIcon = expandedIcon;
            }

            if (isImplicit != null)
            {
                clone.Implicit = isImplicit.Value;
            }

            return clone;
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
        }

        public override bool Equals(object obj)
        {
            if (obj is IDependency other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(IDependency other)
        {
            if (other != null && other.Id.Equals(Id, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

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
            var path = dependency.OriginalItemSpec ?? dependency.Path;
            if (string.IsNullOrEmpty(path) || path.Equals(dependency.Caption, StringComparison.OrdinalIgnoreCase))
            {
                return dependency.Caption;
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", dependency.Caption, path);
            }
        }

        private static string Normalize(string id)
        {
            return id
                .Replace('/', '\\')
                .Replace("..", "__");
        }

        public static string GetID(ITargetFramework targetFramework, string providerType, string modelId)
        {
            Requires.NotNull(targetFramework, nameof(targetFramework));
            Requires.NotNullOrEmpty(providerType, nameof(providerType));
            Requires.NotNullOrEmpty(modelId, nameof(modelId));

            return $"{targetFramework.ShortName}\\{providerType}\\{Normalize(modelId)}".TrimEnd(CommonConstants.BackSlashDelimiter);
        }
    }
}
