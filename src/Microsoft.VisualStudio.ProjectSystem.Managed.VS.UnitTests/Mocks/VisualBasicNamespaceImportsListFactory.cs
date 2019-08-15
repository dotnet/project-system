// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Automation.VisualBasic
{
    internal static class VisualBasicNamespaceImportsListFactory
    {
        public static VisualBasicNamespaceImportsList CreateInstance(params string[] list)
        {
            var newList = new VisualBasicNamespaceImportsList();
            newList.SetList(list.ToList());

            return newList;
        }
    }
}
