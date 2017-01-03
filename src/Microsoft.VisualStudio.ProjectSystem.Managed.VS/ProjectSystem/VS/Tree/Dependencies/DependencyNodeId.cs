// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    /// <summary>
    /// In order to pass provider type to GrpahProvider we need to serialize provider type
    /// to nodes id. In addition, since some nodes can have same ItemSpec (as in the case 
    /// of nuget packages), we need to come up with the way to have unique part in the id.
    /// Thus to allow ids to have some contract to keep same logic we will use this class,
    /// to represent a node's id.
    /// 
    /// (Why this manipulation with IDs even needed? GraphNode when is created only gets 
    /// project path and file path from given IVsHierarchy, so unless we change existing 
    /// logic in progression to allow extensible properties to be added to IDs, the only
    /// way to pass complex data is by serializiung it in FilePath string, and make sure 
    /// Uri.IsAbsolute returns true for FilePath after serialization.)
    /// 
    /// Note: top level nodes should have only ProviderType and ItemSpec (no unique token) 
    /// since they are created via regular IProjecTree provider and data comes from Design
    /// Time build, which sends ItemSpec (we just don't known unique tokens at that point).
    /// 
    /// However lower level nodes must have a unique token in their ids, since they can 
    /// repeat. GraphProvider uses ProviderType to find correct provider for given node and
    /// sends full ID to provider to get a particular node.
    /// </summary>
    public class DependencyNodeId : IEquatable<DependencyNodeId>
    {
        public DependencyNodeId(string providerType,
                                string itemSpec = null,
                                string itemType = null,
                                string uniqueToken = null)
        {
            Requires.NotNullOrEmpty(providerType, nameof(providerType));

            ProviderType = providerType;
            ItemSpec = itemSpec ?? string.Empty;
            ItemType = itemType ?? string.Empty;
            UniqueToken = uniqueToken ?? string.Empty;
            ContextProject = string.Empty;
        }

        /// <summary>
        /// ProviderType that created this node
        /// </summary>
        public string ProviderType { get; private set; }

        /// <summary>
        /// An explicit unique ID of the node. In most cases, it can be equal to IProjectTree.FilePath
        /// </summary>
        public string ItemSpec { get; private set; }

        /// <summary>
        /// Returns an ItemType that is used to get correct properties schema/rule for given node
        /// </summary>
        public string ItemType { get; private set; }

        /// <summary>
        /// Returns a path to the project this node originates from. If empty current project is assumed.
        /// </summary>
        public string ContextProject { get; internal set; }

        /// <summary>
        /// When providers need to make sure that id is unique and itemSpec + itemType is not enough,
        /// they can provide a unique token.
        /// </summary>
        public string UniqueToken { get; private set; }

        /// <summary>
        /// Creates a copy if the ID that has all paths in normal form with '\' slashes instead of '/'.
        /// Note: this is needed to switch back to normal IDs for graph nodes, since graph provider nodes
        /// switch paths in their IDs to uri style with '/'.
        /// </summary>
        /// <returns></returns>
        public DependencyNodeId ToNormalizedId()
        {
            return new DependencyNodeId(ProviderType,
                                        ItemSpec?.Replace('/', '\\'),
                                        ItemType,
                                        UniqueToken?.Replace('/', '\\'))
            {
                ContextProject = string.IsNullOrEmpty(ContextProject)
                                    ? string.Empty
                                    : ContextProject.Replace('/', '\\')
            };
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            // Note: Progression and CPS needs IProjectTree.FilePath to be valid absolute Uri.
            // Since we pass node IDs through FilePath, we have to serialize IDs as valid 
            // absolute Uri, thus make sure "file:///" is appended.
            builder.Append("file:///");
            builder.Append("[");
            builder.Append(ProviderType);

            builder.Append(";");
            builder.Append(Escape(ItemSpec));

            builder.Append(";");
            builder.Append(ItemType);
            
            builder.Append(";");
            builder.Append(UniqueToken);

            builder.Append(";");
            builder.Append(Escape(ContextProject));

            builder.Append("]");

            return builder.ToString();
        }

        public static DependencyNodeId FromString(string serializedId)
        {
            if (string.IsNullOrEmpty(serializedId))
            {
                return null;
            }

            // Note: Progression and CPS needs IProjectTree.FilePath to be valid absolute Uri.
            // Since we pass node IDs through FilePath, we have to serialize IDs as valid 
            // absolute Uri, thus get rid of file:/// in the beginning of the string before parsing.
            if (serializedId.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
            {
                serializedId = serializedId.Substring(8);
            }

            if (!serializedId.StartsWith("[") || !serializedId.EndsWith("]"))
            {
                return null;
            }

            serializedId = serializedId.TrimStart('[').TrimEnd(']').Trim(' ', ';');
            if (string.IsNullOrEmpty(serializedId))
            {
                return null;
            }

            var parts = serializedId?.Split(new[] { ';' });
            if (parts == null || parts.Length <= 0)
            {
                return null;
            }

            var providerType = parts[0];
            var itemSpec = parts.Length > 1 ? Unescape(parts[1]) : string.Empty;
            var itemType = parts.Length > 2 ? parts[2] : string.Empty;
            var uniqueToken = parts.Length > 3 ? parts[3] : string.Empty;
            var contextProject = parts.Length > 4 ? Unescape(parts[4]) : string.Empty;

            return new DependencyNodeId(providerType,
                                        itemSpec,
                                        itemType,
                                        uniqueToken)
            {
                ContextProject = contextProject
            };
        }

        public override int GetHashCode()
        {
            return unchecked(ToString().ToLowerInvariant().GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj is DependencyNodeId other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(DependencyNodeId other)
        {
            if (other != null &&
                other.ToString().Equals(ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Since progression code relies on Uri LocalPath it could break our id string when project's 
        /// ItemSpec has deep relative path like ..\..\..\myproject.csproj, it would convert it to just 
        /// myproject.csproj and cut first half of the id string, which would leave it broken and would 
        /// prevent graph nodes provider from recognizing node. Thus we escape . with * (since it is not 
        /// allowed in paths and can not be faced in real scenarios).
        /// </summary>
        private static string Escape(string str)
        {
            return str.Replace('.', '*');
        }

        /// <summary>
        /// Convert escaped string back to original (see comment for Escape method)
        /// </summary>
        private static string Unescape(string str)
        {
            return str.Replace('*', '.');
        }
    }
}
