' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports System.Drawing

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend NotInheritable Class BuildEventCommandLineDialog
        Inherits Form

        Private Shared m_DefaultInstance As BuildEventCommandLineDialog
        Private Shared m_SyncObject As New Object
        Private m_CommandLine As String
        Private m_Tokens() As String
        Private m_Values() As String
        Private m_DTE As EnvDTE.DTE
        Private m_serviceProvider As IServiceProvider
        Private m_Page As PropPageUserControlBase
        Private m_szIntialFormSize As Size
        Private m_helpTopic As String

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            'Apply Vista Theme to list view
            Common.DTEUtils.ApplyListViewThemeStyles(TokenList.Handle)

            'When we load the macros panel is hidden so don't show the Insert button
            SetInsertButtonState(False)

        End Sub

        Public Function SetFormTitleText(TitleText As String) As Boolean
            Text = TitleText
            Return True
        End Function

        Public Function SetTokensAndValues(Tokens() As String, Values() As String) As Boolean
            m_Tokens = Tokens
            m_Values = Values

            Return ParseAndPopulateTokens()
        End Function

        Public WriteOnly Property DTE() As EnvDTE.DTE
            Set(Value As EnvDTE.DTE)
                m_DTE = Value
            End Set
        End Property

        Public WriteOnly Property Page() As PropPageUserControlBase
            Set(Value As PropPageUserControlBase)
                m_Page = Value
            End Set
        End Property

        Public Property EventCommandLine() As String
            Get
                Return m_CommandLine
            End Get
            Set(Value As String)
                m_CommandLine = Value
                CommandLine.Text = m_CommandLine

                CommandLine.Focus()
                CommandLine.SelectedText = ""
                CommandLine.SelectionStart = Len(m_CommandLine)
                CommandLine.SelectionLength = 0
            End Set
        End Property

        Public Property HelpTopic() As String
            Get
                If m_helpTopic Is Nothing Then
                    If m_Page IsNot Nothing AndAlso m_Page.IsVBProject() Then
                        m_helpTopic = HelpKeywords.VBProjPropBuildEventsBuilder
                    Else
                        m_helpTopic = HelpKeywords.CSProjPropBuildEventsBuilder
                    End If
                End If

                Return m_helpTopic
            End Get
            Set(value As String)
                m_helpTopic = value
            End Set
        End Property

        Private Property ServiceProvider() As IServiceProvider
            Get
                If m_serviceProvider Is Nothing AndAlso m_DTE IsNot Nothing Then
                    Dim isp As OLE.Interop.IServiceProvider = CType(m_DTE, OLE.Interop.IServiceProvider)
                    If isp IsNot Nothing Then
                        m_serviceProvider = New Shell.ServiceProvider(isp)
                    End If
                End If
                Return m_serviceProvider
            End Get
            Set(value As IServiceProvider)
                m_serviceProvider = value
            End Set
        End Property

        Private Sub OKButton_Click(sender As System.Object, e As EventArgs) Handles OKButton.Click
            '// Store the command line
            m_CommandLine = CommandLine.Text

            Close()
        End Sub

        Private Sub CancelButton_Click(sender As System.Object, e As EventArgs) Handles Cancel_Button.Click
            Close()
        End Sub

        Private Sub UpdateDialog_HelpButtonClicked(sender As System.Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            InvokeHelp()
            e.Cancel = True
        End Sub

        Private Function ParseAndPopulateTokens() As Boolean
            '// Walk through the array and add each row to the listview
            Dim i As Integer
            Dim NameItem As ListViewItem

            For i = 0 To m_Tokens.Length - 1
                NameItem = New ListViewItem(m_Tokens(i))

                NameItem.SubItems.Add(m_Values(i))
                TokenList.Items.Add(NameItem)
            Next

            Return True
        End Function

        Private Sub HideMacrosButton_Click(sender As System.Object, e As EventArgs) Handles HideMacrosButton.Click
            ShowCollapsedForm()
        End Sub

        Private Sub ShowMacrosButton_Click(sender As Object, e As EventArgs) Handles ShowMacrosButton.Click
            ShowExpandedForm()
        End Sub

        Private Sub BuildEventCommandLineDialog_Load(sender As System.Object, e As EventArgs) Handles MyBase.Load
            InitializeControlLocations()

            '// Never let them resize to something smaller than the default form size
            MinimumSize = Size
        End Sub

        Private Function InitializeControlLocations() As Boolean
            ShowCollapsedForm()
        End Function

        Private Function ShowCollapsedForm() As Boolean
            '// Show the ShowMacros button
            ShowMacrosButton.Visible = True

            MacrosPanel.Visible = False
            overarchingTableLayoutPanel.RowStyles.Item(1).SizeType = SizeType.AutoSize
            Height = Height - MacrosPanel.Height

            '// Disable and hide the Insert button
            SetInsertButtonState(False)

            Return True
        End Function

        Private Function ShowExpandedForm() As Boolean
            '// Hide this button
            ShowMacrosButton.Visible = False

            MacrosPanel.Visible = True
            overarchingTableLayoutPanel.RowStyles.Item(1).SizeType = SizeType.Percent
            Height = Height + MacrosPanel.Height

            '// Show the Insert button
            SetInsertButtonState(True)
            Return True
        End Function

        Private Sub InsertButton_Click(sender As System.Object, e As EventArgs) Handles InsertButton.Click
            AddCurrentMacroToCommandLine()
        End Sub

        Private Sub TokenList_SelectedIndexChanged(sender As System.Object, e As EventArgs) Handles TokenList.SelectedIndexChanged
            SetInsertButtonEnableState()
        End Sub



        Private Sub TokenList_DoubleClick(sender As Object, e As EventArgs) Handles TokenList.DoubleClick
            AddCurrentMacroToCommandLine()
        End Sub

        Private Function AddCurrentMacroToCommandLine() As Boolean
            Dim selectedRowsCollection As ListView.SelectedListViewItemCollection
            Dim selectedItem As ListViewItem
            Dim textToInsertStringBuilder As StringBuilder = New StringBuilder()

            selectedRowsCollection = TokenList.SelectedItems
            For Each selectedItem In selectedRowsCollection
                textToInsertStringBuilder.Append("$(" + selectedItem.Text + ")")
            Next

            CommandLine.SelectedText = textToInsertStringBuilder.ToString()

            Return True
        End Function

        Private Sub InvokeHelp()
            If Not IsNothing(m_Page) Then
                m_Page.Help(HelpTopic)
            Else
                ' NOTE: the m_Page is nothing for deploy project, we need keep those code ...
                Try
                    Dim sp As IServiceProvider = ServiceProvider
                    If sp IsNot Nothing Then
                        Dim vshelp As VSHelp.Help = CType(sp.GetService(GetType(VSHelp.Help)), VSHelp.Help)
                        vshelp.DisplayTopicFromF1Keyword(HelpTopic)
                    Else
                        Debug.Fail("Can not find ServiceProvider")
                    End If

                Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(InvokeHelp), NameOf(BuildEventCommandLineDialog))
                End Try
            End If
        End Sub

        Private Sub BuildEventCommandLineDialog_HelpRequested(sender As System.Object, hlpevent As HelpEventArgs) Handles MyBase.HelpRequested
            InvokeHelp()
        End Sub

        Private Function SetInsertButtonEnableState() As Boolean
            Dim selectedRowsCollection As ListView.SelectedListViewItemCollection

            selectedRowsCollection = TokenList.SelectedItems
            If selectedRowsCollection.Count > 0 Then
                InsertButton.Enabled = True
            Else
                InsertButton.Enabled = False
            End If
        End Function

        Private Function SetInsertButtonState(bEnable As Boolean) As Boolean
            'Me.InsertButton.Enabled = bEnable
            SetInsertButtonEnableState()

            InsertButton.Visible = bEnable
            Return True
        End Function

        ''' <Summary>
        ''' We shadow the original ShowDialog, because the right way to show dialog in VS is to use the IUIService. So the font/size will be set correctly.
        ''' The caller should pass a valid serviceProvider here. The dialog also hold it to invoke the help system
        ''' </Summary>
        Public Shadows Function ShowDialog(sp As IServiceProvider) As DialogResult
            If sp IsNot Nothing Then
                ServiceProvider = sp
            End If

            If ServiceProvider IsNot Nothing Then
                Dim uiService As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                If uiService IsNot Nothing Then
                    Return uiService.ShowDialog(Me)
                End If
            End If
            Return MyBase.ShowDialog()
        End Function
    End Class
End Namespace
