' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.AppDesCommon
Imports System.Windows.Forms

Public Class VSThemedLinkLabel
    Inherits LinkLabel

    Private _vsThemedLinkColor As Color
    Private _vsThemedLinkColorHover As Color

    Public Sub New()
        MyBase.New()

        _vsThemedLinkColor = LinkColor
        _vsThemedLinkColorHover = LinkColor

    End Sub

    Public Sub SetThemedColor(vsUIShell5 As IVsUIShell5)

        Dim environmentThemeCategory As Guid = New Guid("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d")

        ' The default value is the actual value of DiagReportLinkTextHover and DiagReportLinkText defined by Dev11
        _vsThemedLinkColorHover = ShellUtil.GetDesignerThemeColor(vsUIShell5, environmentThemeCategory, "PanelHyperlinkHover", __THEMEDCOLORTYPE.TCT_Background, Color.FromArgb(&HFF1382CE))
        _vsThemedLinkColor = ShellUtil.GetDesignerThemeColor(vsUIShell5, environmentThemeCategory, "PanelHyperlink", __THEMEDCOLORTYPE.TCT_Background, Color.FromArgb(&HFF1382CE))

        ActiveLinkColor = ShellUtil.GetDesignerThemeColor(vsUIShell5, environmentThemeCategory, "PanelHyperlinkPressed", __THEMEDCOLORTYPE.TCT_Background, Color.FromArgb(&HFF1382CE))
        LinkColor = _vsThemedLinkColor
        LinkBehavior = LinkBehavior.HoverUnderline
    End Sub

    Private Sub VsThemedLinkLabel_MouseEnter(sender As Object, e As EventArgs) Handles MyBase.MouseEnter
        LinkColor = _vsThemedLinkColorHover
    End Sub

    Private Sub VsThemedLinkLabel_MouseLeave(sender As Object, e As EventArgs) Handles MyBase.MouseLeave
        LinkColor = _vsThemedLinkColor
    End Sub
End Class
