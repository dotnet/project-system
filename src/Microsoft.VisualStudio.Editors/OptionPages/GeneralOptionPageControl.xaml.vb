' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows
Imports System.Windows.Controls

Namespace Microsoft.VisualStudio.Editors.OptionPages
    ''' <summary>
    ''' Implements the UI for the Tools | Options | Projects and Solutions | SDK-Style Projects page.
    ''' </summary>
    Partial Friend NotInheritable Class GeneralOptionPageControl
        Inherits UserControl

        ''' <summary>
        ''' The values shown in the Log Level drop down menu.
        ''' </summary>
        Public Shared ReadOnly FastUpToDateLogLevelItemSource As String() = {
            My.Resources.GeneralOptionPageResources.General_FastUpToDateCheck_LogLevel_None,
            My.Resources.GeneralOptionPageResources.General_FastUpToDateCheck_LogLevel_Minimal,
            My.Resources.GeneralOptionPageResources.General_FastUpToDateCheck_LogLevel_Info,
            My.Resources.GeneralOptionPageResources.General_FastUpToDateCheck_LogLevel_Verbose
        }

        Public Sub New()
            Dim labelStyle = New Style(GetType(Label))
            labelStyle.Setters.Add(New Setter(ForegroundProperty, New DynamicResourceExtension(SystemColors.WindowTextBrushKey)))
            labelStyle.Setters.Add(New Setter(MarginProperty, New Thickness(left:=0, top:=7, right:=0, bottom:=7)))
            Resources.Add(GetType(Label), labelStyle)

            Dim checkBoxStyle = New Style(GetType(CheckBox))
            checkBoxStyle.Setters.Add(New Setter(ForegroundProperty, New DynamicResourceExtension(SystemColors.WindowTextBrushKey)))
            checkBoxStyle.Setters.Add(New Setter(MarginProperty, New Thickness(left:=0, top:=7, right:=0, bottom:=7)))
            Resources.Add(GetType(CheckBox), checkBoxStyle)

            Dim comboBoxStyle = New Style(GetType(ComboBox))
            comboBoxStyle.Setters.Add(New Setter(ForegroundProperty, New DynamicResourceExtension(SystemColors.WindowTextBrushKey)))
            comboBoxStyle.Setters.Add(New Setter(MarginProperty, New Thickness(left:=0, top:=7, right:=0, bottom:=7)))
            Resources.Add(GetType(ComboBox), comboBoxStyle)

            Dim groupBoxStyle = New Style(GetType(GroupBox))
            groupBoxStyle.Setters.Add(New Setter(ForegroundProperty, New DynamicResourceExtension(SystemColors.WindowTextBrushKey)))
            groupBoxStyle.Setters.Add(New Setter(MarginProperty, New Thickness(left:=0, top:=0, right:=0, bottom:=3)))
            groupBoxStyle.Setters.Add(New Setter(PaddingProperty, New Thickness(left:=7, top:=7, right:=7, bottom:=0)))
            Resources.Add(GetType(GroupBox), groupBoxStyle)

            InitializeComponent()
        End Sub

    End Class
End Namespace
