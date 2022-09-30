' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Shell.Interop

Imports VSLangProj80

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend NotInheritable Class BuildEventsPropPage
        Inherits PropPageUserControlBase

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
        End Sub

        Public Enum Tokens
            OutDir = 0
            ConfigurationName
            ProjectName
            TargetName
            TargetPath
            ProjectPath
            ProjectFileName
            TargetExt
            TargetFileName
            DevEnvDir
            TargetDir
            ProjectDir
            SolutionFileName
            SolutionPath
            SolutionDir
            SolutionName
            PlatformName
            ProjectExt
            SolutionExt
            Tokens_MAX
        End Enum

        Private Shared ReadOnly s_tokenNames() As String = {
            "OutDir",
            "ConfigurationName",
            "ProjectName",
            "TargetName",
            "TargetPath",
            "ProjectPath",
            "ProjectFileName",
            "TargetExt",
            "TargetFileName",
            "DevEnvDir",
            "TargetDir",
            "ProjectDir",
            "SolutionFileName",
            "SolutionPath",
            "SolutionDir",
            "SolutionName",
            "PlatformName",
            "ProjectExt",
            "SolutionExt"
        }

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                    New PropertyControlData(VsProjPropId2.VBPROJPROPID_PreBuildEvent, "PreBuildEvent", txtPreBuildEventCommandLine, ControlDataFlags.None, New Control() {btnPreBuildBuilder, lblPreBuildEventCommandLine}),
                    New PropertyControlData(VsProjPropId2.VBPROJPROPID_PostBuildEvent, "PostBuildEvent", txtPostBuildEventCommandLine, ControlDataFlags.None, New Control() {btnPostBuildBuilder, lblPostBuildEventCommandLine}),
                    New PropertyControlData(VsProjPropId2.VBPROJPROPID_RunPostBuildEvent, "RunPostBuildEvent", cboRunPostBuildEvent, New Control() {lblRunPostBuildEvent})
                    }
                End If

                Return m_ControlData
            End Get
        End Property

        Protected Overrides Function GetF1HelpKeyword() As String
            If IsVBProject() Then
                Return HelpKeywords.VBProjPropBuildEvents
            Else
                Return HelpKeywords.CSProjPropBuildEvents
            End If
        End Function

        Friend Sub EnsureMinimumSize()
            MinimumSize = overarchingTableLayoutPanel.MinimumSize
        End Sub

        Private Sub PostBuildBuilderButton_Click(sender As Object, e As EventArgs) Handles btnPostBuildBuilder.Click
            Dim CommandLineText As String
            CommandLineText = txtPostBuildEventCommandLine.Text

            LaunchEventBuilder(Me, AddressOf GetTokenValue, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_PostBuildCommandLineTitle, CommandLineText)
            Dim oldCommandLine As String = txtPostBuildEventCommandLine.Text
            txtPostBuildEventCommandLine.Text = CommandLineText
            If oldCommandLine <> CommandLineText Then
                SetDirty(txtPostBuildEventCommandLine, True)
            End If
        End Sub

        Private Sub PreBuildBuilderButton_Click(sender As Object, e As EventArgs) Handles btnPreBuildBuilder.Click
            Dim CommandLineText As String
            CommandLineText = txtPreBuildEventCommandLine.Text

            LaunchEventBuilder(Me, AddressOf GetTokenValue, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_PreBuildCommandLineTitle, CommandLineText)
            Dim oldCommandLine As String = txtPreBuildEventCommandLine.Text
            txtPreBuildEventCommandLine.Text = CommandLineText
            If oldCommandLine <> CommandLineText Then
                SetDirty(txtPreBuildEventCommandLine, True)
            End If
        End Sub

        Friend Delegate Function GetTokenValueFunc(MacroName As String) As String

        Private Function LaunchEventBuilder(Parent As BuildEventsPropPage, valueHelper As GetTokenValueFunc, WindowTitleText As String, ByRef CommandLine As String) As Boolean

            Using dialog As New BuildEventCommandLineDialog
                Dim values() As String = Nothing

                dialog.SetFormTitleText(WindowTitleText)
                dialog.EventCommandLine = CommandLine
                dialog.Page = Parent
                dialog.DTE = Parent.DTE

                GetTokenValues(values, valueHelper)
                dialog.SetTokensAndValues(s_tokenNames, values)

                If dialog.ShowDialog(ServiceProvider) = System.Windows.Forms.DialogResult.OK Then
                    CommandLine = dialog.EventCommandLine
                End If
            End Using

            Return True
        End Function

        Friend Shared Function GetTokenValues(ByRef Values() As String, valueHelper As GetTokenValueFunc) As Boolean
            Dim i As Integer
            Values = CType(Array.CreateInstance(GetType(String), Tokens.Tokens_MAX), String())

            For i = 0 To Tokens.Tokens_MAX - 1
                Values(i) = valueHelper(s_tokenNames(i))
            Next

            Return True
        End Function

        Private Function GetTokenValue(MacroName As String) As String
            Dim MacroEval As IVsBuildMacroInfo
            Dim MacroValue As String = Nothing
            Dim Hier As IVsHierarchy = Nothing
            Dim ItemId As UInteger
            Dim ThisObj As Object = m_Objects(0)

            If TypeOf ThisObj Is IVsBrowseObject Then
                VSErrorHandler.ThrowOnFailure(CType(ThisObj, IVsBrowseObject).GetProjectItem(Hier, ItemId))
            ElseIf TypeOf ThisObj Is IVsCfgBrowseObject Then
                VSErrorHandler.ThrowOnFailure(CType(ThisObj, IVsCfgBrowseObject).GetProjectItem(Hier, ItemId))
            End If
            MacroEval = CType(Hier, IVsBuildMacroInfo)
            VSErrorHandler.ThrowOnFailure(MacroEval.GetBuildMacroValue(MacroName, MacroValue))

            Return MacroValue
        End Function

        Private Sub BuildEventsPropPage_SizeChanged(sender As Object, e As EventArgs) Handles MyBase.SizeChanged
            EnsureMinimumSize()
        End Sub
    End Class

End Namespace
