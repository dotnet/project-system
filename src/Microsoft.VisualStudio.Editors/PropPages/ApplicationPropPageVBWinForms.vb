' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Windows.Forms

Imports Microsoft.VisualBasic.ApplicationServices
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.MyApplication
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Imports VSLangProj110

Imports VSLangProj158

Imports VSLangProj80

Imports VslangProj90

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' The application property page for VB WinForms apps
    ''' - see comments in proppage.vb: "Application property pages (VB and C#)"
    ''' </summary>
    Friend Class ApplicationPropPageVBWinForms
        Inherits ApplicationPropPageVBBase

        'Backing storage for the current MainForm value (without the root namespace)
        Protected MainFormTextboxNoRootNS As New TextBox

        Protected Const Const_SubMain As String = "Sub Main"
        Protected Const Const_MyApplicationEntryPoint As String = "My.MyApplication"
        Protected Const Const_MyApplication As String = "MyApplication"

        Private ReadOnly _shutdownModeStringValues As String()
        Private ReadOnly _authenticationModeStringValues As String()
        Private ReadOnly _noneText As String
        Private _myType As String
        Private ReadOnly _startupObjectLabelText As String 'This one is in the form's resx when initialized
        Private ReadOnly _startupFormLabelText As String 'This one we pull from resources

        'This is the (cached) MyApplication.MyApplicationProperties object returned by the project system
        Private _myApplicationPropertiesCache As IMyApplicationPropertiesInternal
        Private WithEvents _myApplicationPropertiesNotifyPropertyChanged As INotifyPropertyChanged

        'Set to true if we have tried to cache the MyApplication properties value.  If this is True and
        '  _myApplicationPropertiesCache is Nothing, it indicates that the MyApplication property is not
        '  supported in this project system (which may mean the project flavor has turned off this support)
        Private _isMyApplicationPropertiesCached As Boolean

        'Cache whether MyType is one of the disabled values so we don't have to fetch it constantly
        '  from the project properties
        Private _isMyTypeDisabled As Boolean
        Private _isMyTypeDisabledCached As Boolean

        ' If set, we are using my application types as the 'output type'.  Otherwise, we are using
        ' output types provided by the project system
        Private _usingMyApplicationTypes As Boolean = True

        Protected Const Const_EnableVisualStyles As String = "EnableVisualStyles"
        Protected Const Const_AuthenticationMode As String = "AuthenticationMode"
        Protected Const Const_SingleInstance As String = "SingleInstance"
        Protected Const Const_ShutdownMode As String = "ShutdownMode"
        Protected Const Const_SplashScreenNoRootNS As String = "SplashScreen" 'we persist this without the root namespace
        Protected Const Const_CustomSubMain As String = "CustomSubMain"
        Protected Const Const_MainFormNoRootNS As String = "MainForm" 'we persist this without the root namespace
        Protected Const Const_MyType As String = "MyType"
        Protected Const Const_SaveMySettingsOnExit As String = "SaveMySettingsOnExit"

        ' Shared list of all known application types and their properties...
        Private Shared ReadOnly s_applicationTypes As New List(Of ApplicationTypeInfo)

        Private _settingApplicationType As Boolean

        ''' <summary>
        '''  Set up shared state...
        ''' </summary>
        Shared Sub New()
            ' Populate shared list of all known application types allowed on this page
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsApp, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WindowsFormsApp, True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsClassLib, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WindowsClassLib, True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.CommandLineApp, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_CommandLineApp, True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsService, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WindowsService, False))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WebControl, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WebControlLib, False))
        End Sub

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            SetCommonControls()
            AddChangeHandlers()

            'Remember original text of the Start-up object label text
            _startupObjectLabelText = StartupObjectLabel.Text

            'Get text for the forms case from resources
            _startupFormLabelText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_StartupFormLabelText

            _noneText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ComboBoxSelect_None

            'Ordering of strings here determines value stored in MyApplication.myapp
            _shutdownModeStringValues = New String() {My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_MyApplication_StartupMode_FormCloses, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_MyApplication_StartupMode_AppExits}
            _authenticationModeStringValues = New String() {My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_MyApplication_AuthenMode_Windows, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_MyApplication_AuthenMode_ApplicationDefined}
            PageRequiresScaling = False
        End Sub

        ''' <summary>
        ''' Let the base class know which control instances correspond to shared controls
        '''   between this inherited class and the base vb application property page class.
        ''' </summary>
        Private Sub SetCommonControls()
            CommonControls = New CommonPageControls(
                IconCombobox, IconLabel, IconPicturebox)
        End Sub
        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                Dim ControlsThatDependOnStartupObjectProperty As Control() = {
                    StartupObjectLabel, UseApplicationFrameworkCheckBox, WindowsAppGroupBox
                }
                Dim ControlsThatDependOnOutputTypeProperty As Control() = {
                    ApplicationTypeComboBox, ApplicationTypeLabel
                }

                If m_ControlData Is Nothing Then
                    'StartupObject must be kept after OutputType because it depends on the initialization of "OutputType" values
                    ' Custom sub main must come before MainForm, because it will ASSERT on the enable frameowrk checkbox
                    ' StartupObject must be kept after MainForm, because it needs the main form name...
                    ' MyApplication should be kept before all other MyAppDISPIDs properties to make sure that everyting in there
                    ' is initialized correctly...
                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)

                    Dim data As PropertyControlData = New PropertyControlData(VBProjPropId.VBPROJPROPID_MyApplication, Const_MyApplication, Nothing, AddressOf MyApplicationSet, AddressOf MyApplicationGet, ControlDataFlags.UserHandledEvents Or ControlDataFlags.UserPersisted)
                    datalist.Add(data)
                    data = New MyApplicationPersistedPropertyControlData(MyAppDISPIDs.CustomSubMain, Const_CustomSubMain, UseApplicationFrameworkCheckBox, AddressOf CustomSubMainSet, AddressOf CustomSubMainGet, ControlDataFlags.UserPersisted Or ControlDataFlags.UserHandledEvents, AddressOf MyApplicationGet)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_RootNamespace, Const_RootNamespace, RootNamespaceTextBox, New Control() {RootNamespaceLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_RootNamespace
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId110.VBPROJPROPID_OutputTypeEx, Const_OutputTypeEx, Nothing, AddressOf OutputTypeSet, AddressOf OutputTypeGet, ControlDataFlags.None, ControlsThatDependOnOutputTypeProperty)
                    datalist.Add(data)
                    data = New MyApplicationPersistedPropertyControlData(MyAppDISPIDs.MainForm, Const_MainFormNoRootNS, MainFormTextboxNoRootNS, AddressOf MainFormNoRootNSSet, Nothing, ControlDataFlags.UserPersisted, AddressOf MyApplicationGet)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject, Const_StartupObject, StartupObjectComboBox, AddressOf StartupObjectSet, AddressOf StartupObjectGet, ControlDataFlags.UserHandledEvents, ControlsThatDependOnStartupObjectProperty) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_StartupObject
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_AssemblyName, "AssemblyName", AssemblyNameTextBox, New Control() {AssemblyNameLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyName
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_ApplicationIcon, "ApplicationIcon", IconCombobox, AddressOf ApplicationIconSet, AddressOf ApplicationIconGet, ControlDataFlags.UserHandledEvents, New Control() {IconLabel, IconPicturebox}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_ApplicationIcon
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VBProjPropId.VBPROJPROPID_MyType, Const_MyType, Nothing, AddressOf MyTypeSet, AddressOf MyTypeGet)
                    datalist.Add(data)
                    data = New MyApplicationPersistedPropertyControlData(MyAppDISPIDs.EnableVisualStyles, Const_EnableVisualStyles, EnableXPThemesCheckBox, ControlDataFlags.UserPersisted, AddressOf MyApplicationGet)
                    datalist.Add(data)
                    data = New MyApplicationPersistedPropertyControlData(MyAppDISPIDs.AuthenticationMode, Const_AuthenticationMode, AuthenticationModeComboBox, ControlDataFlags.UserPersisted, AddressOf MyApplicationGet)
                    datalist.Add(data)
                    data = New MyApplicationPersistedPropertyControlData(MyAppDISPIDs.SingleInstance, Const_SingleInstance, SingleInstanceCheckBox, ControlDataFlags.UserPersisted, AddressOf MyApplicationGet)
                    datalist.Add(data)
                    data = New MyApplicationPersistedPropertyControlData(MyAppDISPIDs.ShutdownMode, Const_ShutdownMode, ShutdownModeComboBox, ControlDataFlags.UserPersisted, New Control() {ShutdownModeLabel}, AddressOf MyApplicationGet)
                    datalist.Add(data)
                    data = New MyApplicationPersistedPropertyControlData(MyAppDISPIDs.SplashScreen, Const_SplashScreenNoRootNS, SplashScreenComboBox, ControlDataFlags.UserPersisted, New Control() {SplashScreenLabel}, AddressOf MyApplicationGet)
                    datalist.Add(data)
                    data = New MyApplicationPersistedPropertyControlData(MyAppDISPIDs.SaveMySettingsOnExit, Const_SaveMySettingsOnExit, SaveMySettingsCheckbox, ControlDataFlags.UserPersisted, AddressOf MyApplicationGet)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId90.VBPROJPROPID_ApplicationManifest, "ApplicationManifest", Nothing, ControlDataFlags.Hidden)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId158.VBPROJPROPID_AutoGenerateBindingRedirects, "AutoGenerateBindingRedirects", AutoGenerateBindingRedirectsCheckBox)
                    datalist.Add(data)

                    TargetFrameworkPropertyControlData = New TargetFrameworkPropertyControlData(
                            VsProjPropId100.VBPROJPROPID_TargetFrameworkMoniker,
                            TargetFrameworkComboBox,
                            AddressOf SetTargetFrameworkMoniker,
                            AddressOf GetTargetFrameworkMoniker,
                            ControlDataFlags.ProjectMayBeReloadedDuringPropertySet Or ControlDataFlags.NoOptimisticFileCheckout,
                            New Control() {TargetFrameworkLabel})

                    datalist.Add(TargetFrameworkPropertyControlData)

                    m_ControlData = datalist.ToArray()
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <summary>
        ''' Removes references to anything that was passed in to SetObjects
        ''' </summary>
        Protected Overrides Sub CleanupCOMReferences()
            MyBase.CleanupCOMReferences()

            _myApplicationPropertiesCache = Nothing
            _myApplicationPropertiesNotifyPropertyChanged = Nothing
            _isMyApplicationPropertiesCached = False
        End Sub

        Private ReadOnly Property MyApplicationPropertiesSupported As Boolean
            Get
                Return MyApplicationProperties IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Gets the MyApplication.MyApplicationProperties object returned by the project system (which the project system creates by calling into us)
        ''' </summary>
        ''' <value>The value of the MyApplication property, or else Nothing if it is not supported.</value>
        Private ReadOnly Property MyApplicationProperties As IMyApplicationPropertiesInternal
            Get
                Debug.Assert(Implies(_myApplicationPropertiesCache IsNot Nothing, _isMyApplicationPropertiesCached))
                Debug.Assert(Implies(_myApplicationPropertiesNotifyPropertyChanged IsNot Nothing, _isMyApplicationPropertiesCached))
                If Not _isMyApplicationPropertiesCached Then
                    'Set a flag so we don't keep trying to query for this property
                    _isMyApplicationPropertiesCached = True

                    'Only enable MyApplication when capability is present.
                    If ProjectHierarchy.IsCapabilityMatch("EnableMyApplication") Then
                        _myApplicationPropertiesCache = MyApplicationProjectLifetimeTracker.Track(ProjectHierarchy)
                        _myApplicationPropertiesNotifyPropertyChanged = TryCast(_myApplicationPropertiesCache, INotifyPropertyChanged)
                    Else
                        'MyApplication property is not supported in this project system
                        _myApplicationPropertiesCache = Nothing
                        _myApplicationPropertiesNotifyPropertyChanged = Nothing
                    End If
                End If

                Return _myApplicationPropertiesCache
            End Get
        End Property

        ''' <summary>
        ''' Attempts to run the custom tool for the .myapp file.  If an exception
        '''   is thrown, it is displayed to the user and swallowed.
        ''' </summary>
        ''' <returns>True on success.</returns>
        Private Function TryRunCustomToolForMyApplication() As Boolean
            If MyApplicationProperties IsNot Nothing Then
                Try
                    MyApplicationProperties.RunCustomTool()
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(TryRunCustomToolForMyApplication), NameOf(ApplicationPropPageInternalBase))
                    ShowErrorMessage(ex)
                End Try
            End If

            Return True
        End Function

        ''' <summary>
        ''' This is a readonly property, so don't return anything
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function MyApplicationGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = MyApplicationProperties
            Return True
        End Function

        ''' <summary>
        ''' Value given us for "MyApplication" property
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function MyApplicationSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            'Nothing for us to do
            Return True
        End Function

        ''' <summary>
        ''' Returns the value stored in the UI for the MyType property.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function MyTypeGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = _myType
            Return True
        End Function

        ''' <summary>
        ''' Value given us for "MyType" property
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function MyTypeSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            Dim stValue As String = CType(value, String)

            If (stValue IsNot Nothing) AndAlso (stValue.Trim().Length > 0) Then
                _myType = stValue
            Else
                _myType = Nothing
            End If

            UpdateApplicationTypeUI()
            If Not m_fInsideInit Then
                ' We've got to make sure that we run the custom tool whenever we change
                ' the "application type"
                TryRunCustomToolForMyApplication()
            End If

            Return True
        End Function

        ''' <summary>
        ''' Gets the output type from the UI fields
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks>OutputType is obtained from the value in the Application Type field</remarks>
        Protected Function OutputTypeGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If _usingMyApplicationTypes Then
                Dim AppType As ApplicationTypes

                If ApplicationTypeComboBox.SelectedItem IsNot Nothing Then
                    AppType = DirectCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo).ApplicationType
                Else
                    AppType = ApplicationTypes.WindowsApp
                End If
                value = MyApplication.MyApplicationProperties.OutputTypeFromApplicationType(AppType)
            Else
                If ApplicationTypeComboBox.SelectedItem IsNot Nothing Then
                    value = DirectCast(ApplicationTypeComboBox.SelectedItem, OutputTypeComboBoxValue).Value
                Else
                    value = prjOutputTypeEx.prjOutputTypeEx_WinExe
                End If
            End If

            Return True
        End Function

        Protected Function OutputTypeSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean

            If _usingMyApplicationTypes Then
                'No UI for OutputType, ApplicationType provides our UI selection
                UpdateApplicationTypeUI()

                If Not m_fInsideInit Then
                    ' We've got to make sure that we run the custom tool whenever we change
                    ' the "application type"
                    TryRunCustomToolForMyApplication()
                End If
            Else
                Dim uIntValue As UInteger = CUInt(value)

                If SelectItemInOutputTypeComboBox(ApplicationTypeComboBox, uIntValue) Then
                    PopulateStartupObject(StartUpObjectSupported(uIntValue), PopulateDropdown:=False)
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Make sure the application type combobox is showing the appropriate
        ''' value
        ''' </summary>
        Private Sub UpdateApplicationTypeUI()
            If _settingApplicationType Then
                Return
            End If

            Dim oOutputType As Object = Nothing
            Dim oMyType As Object = Nothing
            If GetProperty(VBProjPropId.VBPROJPROPID_MyType, oMyType) AndAlso oMyType IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oMyType) _
                AndAlso GetProperty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, oOutputType) AndAlso oOutputType IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oOutputType) _
            Then
                Dim AppType As ApplicationTypes = MyApplication.MyApplicationProperties.ApplicationTypeFromOutputType(CUInt(oOutputType), CStr(oMyType))
                ApplicationTypeComboBox.SelectedItem = s_applicationTypes.Find(ApplicationTypeInfo.ApplicationTypePredicate(AppType))
                EnableControlSet(AppType)
                PopulateControlSet(AppType)
            Else
                ApplicationTypeComboBox.SelectedIndex = -1
                EnableIconComboBox(False)
                EnableUseApplicationFrameworkCheckBox(False)
            End If
        End Sub

        ''' <summary>
        ''' Getter for the "CustSubMain" property.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks>
        ''' The UI checkbox's logic is reversed from the property ("Enable application frameworks" = Not CustomSubMain).  However, because the property
        '''   is specified as CustomSubMain and I don't want to change it at this point, and the property change notification is based on the
        '''   CustomSubMain property ID, I didn't want to change the PropertyControlData to use a custom property.  So we reverse the logic in
        '''   a custom getter/setter
        ''' </remarks>
        Protected Function CustomSubMainGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If UseApplicationFrameworkCheckBox.CheckState <> CheckState.Indeterminate Then
                value = Not UseApplicationFrameworkCheckBox.Checked 'reversed
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Setter for the "CustSubMain" property.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks>
        ''' The UI checkbox's logic is reversed from the property ("Enable application frameworks" = Not CustomSubMain).  However, because the property
        '''   is specified as CustomSubMain and I don't want to change it at this point, and the property change notification is based on the
        '''   CustomSubMain property ID, I didn't want to change the PropertyControlData to use a custom property.  So we reverse the logic in
        '''   a custom getter/setter
        ''' </remarks>
        Protected Function CustomSubMainSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                UseApplicationFrameworkCheckBox.CheckState = CheckState.Indeterminate
            Else
                UseApplicationFrameworkCheckBox.CheckState = IIf(Not CBool(value), CheckState.Checked, CheckState.Unchecked) 'reversed
            End If

            'Toggle whether the application framework properties are enabled
            WindowsAppGroupBox.Enabled = MyApplicationFrameworkEnabled()

            Return True
        End Function

        Private Shared Function IsClassLibrary(AppType As ApplicationTypes) As Boolean
            If AppType = ApplicationTypes.WindowsClassLib OrElse AppType = ApplicationTypes.WebControl Then
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Enables the "Enable application framework" checkbox (if Enable=True), but only if it is supported in this project with current settings
        ''' </summary>
        ''' <param name="Enable"></param>
        Private Sub EnableUseApplicationFrameworkCheckBox(Enable As Boolean)
            If Enable Then
                Dim useApplicationFrameworkEnabled As Boolean = MyApplicationFrameworkSupported()
                UseApplicationFrameworkCheckBox.Enabled = useApplicationFrameworkEnabled
                Debug.Assert(Not MyApplicationPropertiesSupported OrElse UseApplicationFrameworkCheckBox.Checked = Not MyApplicationProperties.CustomSubMainRaw)

                'The groupbox with My-related properties on the page should only be
                '  enabled if the custom sub main checkbox is enabled but not
                '  checked.
                Debug.Assert(Implies(useApplicationFrameworkEnabled, MyApplicationProperties IsNot Nothing))
                WindowsAppGroupBox.Enabled = useApplicationFrameworkEnabled AndAlso Not MyApplicationProperties.CustomSubMainRaw 'Be sure to use CustomSubMainRaw instead of CustomSubMain - application type might not be set correctly yet
            Else
                UseApplicationFrameworkCheckBox.Enabled = False
                WindowsAppGroupBox.Enabled = False
            End If
        End Sub
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function StartupObjectGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean

            If Not StartUpObjectSupported() Then
                value = ""
                Return True
            End If

            Dim StringValue As String

            'Value in the combobox does not contain the root namespace
            StringValue = NothingToEmptyString(DirectCast(StartupObjectComboBox.SelectedItem, String))

            If MyApplicationFrameworkEnabled() Then
                'Check that the main form is actually a form
                Dim IsAForm As Boolean = False
                Dim FormEntryPoints() As String = GetFormEntryPoints(IncludeSplashScreen:=False)

                If IsNoneText(StringValue) OrElse Const_SubMain.Equals(StringValue, StringComparison.OrdinalIgnoreCase) Then
                    'Not a form
                Else
                    Dim StringValueWithNamespace As String = AddCurrentRootNamespace(StringValue)
                    For Each FormName As String In FormEntryPoints
                        If String.Equals(FormName, StringValueWithNamespace, StringComparison.OrdinalIgnoreCase) Then
                            IsAForm = True
                            Exit For
                        End If
                    Next
                End If

                If Not IsAForm Then
                    If StringValue <> "" AndAlso StringValue.Equals(MyApplicationProperties.SplashScreenNoRootNS, StringComparison.OrdinalIgnoreCase) Then
                        'We couldn't find it because it's the same as the splash screen.  That's not allowed.
                        ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_SplashSameAsStart)
                    Else
                        'When the application framework is enabled, there must be a start-up form selected (MainForm) or there will
                        '  be a compile error or run-time error.  We avoid this when possible by picking the first available
                        '  form.  Also show a messagebox to let the user know about the problem (but don't throw an exception, because
                        '  that would cause problems in applying the other properties on the page).
                        ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_InvalidSubMainStartup)
                    End If

                    If FormEntryPoints IsNot Nothing AndAlso FormEntryPoints.Length() > 0 Then
                        'Change to an arbitrary start-up form and continue...
                        StringValue = RemoveCurrentRootNamespace(FormEntryPoints(0))
                    Else
                        'There is no start-up form available.  To keep from getting a compile or run-time error, we need to turn
                        '  off the application framework.
                        UseApplicationFrameworkCheckBox.CheckState = CheckState.Unchecked
                        SetDirty(MyAppDISPIDs.CustomSubMain, False)
                        value = ""
                        MainFormTextboxNoRootNS.Text = ""
                        SetDirty(MyAppDISPIDs.MainForm, False)
                        Return True
                    End If
                End If
            End If

            'If this is a WindowsApplication with My, then the value in the combobox is what we want
            '  to be the main form - this gets placed into MainFormTextboxNoRootNS and will get persisted
            '  out to MyApplicationProperties.MainFormNoRootNS.  The start-up object must be returned
            '  as a pointer to the start-up method in the My application framework stuff.
            If MyApplicationFrameworkEnabled() Then
                Debug.Assert(Not IsNoneText(StringValue), "None should not have been supported with the My stuff enabled")
                MainFormTextboxNoRootNS.Text = StringValue
                SetDirty(MyAppDISPIDs.MainForm, False)

                'Start-up object needs the root namespace
                value = AddCurrentRootNamespace(Const_MyApplicationEntryPoint)
            Else
                'My framework not enabled, add the root namespace to the raw value in the combobox, and that's the
                '  start-up object (unless it's (None)).
                If Not IsNoneText(StringValue) And Not Const_SubMain.Equals(StringValue, StringComparison.OrdinalIgnoreCase) Then
                    StringValue = AddCurrentRootNamespace(StringValue)
                End If

                value = StringValue
            End If

            Return True
        End Function

        ''' <summary>
        ''' Called by base to set update the UI
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function StartupObjectSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            'This is handled by the ApplicationType set, so do nothing here
            'CONSIDER: The start-up object/MainForm-handling code needs to be reworked - it makes undo/redo/external property changes
            '  more difficult than they should be.  Get code should not be changing the value of other properties.

            If Not m_fInsideInit Then
                'Property has been changed, refresh.
                PopulateStartupObject(StartUpObjectSupported(), False)
            End If

            Return True
        End Function

        ''' <summary>
        ''' Setter for MainForm.  We handle this so that we also get notified when the property
        '''   has changed.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function MainFormNoRootNSSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If Not PropertyControlData.IsSpecialValue(value) Then
                MainFormTextboxNoRootNS.Text = DirectCast(value, String)

                'When this changes, we need to update the start-up object combobox
                If Not m_fInsideInit Then
                    PopulateStartupObject(StartUpObjectSupported(), PopulateDropdown:=False)
                End If
            Else
                MainFormTextboxNoRootNS.Text = ""
            End If

            Return True
        End Function
        ''' <param name="OutputType"></param>
        Private Sub PopulateControlSet(OutputType As UInteger)
            Debug.Assert(m_Objects.Length <= 1, "Multiple project updates not supported")
            PopulateStartupObject(StartUpObjectSupported(OutputType), False)
        End Sub

        Private Sub PopulateControlSet(AppType As ApplicationTypes)
            Debug.Assert(m_Objects.Length <= 1, "Multiple project updates not supported")
            PopulateStartupObject(StartUpObjectSupportedForApplicationType(AppType), False)
        End Sub

        ''' <summary>
        ''' Populates the splash screen combobox's text and optionally dropdown entries
        ''' </summary>
        ''' <param name="PopulateDropdown">If false, only the current text in the combobox is set.  If true, the entire dropdown list is populated.  For performance reasons, False should be used until the user actually drops down the list.</param>
        Protected Sub PopulateSplashScreenList(PopulateDropdown As Boolean)
            'Use the same list as StartupObject, but no sub main

            Dim StartupObjectControlData As PropertyControlData = GetPropertyControlData(Const_StartupObject)
            Dim SplashScreenControlData As PropertyControlData = GetPropertyControlData(Const_SplashScreenNoRootNS)

            If Not MyApplicationPropertiesSupported OrElse StartupObjectControlData.IsMissing OrElse SplashScreenControlData.IsMissing Then
                Debug.Assert(SplashScreenComboBox.Enabled = False) 'Should have been disabled via PropertyControlData mechanism
                Debug.Assert(SplashScreenLabel.Enabled = False) 'Should have been disabled via PropertyControlData mechanism
            Else
                With SplashScreenComboBox
                    .Items.Clear()
                    .Items.Add(_noneText)

                    If PopulateDropdown Then
                        Switches.TracePDPerf("*** Populating splash screen list from the project [may be slow for a large project]")
                        Debug.Assert(Not m_fInsideInit, "PERFORMANCE ALERT: We shouldn't be populating the splash screen dropdown list during page initialization, it should be done later if needed.")
                        Using New WaitCursor
                            Dim CurrentMainForm As String = MyApplicationProperties.MainFormNoRootNamespace

                            For Each SplashForm As String In GetFormEntryPoints(IncludeSplashScreen:=True) _
                                .Select(Function(e) RemoveCurrentRootNamespace(e)) _
                                .OrderBy(Function(n) n)
                                'Only add forms to this list, skip 'Sub Main'
                                If (Not SplashForm.Equals(Const_MyApplicationEntryPoint, StringComparison.OrdinalIgnoreCase)) AndAlso
                                    (Not SplashForm.Equals(Const_SubMain, StringComparison.OrdinalIgnoreCase)) Then
                                    'We don't allow the splash form and main form to be the same, so don't
                                    '  put the main into the splash form list
                                    If Not SplashForm.Equals(CurrentMainForm, StringComparison.OrdinalIgnoreCase) Then
                                        .Items.Add(SplashForm)
                                    End If
                                End If
                            Next
                        End Using
                    End If

                    If MyApplicationProperties.SplashScreenNoRootNS = "" Then
                        'Set to (None)
                        .SelectedIndex = 0
                    Else
                        .SelectedItem = MyApplicationProperties.SplashScreenNoRootNS
                        If .SelectedItem Is Nothing Then
                            'Not in the list - add it
                            .SelectedIndex = .Items.Add(MyApplicationProperties.SplashScreenNoRootNS)
                        End If
                    End If
                End With
            End If
        End Sub

        ''' <summary>
        ''' Returns True iff the My Application framework should be supportable
        '''   in this project.  It does not necessarily mean that it's turned on,
        '''   just that it can be supported.
        ''' </summary>
        Private Function MyApplicationFrameworkSupported() As Boolean
            If Not MyApplicationPropertiesSupported Then
                Return False
            End If

            Dim StartupObjectControlData As PropertyControlData = GetPropertyControlData(Const_StartupObject)
            If StartupObjectControlData.IsMissing Then
                'This project type does not support the Startup-Object property, therefore it can't
                '  support the My application framework.
                Return False
            End If

            If MyTypeDisabled() Then
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Returns True iff the My Application framework stuff is supported
        '''   in this project system *and* it is currently turned on by the
        '''   user.
        ''' This means, among other things, that we have a list of start-up *forms*
        '''   instead of objects.
        ''' </summary>
        Private Function MyApplicationFrameworkEnabled() As Boolean
            If Not MyApplicationFrameworkSupported() Then
                Return False
            End If

            If Not _usingMyApplicationTypes Then
                Return False
            End If

            Dim appType As ApplicationTypeInfo = DirectCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo)
            If appType IsNot Nothing _
                AndAlso appType.ApplicationType = ApplicationTypes.WindowsApp _
                AndAlso UseApplicationFrameworkCheckBox.CheckState = CheckState.Checked _
            Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Retrieve the list of start-up forms (not start-up objects) from the VB compiler
        ''' </summary>
        Private Function GetFormEntryPoints(IncludeSplashScreen As Boolean) As String()
            Try
                Dim EntryPointProvider As Interop.IVBEntryPointProvider = CType(ServiceProvider.GetService(Interop.NativeMethods.VBCompilerGuid), Interop.IVBEntryPointProvider)
                If EntryPointProvider IsNot Nothing Then
                    Dim EntryPoints() As String = Array.Empty(Of String)()
                    Dim cEntryPointsAvailable As UInteger

                    'First call gets estimated number of entrypoints
                    Dim hr As Integer = EntryPointProvider.GetFormEntryPointsList(ProjectHierarchy, 0, Nothing, cEntryPointsAvailable)
                    If VSErrorHandler.Failed(hr) Then
                        Debug.Fail("Failed to get VB Form entry points, hr=0x" & Hex(hr))
                    ElseIf cEntryPointsAvailable > 0 Then
                        'Keep repeating until we give them a large enough array (it's possible the
                        '  number of entry points available has increased since we made our first call)
                        While EntryPoints.Length < cEntryPointsAvailable
                            ReDim EntryPoints(CInt(cEntryPointsAvailable) - 1)
                            EntryPointProvider.GetFormEntryPointsList(ProjectHierarchy, CUInt(EntryPoints.Length), EntryPoints, cEntryPointsAvailable)
                        End While

                        'We might have ended up with fewer than originally estimated...
                        ReDim Preserve EntryPoints(CInt(cEntryPointsAvailable) - 1)

                        If Not IncludeSplashScreen Then
                            'Filter out the splash screen
                            Dim SplashScreen As String = MyApplicationProperties.SplashScreen
                            For i As Integer = 0 To EntryPoints.Length - 1
                                If EntryPoints(i).Equals(SplashScreen, StringComparison.OrdinalIgnoreCase) Then
                                    'Found it - remove it
                                    For j As Integer = i + 1 To EntryPoints.Length - 1
                                        EntryPoints(i) = EntryPoints(j)
                                    Next
                                    ReDim Preserve EntryPoints(EntryPoints.Length - 1 - 1) 'Reduce allocated number by one
                                    Return EntryPoints
                                End If
                            Next
                        End If

                        'And return 'em...
                        Return EntryPoints
                    End If
                Else
                    Debug.Fail("Failed to get IVBEntryPointProvider")
                End If

            Catch ex As Exception When ReportWithoutCrash(ex, "An exception occurred in GetStartupForms() - using empty list", NameOf(ApplicationPropPageVBWinForms))
            End Try

            Return Array.Empty(Of String)
        End Function

        ''' <summary>
        ''' Populates the start-up object combobox box dropdown
        ''' </summary>
        ''' <param name="StartUpObjectSupported">If false, (None) will be the only entry in the list.</param>
        ''' <param name="PopulateDropdown">If false, only the current text in the combobox is set.  If true, the entire dropdown list is populated.  For performance reasons, False should be used until the user actually drops down the list.</param>
        Protected Sub PopulateStartupObject(StartUpObjectSupported As Boolean, PopulateDropdown As Boolean)
            'overridable to support the csharpapplication page (Sub Main isn't used by C#)
            Dim InsideInitSave As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                Dim StartupObjectPropertyControlData As PropertyControlData = GetPropertyControlData(Const_StartupObject)

                If Not StartUpObjectSupported OrElse StartupObjectPropertyControlData.IsMissing Then
                    With StartupObjectComboBox
                        .DropDownStyle = ComboBoxStyle.DropDownList
                        .Items.Clear()
                        .SelectedIndex = .Items.Add(_noneText)
                    End With

                    If StartupObjectPropertyControlData.IsMissing Then
                        StartupObjectComboBox.Enabled = False
                        StartupObjectLabel.Enabled = False
                    End If
                Else
                    Dim prop As PropertyDescriptor = StartupObjectPropertyControlData.PropDesc
                    Dim SwapWithMyAppData As Boolean = MyApplicationFrameworkEnabled()

                    With StartupObjectComboBox
                        .Items.Clear()

                        If PopulateDropdown Then
                            Using New WaitCursor
                                Switches.TracePDPerf("*** Populating start-up object list from the project [may be slow for a large project]")
                                Debug.Assert(Not InsideInitSave, "PERFORMANCE ALERT: We shouldn't be populating the start-up object dropdown list during page initialization, it should be done later if needed.")
                                Dim StartupObjects As ICollection = Nothing
                                If MyApplicationFrameworkEnabled() Then
                                    StartupObjects = GetFormEntryPoints(IncludeSplashScreen:=False)
                                Else
                                    RefreshPropertyStandardValues() 'Force us to see any new start-up objects in the project

                                    'Certain project types may not support standard values
                                    If prop.Converter.GetStandardValuesSupported() Then
                                        StartupObjects = prop.Converter.GetStandardValues()
                                    End If
                                End If

                                If StartupObjects IsNot Nothing Then
                                    For Each o As Object In StartupObjects
                                        Dim EntryPoint As String = RemoveCurrentRootNamespace(TryCast(o, String))
                                        'Remove "My.MyApplication" from the list
                                        If SwapWithMyAppData AndAlso Const_SubMain.Equals(EntryPoint, StringComparison.OrdinalIgnoreCase) Then
                                            'Do not add 'Sub Main' for MY applications
                                        ElseIf Not Const_MyApplicationEntryPoint.Equals(EntryPoint, StringComparison.OrdinalIgnoreCase) Then
                                            .Items.Add(EntryPoint)
                                        End If
                                    Next
                                End If
                            End Using
                        End If

                        '(Okay to use StartupObject's InitialValue because we checked it against IsMissing up above)
                        Dim SelectedItemText As String = RemoveCurrentRootNamespace(CStr(StartupObjectPropertyControlData.InitialValue))
                        If SwapWithMyAppData Then
                            'We're using the My application framework for start-up, so that means we need to show the MainForm from
                            '  our my application stuff instead of what's in the start-up object (which would set to the My application
                            '  start-up).
                            SelectedItemText = MainFormTextboxNoRootNS.Text
                        End If

                        .SelectedItem = SelectedItemText
                        If .SelectedItem Is Nothing AndAlso SelectedItemText <> "" Then
                            .SelectedIndex = .Items.Add(SelectedItemText)
                        End If

                        If .SelectedItem Is Nothing AndAlso SelectedItemText = "" Then
                            .SelectedIndex = .Items.Add(_noneText)
                        End If

                        If PopulateDropdown Then
                            'If "Sub Main" is not in the list and this isn't a WindowsApplication with My, then add it.
                            Dim SubMainIndex As Integer = .Items.IndexOf(Const_SubMain)
                            If SwapWithMyAppData Then
                                'Remove "Sub Main" if this is a MY app
                                If SubMainIndex > 0 Then
                                    .Items.RemoveAt(SubMainIndex)
                                End If
                            ElseIf .Items.IndexOf(Const_SubMain) < 0 Then
                                .Items.Add(Const_SubMain)
                            End If
                        End If
                    End With
                End If
            Finally
                'Restore previous state
                m_fInsideInit = InsideInitSave
            End Try
        End Sub

        Private Sub EnableControlSet(AppType As ApplicationTypes)
            Select Case AppType
                Case ApplicationTypes.CommandLineApp, ApplicationTypes.WindowsService
                    EnableIconComboBox(True)
                    EnableUseApplicationFrameworkCheckBox(False)

                Case ApplicationTypes.WindowsApp
                    EnableIconComboBox(True)
                    EnableUseApplicationFrameworkCheckBox(True)

                Case ApplicationTypes.WindowsClassLib
                    EnableIconComboBox(False)
                    EnableUseApplicationFrameworkCheckBox(False)

                Case ApplicationTypes.WebControl
                    EnableIconComboBox(False)
                    EnableUseApplicationFrameworkCheckBox(False)

                Case Else
                    Debug.Fail("Unexpected ApplicationType")
                    EnableIconComboBox(False)
                    EnableUseApplicationFrameworkCheckBox(False)
            End Select

            EnableMyApplicationControlSet()
            EnableControl(ViewUACSettingsButton, UACSettingsButtonSupported(AppType))
        End Sub
        ''' <param name="OutputType"></param>
        Private Sub EnableControlSet(OutputType As VSLangProj.prjOutputType)
            EnableIconComboBox(OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary)
            EnableMyApplicationControlSet()
            EnableControl(ViewUACSettingsButton, UACSettingsButtonSupported(OutputType))
        End Sub

        ''' <summary>
        ''' Sets the visibility of the MyApplication-related properties
        ''' </summary>
        Private Sub EnableMyApplicationControlSet()
            If Not MyApplicationPropertiesSupported Then
                'If MyApplication property not supported at all, then this project system flavor has disabled it,
                '  and we want to completely hide all my-related controls, so we don't confuse users.
                WindowsAppGroupBox.Visible = False
                UseApplicationFrameworkCheckBox.Visible = False
            Else
                WindowsAppGroupBox.Visible = True
                UseApplicationFrameworkCheckBox.Visible = True
                SaveMySettingsCheckbox.Enabled = MySettingsSupported()
            End If
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropApplication
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

            If Not SupportsOutputTypeProperty() Then

                ApplicationTypeComboBox.Enabled = False
                ApplicationTypeLabel.Enabled = False

            Else

                ' If the project specifies the output types, use the output types instead of the my application types
                _usingMyApplicationTypes = Not PopulateOutputTypeComboBoxFromProjectProperty(ApplicationTypeComboBox)

                If _usingMyApplicationTypes Then
                    PopulateApplicationTypes(ApplicationTypeComboBox, s_applicationTypes)
                End If
            End If

            ShutdownModeComboBox.Items.Clear()
            ShutdownModeComboBox.Items.AddRange(_shutdownModeStringValues)

            AuthenticationModeComboBox.Items.Clear()
            AuthenticationModeComboBox.Items.AddRange(_authenticationModeStringValues)

            PopulateTargetFrameworkComboBox(TargetFrameworkComboBox)

            ' Hide the AssemblyInformation button if project supports Pack capability, and hence has a Package property page with assembly info properties.
            EnableControl(AssemblyInfoButton, Not ProjectHierarchy.IsCapabilityMatch(Pack))
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

            If MyTypeDisabled() Then
                'If the MyType is disabled, we should turn on Custom Sub Main.  This will ensure we don't write any
                '  code for Application.Designer.vb, which would not compile for ApplicationType=WindowsForms.
                If MyApplicationProperties IsNot Nothing Then
                    Try
                        MyApplicationProperties.CustomSubMain = True
                        UseApplicationFrameworkCheckBox.CheckState = CheckState.Unchecked
                    Catch ex As Exception When ReportWithoutCrash(ex, NameOf(PostInitPage), NameOf(ApplicationPropPageVBWinForms))
                    End Try
                End If
            End If

            ' enable/disable controls based upon the current value of the project's
            '   OutputType (.exe, .dll...)
            EnableControlSet(ProjectProperties.OutputType)

            PopulateIconList(False)
            UpdateIconImage(False)
            SetStartupObjectLabelText()

            PopulateSplashScreenList(False)

        End Sub

        ''' <summary>
        ''' Shows or hides the Auto-generate Binding Redirects checkbox depending on the new target
        ''' framework.
        ''' </summary>
        Protected Overrides Sub TargetFrameworkMonikerChanged()
            ShowAutoGeneratedBindingRedirectsCheckBox(AutoGenerateBindingRedirectsCheckBox)
        End Sub

        Public Overrides Function GetUserDefinedPropertyDescriptor(PropertyName As String) As PropertyDescriptor
            If PropertyName = Const_MyApplication Then
                Return New UserPropertyDescriptor(PropertyName, GetType(MyApplicationProperties))

            ElseIf PropertyName = Const_EnableVisualStyles Then
                Return New UserPropertyDescriptor(PropertyName, GetType(Boolean))

            ElseIf PropertyName = Const_AuthenticationMode Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String))

            ElseIf PropertyName = Const_SingleInstance Then
                Return New UserPropertyDescriptor(PropertyName, GetType(Boolean))

            ElseIf PropertyName = Const_ShutdownMode Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String))

            ElseIf PropertyName = Const_SplashScreenNoRootNS Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String))

            ElseIf PropertyName = Const_CustomSubMain Then
                Return New UserPropertyDescriptor(PropertyName, GetType(Boolean))

            ElseIf PropertyName = Const_MainFormNoRootNS Then
                Return New UserPropertyDescriptor(PropertyName, GetType(String))

            ElseIf PropertyName = Const_SaveMySettingsOnExit Then
                Return New UserPropertyDescriptor(PropertyName, GetType(Boolean))

            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Takes a value from the property store, and converts it into the UI-displayable form
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        Public Overrides Function ReadUserDefinedProperty(PropertyName As String, ByRef Value As Object) As Boolean

            If PropertyName = Const_MyApplication Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties
                End If

            ElseIf PropertyName = Const_EnableVisualStyles Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties.EnableVisualStyles
                End If

            ElseIf PropertyName = Const_SingleInstance Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties.SingleInstance
                End If

            ElseIf PropertyName = Const_ShutdownMode Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Dim index As Integer = MyApplicationProperties.ShutdownMode
                    If index < 0 OrElse index > 1 Then
                        'If user horked the values, default to form exit
                        index = 0
                    End If
                    Value = _shutdownModeStringValues(index)
                End If

            ElseIf PropertyName = Const_SplashScreenNoRootNS Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    If MyApplicationProperties.SplashScreenNoRootNS = "" Then
                        Value = _noneText
                    ElseIf IsNoneText(MyApplicationProperties.SplashScreenNoRootNS) Then
                        Debug.Fail("Splash screen should not have been saved as (None)")
                        Value = ""
                    Else
                        Value = MyApplicationProperties.SplashScreenNoRootNS
                    End If
                End If

            ElseIf PropertyName = Const_MainFormNoRootNS Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Dim MainForm As String = MyApplicationProperties.MainFormNoRootNamespace
                    Debug.Assert(Not IsNoneText(MainForm), "MainForm should not have been persisted as (None)")
                    If MainForm = "" Then
                        Value = _noneText
                    ElseIf Not IsNoneText(MainForm) Then
                        Value = MainForm
                    End If
                End If

            ElseIf PropertyName = Const_CustomSubMain Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties.CustomSubMainRaw
                End If

            ElseIf PropertyName = Const_AuthenticationMode Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Dim Index As Integer = MyApplicationProperties.AuthenticationMode
                    If Not [Enum].IsDefined(GetType(AuthenticationMode), Index) Then
                        'If user horked the values, default to Windows authentication
                        Index = AuthenticationMode.Windows
                    End If

                    Value = _authenticationModeStringValues(Index)
                End If
            ElseIf PropertyName = Const_SaveMySettingsOnExit Then
                If Not MyApplicationPropertiesSupported Then
                    Value = PropertyControlData.MissingProperty
                Else
                    Value = MyApplicationProperties.SaveMySettingsOnExit
                End If

            Else
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Takes a value from the UI, converts it and writes it into the property store
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        Public Overrides Function WriteUserDefinedProperty(PropertyName As String, Value As Object) As Boolean
            If PropertyName = Const_MyApplication Then
                Dim x = MyApplicationProperties

            ElseIf PropertyName = Const_EnableVisualStyles Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If
                MyApplicationProperties.EnableVisualStyles = CBool(Value)

            ElseIf PropertyName = Const_SingleInstance Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If
                MyApplicationProperties.SingleInstance = CBool(Value)

            ElseIf PropertyName = Const_ShutdownMode Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                Dim index As Integer
                If _shutdownModeStringValues(1).Equals(CStr(Value), StringComparison.CurrentCultureIgnoreCase) Then
                    'If user horked the values, default to form exit
                    index = 1
                Else
                    index = 0
                End If
                MyApplicationProperties.ShutdownMode = index

            ElseIf PropertyName = Const_SplashScreenNoRootNS Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                Dim SplashScreenNoRootNS As String = Trim(TryCast(Value, String))
                If IsNoneText(SplashScreenNoRootNS) Then
                    'When the splash screen is none, we save it as an empty string
                    SplashScreenNoRootNS = ""
                End If
                MyApplicationProperties.SplashScreenNoRootNS = SplashScreenNoRootNS

            ElseIf PropertyName = Const_MainFormNoRootNS Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                Dim MainForm As String = Trim(TryCast(Value, String))
                If IsNoneText(MainForm) Then
                    MainForm = ""
                End If
                MyApplicationProperties.MainFormNoRootNamespace = MainForm

            ElseIf PropertyName = Const_CustomSubMain Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                MyApplicationProperties.CustomSubMain = CBool(Value)

            ElseIf PropertyName = Const_AuthenticationMode Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If

                Dim Index As Integer
                If _authenticationModeStringValues(AuthenticationMode.Windows).Equals(CStr(Value), StringComparison.CurrentCultureIgnoreCase) Then
                    Index = AuthenticationMode.Windows
                ElseIf _authenticationModeStringValues(AuthenticationMode.ApplicationDefined).Equals(CStr(Value), StringComparison.CurrentCultureIgnoreCase) Then
                    Index = AuthenticationMode.ApplicationDefined
                Else
                    'If user horked the values, default to Windows
                    Index = AuthenticationMode.Windows
                End If
                MyApplicationProperties.AuthenticationMode = Index

            ElseIf PropertyName = Const_SaveMySettingsOnExit Then
                If Not MyApplicationPropertiesSupported Then
                    Debug.Fail("Shouldn't be trying to write this property when MyApplicationProperties is missing")
                    Return True 'defensive
                End If
                MyApplicationProperties.SaveMySettingsOnExit = CBool(Value)

            Else
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Get the current value of MyType from the project properties
        ''' </summary>
        Private Function GetMyTypeFromProject() As String
            Dim MyTypeObject As Object = Nothing
            If GetProperty(VBProjPropId.VBPROJPROPID_MyType, MyTypeObject) Then
                Return TryCast(MyTypeObject, String)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' If MyType is set to "Empty" or "Custom", then the property page should consider
        '''   the MyType function "disabled" and should not show the My application properties,
        '''   nor should it change the MyType to any other value.  This allows programmers to
        '''   effectively turn off My and leave it off.  Returns true if My has been disabled.
        ''' </summary>
        Private Function MyTypeDisabled() As Boolean
            If Not _isMyTypeDisabledCached Then
                _isMyTypeDisabledCached = True
                Dim MyType As String = GetMyTypeFromProject()

                _isMyTypeDisabled = MyType IsNot Nothing _
                    AndAlso (MyType.Equals(MyApplication.MyApplicationProperties.Const_MyType_Empty, StringComparison.OrdinalIgnoreCase) _
                                OrElse MyType.Equals(MyApplication.MyApplicationProperties.Const_MyType_Custom, StringComparison.OrdinalIgnoreCase))
            End If

            Return _isMyTypeDisabled
        End Function

        ''' <summary>
        ''' Sets the current value of MyType based on the application type
        ''' </summary>
        ''' <param name="AppType"></param>
        Private Sub SetMyType(AppType As ApplicationTypes, ReadyToApply As Boolean)
            Debug.Assert(UseApplicationFrameworkCheckBox.CheckState <> CheckState.Indeterminate OrElse Not MyApplicationFrameworkSupported() OrElse MyTypeDisabled(),
                "UseApplicationFrameworkCheckbox shouldn't be indeterminate")
            Dim NewMyType As String = MyTypeFromApplicationType(AppType, UseApplicationFrameworkCheckBox.CheckState = CheckState.Unchecked OrElse Not MyApplicationFrameworkSupported() OrElse MyTypeDisabled())
            Debug.Assert(NewMyType IsNot Nothing)
            NewMyType = NothingToEmptyString(NewMyType)
            Dim CurrentMyType As Object = Nothing
            If MyTypeGet(Nothing, Nothing, CurrentMyType) Then
                If CurrentMyType Is Nothing OrElse TypeOf CurrentMyType IsNot String OrElse Not String.Equals(NewMyType, CStr(CurrentMyType), StringComparison.Ordinal) Then
                    'The value has changed - 
                    ' now poke it into our storage thru the same mechanism that the page-hosting
                    '   infrastructure does.
                    '
                    If MyTypeDisabled() Then
                        Trace.WriteLine("MyType has been disabled (""Empty"" or ""Custom"") - not changing the value of MyType")
                        Return
                    End If

                    ' Save the new MyType
                    Dim stValue As String = CType(NewMyType, String)

                    If (stValue IsNot Nothing) AndAlso (stValue.Trim().Length > 0) Then
                        _myType = stValue
                    Else
                        _myType = Nothing
                    End If

                    SetDirty(VBProjPropId.VBPROJPROPID_MyType, ReadyToApply)
                    If ReadyToApply Then
                        UpdateApplicationTypeUI()
                    End If
                End If
            Else
                Debug.Fail("MyTypeGet failed")
            End If
        End Sub

        ''' <summary>
        ''' Sets the current value of MyType based on the application type
        ''' </summary>
        ''' <param name="AppType"></param>
        Private Shared Function MyTypeFromApplicationType(AppType As ApplicationTypes, CustomSubMain As Boolean) As String
            Dim MyType As String

            Select Case AppType
                Case ApplicationTypes.WindowsApp
                    If CustomSubMain Then
                        MyType = MyApplication.MyApplicationProperties.Const_MyType_WindowsFormsWithCustomSubMain
                    Else
                        MyType = MyApplication.MyApplicationProperties.Const_MyType_WindowsForms
                    End If

                Case ApplicationTypes.WindowsClassLib
                    MyType = MyApplication.MyApplicationProperties.Const_MyType_Windows

                Case ApplicationTypes.CommandLineApp
                    MyType = MyApplication.MyApplicationProperties.Const_MyType_Console

                Case ApplicationTypes.WindowsService
                    MyType = MyApplication.MyApplicationProperties.Const_MyType_Console

                Case ApplicationTypes.WebControl
                    MyType = MyApplication.MyApplicationProperties.Const_MyType_WebControl

                Case Else
                    Debug.Fail("Unexpected Application Type - setting MyType to empty")
                    MyType = ""
            End Select

            Return MyType
        End Function

        ''' <summary>
        ''' Sets the text on the start-up object label to be either "Startup object" or "Startup form" depending
        '''   on whether a custom sub main is being used or not.
        ''' </summary>
        Private Sub SetStartupObjectLabelText()
            If MyApplicationFrameworkEnabled() Then
                StartupObjectLabel.Text = _startupFormLabelText
            Else
                StartupObjectLabel.Text = _startupObjectLabelText
            End If
        End Sub

        Private Sub AssemblyInfoButton_Click(sender As Object, e As EventArgs) Handles AssemblyInfoButton.Click
            ShowChildPage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AssemblyInfo_Title, GetType(AssemblyInfoPropPage), HelpKeywords.VBProjPropAssemblyInfo)
        End Sub

        ''' <summary>
        ''' Set the drop-down width of comboboxes with user-handled events so they'll fit their contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ComboBoxes_DropDown(sender As Object, e As EventArgs) Handles ApplicationTypeComboBox.DropDown
            SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Retrieve the current application type set in the UI
        ''' </summary>
        Private Function GetAppTypeFromUI() As ApplicationTypes
            Dim appTypeInfo As ApplicationTypeInfo = TryCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo)
            If appTypeInfo IsNot Nothing Then
                Return appTypeInfo.ApplicationType
            Else
                Return ApplicationTypes.WindowsApp
            End If
        End Function

        ''' <summary>
        ''' Add required references for the current application type set in the UI
        ''' </summary>
        Private Sub AddRequiredReferences()
            Dim appTypeInfo As ApplicationTypeInfo = TryCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo)
            Dim requiredReferences As String()
            If appTypeInfo Is Nothing Then
                appTypeInfo = s_applicationTypes.Find(ApplicationTypeInfo.ApplicationTypePredicate(ApplicationTypes.WindowsApp))
            End If

            requiredReferences = appTypeInfo.References

            Dim vsProj As VSLangProj.VSProject = CType(DTEProject.Object, VSLangProj.VSProject)
            For Each requiredReference As String In requiredReferences
                vsProj.References.Add(requiredReference)
            Next
        End Sub

        Private Sub ApplicationTypeComboBox_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles ApplicationTypeComboBox.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If

            If _settingApplicationType Then
                Return
            End If

            Try
                _settingApplicationType = True

                Dim outputType As UInteger

                If _usingMyApplicationTypes Then
                    'Disable or enable the controls based on ApplicationType
                    Dim AppType As ApplicationTypes = GetAppTypeFromUI()
                    EnableControlSet(AppType)

                    ' add necessary references...
                    Try
                        AddRequiredReferences()
                    Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ApplicationTypeComboBox_SelectionChangeCommitted), NameOf(ApplicationPropPageVBWinForms)) AndAlso
                        Not IsCheckoutCanceledException(ex)

                        ShowErrorMessage(ex)
                    End Try

                    'Update MyType property
                    '
                    SetMyType(AppType, False)
                    outputType = MyApplication.MyApplicationProperties.OutputTypeFromApplicationType(AppType)
                Else
                    outputType = CUInt(GetControlValueNative(Const_OutputTypeEx))
                End If

                SetStartupObjectLabelText()

                'Mark all fields dirty that need to update with this change
                '
                SetDirty(VsProjPropId110.VBPROJPROPID_OutputTypeEx, False)
                SetDirty(VsProjPropId.VBPROJPROPID_StartupObject, False)

                SetDirty(True)
                If ProjectReloadedDuringCheckout Then
                    Return
                End If

                PopulateControlSet(outputType)

            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ApplicationTypeComboBox_SelectionChangeCommitted), NameOf(ApplicationPropPageVBWinForms))
                ' There are lots of issues with check-out... I leave it to vswhidbey 475879
                Dim appTypeValue As Object = Nothing
                Dim CurrentAppType As ApplicationTypes = CType(appTypeValue, ApplicationTypes)
                ApplicationTypeComboBox.SelectedIndex = CInt(CurrentAppType)
                EnableControlSet(CurrentAppType)
                PopulateControlSet(CurrentAppType)
                ShowErrorMessage(ex)
            Finally
                _settingApplicationType = False

            End Try

            ' We've got to make sure that we run the custom tool whenever we change
            ' the "application type"
            TryRunCustomToolForMyApplication()

        End Sub

        Private Sub StartupObjectComboBox_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles StartupObjectComboBox.SelectionChangeCommitted
            If m_fInsideInit Then
                Return
            End If
            SetDirty(sender, True)
        End Sub

        ''' <summary>
        ''' Handle the "View Code" button's click event.  On this, we navigate to the MyEvents.vb file
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ViewCodeButton_Click(sender As Object, e As EventArgs) Handles ViewCodeButton.Click
            Static IsInViewCodeButtonClick As Boolean
            If IsInViewCodeButtonClick Then
                'Avoid recursive call (possible because of DoEvents work-around in CreateNewMyEventsFile
                Exit Sub
            End If
            IsInViewCodeButtonClick = True

            ' Navigate to events may add a file to the project, which may in turn cause the
            ' project file to be checked out at a later version. This will cause the project
            ' file to be reloaded, which will dispose me and bad things will happen (unless I
            ' tell myself that I'm about to potentially check out stuff)
            EnterProjectCheckoutSection()
            Try
                MyApplicationProperties.NavigateToEvents()
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ViewCodeButton_Click), NameOf(ApplicationPropPageVBWinForms))
                If Not ProjectReloadedDuringCheckout Then
                    ShowErrorMessage(ex)
                End If
            Finally
                LeaveProjectCheckoutSection()
                IsInViewCodeButtonClick = False
            End Try
        End Sub

        ''' <summary>
        ''' Happens when the splash screen combobox box is opened.  Use this to populate it with the
        '''   correct current choices.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub SplashScreenComboBox_DropDown(sender As Object, e As EventArgs) Handles SplashScreenComboBox.DropDown
            PopulateSplashScreenList(True)
            SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Happens when the start-up object combobox box is opened.  Use this to populate it with the
        '''   correct current choices.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub StartupObjectComboBox_DropDown(sender As Object, e As EventArgs) Handles StartupObjectComboBox.DropDown
            PopulateStartupObject(StartUpObjectSupported(), True)
            SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Returns true iff the current project supports the default settings file
        ''' </summary>
        Private Function MySettingsSupported() As Boolean
            Debug.Assert(DTEProject IsNot Nothing)
            If DTEProject IsNot Nothing Then
                Dim SpecialFiles As IVsProjectSpecialFiles = TryCast(ProjectHierarchy, IVsProjectSpecialFiles)
                If SpecialFiles IsNot Nothing Then
                    Dim ItemId As UInteger
                    Dim SpecialFilePath As String = Nothing
                    Dim hr As Integer = SpecialFiles.GetFile(__PSFFILEID2.PSFFILEID_AppSettings, CUInt(__PSFFLAGS.PSFF_FullPath), ItemId, SpecialFilePath)
                    If VSErrorHandler.Succeeded(hr) AndAlso SpecialFilePath <> "" Then
                        'Yes, settings files are supported (doesn't necessarily mean the file currently exists)
                        Return True
                    End If
                Else
                    Debug.Fail("Couldn't get IVsProjectSpecialFiles")
                End If
            End If

            Return False
        End Function

#Region "Application icon"

        Private Sub IconCombobox_DropDown(sender As Object, e As EventArgs) Handles IconCombobox.DropDown
            HandleIconComboboxDropDown(sender)
        End Sub

        Private Sub IconCombobox_DropDownClosed(sender As Object, e As EventArgs) Handles IconCombobox.DropDownClosed
            HandleIconComboboxDropDown(sender)
        End Sub

        Private Sub IconCombobox_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles IconCombobox.SelectionChangeCommitted
            HandleIconComboboxSelectionChangeCommitted(sender)
        End Sub

#End Region

        Private Sub UseApplicationFrameworkCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles UseApplicationFrameworkCheckBox.CheckedChanged
            If m_fInsideInit Then
                Return
            End If

            If UseApplicationFrameworkCheckBox.CheckState = CheckState.Checked Then
                'Having the application framework enabled requires that the start-up object be a form.  If there
                '  is no such form available, the code in StartupObjectGet will not be able to correct the Start-up
                '  object to be a form, and we'll end up possibly with compiler errors in the generated code which will
                '  be confusing to the user.  So if there is no start-up form available in the project, then disable
                '  the application framework again and tell the user why.
                If GetFormEntryPoints(IncludeSplashScreen:=False).Length = 0 Then
                    ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_InvalidSubMainStartup)
                    Try
                        Debug.Assert(Not m_fInsideInit, "This should have been checked at the beginning of this method")
                        m_fInsideInit = True 'Keep this routine from getting called recursively
                        UseApplicationFrameworkCheckBox.CheckState = CheckState.Unchecked
                    Finally
                        m_fInsideInit = False
                    End Try
                    Return
                End If
            End If

            'Checkstate should toggle the enabled state of the application groupbox
            WindowsAppGroupBox.Enabled = MyApplicationFrameworkEnabled()

            'Startupobject must be reset when 'CustomSubMain' is changed
            SetDirty(VsProjPropId.VBPROJPROPID_StartupObject, False)
            SetDirty(MyAppDISPIDs.CustomSubMain, False)
            'MyType may change
            SetMyType(GetAppTypeFromUI, False)
            SetStartupObjectLabelText()

            SetDirty(True)
            If ProjectReloadedDuringCheckout Then
                Return
            End If

            UpdateApplicationTypeUI()
            PopulateStartupObject(StartUpObjectSupported(), False)
        End Sub

        ''' <summary>
        ''' Returns true if start-up objects other than "(None)" are supported for this app type
        ''' </summary>
        Private Shared Function StartUpObjectSupportedForApplicationType(AppType As ApplicationTypes) As Boolean
            Return Not IsClassLibrary(AppType)
        End Function

        ''' <summary>
        ''' Returns True iff the given string is the special value used for "(None)"
        ''' </summary>
        ''' <param name="Value"></param>
        Private Function IsNoneText(Value As String) As Boolean
            'We use ordinal because a) we put the value into the combobox, it could not have magically
            '  changed case, and b) we don't want to use culture-aware because if the user changes cultures
            '  while our page is up, our functionality might be affected
            Return Value IsNot Nothing AndAlso Value.Equals(_noneText, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Fired when any of the MyApplicationProperty values has been changed
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub MyApplicationProperties_PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Handles _myApplicationPropertiesNotifyPropertyChanged.PropertyChanged
            Debug.Assert(e.PropertyName <> "")
            Switches.TracePDProperties(TraceLevel.Info, "MyApplicationProperties_PropertyChanged(""" & e.PropertyName & """)")

            Dim Data As PropertyControlData = GetPropertyControlData(e.PropertyName)

            If Data IsNot Nothing Then
                'Let the base class take care of it in the usual way for external property changes...
                OnExternalPropertyChanged(Data.DispId, "MyApplicationProperties")
            Else
                Debug.Fail("Couldn't find property control data for property changed in MyApplicationProperties")
            End If
        End Sub

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _components IsNot Nothing Then
                    _components.Dispose()
                End If

            End If
            MyBase.Dispose(disposing)
        End Sub

#Region "UAC Settings"

        ''' <summary>
        ''' The View UAC Settings button has been clicked...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ViewUACSettingsButton_Click(sender As Object, e As EventArgs) Handles ViewUACSettingsButton.Click
            ViewUACSettings()
        End Sub

#End Region

    End Class

End Namespace

