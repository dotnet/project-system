// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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

        public Dependency(IDependencyModel dependencyModel, ITargetedDependenciesSnapshot snapshot)
        {
            Requires.NotNull(dependencyModel, nameof(dependencyModel));
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.NotNullOrEmpty(dependencyModel.ProviderType, nameof(dependencyModel.ProviderType));
            Requires.NotNullOrEmpty(dependencyModel.Id, nameof(dependencyModel.Id));

            Snapshot = snapshot;
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
                var normalizedDependencyIDs = new List<string>();
                foreach (var id in dependencyModel.DependencyIDs)
                {
                    normalizedDependencyIDs.Add(GetID(Snapshot.TargetFramework, ProviderType, id));
                }

                DependencyIDs = ImmutableList.CreateRange(normalizedDependencyIDs);
            }
        }

        private Dependency(IDependency model, string modelId)
            : this(model, model.Snapshot)
        {
            _modelId = modelId;
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
                    // we need to replace .. in model id with something else, since IProjectItemTree 
                    // alters it using Uri and .. symbols are gone (it tries to get full path). However
                    // we do need ids to stay original and unique.
                    _id = (Snapshot.TargetFramework == null
                                ? Normalize(_modelId)
                                : GetID(Snapshot.TargetFramework, ProviderType, _modelId));

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
        public bool Implicit { get; }
        public bool Visible { get; }
        public ImageMoniker Icon { get; }
        public ImageMoniker ExpandedIcon { get; }
        public ImageMoniker UnresolvedIcon { get; }
        public ImageMoniker UnresolvedExpandedIcon { get; }
        public int Priority { get; }
        public ProjectTreeFlags Flags { get; set; }

        public IImmutableDictionary<string, string> Properties { get; }

        public IImmutableList<string> DependencyIDs { get; private set; }

        #endregion

        private bool? _hasUnresolvedDependency;
        public bool HasUnresolvedDependency
        {
            get
            {
                if (_hasUnresolvedDependency == null)
                {
                    // CheckForUnresolvedDependencies does dependency tree traversal efficiently,
                    // call it instead of going reqursively through Dependencies property.
                    _hasUnresolvedDependency = Snapshot.CheckForUnresolvedDependencies(this);
                }

                return _hasUnresolvedDependency.Value;
            }
        }

        private IEnumerable<IDependency> _dependencies;
        public IEnumerable<IDependency> Dependencies
        {
            get
            {
                if (_dependencies == null)
                {
                    var dependencies = new List<IDependency>();
                    foreach(var id in DependencyIDs)
                    {
                        if (Snapshot.DependenciesWorld.TryGetValue(id, out IDependency child))
                        {
                            dependencies.Add(child);
                        }                        
                    }

                    _dependencies = dependencies;
                }

                return _dependencies;
            }
        }

        public ITargetedDependenciesSnapshot Snapshot { get; }

        private string _alias = string.Empty;
        public string Alias
        {
            get
            {
                if (string.IsNullOrEmpty(_alias))
                {
                    var path = OriginalItemSpec ?? Path;
                    if (string.IsNullOrEmpty(path) || path.Equals(Caption, StringComparison.OrdinalIgnoreCase))
                    {
                        _alias = Caption;
                    }
                    else
                    {
                        _alias = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", Caption, path);
                    }
                }

                return _alias;
            }
        }

        public IDependency SetProperties(
            string caption = null, 
            bool? resolved = null,
            ProjectTreeFlags? flags = null,
            IImmutableList<string> dependencyIDs = null)
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

            if (dependencyIDs != null)
            {
                clone.DependencyIDs = dependencyIDs;
            }

            return clone;
        }

        public override int GetHashCode()
        {
            return unchecked(Id.ToLowerInvariant().GetHashCode());
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

        private static string Normalize(string id)
        {
            return id.Replace('.', '_').Replace('/', '\\');
        }

        public static string GetID(ITargetFramework targetFramework, string providerType, string modelId)
        {
            Requires.NotNullOrEmpty(providerType, nameof(providerType));

            var normalizedModelId = modelId.Replace('.', '_');
            return $"{targetFramework.ShortName}/{providerType}/{normalizedModelId}".TrimEnd('/').Replace('/', '\\');
        }
    }
}
