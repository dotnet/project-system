' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Shell.Interop

Imports VSLangProj110

Imports VSLangProj80

Imports VslangProj90

'This is the VB version of this page.  BuildPropPage.vb is the C# version.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Friend Class CompilePropPage2
        Inherits BuildPropPageBase
        ' Shared cache of raw and extended configuration objects
        Private _objectCache As FakeAllConfigurationsPropertyControlData.ConfigurationObjectCache
        Private _settingGenerateXmlDocumentation As Boolean
        Private _generateXmlDocumentation As Object
        ' The list of warning ids that are affected by option strict on/off
        Private ReadOnly _optionStrictIDs() As Integer

        ' List of warnings to ignore
        Private _noWarn() As Integer

        ' List of warnings to report as errors
        Private _specWarnAsError() As Integer

        Private _comVisible As Object

        'Localized error/warning strings for notify column
        Private ReadOnly _notifyError As String
        Private ReadOnly _notifyNone As String
        Private ReadOnly _notifyWarning As String
        Private Const ConditionColumnIndex As Integer = 0
        Private Const NotifyColumnIndex As Integer = 1

        Private _optionStrictCustomText As String
        Private _optionStrictOnText As String
        Private _optionStrictOffText As String
        Private _refreshingWarningsList As Boolean

        ' Since the option strict combobox value depends
        ' on a combination of the noWarn, specWarnAsError and
        ' option strict properties, which may all change because
        ' of an undo or load operation, and we have no way to
        ' put all of these updates in a transaction, or have them
        ' ordered in a consistent way, we queue an update of the UI
        ' on the IDLE so that it happens after all the settings have
        ' been set...
        Private _optionStrictComboBoxUpdateQueued As Boolean

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            _notifyError = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_Notification_Error
            _notifyNone = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_Notification_None
            _notifyWarning = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_Notification_Warning
            PageRequiresScaling = False
            AutoScaleMode = AutoScaleMode.Font

            AddChangeHandlers()

            Dim optionStrictErrors As New ArrayList
            For Each ErrorInfo As ErrorInfo In _errorInfos
                If ErrorInfo.ErrorOnOptionStrict Then
                    optionStrictErrors.AddRange(ErrorInfo.ErrList)
                End If
            Next
            ReDim _optionStrictIDs(optionStrictErrors.Count - 1)
            optionStrictErrors.CopyTo(_optionStrictIDs)
            Array.Sort(_optionStrictIDs)

            NotificationColumn.Items.Add(_notifyNone)
            NotificationColumn.Items.Add(_notifyWarning)
            NotificationColumn.Items.Add(_notifyError)
        End Sub

#Region "Queued update of text in option strict combobox"
        Private Delegate Sub QueueUpdateOptionStrictComboBoxDelegate()

        ''' <summary>
        ''' Whenever we programatically change the noWarn,specWarnAsError or OptionStrict
        ''' properties, we need to make sure that we have the right items/display text in
        ''' the option strict combobox
        ''' </summary>
        Private Sub QueueUpdateOptionStrictComboBox()
            If _optionStrictComboBoxUpdateQueued Then
                Return
            End If

            BeginInvoke(New QueueUpdateOptionStrictComboBoxDelegate(AddressOf UpdateOptionStrictComboBox))
            _optionStrictComboBoxUpdateQueued = True
        End Sub

        ''' <summary>
        ''' Update the text (and possibly the contents) of the option strict combobox
        ''' This method does *not* change the underlying property, it only updates the
        ''' UI.
        ''' </summary>
        Private Sub UpdateOptionStrictComboBox()
            Try
                If IsOptionStrictOn() Then
                    ' On means that we should remove the "Custom" from the drop-down
                    OptionStrictComboBox.Items.Remove(_optionStrictCustomText)
                ElseIf IsOptionStrictCustom() Then
                    ' If we are showing "Custom", but the current settings are the same as
                    ' "Off", remove the "Custom" entry from the combobox and change current selection
                    ' to "Off"
                    If Not IsSameAsOptionStrictCustom() Then
                        OptionStrictComboBox.Items.Remove(_optionStrictCustomText)
                        OptionStrictComboBox.SelectedIndex = OptionStrictComboBox.Items.IndexOf(_optionStrictOffText)
                    End If
                ElseIf IsOptionStrictOff() Then
                    ' Off may actually mean "Custom"
                    If Not IsSameAsOptionStrictOff() Then
                        ' Change from showing "Off" to "Custom" in combobox
                        Dim newIndex As Integer = OptionStrictComboBox.Items.IndexOf(_optionStrictCustomText)
                        If newIndex = -1 Then
                            ' Add the option strict custom text since it wasn't already in there...
                            newIndex = OptionStrictComboBox.Items.Add(_optionStrictCustomText)
                        End If
                        OptionStrictComboBox.SelectedIndex = newIndex
                    End If
                End If
            Finally
                _optionStrictComboBoxUpdateQueued = False
            End Try
        End Sub
#End Region

        ''' <summary>
        ''' Returns true if the Register for COM Interop checkbox makes sense in this project context
        ''' </summary>
        Private Function RegisterForComInteropSupported() As Boolean
            If MultiProjectSelect Then
                Return False
            End If

            Try
                Dim value As Object = Nothing
                If Not GetCurrentProperty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, "OutputTypeEx", value) Then
                    Return False
                End If

                Return CUInt(value) = prjOutputTypeEx.prjOutputTypeEx_Library _
                    AndAlso Not GetPropertyControlData(VsProjPropId.VBPROJPROPID_RegisterForComInterop).IsMissing
            Catch ex As Exception When Common.ReportWithoutCrash(ex, NameOf(RegisterForComInteropSupported), NameOf(CompilePropPage2))
                'If the project doesn't support this property, the answer is no.
                Return False
            End Try
        End Function

#Region "Enable / disable controls helpers"
        Protected Overrides Sub EnableAllControls(enabled As Boolean)
            MyBase.EnableAllControls(enabled)

            GetPropertyControlData(VsProjPropId.VBPROJPROPID_DocumentationFile).EnableControls(enabled)
            AdvancedOptionsButton.Enabled = enabled
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_OutputPath).EnableControls(enabled)
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_RegisterForComInterop).EnableControls(enabled AndAlso RegisterForComInteropSupported())

            EnableDisableWarningControls(enabled)
        End Sub

        Private Sub EnableDisableWarningControls(enabled As Boolean)
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_WarningLevel).EnableControls(enabled) 'DisableAllWarningsCheckBox
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_TreatWarningsAsErrors).EnableControls(enabled AndAlso Not DisableAllWarnings())

            EnableDisableGridView(enabled)
        End Sub

        Private Sub EnableDisableGridView(enabled As Boolean)

            If GetPropertyControlData(VsProjPropId2.VBPROJPROPID_NoWarn).IsMissing _
            OrElse GetPropertyControlData(VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors).IsMissing Then
                'Not much sense in having the grid enabled if these properties aren't supported by the flavor
                enabled = False
            End If

            If enabled AndAlso DisableAllWarningsCheckBox.CheckState = CheckState.Unchecked AndAlso WarningsAsErrorCheckBox.CheckState = CheckState.Unchecked Then
                For Each column As DataGridViewColumn In WarningsGridView.Columns
                    column.DefaultCellStyle.BackColor = WarningsGridView.DefaultCellStyle.BackColor
                Next
                If IndeterminateWarningsState Then
                    ' If we don't set the current cell to nothing, changing the read-only mode may
                    ' cause us to go into edit mode, with the subsequent prompt about resetting all the
                    ' changes...
                    WarningsGridView.CurrentCell = Nothing
                End If
                WarningsGridView.Enabled = True
            Else
                For Each column As DataGridViewColumn In WarningsGridView.Columns
                    column.DefaultCellStyle.BackColor = BackColor
                Next
                WarningsGridView.Enabled = False
            End If
        End Sub
#End Region

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If _objectCache IsNot Nothing Then
                    _objectCache.Reset(ProjectHierarchy, ServiceProvider, False)
                End If

                If m_ControlData Is Nothing Then
                    'Note: "TreatSpecificWarningsAsErrors - For the grid that contains the ability to turn warnings on and off for specific warnings,
                    '  we use a hidden textbox with the name "SpecWarnAsErrorTextBox".  In this, we build up the list of warnings to treat
                    '  individually.
                    _objectCache = New FakeAllConfigurationsPropertyControlData.ConfigurationObjectCache(ProjectHierarchy, ServiceProvider)

                    m_ControlData = New PropertyControlData() {
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId2.VBPROJPROPID_NoWarn, "NoWarn", Nothing, AddressOf NoWarnSet, AddressOf NoWarnGet, ControlDataFlags.UserHandledEvents, Nothing),
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors, "TreatSpecificWarningsAsErrors", Nothing, AddressOf SpecWarnAsErrorSet, AddressOf SpecWarnAsErrorGet, ControlDataFlags.UserHandledEvents, Nothing),
                        New PropertyControlData(VsProjPropId.VBPROJPROPID_OptionExplicit, "OptionExplicit", OptionExplicitComboBox, New Control() {OptionExplicitLabel}),
                        New PropertyControlData(VsProjPropId.VBPROJPROPID_OptionStrict, "OptionStrict", OptionStrictComboBox, AddressOf OptionStrictSet, AddressOf OptionStrictGet, ControlDataFlags.UserHandledEvents, New Control() {OptionStrictLabel}),
                        New PropertyControlData(VsProjPropId.VBPROJPROPID_OptionCompare, "OptionCompare", OptionCompareComboBox, New Control() {OptionCompareLabel}),
                        New PropertyControlData(VBProjPropId90.VBPROJPROPID_OptionInfer, "OptionInfer", OptionInferComboBox, New Control() {OptionInferLabel}),
                        New SingleConfigPropertyControlData(SingleConfigPropertyControlData.Configs.Release,
                            VsProjPropId.VBPROJPROPID_OutputPath, "OutputPath", BuildOutputPathTextBox, Nothing, AddressOf OutputPathGet, ControlDataFlags.None, New Control() {BuildOutputPathLabel}),
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId.VBPROJPROPID_DocumentationFile, "DocumentationFile", Nothing, AddressOf DocumentationFileNameSet, AddressOf DocumentationFileNameGet, ControlDataFlags.UserHandledEvents, New Control() {GenerateXMLCheckBox}),
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId.VBPROJPROPID_WarningLevel, "WarningLevel", DisableAllWarningsCheckBox, AddressOf WarningLevelSet, AddressOf WarningLevelGet, ControlDataFlags.UserHandledEvents, Nothing),
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId.VBPROJPROPID_TreatWarningsAsErrors, "TreatWarningsAsErrors", WarningsAsErrorCheckBox, Nothing, Nothing, ControlDataFlags.UserHandledEvents, Nothing),
                        New FakeAllConfigurationsPropertyControlData(_objectCache, VsProjPropId.VBPROJPROPID_RegisterForComInterop, "RegisterForComInterop", RegisterForComInteropCheckBox, Nothing, Nothing, ControlDataFlags.UserHandledEvents, Nothing),
                        New PropertyControlData(VsProjPropId80.VBPROJPROPID_ComVisible, "ComVisible", Nothing, AddressOf ComVisibleSet, AddressOf ComVisibleGet, ControlDataFlags.Hidden Or ControlDataFlags.PersistedInAssemblyInfoFile),
                        New PropertyControlData(VsProjPropId80.VBPROJPROPID_PlatformTarget, "PlatformTarget", TargetCPUComboBox, AddressOf PlatformTargetSet, AddressOf PlatformTargetGet, ControlDataFlags.None, New Control() {TargetCPULabel}),
                        New PropertyControlData(VsProjPropId110.VBPROJPROPID_Prefer32Bit, "Prefer32Bit", Prefer32BitCheckBox, AddressOf Prefer32BitSet, AddressOf Prefer32BitGet)
                    }
                End If
                Return m_ControlData
            End Get
        End Property

#Region "Custom property getters/setters"
#Region "Documentation filename getter and setter"
        Private Function DocumentationFileNameGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Select Case GenerateXMLCheckBox.CheckState
                Case CheckState.Checked
                    If String.IsNullOrEmpty(TryCast(_generateXmlDocumentation, String)) Then
                        _generateXmlDocumentation = ProjectProperties.AssemblyName & ".xml"
                    End If
                    value = _generateXmlDocumentation
                Case CheckState.Unchecked
                    value = ""
                Case Else
                    ' Why are we called to get an indeterminate value?
                    value = PropertyControlData.Indeterminate
            End Select
            Return True
        End Function

        Private Function DocumentationFileNameSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If _settingGenerateXmlDocumentation Then
                Return False
            End If
            _settingGenerateXmlDocumentation = True
            Try
                If PropertyControlData.IsSpecialValue(value) Then
                    GenerateXMLCheckBox.CheckState = CheckState.Indeterminate
                ElseIf String.IsNullOrEmpty(TryCast(value, String)) Then
                    GenerateXMLCheckBox.CheckState = CheckState.Unchecked
                Else
                    GenerateXMLCheckBox.CheckState = CheckState.Checked
                End If

                ' Store this value off for later...
                _generateXmlDocumentation = value
                Return True
            Finally
                _settingGenerateXmlDocumentation = False
            End Try
        End Function

#End Region

#Region "NoWarn getter and setter"
        ''' <summary>
        ''' Custom handling of the NoWarn property. We don't have a single control that is associated with this
        ''' property - it is merged with the TreatSpecificWarningsAsErrors and the Option Strict and displayed
        ''' in the warnings grid view
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function NoWarnGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If _noWarn IsNot Nothing Then
                value = ConcatenateNumbers(_noWarn)
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Custom handling of the NoWarn property. We don't have a single control that is associated with this
        ''' property - it is merged with the TreatSpecificWarningsAsErrors and the Option Strict and displayed
        ''' in the warnings grid view.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function NoWarnSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate OrElse value Is PropertyControlData.MissingProperty Then
                _noWarn = Nothing
            Else
                If TypeOf value IsNot String Then
                    Debug.Fail("Expected a string value for property NoWarn")
                    Throw Common.CreateArgumentException(NameOf(value))
                End If
                _noWarn = SplitToNumbers(DirectCast(value, String))
            End If
            If Not m_fInsideInit Then
                ' Settings this require us to update the warnings grid view...
                UpdateWarningList()
            End If
            Return True
        End Function
#End Region

#Region "TreatSpecificWarningsAsErrors getter and setter"
        ''' <summary>
        ''' Custom handling of the TreatSpecificWarningsAsErrors property. We don't have a single control that is associated with this
        ''' property - it is merged with the NoWarn and the Option Strict and displayed
        ''' in the warnings grid view.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function SpecWarnAsErrorGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Debug.Assert(_specWarnAsError IsNot Nothing)
            value = ConcatenateNumbers(_specWarnAsError)
            Return True
        End Function

        ''' <summary>
        ''' Custom handling of the TreatSpecificWarningsAsErrors property. We don't have a single control that is associated with this
        ''' property - it is merged with the NoWarn and the Option Strict and displayed
        ''' in the warnings grid view.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function SpecWarnAsErrorSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate OrElse value Is PropertyControlData.MissingProperty Then
                _specWarnAsError = Nothing
            Else
                If TypeOf value IsNot String Then
                    Debug.Fail("Expected a string value for property SpecWarnAsError")
                    Throw Common.CreateArgumentException(NameOf(value))
                End If
                _specWarnAsError = SplitToNumbers(DirectCast(value, String))
            End If
            If Not m_fInsideInit Then
                ' Changing this property requires us to update the warnings grid view...
                UpdateWarningList()
            End If
            Return True
        End Function
#End Region

#Region "WarningLevel getter and setter"
        ''' <summary>
        ''' Property getter for warning level
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function WarningLevelGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Select Case DisableAllWarningsCheckBox.CheckState
                Case CheckState.Checked
                    value = VSLangProj.prjWarningLevel.prjWarningLevel0 'Warning Level 0 = off
                    Return True
                Case CheckState.Unchecked
                    value = VSLangProj.prjWarningLevel.prjWarningLevel1 'Warning Level 1 = on
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Property setter for warning level
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function WarningLevelSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If value Is PropertyControlData.Indeterminate Then
                DisableAllWarningsCheckBox.CheckState = CheckState.Indeterminate
            Else
                If CType(value, VSLangProj.prjWarningLevel) = VSLangProj.prjWarningLevel.prjWarningLevel0 Then
                    DisableAllWarningsCheckBox.CheckState = CheckState.Checked
                Else
                    DisableAllWarningsCheckBox.CheckState = CheckState.Unchecked
                End If
            End If
            Return True
        End Function
#End Region

#Region "OptionStrict getter and setter"
        Private Function OptionStrictGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim strValue As String = CStr(OptionStrictComboBox.SelectedItem)
            If _optionStrictCustomText.Equals(strValue, StringComparison.Ordinal) Then
                value = VSLangProj.prjOptionStrict.prjOptionStrictOff
            Else
                value = prop.Converter.ConvertFrom(strValue)
            End If
            Return True
        End Function

        Private Function OptionStrictSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If value IsNot Nothing Then
                Try
                    If Not PropertyControlData.IsSpecialValue(value) Then
                        Dim strValue As String = prop.Converter.ConvertToString(value)
                        OptionStrictComboBox.SelectedIndex = OptionStrictComboBox.Items.IndexOf(strValue)
                    Else
                        OptionStrictComboBox.SelectedIndex = -1
                    End If
                    If Not m_fInsideInit Then
                        QueueUpdateOptionStrictComboBox()
                        UpdateWarningList()
                    End If
                    Return True
                Catch ex As Exception When Common.ReportWithoutCrash(ex, $"Failed to convert {value} to string", NameOf(CompilePropPage2))
                End Try
            Else
                Debug.Fail("Why did we get a NULL value for option strict?")
            End If
            Return False
        End Function
#End Region

#Region "OutputPath getter"
        Private Function OutputPathGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = GetProjectRelativeDirectoryPath(Trim(BuildOutputPathTextBox.Text))
            Return True
        End Function
#End Region
#End Region

        ''' <summary>
        ''' We need to reset our cached extended objects every time someone calls SetObjects
        ''' </summary>
        Public Overrides Sub SetObjects(objects() As Object)
            If _objectCache IsNot Nothing Then
                _objectCache.Reset(ProjectHierarchy, ServiceProvider, True)
            End If
            MyBase.SetObjects(objects)
        End Sub

#Region "Pre/post init page"

        ''' <summary>
        ''' For some reason, AnyCPU includes as space when returned by the IVsConfigProvider.Get*PlatformNames
        ''' but should *not* include a space when passed to the compiler/set the property value
        ''' </summary>
        Private Const AnyCPUPropertyValue As String = "AnyCPU"
        Private Const AnyCPUPlatformName As String = "Any CPU"

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
            'Add any special init code here
            Dock = DockStyle.Fill

            Dim data As PropertyControlData = GetPropertyControlData("OptionStrict")
            Dim _TypeConverter As TypeConverter = data.PropDesc.Converter

            If _TypeConverter IsNot Nothing Then
                'Get the localized text for On/Off
                OptionStrictComboBox.Items.Clear()
                For Each o As Object In _TypeConverter.GetStandardValues()
                    If CInt(o) = VSLangProj.prjOptionStrict.prjOptionStrictOn Then
                        _optionStrictOnText = _TypeConverter.ConvertToString(o)
                        OptionStrictComboBox.Items.Add(_optionStrictOnText)
                    ElseIf CInt(o) = VSLangProj.prjOptionStrict.prjOptionStrictOff Then
                        _optionStrictOffText = _TypeConverter.ConvertToString(o)
                        OptionStrictComboBox.Items.Add(_optionStrictOffText)
                    End If
                Next
            End If

            _optionStrictCustomText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_OptionStrict_Custom

            Dim PlatformEntries As New List(Of String)

            ' Let's try to sniff the supported platforms from our hierarchy (if any)
            TargetCPUComboBox.Items.Clear()
            If ProjectHierarchy IsNot Nothing Then
                Dim oCfgProv As Object = Nothing
                Dim hr As Integer
                hr = ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, oCfgProv)
                If VSErrorHandler.Succeeded(hr) Then
                    Dim cfgProv As IVsCfgProvider2 = TryCast(oCfgProv, IVsCfgProvider2)
                    If cfgProv IsNot Nothing Then
                        Dim actualPlatformCount(0) As UInteger
                        hr = cfgProv.GetSupportedPlatformNames(0, Nothing, actualPlatformCount)
                        If VSErrorHandler.Succeeded(hr) Then
                            Dim platformCount As UInteger = actualPlatformCount(0)
                            Dim platforms(CInt(platformCount)) As String
                            hr = cfgProv.GetSupportedPlatformNames(platformCount, platforms, actualPlatformCount)
                            If VSErrorHandler.Succeeded(hr) Then
                                For platformNo As Integer = 0 To CInt(platformCount - 1)
                                    If AnyCPUPlatformName.Equals(platforms(platformNo), StringComparison.Ordinal) Then
                                        PlatformEntries.Add(AnyCPUPropertyValue)
                                    Else
                                        PlatformEntries.Add(platforms(platformNo))
                                    End If
                                Next
                            End If
                        End If
                    End If
                End If
            End If

            ' ...and if we couldn't get 'em from the project system, let's add a hard-coded list of platforms...
            If PlatformEntries.Count = 0 Then
                Debug.Fail("Unable to get platform list from configuration manager")
                PlatformEntries.AddRange(New String() {"AnyCPU", "x86", "x64", "Itanium"})
            End If
            If VSProductSKU.ProductSKU < VSProductSKU.VSASKUEdition.Enterprise Then
                'For everything lower than VSTS (SKU# = Enterprise), don't target Itanium
                PlatformEntries.Remove("Itanium")
            End If

            ' ... Finally, add the entries to the combobox
            TargetCPUComboBox.Items.AddRange(PlatformEntries.ToArray())

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

            'OutputPath browse button should only be enabled when the text box is enabled and Not ReadOnly
            BuildOutputPathButton.Enabled = BuildOutputPathTextBox.Enabled AndAlso Not BuildOutputPathTextBox.ReadOnly
            EnableControl(RegisterForComInteropCheckBox, RegisterForComInteropSupported())

            'Populate Error/Warnings list
            PopulateErrorList()
            QueueUpdateOptionStrictComboBox()
            EnableAllControls(Enabled)

            'Hide all non-Express controls
            If VSProductSKU.IsExpress Then
                BuildEventsButton.Visible = False
            End If

            RefreshEnabledStatusForPrefer32Bit(Prefer32BitCheckBox)

            MinimumSize = GetPreferredSize(Drawing.Size.Empty)
        End Sub
#End Region

        Public Enum ErrorNotification
            None
            Warning
            [Error]
        End Enum

        Private Class ErrorInfo
            Public ReadOnly Title As String
            Public ReadOnly Numbers As String
            Public ReadOnly Notification As ErrorNotification
            Public ReadOnly ErrorOnOptionStrict As Boolean
            Public Index As Integer
            Public ReadOnly ErrList As Integer()
            Public Sub New(Title As String, Numbers As String, Notification As ErrorNotification, ErrorOnOptionStrict As Boolean, ErrList As Integer())
                Me.Title = Title
                Me.Numbers = Numbers
                Me.Notification = Notification
                Me.ErrorOnOptionStrict = ErrorOnOptionStrict
                Me.ErrList = ErrList
                Array.Sort(Me.ErrList)
            End Sub
        End Class

        Private ReadOnly _errorInfos As ErrorInfo() = {
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42016, "42016,41999", ErrorNotification.None, True, New Integer() {42016, 41999}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42017_42018_42019, "42017,42018,42019,42032,42036", ErrorNotification.None, True, New Integer() {42017, 42018, 42019, 42032, 42036}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42020, "42020,42021,42022", ErrorNotification.None, True, New Integer() {42020, 42021, 42022}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42104, "42104,42108,42109,42030", ErrorNotification.None, False, New Integer() {42104, 42108, 42109, 42030}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42105_42106_42107, "42105,42106,42107", ErrorNotification.None, False, New Integer() {42105, 42106, 42107}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42353_42354_42355, "42353,42354,42355", ErrorNotification.None, False, New Integer() {42353, 42354, 42355}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42024, "42024,42099", ErrorNotification.None, False, New Integer() {42024, 42099}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42025, "42025", ErrorNotification.None, False, New Integer() {42025}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42004, "41998,42004,42026,", ErrorNotification.None, False, New Integer() {41998, 42004, 42026}),
            New ErrorInfo(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_42029, "42029,42031", ErrorNotification.None, False, New Integer() {42029, 42031})}

        Private Sub PopulateErrorList()
            Dim Index As Integer
            Dim row As DataGridViewRow

            With WarningsGridView
                .Rows.Clear()
                .ScrollBars = ScrollBars.Vertical

                For Each ErrorInfo As ErrorInfo In _errorInfos
                    Index = .Rows.Add(ErrorInfo.Title) ', NotificationText)
                    row = .Rows.Item(Index)
                    ErrorInfo.Index = Index
                Next

                .AutoResizeColumn(ConditionColumnIndex, DataGridViewAutoSizeColumnMode.DisplayedCells)
                .AutoResizeColumn(NotifyColumnIndex, DataGridViewAutoSizeColumnMode.DisplayedCells)
                .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
                .RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
            End With

            'Now flip the toggles whether the user has toggled the warning/error
            UpdateWarningList()

        End Sub

#Region "Helper methods to map UI values to properties"

        Private Function IsOptionStrictOn() As Boolean
            Return _optionStrictOnText.Equals(CStr(OptionStrictComboBox.SelectedItem), StringComparison.Ordinal)
        End Function

        Private Function IsOptionStrictOff() As Boolean
            Return _optionStrictOffText.Equals(CStr(OptionStrictComboBox.SelectedItem), StringComparison.Ordinal)
        End Function

        Private Function IsOptionStrictCustom() As Boolean
            Return _optionStrictCustomText.Equals(CStr(OptionStrictComboBox.SelectedItem), StringComparison.Ordinal)
        End Function

        Private Function TreatAllWarningsAsErrors() As Boolean
            Return WarningsAsErrorCheckBox.CheckState = CheckState.Checked
        End Function

        Private Function DisableAllWarnings() As Boolean
            Return DisableAllWarningsCheckBox.CheckState = CheckState.Checked
        End Function

        ''' <summary>
        ''' We are in an indeterminate state if we have conflicting settings in
        ''' different configurations
        ''' </summary>
        ''' <remarks>
        ''' We shouldn't be in this situation unless the user has messed around manually with
        ''' the project file...
        ''' </remarks>
        Private ReadOnly Property IndeterminateWarningsState As Boolean
            Get
                If WarningsAsErrorCheckBox.CheckState = CheckState.Indeterminate Then
                    Return True
                End If

                If DisableAllWarningsCheckBox.CheckState = CheckState.Indeterminate Then
                    Return True
                End If

                If _noWarn Is Nothing Then
                    Return True
                End If

                If _specWarnAsError Is Nothing Then
                    Return True
                End If

                Return False
            End Get
        End Property

#End Region

        Private Sub DisableAllWarningsCheckBox_Checked(sender As Object, e As EventArgs) Handles DisableAllWarningsCheckBox.CheckStateChanged
            If Not m_fInsideInit AndAlso Not DisableAllWarningsCheckBox.CheckState = CheckState.Indeterminate Then
                UpdateWarningList()
                EnableDisableWarningControls(Enabled)
                SetDirty(DisableAllWarningsCheckBox, True)
            End If
        End Sub

        ''' <summary>
        ''' We use an empty cell to indicate that levels conflict
        ''' </summary>
        Private Sub WarningsGridView_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs) Handles WarningsGridView.CellFormatting
            If e.ColumnIndex = NotifyColumnIndex Then
                ' If either this is in an indeterminate state because we have different warning levels 
                ' in different configurations, or if the current value is indeterminate (DBNull) because
                ' only a subset of the values the make up this row's set of warning id's were included in
                ' the string(s), we paint the current cell blank...
                Dim isBlankCell As Boolean
                If e.Value Is DBNull.Value Then
                    isBlankCell = True
                ElseIf IndeterminateWarningsState Then
                    If Not _errorInfos(e.RowIndex).ErrorOnOptionStrict OrElse IsOptionStrictCustom() Then
                        isBlankCell = True
                    End If
                End If
                If isBlankCell Then
                    e.Value = ""
                    e.FormattingApplied = True
                End If
            End If
        End Sub

        Private Sub WarningsGridView_EditingControlShowing(sender As Object, e As DataGridViewEditingControlShowingEventArgs) Handles WarningsGridView.EditingControlShowing
            With e.CellStyle
                .BackColor = WarningsGridView.BackgroundColor
                .ForeColor = WarningsGridView.ForeColor
            End With
        End Sub

        Private Sub WarningsAsErrorCheckBox_Checked(sender As Object, e As EventArgs) Handles WarningsAsErrorCheckBox.CheckStateChanged
            If Not m_fInsideInit AndAlso Not WarningsAsErrorCheckBox.CheckState = CheckState.Indeterminate Then
                UpdateWarningList()
                EnableDisableWarningControls(Enabled)
                SetDirty(WarningsAsErrorCheckBox, True)
            End If
        End Sub

        ''' <summary>
        ''' Make sure we set the Register for COM interop property whenever the
        ''' user checks the corresponding checkbox on the property page
        ''' </summary>
        Private Sub RegisterForComInteropCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles RegisterForComInteropCheckBox.CheckedChanged
            If Not m_fInsideInit Then
                If RegisterForComInteropCheckBox.Checked Then
                    ' Whenever the user checks the register for Com interop, we should also set the COM visible property
                    _comVisible = True
                    SetDirty(VsProjPropId80.VBPROJPROPID_ComVisible, False)
                End If
                SetDirty(VsProjPropId.VBPROJPROPID_RegisterForComInterop, True)
            End If
        End Sub

        ''' <summary>
        ''' Get the value for ComVisible. 
        ''' </summary>
        Private Function ComVisibleGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = _comVisible
            Return True
        End Function

        ''' <summary>
        ''' Set the current value for the COM Visible property
        ''' </summary>
        Private Function ComVisibleSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            _comVisible = value
            Return True
        End Function

        ''' <summary>
        ''' Called whenever the property page detects that a property defined on this property page is changed in the
        '''   project system.  Property changes made directly by an apply or through PropertyControlData will not come
        '''   through this method.
        ''' </summary>
        Protected Overrides Sub OnExternalPropertyChanged(DISPID As Integer, Source As PropertyChangeSource)
            MyBase.OnExternalPropertyChanged(DISPID, Source)

            'If the project's OutputType has changed, the Register for COM Interop control's visibility might need to change
            If Source <> PropertyChangeSource.Direct AndAlso (DISPID = DISPID_UNKNOWN OrElse DISPID = VsProjPropId.VBPROJPROPID_OutputType) Then
                EnableControl(RegisterForComInteropCheckBox, RegisterForComInteropSupported())

                ' Changes to the OutputType may affect whether 'Prefer32Bit' is enabled
                RefreshEnabledStatusForPrefer32Bit(Prefer32BitCheckBox)
            End If
        End Sub

        ''' <summary>
        ''' Disables warnings which are not generated when Option Strict is on
        ''' (Option Strict On will generate error ids, not the warning ids)
        ''' </summary>
        Private Sub UpdateWarningList()
            ' Depending on the order that we are populating the controls,
            ' we may get called to update the warnings list before we have
            ' added any rows (i.e. setting option strict will cause this)
            If WarningsGridView.RowCount = 0 Then
                ' This should only happen during init!
                Debug.Assert(m_fInsideInit, "Why didn't we have any rows in the warnings grid view outside of init?")
                Exit Sub
            End If

            Dim savedRefreshingWarningsList As Boolean = _refreshingWarningsList
            If savedRefreshingWarningsList Then
                Debug.Fail("Recursive update of warnings list...")
            End If
            Try
                _refreshingWarningsList = True
                If WarningsGridView.IsCurrentCellInEditMode Then
                    WarningsGridView.CancelEdit()
                    WarningsGridView.CurrentCell = Nothing
                End If

                Dim rows As DataGridViewRowCollection = WarningsGridView.Rows
                Dim ComboboxCell As DataGridViewComboBoxCell

                For Each ErrorInfo As ErrorInfo In _errorInfos
                    Dim row As DataGridViewRow = rows.Item(ErrorInfo.Index)

                    ComboboxCell = DirectCast(row.Cells(NotifyColumnIndex), DataGridViewComboBoxCell)

                    'Check for this error in NoWarn.Text or SpecWarnAsErrorTextBox.Text
                    If IsOptionStrictOn() AndAlso ErrorInfo.ErrorOnOptionStrict Then
                        ' Option Strict ON overrides everything below
                        ComboboxCell.Value = _notifyError
                    ElseIf DisableAllWarnings() Then
                        ' If the DisableAllWarnings checkbox is checked we will set this to NotifyNone
                        ' and not care about warning levels for specific warnings...
                        ComboboxCell.Value = _notifyNone
                    ElseIf TreatAllWarningsAsErrors() AndAlso _noWarn IsNot Nothing AndAlso AreNumbersInList(_noWarn, ErrorInfo.ErrList) = TriState.False Then
                        ' If the TreatWarningsAsErrors checkbox is checked we will set this to NotifyError
                        ' (since we already know that DisableAllWarnings wasn't checked)
                        ComboboxCell.Value = _notifyError
                    Else
                        ' If none of the above, we have to check the lists of specific errors to
                        ' ignore/report as errors
                        Dim IsNoWarn, IsWarnAsError As TriState
                        If _noWarn IsNot Nothing Then
                            IsNoWarn = AreNumbersInList(_noWarn, ErrorInfo.ErrList)
                        Else
                            IsNoWarn = TriState.UseDefault
                        End If

                        If _specWarnAsError IsNot Nothing Then
                            IsWarnAsError = AreNumbersInList(_specWarnAsError, ErrorInfo.ErrList)
                        Else
                            IsWarnAsError = TriState.UseDefault
                        End If

                        'NOTE: Order of test is important
                        If IsNoWarn = TriState.True Then
                            ComboboxCell.Value = _notifyNone
                        ElseIf IsWarnAsError = TriState.True AndAlso IsNoWarn <> TriState.UseDefault Then
                            ComboboxCell.Value = _notifyError
                        ElseIf IsNoWarn = TriState.False AndAlso IsWarnAsError = TriState.False Then
                            ComboboxCell.Value = _notifyWarning
                        Else
                            ComboboxCell.Value = DBNull.Value
                        End If
                    End If
                Next

                QueueUpdateOptionStrictComboBox()

            Finally
                _refreshingWarningsList = savedRefreshingWarningsList
            End Try
        End Sub

#Region "Set related functions (join/intersect/union)"
        ''' <summary>
        ''' Concatenate an array of integers into a comma-separated string of numbers
        ''' </summary>
        Private Shared Function ConcatenateNumbers(numbers() As Integer) As String
            Dim strNumbers(numbers.Length - 1) As String
            For i As Integer = 0 To numbers.Length - 1
                strNumbers(i) = numbers(i).ToString()
            Next
            Return String.Join(",", strNumbers)
        End Function

        ''' <summary>
        ''' Split a comma-separated string into a sorted array of numbers
        ''' </summary>
        Private Shared Function SplitToNumbers(numberString As String) As Integer()
            If numberString Is Nothing Then
                Debug.Fail("NULL Argument")
                Throw New ArgumentNullException()
            End If

            Dim result As New List(Of Integer)

            For Each strNumber As String In numberString.Split(","c)
                Dim Number As Double
                If Double.TryParse(strNumber, Number) Then
                    If Number >= 0 AndAlso Number < Integer.MaxValue Then
                        result.Add(CInt(Number))
                    End If
                End If
            Next
            result.Sort()
            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Return the intersection of the two *sorted* arrays set1 and set2
        ''' </summary>
        ''' <remarks>Both set1 and set2 must be sorted for this to work correctly!</remarks>
        Private Shared Function Intersect(set1() As Integer, set2() As Integer) As Integer()
            Dim indexSet1 As Integer = 0
            Dim indexSet2 As Integer = 0

            Dim result As New List(Of Integer)
            Do While indexSet1 < set1.Length AndAlso indexSet2 < set2.Length
                ' Walk while the items in set1 are less than the item we are looking
                ' at in set2
                While set1(indexSet1) < set2(indexSet2)
                    indexSet1 += 1
                    If indexSet1 >= set1.Length Then Exit Do
                End While

                ' If the items are equal, add and move to next
                If set1(indexSet1) = set2(indexSet2) Then
                    result.Add(set1(indexSet1))
                    indexSet1 += 1
                End If
                indexSet2 += 1
            Loop
            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Return the union of the two *sorted* arrays set1 and set2
        ''' </summary>
        ''' <remarks>Both set1 and set2 must be sorted for this to work correctly!</remarks>
        Private Shared Function Union(set1() As Integer, set2() As Integer) As Integer()
            Dim indexSet1 As Integer = 0
            Dim indexSet2 As Integer = 0

            Dim result As New List(Of Integer)
            If set1 IsNot Nothing AndAlso set2 IsNot Nothing Then
                Do While indexSet1 < set1.Length AndAlso indexSet2 < set2.Length
                    ' Add all numbers from set1 that are less than the currently selected
                    ' item in set2
                    While set1(indexSet1) < set2(indexSet2)
                        result.Add(set1(indexSet1))
                        indexSet1 += 1
                        If indexSet1 >= set1.Length Then Exit Do
                    End While

                    ' We should only add one of the items if
                    ' the currently selected item in set1 and set2
                    ' are equal - make sure of that by bumping the index
                    ' for set1 up one notch!
                    If set1(indexSet1) = set2(indexSet2) Then
                        indexSet1 += 1
                    End If
                    result.Add(set2(indexSet2))
                    indexSet2 += 1
                Loop

                ' Add the remaining items
                For i As Integer = indexSet1 To set1.Length - 1
                    result.Add(set1(i))
                Next

                For i As Integer = indexSet2 To set2.Length - 1
                    result.Add(set2(i))
                Next
            End If
            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Remove any items in itemsToRemove from completeSet
        ''' </summary>
        ''' <remarks>Both set1 and set2 must be sorted for this to work correctly!</remarks>
        Private Shared Function RemoveItems(completeSet() As Integer, itemsToRemove() As Integer) As Integer()
            Dim indexCompleteSet As Integer = 0
            Dim indexItemsToRemove As Integer = 0

            Dim result As New List(Of Integer)
            If completeSet IsNot Nothing Then
                If itemsToRemove Is Nothing Then
                    itemsToRemove = Array.Empty(Of Integer)
                End If

                Do While indexCompleteSet < completeSet.Length AndAlso indexItemsToRemove < itemsToRemove.Length
                    ' Walk while the items in the set to remove is less than the items in the
                    ' complete set
                    While itemsToRemove(indexItemsToRemove) < completeSet(indexCompleteSet)
                        indexItemsToRemove += 1
                        If indexItemsToRemove >= itemsToRemove.Length Then Exit Do
                    End While

                    ' If we have a match, we should skip this item from adding to the result set
                    If itemsToRemove(indexItemsToRemove) = completeSet(indexCompleteSet) Then
                        indexItemsToRemove += 1
                    Else
                        result.Add(completeSet(indexCompleteSet))
                    End If
                    indexCompleteSet += 1
                Loop

                ' Add the remaining items from the complete set
                For i As Integer = indexCompleteSet To completeSet.Length - 1
                    result.Add(completeSet(i))
                Next
            End If
            Return result.ToArray()
        End Function

        ''' <summary>
        ''' Check if the numbers specified in SearchForNumbers are all included in the CompleteList
        ''' </summary>
        Private Shared Function AreNumbersInList(CompleteList As Integer(), SearchForNumbers As Integer()) As TriState
            Dim foundNumbers As Integer = Intersect(CompleteList, SearchForNumbers).Length
            Dim numberOfItemsToFind As Integer = SearchForNumbers.Length

            If foundNumbers = numberOfItemsToFind Then
                Return TriState.True
            ElseIf foundNumbers = 0 Then
                Return TriState.False
            Else
                Return TriState.UseDefault
            End If
        End Function
#End Region

        Private Sub AdvancedOptionsButton_Click(sender As Object, e As EventArgs) Handles AdvancedOptionsButton.Click
            ShowChildPage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AdvancedCompilerSettings_Title, GetType(AdvCompilerSettingsPropPage), HelpKeywords.VBProjPropAdvancedCompile)
        End Sub

        Private Sub BuildEventsButton_Click(sender As Object, e As EventArgs) Handles BuildEventsButton.Click
            ShowChildPage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_BuildEventsTitle, GetType(BuildEventsPropPage))
        End Sub

        Private Sub BuildOutputPathButton_Click(sender As Object, e As EventArgs) Handles BuildOutputPathButton.Click
            Dim value As String = Nothing
            If GetDirectoryViaBrowseRelativeToProject(BuildOutputPathTextBox.Text, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_SelectOutputPathTitle, value) Then
                BuildOutputPathTextBox.Text = value
                SetDirty(BuildOutputPathTextBox, True)
            End If
        End Sub

        Private Sub GenerateXMLCheckBox_CheckStateChanged(sender As Object, e As EventArgs) Handles GenerateXMLCheckBox.CheckStateChanged
            If Not m_fInsideInit AndAlso Not _settingGenerateXmlDocumentation Then
                SetDirty(VsProjPropId.VBPROJPROPID_DocumentationFile, True)
            End If
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropCompile
        End Function

        Private Function PlatformTargetSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                TargetCPUComboBox.SelectedIndex = -1
            Else
                If IsNothing(TryCast(value, String)) OrElse TryCast(value, String) = "" Then
                    TargetCPUComboBox.SelectedItem = AnyCPUPropertyValue
                Else
                    TargetCPUComboBox.SelectedItem = TryCast(value, String)
                End If
            End If

            Return True
        End Function

        Private Function PlatformTargetGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = TargetCPUComboBox.SelectedItem
            Return True
        End Function

#Region "Check if the current warning level settings correspond to option strict on, off or custom"
        ''' <summary>
        ''' Check to see if the warnings lists exactly correspond to the
        ''' Option Strict OFF settings
        ''' </summary>
        Private Function IsSameAsOptionStrictOff() As Boolean
            If _specWarnAsError IsNot Nothing AndAlso
               _noWarn IsNot Nothing AndAlso
               AreNumbersInList(_noWarn, _optionStrictIDs) = TriState.True AndAlso
               AreNumbersInList(_specWarnAsError, _optionStrictIDs) = TriState.False _
            Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Check to see if the warnings lists exactly correspond to the
        ''' Option Strict ON settings
        ''' </summary>
        Private Function IsSameAsOptionStrictOn() As Boolean
            If _specWarnAsError IsNot Nothing AndAlso
               _noWarn IsNot Nothing AndAlso
               AreNumbersInList(_specWarnAsError, _optionStrictIDs) = TriState.True AndAlso
               AreNumbersInList(_noWarn, _optionStrictIDs) = TriState.False _
            Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Is this option strict custom?
        ''' </summary>
        Private Function IsSameAsOptionStrictCustom() As Boolean
            Return Not IsSameAsOptionStrictOn() AndAlso Not IsSameAsOptionStrictOff()
        End Function
#End Region

        Private Sub UpdatePropertiesFromCurrentState()
            ' If we are inside init, we are in the process of updating the list or if we have set
            ' a "global" treatment of (disable all warnings/treat all warnings as errors) we skip the actual set...
            If Not (m_fInsideInit OrElse _refreshingWarningsList OrElse TreatAllWarningsAsErrors() OrElse DisableAllWarnings()) Then
                'Get/Set the entire property set
                'Enumerate the rows and get the values to write
                Dim cell As DataGridViewCell
                Dim CellValue As String

                Dim ErrorsList As New List(Of Integer)
                Dim NoNotifyList As New List(Of Integer)

                For Index As Integer = 0 To WarningsGridView.Rows.Count - 1
                    cell = WarningsGridView.Rows.Item(Index).Cells.Item(1)
                    CellValue = DirectCast(cell.EditedFormattedValue, String)
                    Dim Numbers As String = _errorInfos(Index).Numbers
                    If Numbers <> "" Then
                        If CellValue.Equals(_notifyNone) Then
                            For Each err As Integer In _errorInfos(Index).ErrList
                                NoNotifyList.Add(err)
                            Next
                        ElseIf CellValue.Equals(_notifyError) Then
                            For Each err As Integer In _errorInfos(Index).ErrList
                                ErrorsList.Add(err)
                            Next
                        ElseIf CellValue = "" Then
                            ' This is an indeterminate value - we should keep whatever we have in there
                            ' from before...
                            If _noWarn Is Nothing Then
                                Debug.Fail("Why did we try to update properties from current set with an empty noWarn?")
                                _noWarn = Array.Empty(Of Integer)
                            End If
                            For Each err As Integer In Intersect(_errorInfos(Index).ErrList, _noWarn)
                                NoNotifyList.Add(err)
                            Next
                            If _specWarnAsError Is Nothing Then
                                Debug.Fail("Why did we try to update properties from current set with an empty specWarnAsError?")
                                _specWarnAsError = Array.Empty(Of Integer)
                            End If
                            For Each err As Integer In Intersect(_errorInfos(Index).ErrList, _specWarnAsError)
                                ErrorsList.Add(err)
                            Next
                        End If
                    End If
                Next

                _noWarn = NoNotifyList.ToArray()
                _specWarnAsError = ErrorsList.ToArray()

                Array.Sort(_noWarn)
                Array.Sort(_specWarnAsError)

                ' Update option strict combobox...
                Dim optionStrictChanged As Boolean = False
                If (Not IsSameAsOptionStrictOn()) AndAlso IsOptionStrictOn() Then
                    OptionStrictComboBox.SelectedIndex = OptionStrictComboBox.Items.IndexOf(_optionStrictOffText)
                    optionStrictChanged = True
                    ' Potentially update option strict to "custom"
                ElseIf IsSameAsOptionStrictOn() AndAlso (Not IsOptionStrictOn()) Then
                    optionStrictChanged = True
                    OptionStrictComboBox.SelectedIndex = OptionStrictComboBox.Items.IndexOf(_optionStrictOnText)
                End If

                QueueUpdateOptionStrictComboBox()
                SetDirty(VsProjPropId2.VBPROJPROPID_NoWarn, False)
                SetDirty(VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors, False)
                If optionStrictChanged Then
                    SetDirty(VsProjPropId.VBPROJPROPID_OptionStrict, False)
                End If
                SetDirty(True)
            End If
        End Sub

        ''' <summary>
        ''' Set the warnings to ignore/warnings to report as error to correspond to the
        ''' option strictness that we have...
        ''' </summary>
        ''' <param name="Value"></param>
        Private Sub ResetOptionStrictness(Value As String)
            OptionStrictComboBox.SelectedItem = Value
            Select Case Value
                Case _optionStrictOnText
                    _noWarn = RemoveItems(_noWarn, _optionStrictIDs)
                    _specWarnAsError = Union(_specWarnAsError, _optionStrictIDs)
                Case _optionStrictOffText
                    _specWarnAsError = RemoveItems(_specWarnAsError, _optionStrictIDs)
                    _noWarn = Union(_noWarn, _optionStrictIDs)
                Case _optionStrictCustomText
                    ' Just leave things as they are...
                Case Else
                    Debug.Fail("Unknown option strict level: " & Value)
            End Select

            SetDirty(VsProjPropId80.VBPROJPROPID_TreatSpecificWarningsAsErrors, False)
            SetDirty(VsProjPropId2.VBPROJPROPID_NoWarn, False)
            SetDirty(OptionStrictComboBox, False)
            SetDirty(True)
            If ProjectReloadedDuringCheckout Then
                Return
            End If
            UpdateWarningList()
        End Sub

        ''' <summary>
        ''' Whenever the user changes the option strict combobox in the UI, we have to
        ''' update the corresponding project properties
        ''' </summary>
        Private Sub OptionStrictComboBox_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles OptionStrictComboBox.SelectionChangeCommitted
            If Not m_fInsideInit Then
                ResetOptionStrictness(TryCast(OptionStrictComboBox.SelectedItem, String))
            End If
        End Sub

        Private Sub TargetCPUComboBox_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles TargetCPUComboBox.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            ' Changes to the TargetCPU may affect whether 'Prefer32Bit' is enabled
            RefreshEnabledStatusForPrefer32Bit(Prefer32BitCheckBox)
        End Sub

        ''' <summary>
        ''' Override PreApplyPageChanges to validate and potentially warn the user about untrusted output
        ''' path.
        ''' </summary>
        Protected Overrides Sub PreApplyPageChanges()
            If GetPropertyControlData(VsProjPropId.VBPROJPROPID_OutputPath).IsDirty Then
                Try
                    Dim absPath As String = Path.Combine(GetProjectPath(), GetProjectRelativeDirectoryPath(Trim(BuildOutputPathTextBox.Text)))
                    If Not CheckPath(absPath) Then
                        If DesignerFramework.DesignerMessageBox.Show(ServiceProvider,
                                                                    My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_OutputPathNotSecure,
                                                                    DesignerFramework.DesignUtil.GetDefaultCaption(ServiceProvider),
                                                                    MessageBoxButtons.OKCancel,
                                                                    MessageBoxIcon.Warning) = DialogResult.Cancel _
                        Then
                            ' Set the focus back to the offending control!
                            BuildOutputPathTextBox.Focus()
                            BuildOutputPathTextBox.Clear()
                            Throw New System.Runtime.InteropServices.COMException("", Interop.Win32Constant.OLE_E_PROMPTSAVECANCELLED)
                        End If
                    End If
                Catch ex As ApplicationException
                    ' The old behavior was to assume a secure path if exception occured...
                End Try
            End If
            MyBase.PreApplyPageChanges()
        End Sub

        ''' <summary>
        ''' Check if the path is a trusted path or not
        ''' </summary>
        ''' <param name="path"></param>
        ''' <remarks>
        ''' This code was ported from langutil.cpp (function LuCheckSecurityLevel)
        ''' If that code ever changes, we've gotta update this as well...
        ''' </remarks>
        Private Function CheckPath(path As String) As Boolean
            Dim zone As Security.SecurityZone = Common.GetSecurityZoneOfFile(path, ServiceProvider)

            Dim folderEvidence As Security.Policy.Evidence = New Security.Policy.Evidence()
            folderEvidence.AddHostEvidence(New Security.Policy.Url("file:///" & IO.Path.GetFullPath(path)))
            folderEvidence.AddHostEvidence(New Security.Policy.Zone(zone))
            Dim folderPSet As Security.PermissionSet = Security.SecurityManager.GetStandardSandbox(folderEvidence)

            ' Get permission set that is granted to local code running on the local machine.
            Dim localEvidence As New Security.Policy.Evidence()
            localEvidence.AddHostEvidence(New Security.Policy.Zone(Security.SecurityZone.MyComputer))

            Dim localPSet As Security.PermissionSet = Security.SecurityManager.GetStandardSandbox(localEvidence)
            localPSet.RemovePermission(New Security.Permissions.ZoneIdentityPermission(Security.SecurityZone.MyComputer).GetType())

            ' Return true if permission set that would be granted to code in
            ' target folder is equal (or greater than) that granted to local code.
            If localPSet.IsSubsetOf(folderPSet) Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Set the drop-down width of comboboxes with user-handled events so they'll fit their contents
        ''' </summary>
        Private Sub ComboBoxes_DropDown(sender As Object, e As EventArgs) Handles OptionStrictComboBox.DropDown
            Common.SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Event handler for value changed events fired from the warnings grid view
        ''' </summary>
        Private Sub NotificationLevelChanged(sender As Object, e As DataGridViewCellEventArgs) Handles WarningsGridView.CellValueChanged
            If Not m_fInsideInit AndAlso Not _refreshingWarningsList AndAlso e.RowIndex >= 0 AndAlso e.ColumnIndex = NotifyColumnIndex Then
                UpdatePropertiesFromCurrentState()
            End If
        End Sub

        ''' <summary>
        ''' If we have indeterminate values for either the noWarn or specWarnAsError, we have got to
        ''' reset the properties in a known state before we can make any changes.
        '''
        ''' Let's prompt the user so they can make this decision.
        ''' </summary>
        Private Sub EnsureNotConflictingSettings(sender As Object, e As DataGridViewCellCancelEventArgs) Handles WarningsGridView.CellBeginEdit
            If IndeterminateWarningsState Then
                'Prompt user for resetting settings...
                If DesignerFramework.DesignUtil.ShowMessage(ServiceProvider, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Compile_ResetIndeterminateWarningLevels, DesignerFramework.DesignUtil.GetDefaultCaption(ServiceProvider), MessageBoxButtons.OKCancel, MessageBoxIcon.Question) = DialogResult.OK Then
                    _noWarn = _optionStrictIDs
                    _specWarnAsError = Array.Empty(Of Integer)
                    UpdateWarningList()
                Else
                    e.Cancel = True
                End If
            End If
        End Sub

        ''' <summary>
        ''' This is a workaround for event notification not being sent by the datagridview when
        ''' the combobox sends SelectionChangeCommitted
        ''' </summary>
        Friend Class InternalDataGridView
            Inherits DesignerFramework.DesignerDataGridView

            Public Overrides Sub NotifyCurrentCellDirty(dirty As Boolean)
                MyBase.NotifyCurrentCellDirty(dirty)

                If dirty Then
                    CommitEdit(DataGridViewDataErrorContexts.Commit)
                End If
            End Sub

        End Class

        ''' <summary>
        ''' PropertyControlData that always acts as if you have selected the "all configurations/all platforms"
        ''' </summary>
        Friend Class FakeAllConfigurationsPropertyControlData
            Inherits PropertyControlData

            ''' <summary>
            ''' Since it is expensive to get the extended objects, and all PropertyControlDatas on the same
            ''' page share the same set, we keep a shared cache around... All we need is a service provider
            ''' and someone to Reset us when the SetObjects is called...
            ''' </summary>
            Friend Class ConfigurationObjectCache
                ' Cached properties for the extended and raw config objects
                Private _extendedObjects() As Object

                ' Cached instance of our IVsCfgProvider2 instance
                Private _vscfgprovider As IVsCfgProvider2

                ' Cached hierarchy
                Private _hierarchy As IVsHierarchy

                ' Cached service provider
                Private _serviceProvider As IServiceProvider

                ''' <summary>
                ''' Create a new instance of
                ''' </summary>
                Friend Sub New(Hierarchy As IVsHierarchy, ServiceProvider As IServiceProvider)
                    _hierarchy = Hierarchy
                    _serviceProvider = ServiceProvider
                End Sub

                ''' <summary>
                ''' Reset our cached values if we have a new hierarchy and/or serviceprovider
                ''' </summary>
                Friend Sub Reset(Hierarchy As IVsHierarchy, ServiceProvider As IServiceProvider, forceReset As Boolean)
                    If forceReset OrElse _hierarchy IsNot Hierarchy OrElse _serviceProvider IsNot ServiceProvider Then
                        _hierarchy = Hierarchy
                        _serviceProvider = ServiceProvider
                        _extendedObjects = Nothing
                        _vscfgprovider = Nothing
                    End If
                End Sub

                ''' <summary>
                ''' Private getter for the IVsCfgProvider2 for the associated proppage's hierarchy
                ''' </summary>
                Private ReadOnly Property VsCfgProvider As IVsCfgProvider2
                    Get
                        If _vscfgprovider Is Nothing Then
                            Dim Value As Object = Nothing
                            VSErrorHandler.ThrowOnFailure(_hierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID.VSHPROPID_ConfigurationProvider, Value))
                            _vscfgprovider = TryCast(Value, IVsCfgProvider2)
                        End If
                        Debug.Assert(_vscfgprovider IsNot Nothing, "Failed to get config provider")
                        Return _vscfgprovider
                    End Get
                End Property

                ''' <summary>
                ''' Getter for the raw config objects. We override this to always return the properties for all
                ''' configurations to make this property look like a config independent-ish property
                ''' </summary>
                Friend ReadOnly Property ConfigRawPropertiesObjects As Object()
                    Get
                        Dim tmpRawObjects() As IVsCfg
                        Dim ConfigCount As UInteger() = New UInteger(0) {} 'Interop declaration requires us to use an array
                        VSErrorHandler.ThrowOnFailure(VsCfgProvider.GetCfgs(0, Nothing, ConfigCount, Nothing))
                        Debug.Assert(ConfigCount(0) > 0, "Why no configs?")
                        tmpRawObjects = New IVsCfg(CInt(ConfigCount(0)) - 1) {}
                        Dim ActualCount As UInteger() = New UInteger(0) {}
                        VSErrorHandler.ThrowOnFailure(VsCfgProvider.GetCfgs(CUInt(tmpRawObjects.Length), tmpRawObjects, ActualCount, Nothing))
                        Debug.Assert(ActualCount(0) = ConfigCount(0), "Unexpected # of configs returned")
                        Dim rawObjects(tmpRawObjects.Length - 1) As Object
                        tmpRawObjects.CopyTo(rawObjects, 0)
                        Return rawObjects
                    End Get
                End Property

                ''' <summary>
                ''' Getter for the extended config objects. We override this to always return the properties for all
                ''' configurations to make this property look like a config independent-ish property
                ''' </summary>
                Friend ReadOnly Property ConfigExtendedPropertiesObjects As Object()
                    Get
                        If _extendedObjects Is Nothing Then
                            Dim aem As AutomationExtenderManager
                            aem = AutomationExtenderManager.GetAutomationExtenderManager(_serviceProvider)
                            _extendedObjects = aem.GetExtendedObjects(ConfigRawPropertiesObjects)
                            Debug.Assert(_extendedObjects IsNot Nothing, "Extended objects unavailable")
                        End If
                        Return _extendedObjects
                    End Get
                End Property
            End Class

            ' Shared cache of raw & extended configuration objects
            Private ReadOnly _configurationObjectCache As ConfigurationObjectCache

            ' Create a new instance
            Public Sub New(ConfigurationObjectCache As ConfigurationObjectCache, id As Integer, name As String, control As Control, setter As SetDelegate, getter As GetDelegate, flags As ControlDataFlags, AssocControls As Control())
                MyBase.New(id, name, control, setter, getter, flags, AssocControls)
                _configurationObjectCache = ConfigurationObjectCache
            End Sub

            ''' <summary>
            ''' Getter for the raw config objects. We override this to always return the properties for all
            ''' configurations to make this property look like a config independent-ish property
            ''' </summary>
            Public Overrides ReadOnly Property RawPropertiesObjects As Object()
                Get
                    Return _configurationObjectCache.ConfigRawPropertiesObjects()
                End Get
            End Property

            ''' <summary>
            ''' Getter for the extended config objects. We override this to always return the properties for all
            ''' configurations to make this property look like a config independent-ish property
            ''' </summary>
            Public Overrides ReadOnly Property ExtendedPropertiesObjects As Object()
                Get
                    Return _configurationObjectCache.ConfigExtendedPropertiesObjects()
                End Get
            End Property

        End Class

    End Class

End Namespace
