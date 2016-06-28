' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.Win32
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.PlatformUI
Imports VSLangProj80

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Friend Class ReferencePathsPropPage
        Inherits PropPageUserControlBase

        ' We map colors for all bitmap buttons on the page, because the default one is too dark in high-contrast mode, and it is difficult to know whether it is disabled
        Private _moveUpImageOriginal As Image
        Private _moveUpImage As Image
        Private _moveUpGreyImage As Image
        Private _moveDownImageOriginal As Image
        Private _moveDownImage As Image
        Private _moveDownGreyImage As Image
        Private _removeFolderImageOriginal As Image
        Private _removeFolderImage As Image
        Private _removeFolderGreyImage As Image
        Private _inContrastMode As Boolean   ' whether we are in ContrastMode

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            ' Scale buttons
            MoveUp.Size = DpiHelper.LogicalToDeviceUnits(MoveUp.Size)
            MoveDown.Size = DpiHelper.LogicalToDeviceUnits(MoveDown.Size)
            RemoveFolder.Size = DpiHelper.LogicalToDeviceUnits(RemoveFolder.Size)

            'Add any initialization after the InitializeComponent() call
            Me.MinimumSize = Me.Size

            ' Recalculate all images for the button from the default image we put in the resource file
            _moveUpImageOriginal = Me.MoveUp.Image
            _moveDownImageOriginal = Me.MoveDown.Image
            _removeFolderImageOriginal = Me.RemoveFolder.Image

            ' Rescale images
            DpiHelper.LogicalToDeviceUnits(_moveUpImageOriginal)
            DpiHelper.LogicalToDeviceUnits(_moveDownImageOriginal)
            DpiHelper.LogicalToDeviceUnits(_removeFolderImageOriginal)

            GenerateButtonImages()
            UpdateButtonImages()

            AddChangeHandlers()
            EnableReferencePathGroup()

            AddHandler SystemEvents.UserPreferenceChanged, AddressOf Me.SystemEvents_UserPreferenceChanged
        End Sub

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                        New PropertyControlData(VsProjPropId.VBPROJPROPID_ReferencePath, "ReferencePath", Nothing, AddressOf Me.ReferencePathSet, AddressOf Me.ReferencePathGet, ControlDataFlags.PersistedInProjectUserFile)}
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <summary>
        '''  Return true if the page can be resized...
        ''' </summary>
        Public Overrides ReadOnly Property PageResizable() As Boolean
            Get
                Return True
            End Get
        End Property

        Private Function ReferencePathSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            'enable when the enum comes online
            Dim RefPathList As String() = Split(DirectCast(value, String), ";")

            ReferencePath.BeginUpdate()
            Try
                Dim ItemText As String
                ReferencePath.Items.Clear()
                For i As Integer = 0 To RefPathList.Length - 1
                    ItemText = Trim(RefPathList(i))
                    If Len(ItemText) > 0 Then
                        ReferencePath.Items.Add(ItemText)
                    End If
                Next i
            Finally
                ReferencePath.EndUpdate()
            End Try
            Return True
        End Function

        Private Function ReferencePathGetValue() As String
            Dim RefPath As String
            Dim count As Integer = ReferencePath.Items.Count

            If count = 0 Then
                Return ""
            End If

            RefPath = DirectCast(ReferencePath.Items(0), String)
            For i As Integer = 1 To ReferencePath.Items.Count - 1
                RefPath &= ";" & DirectCast(ReferencePath.Items(i), String)
            Next i
            Return RefPath
        End Function

        Private Function ReferencePathGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = ReferencePathGetValue()
            Return True
        End Function

        ''' <summary>
        ''' Customizable processing done after base class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()
            EnableReferencePathGroup()
        End Sub

        Private Function IsValidFolderPath(ByRef Dir As String) As Boolean
            Return System.IO.Directory.Exists(Dir)
        End Function

        Private Sub AddFolder_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AddFolder.Click
            Dim FolderText As String = GetCurrentFolderPathAbsolute()
            If Len(FolderText) > 0 AndAlso ReferencePath.FindStringExact(FolderText) = -1 Then
                If IsValidFolderPath(FolderText) Then
                    Me.ReferencePath.SelectedIndex = Me.ReferencePath.Items.Add(FolderText)
                    SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
                Else
                    ShowErrorMessage(SR.GetString(SR.PPG_InvalidFolderPath))
                End If
            End If
        End Sub

        Private Sub UpdateFolder_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles UpdateFolder.Click
            Dim FolderText As String = GetCurrentFolderPathAbsolute()
            Dim index As Integer = Me.ReferencePath.SelectedIndex

            If index >= 0 AndAlso Len(FolderText) > 0 Then
                If IsValidFolderPath(FolderText) Then
                    Me.ReferencePath.Items(index) = FolderText
                    SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
                    UpdateFolder.Enabled = False
                Else
                    ShowErrorMessage(SR.GetString(SR.PPG_InvalidFolderPath))
                End If
            End If
        End Sub

        Private Sub RemoveFolder_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RemoveFolder.Click
            '
            RemoveCurrentPath()
        End Sub

        Private Sub RemoveCurrentPath()
            Dim SelectedIndex As Integer = ReferencePath.SelectedIndex

            If SelectedIndex >= 0 Then
                ReferencePath.BeginUpdate()
                ReferencePath.Items.RemoveAt(SelectedIndex)
                ReferencePath.EndUpdate()
                SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
            End If
        End Sub

        Private Sub MoveUp_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MoveUp.Click
            '
            Dim SelectedIndex As Integer = ReferencePath.SelectedIndex
            Dim SelectedItem As Object = ReferencePath.SelectedItem

            If SelectedIndex > 0 Then
                ReferencePath.BeginUpdate()
                'To prevent the flashing of the Remove button, Insert new item, change selection, then remove old item
                'Insert item copy
                ReferencePath.Items.Insert(SelectedIndex - 1, SelectedItem)
                'Change selection
                ReferencePath.SelectedIndex = SelectedIndex - 1
                'Remove old copy
                ReferencePath.Items.RemoveAt(SelectedIndex + 1) 'add 1 because of insertion
                ReferencePath.EndUpdate()
                SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
            End If
        End Sub

        Private Sub MoveDown_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MoveDown.Click
            '
            Dim SelectedIndex As Integer = ReferencePath.SelectedIndex
            Dim SelectedItem As Object = ReferencePath.SelectedItem

            If SelectedIndex <> -1 AndAlso SelectedIndex < (ReferencePath.Items.Count - 1) Then
                ReferencePath.BeginUpdate()
                'To prevent the flashing of the Remove button, Insert new item, change selection, then remove old item
                'Insert item copy
                ReferencePath.Items.Insert(SelectedIndex + 2, SelectedItem)
                'Change item selection
                ReferencePath.SelectedIndex = SelectedIndex + 2
                'Remove old location
                ReferencePath.Items.RemoveAt(SelectedIndex) 'add 1 because of insertion
                ReferencePath.EndUpdate()
                SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
            End If
        End Sub

        Private Sub ReferencePath_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ReferencePath.SelectedIndexChanged
            If Not m_fInsideInit Then
                Dim FolderText As String
                Dim SelectedIndex As Integer = Me.ReferencePath.SelectedIndex

                If SelectedIndex = -1 Then
                    FolderText = ""
                Else
                    FolderText = DirectCast(Me.ReferencePath.Items(SelectedIndex), String)
                End If
                If Me.Folder.Text <> FolderText Then
                    Me.Folder.Text = FolderText
                    If Me.Folder.Focused Then
                        'Set caret at end of text
                        Me.Folder.SelectionLength = 0
                        Me.Folder.SelectionStart = FolderText.Length
                    End If
                End If
                Me.EnableReferencePathGroup()
            End If
        End Sub

        ''' <summary>
        '''  process key event on the ReferencePath ListBox
        ''' </summary>
        Private Sub ReferencePath_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles ReferencePath.KeyDown
            If e.KeyCode = Keys.Delete Then
                Dim SelectedIndex As Integer = ReferencePath.SelectedIndex
                If SelectedIndex >= 0 Then
                    RemoveCurrentPath()

                    If SelectedIndex < ReferencePath.Items.Count Then
                        ReferencePath.SelectedIndex = SelectedIndex
                    ElseIf SelectedIndex > 0 Then
                        ReferencePath.SelectedIndex = SelectedIndex - 1
                    End If
                End If
            End If
        End Sub

        Private Sub EnableReferencePathGroup()
            Dim ItemIndices As ListBox.SelectedIndexCollection = Me.ReferencePath.SelectedIndices
            Dim SelectedCount As Integer = ItemIndices.Count
            Dim FolderText As String = GetCurrentFolderPathAbsolute()

            'Enable/Disable RemoveFolder button
            Me.RemoveFolder.Enabled = (SelectedCount > 0)

            'Enable/Disable Add/UpdateFolder buttons
            Dim HasFolderEntry As Boolean = (Len(FolderText) > 0)
            Me.AddFolder.Enabled = HasFolderEntry
            Me.UpdateFolder.Enabled = HasFolderEntry AndAlso (SelectedCount = 1) AndAlso String.Compare(FolderText, DirectCast(Me.ReferencePath.SelectedItem, String), StringComparison.OrdinalIgnoreCase) <> 0

            'Enable/Disable MoveUp/MoveDown buttons
            Me.MoveUp.Enabled = (SelectedCount = 1) AndAlso (ItemIndices.Item(0) > 0)
            Me.MoveDown.Enabled = (SelectedCount = 1) AndAlso (ItemIndices.Item(0) < (ReferencePath.Items.Count - 1))
        End Sub


        ''' <include file='doc\Control.uex' path='docs/doc[@for="Control.ProcessDialogKey"]/*' />
        ''' <summary>
        '''     Processes a dialog key. This method is called during message
        '''     pre-processing to handle dialog characters, such as TAB, RETURN, ESCAPE,
        '''     and arrow keys. This method is called only if the isInputKey() method
        '''     indicates that the control isn't interested in the key.
        ''' processDialogKey() simply sends the character to the parent's
        '''     processDialogKey() method, or returns false if the control has no
        '''     parent. The Form class overrides this method to perform actual
        '''     processing of dialog keys.
        ''' When overriding processDialogKey(), a control should return true to
        '''     indicate that it has processed the key. For keys that aren't processed
        '''     by the control, the result of "base.processDialogKey(...)" should be
        '''     returned.
        ''' Controls will seldom, if ever, need to override this method.
        ''' </summary>
        Protected Overrides Function ProcessDialogKey(ByVal KeyData As Keys) As Boolean
            If (KeyData And (Keys.Alt Or Keys.Control)) = Keys.None Then
                Dim keyCode As Keys = KeyData And Keys.KeyCode
                If keyCode = Keys.Enter Then
                    If ProcessEnterKey() Then
                        Return True
                    End If
                End If
            End If

            Return MyBase.ProcessDialogKey(KeyData)
        End Function


        ''' <summary>
        ''' Processes the ENTER key for this dialog.  We use this instead of KeyPress/Down events
        '''   because the OK key on the modal dialog base (PropPageHostDialog) grabs the ENTER key
        '''   and uses it to shut down the dialog.
        ''' </summary>
        ''' <returns>True iff the ENTER key is actually used.  False indicates it should be allowed
        '''   to be passed along and processed normally.</returns>
        ''' <remarks></remarks>
        Private Function ProcessEnterKey() As Boolean
            'If the focus is on the Folder textbox, and the AddFolder button is enabled, then 
            '  we interpret ENTER as meaning, "Add this folder", i.e., click on the AddFolder button.
            If ActiveControl Is Me.Folder AndAlso AddFolder.Enabled Then
                AddFolder.PerformClick()
                Return True
            End If

            Return False
        End Function

        Private Sub Folder_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Folder.TextChanged
            If Not m_fInsideInit Then
                EnableReferencePathGroup()
            End If
        End Sub

        ''' <summary>
        ''' Gets the absolute path to the path currently in the Folder textbox.  If the path is invalid (contains bad
        '''   characters, etc.), returns simply the current text.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetCurrentFolderPathAbsolute() As String
            Dim FolderText As String = Trim(Folder.Text)
            If FolderText.Length > 0 Then
                Try
                    'Interpret as relative to the project path, and make it absolute
                    FolderText = IO.Path.Combine(GetProjectPath(), FolderText)
                    FolderText = Utils.AppendBackslash(FolderText)
                Catch ex As Exception When Common.Utils.ReportWithoutCrash(ex, NameOf(GetCurrentFolderPathAbsolute), NameOf(ReferencePathsPropPage))
                End Try
            End If

            Return FolderText
        End Function

        Private Sub FolderBrowse_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles FolderBrowse.Click
            Dim value As String = Nothing
            If GetDirectoryViaBrowse(GetCurrentFolderPathAbsolute(), SR.GetString(SR.PPG_SelectReferencePath), value) Then
                Folder.Text = GetProjectRelativeDirectoryPath(value)
            End If
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            If IsJSProject() Then
                Return HelpKeywords.JSProjPropReferencePaths
            ElseIf IsCSProject() Then
                Return HelpKeywords.CSProjPropReferencePaths
            Else
                Debug.Assert(IsVBProject, "Unknown project type")
                Return HelpKeywords.VBProjPropReferencePaths
            End If
        End Function

        '''<summary>
        ''' Handle button Enabled property changing event to reset its image
        '''</summary>
        Private Sub GraphicButton_OnEnabledChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles MoveUp.EnabledChanged, MoveDown.EnabledChanged, RemoveFolder.EnabledChanged
            UpdateButtonImages()
        End Sub

        '''<summary>
        ''' We change the image when button is disabled/enabled.
        ''' WinForm could generate default image for disabled button, but in high-contrast mode, it didn't work very well for our buttons
        '''</summary>
        Private Sub UpdateButtonImages()
            If MoveUp.Enabled Then
                MoveUp.Image = _moveUpImage
            Else
                MoveUp.Image = _moveUpGreyImage
            End If

            If MoveDown.Enabled Then
                MoveDown.Image = _moveDownImage
            Else
                MoveDown.Image = _moveDownGreyImage
            End If

            If RemoveFolder.Enabled Then
                RemoveFolder.Image = _removeFolderImage
            Else
                RemoveFolder.Image = _removeFolderGreyImage
            End If
        End Sub

        ''' <summary>
        '''  Generate button images in different system setting.
        ''' WinForm could generate default image for disabled button, but in high-contrast mode, it didn't work very well for our buttons
        ''' </summary>
        Private Sub GenerateButtonImages()
            Dim greyColor As Color = SystemColors.ControlDark

            If SystemInformation.HighContrast Then
                _inContrastMode = True
                greyColor = SystemColors.Control
            Else
                _inContrastMode = False
            End If

            Dim originalImage As Image = _moveUpImageOriginal
            _moveUpImage = Utils.MapBitmapColor(originalImage, Color.Black, SystemColors.ControlText)
            _moveUpGreyImage = Utils.MapBitmapColor(originalImage, Color.Black, greyColor)

            originalImage = _moveDownImageOriginal
            _moveDownImage = Utils.MapBitmapColor(originalImage, Color.Black, SystemColors.ControlText)
            _moveDownGreyImage = Utils.MapBitmapColor(originalImage, Color.Black, greyColor)

            originalImage = _removeFolderImageOriginal
            _removeFolderImage = Utils.MapBitmapColor(originalImage, Color.Black, SystemColors.ControlText)
            _removeFolderGreyImage = Utils.MapBitmapColor(originalImage, Color.Black, greyColor)
        End Sub

        ''' <summary>
        '''  Handle SystemEvents, so we will update Buttom image when SystemColor was changed...
        ''' </summary>
        Private Sub SystemEvents_UserPreferenceChanged(ByVal sender As Object, ByVal e As UserPreferenceChangedEventArgs)
            Select Case e.Category
                Case UserPreferenceCategory.Accessibility
                    If _inContrastMode <> SystemInformation.HighContrast Then
                        GenerateButtonImages()
                        UpdateButtonImages()
                    End If
                Case UserPreferenceCategory.Color
                    GenerateButtonImages()
                    UpdateButtonImages()
            End Select
        End Sub

    End Class

End Namespace
