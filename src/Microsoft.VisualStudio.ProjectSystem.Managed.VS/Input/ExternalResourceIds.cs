// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.Input
{
    // from VS: src\vsproject\cool\coolpkg\resource.h
    internal enum LegacyCSharpStringResourceIds : uint
    {
        IDS_TEMPLATE_NEWWFCWIN32FORM = 2237,
        IDS_TEMPLATE_DIRLOCALITEMS = 2339,
        IDS_TEMPLATE_NEWCSharpCLASS = 2245,
        IDS_TEMPLATE_NEWWFCCOMPONENT = 2246,
        IDS_TEMPLATE_NEWUSERCONTROL = 2295,
        IDS_PROJECTITEMTYPE_STR = 2346,
    }

    // from VS: src\vsproject\vb\vbprj\vbprjstr.h
    internal enum LegacyVBStringResourceIds : uint
    {
        IDS_VSDIR_ITEM_CLASS = 3020,
        IDS_VSDIR_ITEM_COMPONENT = 3024,
        IDS_VSDIR_ITEM_MODULE = 3028,
        IDS_VSDIR_ITEM_USERCTRL = 3048,
        IDS_VSDIR_ITEM_WINFORM = 3050,
        IDS_VSDIR_CLIENTPROJECTITEMS = 3081,
        IDS_VSDIR_VBPROJECTFILES = 3082,
    }

    // from VS: src\vsproject\fidalgo\WPF\Flavor\WPFFlavor\WPFProject.cs
    internal enum WPFTemplateNames : uint
    {
        WPFPage = 4658,
        WPFResourceDictionary = 4662,
        WPFUserControl = 4664,
        WPFWindow = 4666,
    }
}
