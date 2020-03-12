' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Drawing
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop

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
        SetThemedColor(vsUIShell5, supportsTheming:=True)
    End Sub

    Public Sub SetThemedColor(vsUIShell5 As IVsUIShell5, supportsTheming As Boolean)
        If supportsTheming Then
            _vsThemedLinkColorHover = ShellUtil.GetEnvironmentThemeColor(vsUIShell5, "PanelHyperlinkHover", __THEMEDCOLORTYPE.TCT_Background, SystemColors.HotTrack)
            _vsThemedLinkColor = ShellUtil.GetEnvironmentThemeColor(vsUIShell5, "PanelHyperlink", __THEMEDCOLORTYPE.TCT_Background, SystemColors.HotTrack)
            ActiveLinkColor = ShellUtil.GetEnvironmentThemeColor(vsUIShell5, "PanelHyperlinkPressed", __THEMEDCOLORTYPE.TCT_Background, SystemColors.HotTrack)
        Else
            _vsThemedLinkColorHover = SystemColors.HotTrack
            _vsThemedLinkColor = SystemColors.HotTrack
            ActiveLinkColor = SystemColors.HotTrack
        End If

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
