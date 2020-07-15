' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Forms

Imports VSLangProj80
Imports VSLangProj165

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Friend Class DebugPropPage
        Inherits PropPageUserControlBase

        Private _controlGroup As Control()()

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False
        End Sub

#Region "Class MultilineTextBoxRejectsEnter"

        ''' <summary>
        ''' A multi-line textbox control which does not accept ENTER as valid input.
        ''' </summary>
        Friend Class MultilineTextBoxRejectsEnter
            Inherits TextBox

            Public Sub New()
                Multiline = True
            End Sub

            Protected Overrides Function IsInputChar(charCode As Char) As Boolean
                If charCode = vbLf OrElse charCode = vbCr Then
                    Return False
                End If

                Return MyBase.IsInputChar(charCode)
            End Function

            Protected Overrides Function ProcessDialogChar(charCode As Char) As Boolean
                If charCode = vbLf OrElse charCode = vbCr Then
                    Return True
                End If

                Return MyBase.ProcessDialogChar(charCode)
            End Function
        End Class

#End Region

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)
                    Dim data As PropertyControlData

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartURL, "StartURL", StartURL, ControlDataFlags.PersistedInProjectUserFile) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_StartURL
                    }
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartArguments, "StartArguments", StartArguments, ControlDataFlags.PersistedInProjectUserFile, New Control() {CommandLineArgsLabel})
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartWorkingDirectory, "StartWorkingDirectory", StartWorkingDirectory, ControlDataFlags.PersistedInProjectUserFile, New Control() {StartWorkingDirectoryBrowse, WorkingDirLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_StartWorkingDirectory
                    }
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartProgram, "StartProgram", StartProgram, ControlDataFlags.PersistedInProjectUserFile) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_StartProgram
                    }
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartAction, "StartAction", Nothing,
                                AddressOf StartActionSet, AddressOf StartActionGet, ControlDataFlags.PersistedInProjectUserFile,
                                New Control() {startActionTableLayoutPanel, rbStartProject, rbStartProgram, rbStartURL, StartProgram, StartURL, StartProgramBrowse})
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_EnableSQLServerDebugging, "EnableSQLServerDebugging", EnableSQLServerDebugging, ControlDataFlags.PersistedInProjectUserFile)
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_EnableUnmanagedDebugging, "EnableUnmanagedDebugging", EnableUnmanagedDebugging, ControlDataFlags.PersistedInProjectUserFile)
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_RemoteDebugMachine, "RemoteDebugMachine", RemoteDebugMachine, ControlDataFlags.PersistedInProjectUserFile) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_RemoteDebugMachine
                    }
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_RemoteDebugEnabled, "RemoteDebugEnabled", RemoteDebugEnabled, ControlDataFlags.PersistedInProjectUserFile)
                    datalist.Add(data)

                    data = New PropertyControlData(VsProjPropId165.VBPROJPROPID_AuthenticationMode, "AuthenticationMode", AuthenticationMode, AddressOf SetAuthenticationMode, AddressOf GetAuthenticationMode, ControlDataFlags.PersistedInProjectUserFile, New Control() {AuthenticationModeLabel})
                    datalist.Add(data)

                    m_ControlData = datalist.ToArray()

                End If
                Return m_ControlData
            End Get
        End Property

        Protected Overrides ReadOnly Property ValidationControlGroups As Control()()
            Get
                If _controlGroup Is Nothing Then
                    _controlGroup = New Control()() {
                        New Control() {rbStartProject, rbStartProgram, rbStartURL, StartProgram, StartURL, StartProgramBrowse},
                        New Control() {RemoteDebugEnabled, StartWorkingDirectory, RemoteDebugMachine, StartWorkingDirectoryBrowse}
                        }
                End If
                Return _controlGroup
            End Get
        End Property

        Private Function StartActionSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            Dim originalInsideInit As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                Dim action As VSLangProj.prjStartAction
                If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate or IsMissing
                    action = VSLangProj.prjStartAction.prjStartActionNone
                Else
                    action = CType(value, VSLangProj.prjStartAction)
                End If

                rbStartProject.Checked = action = VSLangProj.prjStartAction.prjStartActionProject
                rbStartProgram.Checked = action = VSLangProj.prjStartAction.prjStartActionProgram
                rbStartURL.Checked = action = VSLangProj.prjStartAction.prjStartActionURL
                StartURL.Enabled = action = VSLangProj.prjStartAction.prjStartActionURL
            Finally
                m_fInsideInit = originalInsideInit
            End Try
            Return True
        End Function

        Private Function StartActionGetValue() As VSLangProj.prjStartAction
            Dim action As VSLangProj.prjStartAction

            If rbStartProject.Checked Then
                action = VSLangProj.prjStartAction.prjStartActionProject
            ElseIf rbStartProgram.Checked Then
                action = VSLangProj.prjStartAction.prjStartActionProgram
            ElseIf rbStartURL.Checked Then
                action = VSLangProj.prjStartAction.prjStartActionURL
            Else
                action = VSLangProj.prjStartAction.prjStartActionNone
            End If

            Return action
        End Function

        Private Function StartActionGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = StartActionGetValue()
            Return True
        End Function

        Private Function SetAuthenticationMode(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            Dim mode As AuthenticationMode = VSLangProj165.AuthenticationMode.AuthenticationMode_Windows

            Try
                mode = CType(value, AuthenticationMode)
            Catch
            End Try

            Select Case mode
                Case VSLangProj165.AuthenticationMode.AuthenticationMode_None
                    AuthenticationMode.SelectedItem = My.Resources.Strings.NoAuthentication
                Case Else
                    AuthenticationMode.SelectedItem = My.Resources.Strings.WindowsAuthentication
            End Select

            Return True

        End Function

        Private Function GetAuthenticationMode(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            Select Case CStr(AuthenticationMode.SelectedItem)
                Case My.Resources.Strings.NoAuthentication
                    value = VSLangProj165.AuthenticationMode.AuthenticationMode_None
                Case My.Resources.Strings.WindowsAuthentication
                    value = VSLangProj165.AuthenticationMode.AuthenticationMode_Windows
            End Select

            Return True

        End Function

        Protected Overrides Sub EnableAllControls(enabled As Boolean)
            MyBase.EnableAllControls(enabled)

            GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartAction).EnableControls(enabled)
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartProgram).EnableControls(enabled)
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartURL).EnableControls(enabled)
        End Sub

        ''' <summary>
        ''' Customizable processing done before the class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PreInitPage()

            MyBase.PreInitPage()

            AuthenticationMode.Items.Clear()
            AuthenticationMode.Items.Add(My.Resources.Strings.NoAuthentication)
            AuthenticationMode.Items.Add(My.Resources.Strings.WindowsAuthentication)

        End Sub

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

            If SKUMatrix.IsHidden(VsProjPropId.VBPROJPROPID_StartAction) Then
                'Hide the Start Action panel
                startActionTableLayoutPanel.Visible = False
                rbStartProject.Visible = False
                rbStartProgram.Visible = False
                rbStartURL.Visible = False
                StartProgram.Visible = False
                StartURL.Visible = False
                StartProgramBrowse.Visible = False
            End If

            If SKUMatrix.IsHidden(VsProjPropId.VBPROJPROPID_RemoteDebugEnabled) Then
                ' Also hide remote debugging features for express SKUs...
                RemoteDebugEnabled.Visible = False
                RemoteDebugMachine.Visible = False
            Else
                RemoteDebugMachine.Enabled = RemoteDebugEnabled.Checked
                AuthenticationMode.Enabled = RemoteDebugEnabled.Checked
            End If

            If SKUMatrix.IsHidden(VsProjPropId.VBPROJPROPID_EnableUnmanagedDebugging) Then
                EnableUnmanagedDebugging.Visible = False
            End If

            If SKUMatrix.IsHidden(VsProjPropId.VBPROJPROPID_EnableSQLServerDebugging) Then
                EnableSQLServerDebugging.Visible = False

                If Not EnableUnmanagedDebugging.Visible Then
                    enableDebuggersTableLayoutPanel.Visible = False
                End If
            End If

            'We want the page to grow as needed.  However, we can't use AutoSize, because
            '  if the container window is made too small to show all the controls, we need
            '  the property page to *not* shrink past that point, otherwise we won't get
            '  a horizontal scrollbar.
            'So we fix the width at the width that the page naturally wants to be.
            Size = GetPreferredSize(Drawing.Size.Empty)
        End Sub

        Private Sub rbStartAction_CheckedChanged(sender As Object, e As EventArgs) Handles rbStartProgram.CheckedChanged, rbStartProject.CheckedChanged, rbStartURL.CheckedChanged
            Dim action As VSLangProj.prjStartAction = StartActionGetValue()
            StartProgram.Enabled = action = VSLangProj.prjStartAction.prjStartActionProgram
            StartProgramBrowse.Enabled = StartProgram.Enabled
            StartURL.Enabled = action = VSLangProj.prjStartAction.prjStartActionURL

            If Not m_fInsideInit Then
                Dim button As RadioButton = CType(sender, RadioButton)
                If button.Checked Then
                    SetDirty(VsProjPropId.VBPROJPROPID_StartAction, True)
                Else
                    'IsDirty = True
                    SetDirty(VsProjPropId.VBPROJPROPID_StartAction, False)
                End If

                If StartProgram.Enabled Then
                    StartProgram.Focus()
                    DelayValidate(StartProgram)     ' we need validate StartProgram to make sure it is not empty
                ElseIf StartURL.Enabled Then
                    StartURL.Focus()
                    DelayValidate(StartURL)
                End If
            End If
        End Sub

        Private Sub StartWorkingDirectoryBrowse_Click(sender As Object, e As EventArgs) Handles StartWorkingDirectoryBrowse.Click
            Dim sInitialDirectory As String
            Dim DirName As String = ""

            SkipValidating(StartWorkingDirectory)   ' skip this because we will pop up dialog to edit it...
            ProcessDelayValidationQueue(False)

            sInitialDirectory = Trim(StartWorkingDirectory.Text)
            If sInitialDirectory = "" Then
                Try
                    sInitialDirectory = Path.Combine(GetProjectPath(), GetSelectedConfigOutputPath())
                Catch ex As IOException
                    'Ignore
                Catch ex As Exception When Common.ReportWithoutCrash(ex, "Exception getting project output path for selected config", NameOf(DebugPropPage))
                End Try
            End If

            If GetDirectoryViaBrowse(sInitialDirectory, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_SelectWorkingDirectoryTitle, DirName) Then
                StartProgramBrowse.Focus()
                StartWorkingDirectory.Text = DirName
                SetDirty(StartWorkingDirectory, True)
            Else
                StartWorkingDirectoryBrowse.Focus()
                DelayValidate(StartWorkingDirectory)
            End If

        End Sub

        Private Sub RemoteDebugEnabled_CheckedChanged(sender As Object, e As EventArgs) Handles RemoteDebugEnabled.CheckedChanged
            RemoteDebugMachine.Enabled = RemoteDebugEnabled.Checked
            AuthenticationMode.Enabled = RemoteDebugEnabled.Checked

            If Not m_fInsideInit Then
                If RemoteDebugEnabled.Checked Then
                    RemoteDebugMachine.Focus()
                    DelayValidate(RemoteDebugMachine)
                Else
                    DelayValidate(StartWorkingDirectory)
                End If
            End If
        End Sub

        Private Sub StartProgramBrowse_Click(sender As Object, e As EventArgs) Handles StartProgramBrowse.Click
            Dim FileName As String = Nothing

            SkipValidating(StartProgram)
            ProcessDelayValidationQueue(False)

            If GetFileViaBrowse("", FileName, Common.CreateDialogFilter(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ExeFilesFilter, ".exe")) Then
                StartProgramBrowse.Focus()
                StartProgram.Text = FileName
                SetDirty(StartProgram, True)
            Else
                StartProgramBrowse.Focus()
                DelayValidate(StartProgram)
            End If

        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropDebug
        End Function

        ''' <summary>
        ''' validate a property
        ''' </summary>
        Protected Overrides Function ValidateProperty(controlData As PropertyControlData, ByRef message As String, ByRef returnControl As Control) As ValidationResult
            Select Case controlData.DispId
                Case VsProjPropId.VBPROJPROPID_StartProgram
                    If rbStartProgram.Checked Then
                        'DO NOT Validate file existence if we are remote debugging, as they are local to the remote machine
                        If Not RemoteDebugEnabled.Checked Then
                            If Not File.Exists(StartProgram.Text) Then
                                message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_ProgramNotExist
                                Return ValidationResult.Warning
                            End If
                        End If
                        If Trim(StartProgram.Text).Length = 0 Then
                            message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_NeedExternalProgram
                            Return ValidationResult.Warning
                        ElseIf Not StartProgram.Text.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) Then
                            message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_NotAnExeError
                            Return ValidationResult.Warning
                        End If
                    End If
                Case VsProjPropId.VBPROJPROPID_StartURL
                    If rbStartURL.Checked Then
                        Dim newURL As String = Trim(StartURL.Text)
                        If newURL.Length = 0 Then
                            message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_NeedURL
                            Return ValidationResult.Warning
                        Else
                            If newURL.IndexOf(":"c) < 0 Then
                                newURL = "http://" & newURL
                            End If
                            Try
                                Dim t As Uri = New Uri(newURL)
                            Catch ex As UriFormatException
                                message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_InvalidURL
                                Return ValidationResult.Warning
                            End Try
                        End If
                    End If
                Case VsProjPropId.VBPROJPROPID_RemoteDebugMachine
                    If RemoteDebugEnabled.Checked AndAlso Len(Trim(RemoteDebugMachine.Text)) = 0 Then
                        message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_RemoteMachineBlankError
                        Return ValidationResult.Warning
                    End If
                Case VsProjPropId.VBPROJPROPID_StartWorkingDirectory
                    'DO NOT Validate working directory if we are remote debugging, as they are local to the remote machine
                    If Not RemoteDebugEnabled.Checked AndAlso Trim(StartWorkingDirectory.Text).Length <> 0 AndAlso Not Directory.Exists(StartWorkingDirectory.Text) Then
                        ' Warn the user when working dir is invalid
                        message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_WorkingDirError
                        Return ValidationResult.Warning
                    End If
            End Select
            Return ValidationResult.Succeeded
        End Function

        ''' <summary>
        ''' Attempts to get the output path for the currently selected configuration.  If there
        '''   are multiple configurations selected, gets the output path for the first one.
        ''' </summary>
        ''' <returns>The output path, relative to the project's folder.</returns>
        Private Function GetSelectedConfigOutputPath() As String
            'If there are multiple selected configs, we'll just use the first one
            Dim Properties As PropertyDescriptorCollection = TypeDescriptor.GetProperties(m_Objects(0))
            Dim OutputPathDescriptor As PropertyDescriptor = Properties("OutputPath")
            Return CStr(OutputPathDescriptor.GetValue(m_ExtendedObjects(0)))
        End Function

        ''' <summary>
        ''' Sets the Startup Arguments textbox's height
        ''' </summary>
        Private Sub SetStartArgumentsHeight()
            'Set StartArguments text to be approximately four lines high
            '  (it won't necessarily be exact due to GDI/GDI+ differences)
            Const ApproximateDesiredHeightInLines As Integer = 4
            Using g As Drawing.Graphics = CreateGraphics()
                Const ApproximateBorderHeight As Integer = 2 + 1 '+1 for a little extra buffer
                StartArguments.Height = 2 * ApproximateBorderHeight _
                    + CInt(Math.Ceiling(g.MeasureString(" " & New String(CChar(vbLf), ApproximateDesiredHeightInLines - 1) & " ", StartArguments.Font, Integer.MaxValue).Height))
            End Using
        End Sub

        Protected Overrides Sub OnLayout(levent As LayoutEventArgs)
            SetStartArgumentsHeight()
            MyBase.OnLayout(levent)
        End Sub

    End Class

End Namespace
