' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Shell

Imports VSLangProj110

Imports VSLangProj158

Imports VSLangProj80

Imports VslangProj90

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Not currently used directly (but it's inherited from)
    '''   - see comments in proppage.vb: "Application property pages (VB and C#)"
    ''' </summary>
    Partial Friend Class ApplicationPropPage
        Inherits ApplicationPropPageInternalBase

        Protected Const Const_SubMain As String = "Sub Main"
        Protected Const Const_DefaultNamespace As String = "DefaultNamespace"
        Protected Const Const_StartupObject As String = "StartupObject"
        Protected Const Const_ApplicationIcon As String = "ApplicationIcon"
        Protected Const Const_ApplicationManifest As String = "ApplicationManifest"
        Friend Const Const_TargetFrameworkMoniker As String = "TargetFrameworkMoniker"
        Friend Const Const_TargetFramework As String = "FriendlyTargetFramework"
        Protected Const INDEX_WINDOWSAPP As Integer = 0
        Protected Const INDEX_COMMANDLINEAPP As Integer = 1
        Protected Const INDEX_WINDOWSCLASSLIB As Integer = 2
        Private _rootNamespace As String
        Private ReadOnly _outputTypeDefaultValues As OutputTypeComboBoxValue()
        Private _controlGroup As Control()()

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            _outputTypeDefaultValues = New OutputTypeComboBoxValue(INDEX_WINDOWSCLASSLIB) {}
            _outputTypeDefaultValues(INDEX_WINDOWSAPP) = New OutputTypeComboBoxValue(INDEX_WINDOWSAPP)
            _outputTypeDefaultValues(INDEX_COMMANDLINEAPP) = New OutputTypeComboBoxValue(INDEX_COMMANDLINEAPP)
            _outputTypeDefaultValues(INDEX_WINDOWSCLASSLIB) = New OutputTypeComboBoxValue(INDEX_WINDOWSCLASSLIB)

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If components IsNot Nothing Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        Private Sub AssemblyInfoButton_Click(sender As Object, e As EventArgs) Handles AssemblyInfoButton.Click
            ShowChildPage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AssemblyInfo_Title, GetType(AssemblyInfoPropPage), HelpKeywords.VBProjPropAssemblyInfo)
        End Sub
        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then

                    TargetFrameworkPropertyControlData = New TargetFrameworkPropertyControlData(VslangProj100.VsProjPropId100.VBPROJPROPID_TargetFrameworkMoniker, TargetFramework, AddressOf SetTargetFrameworkMoniker, AddressOf GetTargetFrameworkMoniker, ControlDataFlags.ProjectMayBeReloadedDuringPropertySet Or ControlDataFlags.NoOptimisticFileCheckout, New Control() {TargetFrameworkLabel})

                    'StartupObject must be kept at the end of the list because it depends on the initialization of "OutputType" values
                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)
                    Dim data As PropertyControlData = New PropertyControlData(VsProjPropId.VBPROJPROPID_AssemblyName, "AssemblyName", AssemblyName, New Control() {AssemblyNameLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyName
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_DefaultNamespace, Const_DefaultNamespace, RootNamespaceTextBox, New Control() {RootNamespaceLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_RootNamespace
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_ApplicationIcon, "ApplicationIcon", ApplicationIcon, AddressOf ApplicationIconSet, AddressOf ApplicationIconGet, ControlDataFlags.UserHandledEvents, New Control() {AppIconImage, AppIconBrowse, IconRadioButton, ApplicationIconLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_ApplicationIcon
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId110.VBPROJPROPID_OutputTypeEx, Const_OutputTypeEx, OutputType, AddressOf OutputTypeSet, AddressOf OutputTypeGet, ControlDataFlags.UserHandledEvents, New Control() {OutputTypeLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject, "StartupObject", StartupObject, AddressOf StartupObjectSet, AddressOf StartupObjectGet, ControlDataFlags.UserHandledEvents, New Control() {StartupObjectLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_StartupObject
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_Win32ResourceFile, "Win32ResourceFile", Win32ResourceFile, AddressOf Win32ResourceSet, AddressOf Win32ResourceGet, ControlDataFlags.None, New Control() {Win32ResourceFileBrowse, Win32ResourceRadioButton})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId90.VBPROJPROPID_ApplicationManifest, "ApplicationManifest", ApplicationManifest, AddressOf ApplicationManifestSet, AddressOf ApplicationManifestGet, ControlDataFlags.UserHandledEvents, New Control() {ApplicationManifest, ApplicationManifestLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId158.VBPROJPROPID_AutoGenerateBindingRedirects, "AutoGenerateBindingRedirects", AutoGenerateBindingRedirects)
                    datalist.Add(data)
                    datalist.Add(TargetFrameworkPropertyControlData)
                    m_ControlData = datalist.ToArray()

                End If
                Return m_ControlData
            End Get
        End Property

        Protected Overrides ReadOnly Property ValidationControlGroups As Control()()
            Get
                If _controlGroup Is Nothing Then
                    _controlGroup = New Control()() {
                        New Control() {IconRadioButton, Win32ResourceRadioButton, ApplicationIcon, ApplicationManifest, Win32ResourceFile, AppIconBrowse, Win32ResourceFileBrowse}
                        }
                End If
                Return _controlGroup
            End Get
        End Property
        ''' <param name="OutputType"></param>
        Private Sub PopulateControlSet(OutputType As UInteger)
            Debug.Assert(m_Objects.Length <= 1, "Multiple project updates not supported")
            PopulateStartupObject(StartUpObjectSupported(OutputType), False)
        End Sub

        ''' <summary>
        ''' Populates the start-up object combobox box dropdown
        ''' </summary>
        ''' <param name="StartUpObjectSupported">If false, (None) will be the only entry in the list.</param>
        ''' <param name="PopulateDropdown">If false, only the current text in the combobox is set.  If true, the entire dropdown list is populated.  For performance reasons, False should be used until the user actually drops down the list.</param>
        Protected Overridable Sub PopulateStartupObject(StartUpObjectSupported As Boolean, PopulateDropdown As Boolean)
            'overridable to support the csharpapplication page (Sub Main isn't used by C#)

            Dim InsideInitSave As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                Dim StartupObjectPropertyControlData As PropertyControlData = GetPropertyControlData("StartupObject")

                If Not StartUpObjectSupported OrElse StartupObjectPropertyControlData.IsMissing Then
                    With StartupObject
                        .DropDownStyle = ComboBoxStyle.DropDownList
                        .Items.Clear()
                        .SelectedItem = .Items.Add(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_StartupObjectNotSet)
                        .Text = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_StartupObjectNotSet
                        .SelectedIndex = 0  ' Set it to NotSet
                    End With

                    If StartupObjectPropertyControlData.IsMissing Then
                        StartupObject.Enabled = False
                        StartupObjectLabel.Enabled = False
                    End If
                Else

                    Dim prop As PropertyDescriptor = StartupObjectPropertyControlData.PropDesc

                    With StartupObject
                        .DropDownStyle = ComboBoxStyle.DropDown
                        .Items.Clear()

                        ' (Not Set) should always be available in the list
                        .Items.Add(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_StartupObjectNotSet)

                        If PopulateDropdown Then
                            RefreshPropertyStandardValues()

                            'Certain project types may not support standard values
                            If prop.Converter.GetStandardValuesSupported() Then
                                Switches.TracePDPerf("*** Populating start-up object list from the project [may be slow for a large project]")
                                Debug.Assert(Not InsideInitSave, "PERFORMANCE ALERT: We shouldn't be populating the start-up object dropdown list during page initialization, it should be done later if needed.")
                                Using New WaitCursor
                                    For Each o As Object In prop.Converter.GetStandardValues()
                                        .Items.Add(RemoveRootNamespace(prop.Converter.ConvertToString(o)))
                                    Next
                                End Using
                            End If
                        End If

                        Dim SelectedItemText As String = RemoveRootNamespace(CStr(StartupObjectPropertyControlData.InitialValue))
                        .SelectedItem = SelectedItemText
                        If .SelectedItem Is Nothing Then
                            .Items.Add(SelectedItemText)
                            'CONSIDER: Can we use the object returned by .Items.Add to set the selection?
                            .SelectedItem = SelectedItemText
                        End If
                        'If "Sub Main" is not in the list, then add it.
                        If .Items.IndexOf(Const_SubMain) < 0 Then
                            .Items.Add(Const_SubMain)
                        End If
                    End With
                End If
            Finally
                'Restore previous state
                m_fInsideInit = InsideInitSave
            End Try
        End Sub
        Private Sub EnableControlSet()
            UpdateIconImage(False)
        End Sub
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Overridable Function OutputTypeGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If OutputType.SelectedIndex = -1 Then
                ' We're indeterminate. Just let the architecture handle it
                Return False
            End If

            Dim currentValue As OutputTypeComboBoxValue = TryCast(OutputType.SelectedItem, OutputTypeComboBoxValue)

            If currentValue Is Nothing Then
                Return False
            End If

            value = currentValue.Value
            Return True

        End Function
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Overridable Function OutputTypeSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            Dim didSelectItem As Boolean = False

            If Not PropertyControlData.IsSpecialValue(value) Then

                Dim uIntValue As UInteger = CUInt(value)
                didSelectItem = SelectItemInOutputTypeComboBox(OutputType, uIntValue)

                If didSelectItem Then
                    PopulateControlSet(uIntValue)
                End If
            End If

            If Not didSelectItem Then
                ' We're indeterminate 
                OutputType.SelectedIndex = -1

                ' Set the startup object to indeterminate as well
                StartupObject.SelectedIndex = -1
            End If
            Return True
        End Function

        Private Function ApplicationIconSupported() As Boolean
            Return Not GetPropertyControlData(VsProjPropId.VBPROJPROPID_ApplicationIcon).IsMissing
        End Function

        Private Function Win32ResourceFileSupported() As Boolean
            Return Not GetPropertyControlData(VsProjPropId80.VBPROJPROPID_Win32ResourceFile).IsMissing
        End Function

        Private Function SetIconAndWin32ResourceFile() As Boolean
            Dim obj As Object
            Dim propWin32ResourceFile As PropertyDescriptor
            Dim stWin32ResourceFile As String = Nothing

            Dim propApplicationIcon As PropertyDescriptor
            Dim stApplicationIcon As String = Nothing

            Dim propApplicationManifest As PropertyDescriptor
            Dim stApplicationManifest As String = Nothing

            propApplicationIcon = GetPropertyDescriptor("ApplicationIcon")
            propApplicationManifest = GetPropertyDescriptor("ApplicationManifest")
            propWin32ResourceFile = GetPropertyDescriptor("Win32ResourceFile")

            obj = TryGetNonCommonPropertyValue(propApplicationIcon)
            If Not PropertyControlData.IsSpecialValue(obj) Then

                stApplicationIcon = TryCast(obj, String)

                If Trim(stApplicationIcon) = "" Then
                    If ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                        stApplicationIcon = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_DefaultIconText
                    Else
                        ' ApplicationIcon can be empty for dlls
                    End If
                End If
            End If

            obj = TryGetNonCommonPropertyValue(propApplicationManifest)
            If Not PropertyControlData.IsSpecialValue(obj) Then

                stApplicationManifest = TryCast(obj, String)
                stApplicationManifest = Trim(stApplicationManifest)

                If String.Equals(stApplicationManifest, prjApplicationManifestValues.prjApplicationManifest_Default, StringComparison.OrdinalIgnoreCase) Then
                    stApplicationManifest = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_DefaultManifestText
                ElseIf String.Equals(stApplicationManifest, prjApplicationManifestValues.prjApplicationManifest_NoManifest, StringComparison.OrdinalIgnoreCase) Then
                    stApplicationManifest = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_NoManifestText
                ElseIf String.IsNullOrEmpty(stApplicationManifest) Then
                    If ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                        stApplicationManifest = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_DefaultManifestText
                    Else
                        ' ApplicationManifest can be empty for dlls
                    End If
                End If
            End If

            obj = TryGetNonCommonPropertyValue(propWin32ResourceFile)
            If Not PropertyControlData.IsSpecialValue(obj) Then
                stWin32ResourceFile = TryCast(obj, String)
            End If

            If stApplicationIcon Is Nothing AndAlso stWin32ResourceFile Is Nothing Then
                ' indeterminate
                If Not IconEntryIsDefault(ApplicationIcon.Text) Then
                    ApplicationIcon.Text = ""
                End If
                EnableControl(AppIconBrowse, ApplicationIconSupported())
                EnableControl(ApplicationIcon, ApplicationIconSupported())
                EnableControl(ApplicationIconLabel, ApplicationIconSupported())
                ManifestExplanationLabel.Enabled = False
                IconRadioButton.Checked = False
                If Not ApplicationManifestEntryIsDefault(ApplicationManifest.Text) Then
                    ApplicationManifest.Text = String.Empty
                End If
                If ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                    EnableControl(ApplicationManifestLabel, ApplicationManifestSupported())
                    EnableControl(ApplicationManifest, ApplicationManifestSupported())
                Else
                    ApplicationManifestLabel.Enabled = False
                    ApplicationManifest.Enabled = False
                End If
                Win32ResourceFile.Text = ""
                EnableControl(Win32ResourceFile, Win32ResourceFileSupported())
                EnableControl(Win32ResourceFileBrowse, Win32ResourceFileSupported())
                Win32ResourceRadioButton.Checked = False

            ElseIf Not IsNothing(stWin32ResourceFile) AndAlso stWin32ResourceFile <> "" Then

                Win32ResourceFile.Text = stWin32ResourceFile
                EnableControl(Win32ResourceFile, Win32ResourceFileSupported())
                EnableControl(Win32ResourceFileBrowse, Win32ResourceFileSupported())
                Win32ResourceRadioButton.Checked = True

                ApplicationIcon.Text = ""
                AppIconBrowse.Enabled = False
                ApplicationIcon.Enabled = False
                ApplicationIconLabel.Enabled = False
                ManifestExplanationLabel.Enabled = False
                IconRadioButton.Checked = False
                ApplicationManifest.Text = String.Empty
                ApplicationManifestLabel.Enabled = False
                ApplicationManifest.Enabled = False

            Else

                ApplicationIcon.Text = stApplicationIcon
                EnableControl(ApplicationIconLabel, ApplicationIconSupported())
                EnableControl(ApplicationIcon, ApplicationIconSupported())
                EnableControl(AppIconBrowse, ApplicationIconSupported())
                ManifestExplanationLabel.Enabled = True
                IconRadioButton.Checked = True
                If ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                    ApplicationManifest.Text = stApplicationManifest
                    EnableControl(ApplicationManifestLabel, ApplicationManifestSupported())
                    EnableControl(ApplicationManifest, ApplicationManifestSupported())
                Else
                    ApplicationManifest.Text = String.Empty
                    ApplicationManifestLabel.Enabled = False
                    ApplicationManifest.Enabled = False
                End If
                Win32ResourceFile.Text = ""
                Win32ResourceFile.Enabled = False
                Win32ResourceFileBrowse.Enabled = False
                Win32ResourceRadioButton.Checked = False

            End If
            Return True

        End Function
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Shadows Function ApplicationIconGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If IconRadioButton.Checked = True Then
                If ApplicationIcon.Text.Equals(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_DefaultIconText, StringComparison.OrdinalIgnoreCase) Then
                    value = ""
                Else
                    value = ApplicationIcon.Text
                End If
                Return True
            ElseIf Win32ResourceRadioButton.Checked = True Then
                value = ""
                Return True
            Else
                Return False
            End If
        End Function
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Overridable Function ApplicationIconSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            Return SetIconAndWin32ResourceFile()
        End Function
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Shadows Function ApplicationManifestGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If IconRadioButton.Checked = True Then
                If ApplicationManifest.Text.Equals(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_DefaultManifestText, StringComparison.CurrentCultureIgnoreCase) Then
                    value = prjApplicationManifestValues.prjApplicationManifest_Default
                ElseIf ApplicationManifest.Text.Equals(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_NoManifestText, StringComparison.CurrentCultureIgnoreCase) Then
                    value = prjApplicationManifestValues.prjApplicationManifest_NoManifest
                Else
                    value = ApplicationManifest.Text.Trim()
                End If
                Return True
            ElseIf Win32ResourceRadioButton.Checked = True Then
                ' Reset it to default.
                value = String.Empty
                Return True
            Else
                Return False
            End If
        End Function
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Overridable Function ApplicationManifestSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            Return SetIconAndWin32ResourceFile()
        End Function
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Overridable Function Win32ResourceGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If Win32ResourceRadioButton.Checked = True Then
                value = Win32ResourceFile.Text
                Return True
            ElseIf IconRadioButton.Checked = True Then
                value = ""
                Return True
            Else
                Return False
            End If
        End Function
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Overridable Function Win32ResourceSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            Return SetIconAndWin32ResourceFile()
        End Function
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub IconResourceFile_CheckedChanged(sender As Object, e As EventArgs) Handles IconRadioButton.CheckedChanged, Win32ResourceRadioButton.CheckedChanged
            If IconRadioButton.Checked = True Then
                ManifestExplanationLabel.Enabled = True
                EnableControl(ApplicationIconLabel, ApplicationIconSupported())
                EnableControl(ApplicationIcon, ApplicationIconSupported())
                EnableControl(AppIconBrowse, ApplicationIconSupported())
                If ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                    EnableControl(ApplicationManifestLabel, ApplicationManifestSupported())
                    EnableControl(ApplicationManifest, ApplicationManifestSupported())
                Else
                    ApplicationManifestLabel.Enabled = False
                    ApplicationManifest.Enabled = False
                End If
                Win32ResourceFile.Enabled = False
                Win32ResourceFileBrowse.Enabled = False
            ElseIf Win32ResourceRadioButton.Checked = True Then
                ManifestExplanationLabel.Enabled = False
                ApplicationIconLabel.Enabled = False
                ApplicationIcon.Enabled = False
                AppIconBrowse.Enabled = False
                ApplicationManifestLabel.Enabled = False
                ApplicationManifest.Enabled = False
                EnableControl(Win32ResourceFile, Win32ResourceFileSupported())
                EnableControl(Win32ResourceFileBrowse, Win32ResourceFileSupported())
            End If

            UpdateIconImage(False)

            SetDirty(ApplicationIcon, False)
            SetDirty(ApplicationManifest, False)
            SetDirty(Win32ResourceFile, True)
        End Sub

        Protected Overrides Function ProcessDialogKey(keyData As Keys) As Boolean
            ' Our control is currently setup so that the radio buttons and the corresponding controls are all siblings
            ' This prevents Up/Down from just navigating between the radio buttons and breaks accessibility
            If ActiveControl Is IconRadioButton OrElse ActiveControl Is Win32ResourceRadioButton Then
                If keyData = Keys.Down OrElse keyData = Keys.Up Then
                    If ActiveControl Is IconRadioButton Then
                        Win32ResourceRadioButton.Select()
                    Else
                        IconRadioButton.Select()
                    End If
                    Return True
                End If
            End If
            Return MyBase.ProcessDialogKey(keyData)
        End Function

        ''' <summary>
        ''' validate a property
        ''' </summary>
        ''' <param name="controlData"></param>
        ''' <param name="message"></param>
        ''' <param name="returnControl"></param>
        Protected Overrides Function ValidateProperty(controlData As PropertyControlData, ByRef message As String, ByRef returnControl As Control) As ValidationResult
            Select Case controlData.DispId
                Case VsProjPropId.VBPROJPROPID_ApplicationIcon
                    If IconRadioButton.Checked Then
                        If ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                            If Trim(ApplicationIcon.Text).Length = 0 Then
                                message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_BadIcon
                                Return ValidationResult.Warning
                            ElseIf Trim(ApplicationIcon.Text).Equals(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_DefaultIconText, StringComparison.OrdinalIgnoreCase) Then
                                ' This is valid
                                Return ValidationResult.Succeeded
                            End If
                        Else
                            ' We allow empty string for class libraries so don't display error
                        End If
                    End If
                Case VsProjPropId90.VBPROJPROPID_ApplicationManifest
                    If IconRadioButton.Checked Then
                        If ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                            If String.IsNullOrEmpty(Trim(ApplicationManifest.Text)) Then
                                message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_BadManifest
                                Return ValidationResult.Warning
                            Else
                                ' This is valid
                                Return ValidationResult.Succeeded
                            End If
                        Else
                            ' We allow empty string for class libraries so don't display error
                        End If
                    End If
                Case VsProjPropId80.VBPROJPROPID_Win32ResourceFile
                    If Win32ResourceRadioButton.Checked Then
                        Dim FirstInvalidCharacter As Integer = Win32ResourceFile.Text.IndexOfAny(Path.GetInvalidPathChars())
                        If Trim(Win32ResourceFile.Text).Length = 0 Then
                            message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_NeedResFile
                            Return ValidationResult.Warning
                        ElseIf Not FirstInvalidCharacter = -1 Then
                            message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_InvalidCharactersInFilePath & vbCrLf & "Remove the following character: " & Win32ResourceFile.Text.Substring(FirstInvalidCharacter, 1)
                            Return ValidationResult.Failed
                        ElseIf Not File.Exists(Win32ResourceFile.Text) Then
                            message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PropPage_ResourceFileNotExist
                            Return ValidationResult.Warning
                        End If
                    End If
            End Select
            Return ValidationResult.Succeeded
        End Function
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Overridable Function StartupObjectGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            'overridable to support the csharpapplication page (C# doesn't use root namespace)
            If Not StartUpObjectSupported() Then
                value = ""
            Else
                'Append the RootNamespace to the startup object name
                Dim StringValue As String = DirectCast(GetControlValue(Const_StartupObject), String)
                _rootNamespace = DirectCast(GetControlValue(Const_DefaultNamespace), String)
                If _rootNamespace <> "" AndAlso StringValue <> Const_SubMain Then
                    value = _rootNamespace & "." & StringValue
                End If
            End If
            Return True
        End Function

        Protected Overridable Function StartupObjectSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            'overridable to support the csharpapplication page (C# doesn't use root namespace)

            Dim OutputTypeControlData As PropertyControlData = GetPropertyControlData(Const_OutputTypeEx)

            If OutputTypeControlData.IsMissing OrElse Not SupportsOutputTypeProperty() Then
                'Property is not supported by this project type
                ' hide associated fields
                OutputType.Enabled = False
                OutputTypeLabel.Enabled = False

                'Populate
                PopulateStartupObject(True, False)
            Else
                '(Okay to use OutputTypeControlData.InitialValue because we checked IsMissing above)
                PopulateControlSet(CUInt(OutputTypeControlData.InitialValue))
                EnableControlSet()
                Return True
            End If
            Return True
        End Function

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

            OutputType.Items.Clear()

            If Not SupportsOutputTypeProperty() Then

                OutputType.Enabled = False
                OutputTypeLabel.Enabled = False

            ElseIf Not PopulateOutputTypeComboBoxFromProjectProperty(OutputType) Then

                OutputType.Items.AddRange(_outputTypeDefaultValues)

            End If

            'Populate the target framework combobox
            PopulateTargetFrameworkComboBox(TargetFramework)

            ' Hide the AssemblyInformation button if project supports Pack capability, and hence has a Package property page with assembly info properties.
            If ProjectHierarchy.IsCapabilityMatch(Pack) Then
                AssemblyInfoButton.Visible = False
            End If
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

            EnableControlSet()

            PopulateIconList(False)
            PopulateManifestList(False)
            UpdateIconImage(False)
        End Sub

        ''' <summary>
        ''' Shows or hides the Auto-generate Binding Redirects checkbox depending on the new target
        ''' framework.
        ''' </summary>
        Protected Overrides Sub TargetFrameworkMonikerChanged()
            ShowAutoGeneratedBindingRedirectsCheckBox(AutoGenerateBindingRedirects)
        End Sub
        ''' <param name="value"></param>
        Private Function RemoveRootNamespace(value As String) As String
            Dim root As String
            Dim RootLength As Integer

            If _rootNamespace Is Nothing Then
                _rootNamespace = Trim(TryCast(GetPropertyControlData(Const_DefaultNamespace).InitialValue, String)) 'TryCast because InitialValue will be an object if RootNamespace property not supported
            End If

            root = _rootNamespace

            If root IsNot Nothing Then
                'Append period for comparison check
                root &= "."
                RootLength = root.Length
            End If

            If value Is Nothing Then
                value = ""
            End If

            If RootLength > 0 AndAlso value.Length > RootLength Then
                If String.Compare(root, 0, value, 0, RootLength) = 0 Then
                    'Now check that we have a period '.' following the name
                    value = value.Substring(RootLength)
                End If
            End If
            Return value
        End Function
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub OutputType_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles OutputType.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            Dim OutputType As UInteger = CUInt(GetControlValueNative(Const_OutputTypeEx))

            EnableControlSet()

            SetDirty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, False)
            SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, False)
            SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, False)
            SetDirty(VsProjPropId.VBPROJPROPID_StartupObject, False)
            SetDirty(True) 'True forces Apply
            If ProjectReloadedDuringCheckout Then
                Return
            End If

            PopulateControlSet(OutputType)

            SetIconAndWin32ResourceFile()
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Debug.Assert(IsCSProject, "Unknown project type")
            Return HelpKeywords.CSProjPropApplication
        End Function
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub Win32ResourceFileBrowse_Click(sender As Object, e As EventArgs) Handles Win32ResourceFileBrowse.Click

            SkipValidating(Win32ResourceFile)   ' skip this because we will pop up dialog to edit it...
            ProcessDelayValidationQueue(False)

            Dim sInitialDirectory As String = Nothing
            Dim sFileName As String

            If sInitialDirectory = "" Then
                sFileName = ""
                sInitialDirectory = ""
            Else
                sFileName = Path.GetFileName(sInitialDirectory)
                sInitialDirectory = Path.GetDirectoryName(sInitialDirectory)
            End If

            Dim fileNames As ArrayList = GetFilesViaBrowse(ServiceProvider, Handle, sInitialDirectory, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AddWin32ResourceTitle,
                    CombineDialogFilters(
                        CreateDialogFilter(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AddWin32ResourceFilter, "res"),
                        GetAllFilesDialogFilter()
                        ),
                        0, False, sFileName)
            If fileNames IsNot Nothing AndAlso fileNames.Count = 1 Then
                sFileName = CStr(fileNames(0))
                If File.Exists(sFileName) Then
                    Win32ResourceFile.Text = sFileName
                    SetDirty(Win32ResourceFile, True)
                Else
                    DelayValidate(Win32ResourceFile)
                End If
            Else
                DelayValidate(Win32ResourceFile)
            End If
        End Sub

        'Update the list of available items whenever the start-up object combobox is opened.
        Private Sub StartupObject_DropDown(sender As Object, e As EventArgs) Handles StartupObject.DropDown
            PopulateStartupObject(StartUpObjectSupported(), PopulateDropdown:=True)
            SetComboBoxDropdownWidth(StartupObject)
        End Sub

        Private Sub StartupObject_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles StartupObject.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            SetDirty(VsProjPropId.VBPROJPROPID_StartupObject, True)
        End Sub

        ''' <summary>
        ''' Set the drop-down width of comboboxes with user-handled events so they'll fit their contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ComboBoxes_DropDown(sender As Object, e As EventArgs) Handles OutputType.DropDown
            SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

#Region "Application icon"

        ''' <summary>
        ''' Populates the given application icon combobox with appropriate entries
        ''' </summary>
        ''' <param name="FindIconsInProject">If False, only the standard items are added (this is faster
        '''   and so may be appropriate for page initialization).</param>
        Private Overloads Sub PopulateIconList(FindIconsInProject As Boolean)
            PopulateIconList(FindIconsInProject, ApplicationIcon, CType(GetControlValueNative(Const_ApplicationIcon), String))
        End Sub

        ''' <summary>
        ''' Update the image displayed for the currently-selected application icon
        ''' </summary>
        Private Overloads Sub UpdateIconImage(AddToProject As Boolean)
            UpdateIconImage(ApplicationIcon, AppIconImage, AddToProject)
        End Sub

        Private Sub ApplicationIcon_DropDown(sender As Object, e As EventArgs) Handles ApplicationIcon.DropDown
            If GetPropertyControlData(Const_ApplicationIcon).IsDirty() Then
                UpdateIconImage(True)
                SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
            End If

            'When the icon combobox is dropped down, update it with all current entries from the project
            PopulateIconList(True)
            SetComboBoxDropdownWidth(ApplicationIcon)
        End Sub
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ApplicationIcon_LostFocus(sender As Object, e As EventArgs) Handles ApplicationIcon.LostFocus
            If m_fInsideInit Then
                Return
            End If

            If GetPropertyControlData(Const_ApplicationIcon).IsDirty() Then
                UpdateIconImage(True)
                SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
            End If
        End Sub

        Private Sub ApplicationIcon_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles ApplicationIcon.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            UpdateIconImage(True)
            SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
        End Sub

        Private Sub ApplicationIcon_TextChanged(sender As Object, e As EventArgs) Handles ApplicationIcon.TextChanged
            If m_fInsideInit Then
                Return
            End If

            SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, False)
        End Sub
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub AppIconBrowse_Click(sender As Object, e As EventArgs) Handles AppIconBrowse.Click
            BrowseForAppIcon(ApplicationIcon, AppIconImage)
        End Sub

#End Region

#Region "Application Manifest"

        ''' <summary>
        ''' Populates the given application manifest combobox with appropriate entries
        ''' </summary>
        ''' <param name="FindManifestInProject">If False, only the standard items are added (this is faster
        '''   and so may be appropriate for page initialization).</param>
        Private Overloads Sub PopulateManifestList(FindManifestInProject As Boolean)
            PopulateManifestList(FindManifestInProject, ApplicationManifest, CType(GetControlValueNative(Const_ApplicationManifest), String))
        End Sub

        Private Sub ApplicationManifest_DropDown(sender As Object, e As EventArgs) Handles ApplicationManifest.DropDown
            If GetPropertyControlData(Const_ApplicationManifest).IsDirty() Then
                SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, True)
            End If

            'When the icon combobox is dropped down, update it with all current entries from the project
            PopulateManifestList(True)
            SetComboBoxDropdownWidth(ApplicationManifest)
        End Sub
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ApplicationManifest_LostFocus(sender As Object, e As EventArgs) Handles ApplicationManifest.LostFocus
            If m_fInsideInit Then
                Return
            End If

            If GetPropertyControlData(Const_ApplicationManifest).IsDirty() Then
                SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, True)
            End If
        End Sub

        Private Sub ApplicationManifest_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles ApplicationManifest.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, True)
        End Sub

        Private Sub ApplicationManifest_TextChanged(sender As Object, e As EventArgs) Handles ApplicationManifest.TextChanged
            If m_fInsideInit Then
                Return
            End If

            SetDirty(VsProjPropId90.VBPROJPROPID_ApplicationManifest, False)
        End Sub

#End Region

        Private Sub iconTableLayoutPanel_Paint(sender As Object, e As PaintEventArgs) Handles iconTableLayoutPanel.Paint

        End Sub
    End Class

End Namespace
