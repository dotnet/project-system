// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    /// <summary>
    /// Common strings found as values for the
    /// <see cref="Microsoft.Build.Framework.XamlTypes.DataSource.SourceType"/>
    /// property.
    /// </summary>
    internal static class RuleDataSourceTypes
    {
        /// <summary>
        /// All resolved Dependency nodes must have persistence "ResolvedReference" to be able to 
        /// differentiate between unresolved and resolved rule.
        /// </summary>
        internal const string PersistenceResolvedReference = "ResolvedReference";

        /// <summary>
        /// The data comes from items or item metadata in the project.
        /// </summary>
        internal const string Item = nameof(Item);

        /// <summary>
        /// The data comes from item definition metadata in the project.
        /// </summary>
        internal const string ItemDefinition = nameof(ItemDefinition);

        /// <summary>
        /// The data comes from project properties.
        /// </summary>
        internal const string Property = nameof(Property);
    }
}
