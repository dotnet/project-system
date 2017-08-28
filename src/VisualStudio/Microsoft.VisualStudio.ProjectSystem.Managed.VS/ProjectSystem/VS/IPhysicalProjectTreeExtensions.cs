// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.ProjectSystem.VS
{

    internal static class IPhysicalProjectTreeExtensions
    {
        public static bool NodeCanHaveAdditions(this IPhysicalProjectTree tree, IProjectTree node) =>
            tree.TreeProvider.GetAddNewItemDirectory(node) != null;
    }
}
