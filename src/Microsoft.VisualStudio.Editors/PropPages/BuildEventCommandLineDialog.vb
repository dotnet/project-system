' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Text
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend NotInheritable Class BuildEventCommandLineDialog
        Inherits Form

        Private _eventCommandLine As String
        Private _tokens() As String
        Private _values() As String
        Private _dte As EnvDTE.DTE
        Private _serviceProvider As IServiceProvider
        Private _page As PropPageUserControlBase
        Private _helpTopic As String

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
            _tokens = Tokens
            _values = Values

            Return ParseAndPopulateTokens()
        End Function

        Public WriteOnly Property DTE As EnvDTE.DTE
            Set
                _dte = Value
            End Set
        End Property

        Public WriteOnly Property Page As PropPageUserControlBase
            Set
                _page = Value
            End Set
        End Property

        Public Property EventCommandLine As String
            Get
                Return _eventCommandLine
            End Get
            Set
                _eventCommandLine = Value
                CommandLine.Text = _eventCommandLine

                CommandLine.Focus()
                CommandLine.SelectedText = ""
                CommandLine.SelectionStart = Len(_eventCommandLine)
                CommandLine.SelectionLength = 0
            End Set
        End Property

        Public Property HelpTopic As String
            Get
                If _helpTopic Is Nothing Then
                    If _page IsNot Nothing AndAlso _page.IsVBProject() Then
                        _helpTopic = HelpKeywords.VBProjPropBuildEventsBuilder
                    Else
                        _helpTopic = HelpKeywords.CSProjPropBuildEventsBuilder
                    End If
                End If

                Return _helpTopic
            End Get
            Set
                _helpTopic = value
            End Set
        End Property

        Private Property ServiceProvider As IServiceProvider
            Get
                If _serviceProvider Is Nothing AndAlso _dte IsNot Nothing Then
                    Dim isp As OLE.Interop.IServiceProvider = CType(_dte, OLE.Interop.IServiceProvider)
                    If isp IsNot Nothing Then
                        _serviceProvider = New Shell.ServiceProvider(isp)
                    End If
                End If
                Return _serviceProvider
            End Get
            Set
                _serviceProvider = value
            End Set
        End Property

        Private Sub OKButton_Click(sender As Object, e As EventArgs) Handles OKButton.Click
            ' Store the command line
            _eventCommandLine = CommandLine.Text

            Close()
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As EventArgs) Handles Cancel_Button.Click
            Close()
        End Sub

        Private Sub UpdateDialog_HelpButtonClicked(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.HelpButtonClicked
            InvokeHelp()
            e.Cancel = True
        End Sub

        Private Function ParseAndPopulateTokens() As Boolean
            ' Walk through the array and add each row to the listview
            Dim i As Integer
            Dim NameItem As ListViewItem

            For i = 0 To _tokens.Length - 1
                NameItem = New ListViewItem(_tokens(i))

                NameItem.SubItems.Add(_values(i))
                TokenList.Items.Add(NameItem)
            Next

            Return True
        End Function

        Private Sub HideMacrosButton_Click(sender As Object, e As EventArgs) Handles HideMacrosButton.Click
            ShowCollapsedForm()
        End Sub

        Private Sub ShowMacrosButton_Click(sender As Object, e As EventArgs) Handles ShowMacrosButton.Click
            ShowExpandedForm()
        End Sub

        Private Sub BuildEventCommandLineDialog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            InitializeControlLocations()

            ' Never let them resize to something smaller than the default form size
            MinimumSize = Size
        End Sub

        Private Function InitializeControlLocations() As Boolean
            ShowCollapsedForm()
        End Function

        Private Function ShowCollapsedForm() As Boolean
            ' Show the ShowMacros button
            ShowMacrosButton.Visible = True

            MacrosPanel.Visible = False
            overarchingTableLayoutPanel.RowStyles.Item(1).SizeType = SizeType.AutoSize
            Height -= MacrosPanel.Height

            ShowMacrosButton.Focus()

            ' Disable and hide the Insert button
            SetInsertButtonState(False)

            Return True
        End Function

        Private Function ShowExpandedForm() As Boolean
            ' Hide this button
            ShowMacrosButton.Visible = False

            MacrosPanel.Visible = True
            overarchingTableLayoutPanel.RowStyles.Item(1).SizeType = SizeType.Percent
            Height += MacrosPanel.Height

            HideMacrosButton.Focus()

            ' Show the Insert button
            SetInsertButtonState(True)
            Return True
        End Function

        Private Sub InsertButton_Click(sender As Object, e As EventArgs) Handles InsertButton.Click
            AddCurrentMacroToCommandLine()
        End Sub

        Private Sub TokenList_SelectedIndexChanged(sender As Object, e As EventArgs) Handles TokenList.SelectedIndexChanged
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
            If Not IsNothing(_page) Then
                _page.Help(HelpTopic)
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

        Private Sub BuildEventCommandLineDialog_HelpRequested(sender As Object, hlpevent As HelpEventArgs) Handles MyBase.HelpRequested
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

        ''' <summary>
        ''' We shadow the original ShowDialog, because the right way to show dialog in VS is to use the IUIService. So the font/size will be set correctly.
        ''' The caller should pass a valid serviceProvider here. The dialog also hold it to invoke the help system
        ''' </summary>
        Public Shadows Function ShowDialog(sp As IServiceProvider) As DialogResult
            If sp IsNot Nothing Then
                ServiceProvider = sp
            End If
            Using DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)
                If ServiceProvider IsNot Nothing Then
                    Dim uiService As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                    If uiService IsNot Nothing Then
                        Return uiService.ShowDialog(Me)
                    End If
                End If
                Return MyBase.ShowDialog()
            End Using
        End Function
    End Class
End Namespace
