// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.Web.Tree
{
    internal static class SpecialWebFolderFlag
    {
        public static readonly ProjectTreeFlags CodeFolder = ProjectTreeFlags.Create("App_Code");
        public static readonly ProjectTreeFlags BinFolder = ProjectTreeFlags.Create("AspNet_Bin");
        public static readonly ProjectTreeFlags GlobalResourcesFolder = ProjectTreeFlags.Create("App_GlobalResources");
        public static readonly ProjectTreeFlags DataFolder = ProjectTreeFlags.Create("App_Data");
        public static readonly ProjectTreeFlags ThemesFolder = ProjectTreeFlags.Create("App_Themes");
        public static readonly ProjectTreeFlags BrowsersFolder = ProjectTreeFlags.Create("App_Browsers");
        public static readonly ProjectTreeFlags LocalResourcesFolder = ProjectTreeFlags.Create("App_LocalResources");
    }
}
