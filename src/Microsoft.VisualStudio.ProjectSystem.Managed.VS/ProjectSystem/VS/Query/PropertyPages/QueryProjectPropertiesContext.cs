// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.Query;
using Microsoft.VisualStudio.ProjectSystem.Query.Framework;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Query
{
    /// <summary>
    /// An implementation of <see cref="IProjectPropertiesContext"/> for use in the
    /// Project Query API implementation.
    /// </summary>
    /// <remarks>
    /// The point here is to capture the context for a part of a query execution in a
    /// manner that can be passed from one provider to another and is also suitable as a
    /// key into a cache (such as the <see cref="IProjectState"/>).
    /// </remarks>
    internal sealed class QueryProjectPropertiesContext : IProjectPropertiesContext, IEquatable<QueryProjectPropertiesContext>
    {
        /// <summary>
        /// A well-known context representing the project file as a whole.
        /// </summary>
        /// <remarks>
        /// Note that if an <see cref="IProjectPropertiesContext"/> has the <see cref="IProjectPropertiesContext.IsProjectFile"/>
        /// property is set to <c>true</c> and the <see cref="IProjectPropertiesContext.ItemType"/>
        /// and <see cref="IProjectPropertiesContext.ItemName"/> properties are <see langword="null"/>
        /// then the properties system treats it as referring to the project file as a whole
        /// regardless of the <see cref="IProjectPropertiesContext.File"/> property. This
        /// lets us get away with setting it to the empty string and re-using the same
        /// instance across projects.
        /// </remarks>
        public static readonly QueryProjectPropertiesContext ProjectFile = new(isProjectFile: true, file: string.Empty, itemType: null, itemName: null);

        public QueryProjectPropertiesContext(bool isProjectFile, string file, string? itemType, string? itemName)
        {
            IsProjectFile = isProjectFile;
            File = file;
            ItemType = itemType;
            ItemName = itemName;
        }

        public bool IsProjectFile { get; }

        public string File { get; }

        public string? ItemType { get; }

        public string? ItemName { get; }

        public bool Equals(QueryProjectPropertiesContext? other)
        {
            return other is not null
                && IsProjectFile == other.IsProjectFile
                && StringComparers.Paths.Equals(File, other.File)
                && StringComparers.ItemTypes.Equals(ItemType, other.ItemType)
                && StringComparers.ItemNames.Equals(ItemName, other.ItemName);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as QueryProjectPropertiesContext);
        }

        public override int GetHashCode()
        {
            int hashCode = IsProjectFile.GetHashCode();
            hashCode = (hashCode * -1521134295) + StringComparers.Paths.GetHashCode(File);
            
            if (ItemType is not null)
            {
                hashCode = (hashCode * -1521134295) + StringComparers.ItemTypes.GetHashCode(ItemType);
            }
            
            if (ItemName is not null)
            {
                hashCode = (hashCode * -1521134295) + StringComparers.ItemNames.GetHashCode(ItemName);
            }

            return hashCode;
        }

        /// <summary>
        /// Creates a <see cref="QueryProjectPropertiesContext"/> from a Project Query API
        /// <see cref="EntityIdentity"/>.
        /// </summary>
        public static bool TryCreateFromEntityId(EntityIdentity id, [NotNullWhen(true)] out QueryProjectPropertiesContext? propertiesContext)
        {
            if (id.TryGetValue(ProjectModelIdentityKeys.ProjectPath, out string? projectPath))
            {
                id.TryGetValue(ProjectModelIdentityKeys.SourceItemType, out string? itemType);
                id.TryGetValue(ProjectModelIdentityKeys.SourceItemName, out string? itemName);
                propertiesContext = new QueryProjectPropertiesContext(isProjectFile: true, projectPath, itemType, itemName);
                return true;
            }

            propertiesContext = null;
            return false;
        }
    }
}
