' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Windows.Forms
Imports System.Globalization
Imports Microsoft.VisualStudio.PlatformUI
Imports VBStrings = Microsoft.VisualBasic.Strings
Imports VSLangProj80
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class AdvBuildSettingsPropPage
        Inherits PropPageUserControlBase

        Protected m_bDebugSymbols As Boolean = False

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            ' Scale the width of the overarching table layout panel
            Me.overarchingTableLayoutPanel.Width = DpiHelper.LogicalToDeviceUnitsX(overarchingTableLayoutPanel.Width)

            Me.MinimumSize = Me.PreferredSize()

            AddChangeHandlers()

            Me.AutoScaleMode = AutoScaleMode.Font
            MyBase.PageRequiresScaling = False
        End Sub

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() {
                    New PropertyControlData(CSharpProjPropId.CSPROJPROPID_LanguageVersion, "LanguageVersion", Me.cboLanguageVersion, AddressOf LanguageVersionSet, AddressOf LanguageVersionGet, ControlDataFlags.None, New Control() {Me.lblLanguageVersion}),
                    New PropertyControlData(CSharpProjPropId.CSPROJPROPID_ErrorReport, "ErrorReport", Me.cboReportCompilerErrors, AddressOf ErrorReportSet, AddressOf ErrorReportGet, ControlDataFlags.None, New Control() {Me.lblReportCompilerErrors}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_CheckForOverflowUnderflow, "CheckForOverflowUnderflow", Me.chkOverflow, AddressOf OverflowUnderflowSet, AddressOf OverflowUnderflowGet),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_FileAlignment, "FileAlignment", Me.cboFileAlignment, AddressOf FileAlignmentSet, AddressOf FileAlignmentGet, ControlDataFlags.None, New Control() {Me.lblFileAlignment}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_BaseAddress, "BaseAddress", Me.txtDLLBase, AddressOf BaseAddressSet, AddressOf BaseAddressGet, ControlDataFlags.None, New Control() {Me.lblDLLBase}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_DebugInfo, "DebugInfo", Me.cboDebugInfo, AddressOf DebugInfoSet, AddressOf DebugInfoGet, ControlDataFlags.None, New Control() {Me.lblDebugInfo}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_DebugSymbols, "DebugSymbols", Nothing, AddressOf DebugSymbolsSet, AddressOf DebugSymbolsGet)}
                End If
                Return m_ControlData
            End Get
        End Property

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

            Me.cboLanguageVersion.Items.Clear()

            Me.cboLanguageVersion.Items.AddRange(CSharpLanguageVersionUtilities.GetAllLanguageVersions())
            Me.cboLanguageVersion.SelectedIndex = 0

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
        End Sub

        Private Function LanguageVersionSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean

            Me.cboLanguageVersion.SelectedIndex = -1

            If PropertyControlData.IsSpecialValue(value) Then
                'Leave it unselected
            Else
                Dim stValue As String = CType(value, String)
                If stValue = "" Then
                    stValue = CSharpLanguageVersion.Default.Value
                End If

                For Each entry As CSharpLanguageVersion In Me.cboLanguageVersion.Items
                    If entry.Value = stValue Then
                        Me.cboLanguageVersion.SelectedItem = entry
                        Exit For
                    End If
                Next

            End If
            Return True
        End Function

        Private Function LanguageVersionGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            Dim currentVersion As CSharpLanguageVersion = CType(CType(control, ComboBox).SelectedItem, CSharpLanguageVersion)
            If currentVersion IsNot Nothing Then
                value = currentVersion.Value
                Return True
            End If

            Debug.Fail("The combobox should not have still been unselected yet be dirty")
            Return False

        End Function

        Private Function ErrorReportSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If (Not (PropertyControlData.IsSpecialValue(value))) Then
                Dim stValue As String = CType(value, String)
                If stValue <> "" Then
                    Me.cboReportCompilerErrors.Text = stValue
                Else
                    Me.cboReportCompilerErrors.SelectedIndex = 0        '// Zero is the (none) entry in the list
                End If
                Return True
            Else
                Me.cboReportCompilerErrors.SelectedIndex = -1        '// Indeterminate state
            End If
        End Function

        Private Function ErrorReportGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If (Me.cboReportCompilerErrors.SelectedIndex <> -1) Then
                value = Me.cboReportCompilerErrors.Text
                Return True
            Else
                Return False         '// Indeterminate - let the architecture handle it
            End If
        End Function

        Private Function DebugSymbolsSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate/IsMissing 
                m_bDebugSymbols = False
            Else
                m_bDebugSymbols = CType(value, Boolean)
            End If
            Return True
        End Function

        Private Function DebugSymbolsGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = m_bDebugSymbols
            Return True
        End Function

        Private Function BaseAddressSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If (IsExeProject()) Then
                '// EXE's don't support base addresses so just disable the control and set the disabled text to the default for 
                '// EXE's.

                Me.txtDLLBase.Enabled = False
                Me.txtDLLBase.Text = "0x00400000"
            Else
                '// The default for DLL projects is 0x11000000
                Me.txtDLLBase.Enabled = True

                Dim iBaseAddress As UInteger

                If (TypeOf (value) Is UInteger) Then
                    iBaseAddress = CUInt(value)
                Else
                    '// Since it's bogus just use the default for DLLs
                    iBaseAddress = &H11000000   '// 0x11000000
                End If

                Dim stHexValue As String = "0x" & iBaseAddress.ToString("x", CultureInfo.CurrentUICulture)
                If value Is PropertyControlData.Indeterminate Then
                    stHexValue = ""
                End If
                Me.txtDLLBase.Text = stHexValue
            End If

            Return True
        End Function

        Private Function IsExeProject() As Boolean

            Dim obj As Object = Nothing
            Dim OutputType As VSLangProj.prjOutputType

            Try
                GetCurrentProperty(VsProjPropId.VBPROJPROPID_OutputType, "OutputType", obj)
                OutputType = CType(obj, VSLangProj.prjOutputType)
            Catch ex As InvalidCastException
                '// When all else fails assume dll (so they can edit it)
                OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary
            Catch ex As TargetInvocationException
                ' Property must be missing for this project flavor
                OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary
            End Try

            If (OutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary) Then
                Return False
            Else
                Return True
            End If
        End Function

        Private Function BaseAddressGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            Dim StringValue As String = Trim(control.Text)

            'DLL Baseaddress must be 0xNNNNNNNN format
            If String.Compare(VBStrings.Left(StringValue, 2), "0x", StringComparison.OrdinalIgnoreCase) = 0 Then
                StringValue = "&h" + VBStrings.Mid(StringValue, 3)
                If IsNumeric(StringValue) Then
                    Dim LongValue As ULong
                    Try
                        LongValue = CULng(StringValue)
                        If LongValue < UInt32.MaxValue Then
                            value = CUInt(LongValue)
                            Return True
                        End If
                    Catch ex As Exception
                        'Let throw below
                    End Try
                End If
            End If
            Throw New Exception(SR.GetString(SR.PPG_AdvancedBuildSettings_InvalidBaseAddress))

        End Function

        Private Function DebugInfoSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate or IsMissing
                Me.cboDebugInfo.SelectedIndex = -1
            Else
                Dim stValue As String = TryCast(value, String)
                If (Not stValue Is Nothing) AndAlso (stValue.Trim().Length > 0) Then

                    '// Need to special case pdb-only becuase it's stored in the property without the dash but it's
                    '// displayed in the dialog with a dash.

                    If (String.Compare(stValue, "pdbonly", StringComparison.OrdinalIgnoreCase) <> 0) Then
                        Me.cboDebugInfo.Text = stValue
                    Else
                        Me.cboDebugInfo.Text = "pdb-only"
                    End If
                Else
                    Me.cboDebugInfo.SelectedIndex = 0        '// Zero is the (none) entry in the list
                End If
            End If
            Return True
        End Function

        Private Function DebugInfoGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean

            '// Need to special case pdb-only because the display name has a dash while the actual property value
            '// doesn't have the dash.
            If (String.Compare(Me.cboDebugInfo.Text, "pdb-only", StringComparison.OrdinalIgnoreCase) <> 0) Then
                value = Me.cboDebugInfo.Text
            Else
                value = "pdbonly"
            End If
            Return True
        End Function

        Private Sub DebugInfo_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cboDebugInfo.SelectedIndexChanged
            If Me.cboDebugInfo.SelectedIndex = 0 Then
                '// user selcted none
                m_bDebugSymbols = False
            Else
                m_bDebugSymbols = True
            End If

            SetDirty(VsProjPropId.VBPROJPROPID_DebugSymbols, False)
            SetDirty(VsProjPropId80.VBPROJPROPID_DebugInfo, False)
            SetDirty(True)
        End Sub


        Private Function FileAlignmentSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                Me.cboFileAlignment.SelectedIndex = -1
            Else
                Dim stValue As String = CType(value, String)
                If stValue <> "" Then
                    Me.cboFileAlignment.Text = stValue
                Else
                    Me.cboFileAlignment.SelectedIndex = 0        '// Zero is the (none) entry in the list
                End If
            End If
            Return True
        End Function

        Private Function FileAlignmentGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = Me.cboFileAlignment.Text
            Return True
        End Function

        Private Function OverflowUnderflowSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate Then
                Me.chkOverflow.CheckState = CheckState.Indeterminate
            Else
                Me.chkOverflow.Checked = CType(value, Boolean)
            End If
            Return True
        End Function

        Private Function OverflowUnderflowGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = Me.chkOverflow.Checked
            Return True
        End Function

    End Class

End Namespace
