// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Models
{
    internal static class IDependencyModelExtensions
    {
        public static IDependencyViewModel ToViewModel(this IDependencyModel self, bool hasUnresolvedDependency)
        {
            return new DependencyViewModel
            {
                Caption = self.Caption,
                FilePath = self.Id,
                SchemaName = self.SchemaName,
                SchemaItemType = self.SchemaItemType,
                Priority = self.Priority,
                Icon = hasUnresolvedDependency ? self.UnresolvedIcon : self.Icon,
                ExpandedIcon = hasUnresolvedDependency ? self.UnresolvedExpandedIcon : self.ExpandedIcon,
                Properties = self.Properties,
                Flags = self.Flags
            };
        }
    }
}
