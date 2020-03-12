// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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
