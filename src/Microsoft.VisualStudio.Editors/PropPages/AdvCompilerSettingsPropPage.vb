' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'This is the advanced compiler options page for VB only.

Imports System.ComponentModel
Imports System.Windows.Forms
Imports VBStrings = Microsoft.VisualBasic.Strings
Imports VSLangProj80
Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Friend Class AdvCompilerSettingsPropPage
        Inherits PropPageUserControlBase

        'The state of the DebugSymbols value - true or false
        '  This is automatically set to true whenever the value in the DebugInfo combobox is set to something else
        '  than None, and false otherwise
        Private _debugSymbols As Object

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            'We don't want this localized, and the WinForms designer will do that automatically if
            '  we have it in InitializeComponent.
            Me.DebugInfoComboBox.Items.AddRange(New Object() {"none", "full", "pdb-only", "portable", "embedded"})

            Me.MinimumSize = Me.PreferredSize()

            AddChangeHandlers()
            MyBase.PageRequiresScaling = False
        End Sub

        Public Enum TreatWarningsSetting
            WARNINGS_ALL
            WARNINGS_SPECIFIC
            WARNINGS_NONE
        End Enum

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then

                    m_ControlData = New PropertyControlData() {
                    New PropertyControlData(
                        VsProjPropId.VBPROJPROPID_RemoveIntegerChecks, "RemoveIntegerChecks", Me.RemoveIntegerChecks),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId.VBPROJPROPID_Optimize, "Optimize", Me.Optimize),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_BaseAddress, "BaseAddress", Me.DllBaseTextbox, AddressOf Me.SetBaseAddress, AddressOf Me.GetBaseAddress, ControlDataFlags.None, New Control() {Me.DllBaseLabel}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release, VsProjPropId.VBPROJPROPID_DebugSymbols, "DebugSymbols", Nothing, AddressOf Me.DebugSymbolsSet, AddressOf Me.DebugSymbolsGet),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_DebugInfo, "DebugInfo", DebugInfoComboBox, AddressOf DebugInfoSet, AddressOf DebugInfoGet, ControlDataFlags.UserHandledEvents, New Control() {Me.GenerateDebugInfoLabel}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release, VsProjPropId.VBPROJPROPID_DefineDebug, "DefineDebug", Me.DefineDebug),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_DefineTrace, "DefineTrace", Me.DefineTrace),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_DefineConstants, "DefineConstants", Me.DefineConstantsTextbox, New Control() {Me.CustomConstantsLabel, Me.CustomConstantsExampleLabel}),
                    New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                        VsProjPropId80.VBPROJPROPID_GenerateSerializationAssemblies, "GenerateSerializationAssemblies", Me.GenerateSerializationAssemblyComboBox, AssocControls:=New Control() {GenerateSerializationAssembliesLabel}),
                    New PropertyControlData(VsProjPropId.VBPROJPROPID_OutputType, "OutputType", Nothing, ControlDataFlags.Hidden),
                    New HiddenIfMissingPropertyControlData(1, "UseDotNetNativeToolchain", Me.CompileWithDotNetNative),
                    New HiddenIfMissingPropertyControlData(1, "RunGatekeeperAudit", Me.EnableGatekeeperAnAlysis)
                    }
                End If
                Return m_ControlData
            End Get
        End Property

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

            ' Only enable the dll base adress for library type projects...
            Dim dllBaseEnabled As Boolean = False
            Dim pcd As PropertyControlData = Me.GetPropertyControlData(VsProjPropId.VBPROJPROPID_OutputType)
            If pcd IsNot Nothing Then
                Dim oOutputType As Object = pcd.TryGetPropertyValueNative(m_ExtendedObjects)
                If Not PropertyControlData.IsSpecialValue(oOutputType) Then
                    Dim prjOutputType As VSLangProj.prjOutputType = CType(oOutputType, VSLangProj.prjOutputType)
                    If prjOutputType = VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                        dllBaseEnabled = True
                    End If
                End If
            End If
            Me.DllBaseTextbox.Enabled = dllBaseEnabled
        End Sub

        ''' <summary>
        ''' Format baseaddress value into VB hex notation
        ''' </summary>
        Private Function ToHexAddress(BaseAddress As UInt64) As String
            Debug.Assert(BaseAddress >= 0 AndAlso BaseAddress <= UInteger.MaxValue, "Invalid baseaddress value")

            Return "&H" & String.Format("{0:X8}", CUInt(BaseAddress))
        End Function

        ''' <summary>
        ''' Converts BaseAddress property to VB hext format for UI
        ''' Called by base class code through delegate
        ''' </summary>
        Private Function SetBaseAddress(control As Control, prop As PropertyDescriptor, obj As Object) As Boolean
            control.Text = "&H" & String.Format("{0:X8}", obj)
            Return True
        End Function

        ''' <summary>
        ''' Converts the string BaseAddress text to the native property type of UInt32
        ''' Called by base class code through delegate
        ''' </summary>
        Private Function GetBaseAddress(control As Control, prop As PropertyDescriptor, ByRef obj As Object) As Boolean
            obj = GetBaseAddressFromControl(control)
            Return True
        End Function

        ''' <summary>
        ''' Converts the string BaseAddress text to the native property type of UInt32
        ''' Called by base class code through delegate
        ''' </summary>
        Private Function GetBaseAddressFromControl(control As Control) As UInteger
            Dim StringValue As String = Trim(control.Text)
            Dim LongValue As ULong = 0

            'DLL Baseaddress must be &Hxxxxxxxx format
            If String.Compare(VBStrings.Left(StringValue, 2), "&H", StringComparison.OrdinalIgnoreCase) = 0 AndAlso IsNumeric(StringValue) Then
                Try
                    LongValue = CULng(StringValue)
                    If LongValue < UInteger.MaxValue Then
                        Return CUInt(LongValue)
                    End If
                Catch ex As Exception
                    'Let throw below
                End Try
            End If
            Throw New FormatException(SR.GetString(SR.PPG_InvalidHexString))
        End Function

        ''' <summary>
        ''' Get the debug symbols flag. 
        ''' </summary>
        Private Function DebugSymbolsGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If TypeOf _debugSymbols Is Boolean Then
                value = CType(_debugSymbols, Boolean)
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Set the debug symbols flag
        ''' </summary>
        Private Function DebugSymbolsSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            _debugSymbols = value
            Return True
        End Function

        ''' <summary>
        ''' Gets the DebugInfo property (DebugType in the proj file). 
        '''   In the VB property pages, the user is given only the choice of whether
        '''   to generate debug info or not.  But setting only that property on/off
        '''   without also changing the DebugInfo property can lead to confusion in the
        '''   build engine (esp. if the DebugType is also set in the proj file).  So we
        '''   change this property when the DebugSymbols property is set.
        ''' </summary>
        Private Function DebugInfoSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then 'Indeterminate or IsMissing
                Me.DebugInfoComboBox.SelectedIndex = -1
            Else
                Dim stValue As String = TryCast(value, String)
                If (Not stValue Is Nothing) AndAlso (stValue.Trim().Length > 0) Then

                    '// Need to special case pdb-only becuase it's stored in the property without the dash but it's
                    '// displayed in the dialog with a dash.

                    If (String.Compare(stValue, "pdbonly", StringComparison.OrdinalIgnoreCase) <> 0) Then
                        Me.DebugInfoComboBox.Text = stValue
                    Else
                        Me.DebugInfoComboBox.Text = "pdb-only"
                    End If
                Else
                    Me.DebugInfoComboBox.SelectedIndex = 0        '// Zero is the (none) entry in the list
                End If
            End If
            Return True
        End Function

        Private Function DebugInfoGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            ' Need to special case pdb-only because the display name has a dash while the actual property value
            ' doesn't have the dash.
            If String.Equals(Me.DebugInfoComboBox.Text, "pdb-only", StringComparison.OrdinalIgnoreCase) Then
                value = "pdbonly"
            Else
                value = Me.DebugInfoComboBox.Text
            End If
            Return True
        End Function

        ''' <summary>
        ''' Whenever the user changes the selection in the debug info combobox, we have to update both the
        ''' DebugInfo and DebugSymbols properties...
        ''' </summary>
        Private Sub DebugInfoComboBox_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles DebugInfoComboBox.SelectionChangeCommitted
            If DebugInfoComboBox.SelectedIndex = 0 Then
                ' Index 0 corresponds to "None" 
                _debugSymbols = False
            Else
                _debugSymbols = True
            End If
            SetDirty(VsProjPropId80.VBPROJPROPID_DebugInfo, False)
            SetDirty(VsProjPropId.VBPROJPROPID_DebugSymbols, False)
            SetDirty(True)
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropAdvancedCompile
        End Function

        ''' <summary>
        ''' Validation method for BaseAddress
        ''' no cancellation, just normalizes value if not an error condition
        ''' </summary>
        Private Sub BaseAddress_Validating(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles DllBaseTextbox.Validating
            Dim StringValue As String = Trim(Me.DllBaseTextbox.Text)

            Const DEFAULT_DLLBASEADDRESS As String = "&H11000000"

            If StringValue = "" Then
                Me.DllBaseTextbox.Text = DEFAULT_DLLBASEADDRESS

            ElseIf String.Compare(VBStrings.Left(StringValue, 2), "&H", StringComparison.OrdinalIgnoreCase) = 0 AndAlso IsNumeric(StringValue) Then
                Dim LongValue As ULong = CULng(StringValue)
                If LongValue < UInteger.MaxValue Then
                    'Reformat into clean
                    DllBaseTextbox.Text = ToHexAddress(LongValue)
                Else
                    'Cancel here prevents swithing to another window
                    'e.Cancel = True
                    'Throw New Exception(SR.GetString(SR.PPG_InvalidHexString))
                End If

            Else
                'Should we put up a UI glyph beside the textbox showing the error?
                'Status bar error text?

                'e.Cancel = True
            End If
        End Sub

        ''' <summary>
        ''' Validation properties
        ''' </summary>
        Protected Overrides Function ValidateProperty(controlData As PropertyControlData, ByRef message As String, ByRef returnControl As System.Windows.Forms.Control) As ValidationResult
            If controlData.FormControl Is DllBaseTextbox Then
                Try
                    GetBaseAddressFromControl(DllBaseTextbox)
                Catch ex As FormatException
                    message = ex.Message
                    Return ValidationResult.Failed
                End Try
            End If
            Return ValidationResult.Succeeded
        End Function
    End Class

End Namespace

