' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports Microsoft.VisualStudio.Editors.AppDesInterop

Namespace Microsoft.VisualStudio.Editors.AppDesCommon

    Friend Class WmUserConstants
        Public Const WM_REFPAGE_REFERENCES_REFRESH As Integer = Win32Constant.WM_USER + 21
        Public Const WM_REFPAGE_IMPORTCHANGED As Integer = Win32Constant.WM_USER + 22
        Public Const WM_REFPAGE_IMPORTS_REFRESH As Integer = Win32Constant.WM_USER + 24
        Public Const WM_PAGE_POSTVALIDATION As Integer = Win32Constant.WM_USER + 25
        Public Const WM_UPDATE_PROPERTY_GRID As Integer = Win32Constant.WM_USER + 26
        Public Const WM_REFPAGE_SERVICEREFERENCES_REFRESH As Integer = Win32Constant.WM_USER + 27
    End Class

End Namespace

