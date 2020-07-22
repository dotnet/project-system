' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend NotInheritable Class CodeAnalysisPropPage
        Inherits PropPageUserControlBase

        Private Const RoslynAnalyzersDocumentationLink As String = "https://docs.microsoft.com/visualstudio/code-quality/roslyn-analyzers-overview"

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
        End Sub

        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()
        End Sub

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                        New PropertyControlData(1, "RunAnalyzersDuringBuild", RunAnalyzersDuringBuild, ControlDataFlags.None),
                        New PropertyControlData(2, "RunAnalyzersDuringLiveAnalysis", RunAnalyzersDuringLiveAnalysis, ControlDataFlags.PersistedInProjectUserFile)
                    }
                End If
                Return m_ControlData
            End Get
        End Property

        Protected Overrides Function GetF1HelpKeyword() As String
            ' TODO: New help keyword
            Return HelpKeywords.VBProjPropAssemblyInfo
        End Function

        Private Sub RoslynAnalyzersLabel_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles RoslynAnalyzersHelpLinkLabel.LinkClicked
            RoslynAnalyzersHelpLinkLabel.LinkVisited = True
            Process.Start(RoslynAnalyzersDocumentationLink)
        End Sub

    End Class

End Namespace
