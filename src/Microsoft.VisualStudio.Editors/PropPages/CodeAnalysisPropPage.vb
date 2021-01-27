' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend NotInheritable Class CodeAnalysisPropPage
        Inherits PropPageUserControlBase

        Private Const RoslynAnalyzersDocumentationLink As String = "https://docs.microsoft.com/visualstudio/code-quality/roslyn-analyzers-overview"
        Private Const NETAnalyzersDocumentationLink As String = "https://aka.ms/dotnetanalyzers"

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()

            PopulateAnalysisLevelComboBoxItems()
        End Sub

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                        New PropertyControlData(1, "RunAnalyzersDuringBuild", RunAnalyzersDuringBuild, ControlDataFlags.None),
                        New PropertyControlData(2, "RunAnalyzersDuringLiveAnalysis", RunAnalyzersDuringLiveAnalysis, ControlDataFlags.PersistedInProjectUserFile),
                        New PropertyControlData(3, "EnforceCodeStyleInBuild", EnforceCodeStyleInBuildCheckBox, ControlDataFlags.None),
                        New PropertyControlData(4, "EnableNETAnalyzers", EnableNETAnalyzersCheckBox, ControlDataFlags.None),
                        New PropertyControlData(5, "AnalysisLevel", AnalysisLevelComboBox, AddressOf AnalysisLevelSet, AddressOf AnalysisLevelGet, ControlDataFlags.None)
                    }
                End If
                Return m_ControlData
            End Get
        End Property

        Private Function AnalysisLevelSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            Dim selectionValue As String = CType(value, String)

            If selectionValue <> "" Then
                For index = 0 To AnalysisLevelComboBox.Items.Count - 1
                    Dim item = AnalysisLevelComboBox.Items(index)
                    Dim itemText = AnalysisLevelComboBox.GetItemText(item)
                    If StringComparers.SettingNames.Equals(itemText, selectionValue) Then
                        AnalysisLevelComboBox.SelectedIndex = index
                    End If
                Next
            Else
                AnalysisLevelComboBox.SelectedIndex = 1
            End If
        End Function

        Private Function AnalysisLevelGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = AnalysisLevelComboBox.Text
            Return True
        End Function

        Protected Overrides Function GetF1HelpKeyword() As String
            ' TODO: New help keyword
            Return HelpKeywords.VBProjPropAssemblyInfo
        End Function

        Private Sub RoslynAnalyzersLabel_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles RoslynAnalyzersHelpLinkLabel.LinkClicked
            RoslynAnalyzersHelpLinkLabel.LinkVisited = True
            Process.Start(RoslynAnalyzersDocumentationLink)
        End Sub

        Private Sub NETAnalyzersLinkLabel_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles NETAnalyzersLinkLabel.LinkClicked
            NETAnalyzersLinkLabel.LinkVisited = True
            Process.Start(NETAnalyzersDocumentationLink)
        End Sub

        Private Sub PopulateAnalysisLevelComboBoxItems()
            If AnalysisLevelComboBox.Items.Count = 0 Then
                AnalysisLevelComboBox.Items.Add(My.Resources.Strings.preview)
                AnalysisLevelComboBox.Items.Add(My.Resources.Strings.latest)
                AnalysisLevelComboBox.Items.Add("5.0")
                AnalysisLevelComboBox.Items.Add(My.Resources.Strings.none)
            End If
        End Sub

    End Class

End Namespace
