' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Imports EnvDTE

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.MyApplication
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop

Imports VslangProj100

Imports VSLangProj158

Imports VSLangProj80

Imports VslangProj90

Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    ''' <summary>
    ''' The application property page for VB WPF apps
    ''' - see comments in proppage.vb: "Application property pages (VB and C#)"
    ''' </summary>
    Partial Friend Class ApplicationPropPageVBWPF
        Inherits ApplicationPropPageVBBase

        'Holds the DocData for the Application.xaml file
        Private WithEvents _applicationXamlDocData As DocData

        Private Shared ReadOnly s_noneText As String '(None)" in the startup object combobox
        Private Shared ReadOnly s_startupObjectLabelText As String 'The label text to use for a startup object
        Private Shared ReadOnly s_startupUriLabelText As String 'The label text to use for a startup Uri

        Protected Const STARTUPOBJECT_SubMain As String = "Sub Main"

        Private Const VB_EXTENSION As String = ".vb"

        Private Const BUILDACTION_PAGE As String = "Page"
        Private Const BUILDACTION_APPLICATIONDEFINITION As String = "ApplicationDefinition"

#Region "User-defined properties for this page"

        Private Const PROPID_StartupObjectOrUri As Integer = 100
        Private Const PROPNAME_StartupObjectOrUri As String = "StartupObjectOrUri"

        Private Const PROPID_ShutDownMode As Integer = 101
        Private Const PROPNAME_ShutDownMode As String = "ShutdownMode"

        Private Const PROPID_UseApplicationFramework As Integer = 102
        Private Const PROPNAME_UseApplicationFramework As String = "UseApplicationFramework"

        'This property is added by the WPF flavor as an extended property
        Private Const PROPID_HostInBrowser As Integer = 103
        Private Const PROPNAME_HostInBrowser As String = "HostInBrowser"

#End Region

#Region "Dispose"

        'UserControl overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            '
            'NOTE:
            '  Most clean-up should be done in the overridden CleanupCOMReferences
            '  function, which is called by the base in its Dispose method and also
            '  when requested by the property page host.
            If disposing Then
                If _components IsNot Nothing Then
                    _components.Dispose()
                End If

                CleanUpApplicationXamlDocData()
            End If
            MyBase.Dispose(disposing)
        End Sub

#End Region

#Region "Clean-up"

        ''' <summary>
        ''' Removes references to anything that was passed in to SetObjects
        ''' </summary>
        Protected Overrides Sub CleanupCOMReferences()
            TrySaveDocDataIfLastEditor()
            _docDataHasChanged = False

            MyBase.CleanupCOMReferences()

        End Sub

        ''' <summary>
        ''' Closes our copy of the Application.xaml doc data.  If we're the last editor on it,
        '''   then we save it first.
        ''' </summary>
        Private Sub CleanUpApplicationXamlDocData()
            TrySaveDocDataIfLastEditor()
            If _applicationXamlDocData IsNot Nothing Then
                Dim docData As DocData = _applicationXamlDocData
                _applicationXamlDocData = Nothing
                docData.Dispose()
            End If
        End Sub

#End Region

#Region "Shared Sub New"

        ''' <summary>
        '''  Set up shared state...
        ''' </summary>
        Shared Sub New()
            InitializeApplicationTypes()
            InitializeShutdownModeValues()

            s_noneText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_ComboBoxSelect_None

            'Get text for the Startup Object/Uri label from resources
            s_startupUriLabelText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_StartupUriLabelText
            s_startupObjectLabelText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_StartupObjectLabelText
        End Sub

#End Region

#Region "Sub New"
        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            SetCommonControls()
            AddChangeHandlers()

            PageRequiresScaling = False
        End Sub

#End Region

#Region "PropertyControlData"
        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                Dim ControlsThatDependOnStartupObjectOrUriProperty As Control() = {
                    StartupObjectOrUriLabel, UseApplicationFrameworkCheckBox, WindowsAppGroupBox
                }

                If m_ControlData Is Nothing Then
                    Dim list As New List(Of PropertyControlData)
                    Dim data As PropertyControlData

                    'StartupObject.  
                    'StartupObjectOrUri must be kept after OutputType because it depends on the initialization of "OutputType" values
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject, Const_StartupObject, Nothing, ControlDataFlags.Hidden) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_StartupObject
                    }
                    list.Add(data)

                    'RootNamespace
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_RootNamespace, Const_RootNamespace, RootNamespaceTextBox, New Control() {RootNamespaceLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_RootNamespace
                    }
                    list.Add(data)

                    'OutputType
                    'Use RefreshAllPropertiesWhenChanged because changing the OutputType (application type) affects
                    '  the enabled state of other controls
                    list.Add(New PropertyControlData(VsProjPropId.VBPROJPROPID_OutputType, Const_OutputType, ApplicationTypeComboBox, AddressOf SetOutputTypeIntoUI, AddressOf GetOutputTypeFromUI, ControlDataFlags.RefreshAllPropertiesWhenChanged, New Control() {ApplicationTypeComboBox, ApplicationTypeLabel}))

                    'StartupObjectOrUri (user-defined)
                    'NoOptimisticFileCheckout - this property is stored in either the project file or the
                    '  application definition file, depending on whether we're storing a startup URI or a
                    '  startup object.  So we turn off the automatic file checkout so we don't require
                    '  the user to check out files they don't need to.  This is okay - the property change
                    '  will still cause a file checkout, it just won't be grouped together if there are
                    '  any other files needing to be checked out at the same time.
                    list.Add(New PropertyControlData(
                        PROPID_StartupObjectOrUri, PROPNAME_StartupObjectOrUri,
                        StartupObjectOrUriComboBox,
                        AddressOf SetStartupObjectOrUriIntoUI, AddressOf GetStartupObjectOrUriFromUI,
                        ControlDataFlags.UserPersisted Or ControlDataFlags.NoOptimisticFileCheckout,
                        ControlsThatDependOnStartupObjectOrUriProperty))

                    'AssemblyName
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_AssemblyName, "AssemblyName", AssemblyNameTextBox, New Control() {AssemblyNameLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyName
                    }
                    list.Add(data)

                    'ApplicationIcon
                    data = New PropertyControlData(VsProjPropId.VBPROJPROPID_ApplicationIcon, "ApplicationIcon", IconCombobox, AddressOf ApplicationIconSet, AddressOf ApplicationIconGet, ControlDataFlags.UserHandledEvents, New Control() {IconLabel, IconPicturebox}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_ApplicationIcon
                    }
                    list.Add(data)

                    'ShutdownMode (user-defined)
                    list.Add(New PropertyControlData(
                        PROPID_ShutDownMode, PROPNAME_ShutDownMode,
                        ShutdownModeComboBox,
                        AddressOf SetShutdownModeIntoUI, AddressOf GetShutdownModeFromUI,
                        ControlDataFlags.UserPersisted Or ControlDataFlags.PersistedInApplicationDefinitionFile,
                        New Control() {ShutdownModeLabel}))

                    'UseApplicationFramework (user-defined)
                    'Use RefreshAllPropertiesWhenChanged to force other property controls to get re-enabled/disabled when this changes
                    list.Add(New PropertyControlData(
                        PROPID_UseApplicationFramework, PROPNAME_UseApplicationFramework, UseApplicationFrameworkCheckBox,
                        AddressOf SetUseApplicationFrameworkIntoUI, AddressOf GetUseApplicationFrameworkFromUI,
                        ControlDataFlags.UserPersisted Or ControlDataFlags.RefreshAllPropertiesWhenChanged,
                        New Control() {WindowsAppGroupBox}))

                    'HostInBrowser (Avalon flavor extended property)
                    '  Tells whether the project is an XBAP app
                    list.Add(New PropertyControlData(
                        PROPID_HostInBrowser, PROPNAME_HostInBrowser, Nothing,
                        ControlDataFlags.Hidden))

                    ' ApplicationManifest - added simply to enable flavoring visibility of the button
                    list.Add(New PropertyControlData(VsProjPropId90.VBPROJPROPID_ApplicationManifest, "ApplicationManifest", Nothing, ControlDataFlags.Hidden))

                    'AutoGenerateBindingRedirects
                    data = New PropertyControlData(VsProjPropId158.VBPROJPROPID_AutoGenerateBindingRedirects, "AutoGenerateBindingRedirects", AutoGenerateBindingRedirectsCheckBox)
                    list.Add(data)

                    TargetFrameworkPropertyControlData = New TargetFrameworkPropertyControlData(
                        VsProjPropId100.VBPROJPROPID_TargetFrameworkMoniker,
                        TargetFrameworkComboBox,
                        AddressOf SetTargetFrameworkMoniker,
                        AddressOf GetTargetFrameworkMoniker,
                        ControlDataFlags.ProjectMayBeReloadedDuringPropertySet Or ControlDataFlags.NoOptimisticFileCheckout,
                        New Control() {TargetFrameworkLabel})

                    list.Add(TargetFrameworkPropertyControlData)

                    m_ControlData = list.ToArray()
                End If

                Return m_ControlData
            End Get
        End Property

#End Region

#Region "Common controls (used by base)"

        ''' <summary>
        ''' Let the base class know which control instances correspond to shared controls
        '''   between this inherited class and the base vb application property page class.
        ''' </summary>
        Private Sub SetCommonControls()
            CommonControls = New CommonPageControls(
                IconCombobox, IconLabel, IconPicturebox)
        End Sub

#End Region

#Region "Pre-init and post-init page initialization customization"

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

            PopulateApplicationTypes(ApplicationTypeComboBox, s_applicationTypes)

            ShutdownModeComboBox.Items.Clear()
            ShutdownModeComboBox.Items.AddRange(s_shutdownModes.ToArray())

            DisplayErrorControlIfAppXamlIsInvalid()

            PopulateTargetFrameworkComboBox(TargetFrameworkComboBox)
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

            PopulateIconList(False)
            UpdateIconImage(False)
            DisableControlsForXBAPProjects()

            ' Enable/disable the "View UAC Settings" button
            EnableControl(ViewUACSettingsButton, UACSettingsButtonSupported(ProjectProperties.OutputType))
        End Sub

#End Region

#Region "F1 help"

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropApplicationWPF
        End Function

#End Region

#Region "Saving the doc data"

        Private Sub TrySaveDocDataIfLastEditor()
            If _applicationXamlDocData IsNot Nothing AndAlso ServiceProvider IsNot Nothing Then
                Try
                    Dim rdt As IVsRunningDocumentTable = TryCast(ServiceProvider.GetService(GetType(IVsRunningDocumentTable)), IVsRunningDocumentTable)
                    Debug.Assert(rdt IsNot Nothing, "What?  No RDT?")
                    If rdt Is Nothing Then Throw New PropertyPageException("No RDT")

                    Dim hier As IVsHierarchy = Nothing
                    Dim flags As UInteger
                    Dim localPunk As IntPtr = IntPtr.Zero
                    Dim localFileName As String = Nothing
                    Dim itemId As UInteger
                    Dim docCookie As UInteger = 0
                    Dim readLocks As UInteger = 0
                    Dim editLocks As UInteger = 0

                    Try
                        VSErrorHandler.ThrowOnFailure(rdt.FindAndLockDocument(CType(_VSRDTFLAGS.RDT_NoLock, UInteger), _applicationXamlDocData.Name, hier, itemId, localPunk, docCookie))
                    Finally
                        If Not localPunk.Equals(IntPtr.Zero) Then
                            Marshal.Release(localPunk)
                            localPunk = IntPtr.Zero
                        End If
                    End Try

                    Debug.Assert(hier Is ProjectHierarchy, "RunningDocumentTable.FindAndLockDocument returned a different hierarchy than the one I was constructed with?")

                    Try
                        VSErrorHandler.ThrowOnFailure(rdt.GetDocumentInfo(docCookie, flags, readLocks, editLocks, localFileName, hier, itemId, localPunk))
                    Finally
                        If Not localPunk.Equals(IntPtr.Zero) Then
                            Marshal.Release(localPunk)
                            localPunk = IntPtr.Zero
                        End If
                    End Try

                    If editLocks = 1 Then
                        ' we're the only person with it open, save the document
                        VSErrorHandler.ThrowOnFailure(rdt.SaveDocuments(CUInt(__VSRDTSAVEOPTIONS.RDTSAVEOPT_SaveIfDirty), hier, itemId, docCookie))
                    End If
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(TrySaveDocDataIfLastEditor), NameOf(ApplicationPropPageVBWPF))
                    ShowErrorMessage(ex)
                End Try
            End If
        End Sub

#End Region

#Region "Application type"

        ' Shared list of all known application types and their properties...
        Private Shared ReadOnly s_applicationTypes As New List(Of ApplicationTypeInfo)

        ''' <summary>
        ''' Initialize the application types applicable to this page (logic is in the base class)
        ''' </summary>
        Private Shared Sub InitializeApplicationTypes()
            '   Note: WPF application page does not support NT service or Web control application types
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsApp, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WindowsApp_WPF, True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.WindowsClassLib, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WindowsClassLib_WPF, True))
            s_applicationTypes.Add(New ApplicationTypeInfo(ApplicationTypes.CommandLineApp, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_CommandLineApp_WPF, True))
        End Sub

#End Region

#Region "Application icon"

        '
        'Delegate to the base class for all functionality related to the icon combobox
        '

        Private Sub IconCombobox_DropDown(sender As Object, e As EventArgs) Handles IconCombobox.DropDown
            HandleIconComboboxDropDown(sender)
        End Sub

        Private Sub IconCombobox_DropDownClosed(sender As Object, e As EventArgs) Handles IconCombobox.DropDownClosed
            HandleIconComboboxDropDown(sender)
        End Sub

        Private Sub IconCombobox_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles IconCombobox.SelectionChangeCommitted
            HandleIconComboboxSelectionChangeCommitted(sender)
        End Sub

        ''' <summary>
        ''' Enables the Icon combobox (if Enable=True), but only if the associated property is supported
        ''' </summary>
        Protected Overrides Sub EnableIconComboBox(Enable As Boolean)
            'Icon combobox shouldn't be enabled for XBAP projects
            EnableControl(CommonControls.IconCombobox, Enable AndAlso Not IsXBAP())
            UpdateIconImage(False)
        End Sub

#End Region

#Region "Assembly Information button"

        ''' <summary>
        ''' Display the assembly information dialog
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub AssemblyInfoButton_Click(sender As Object, e As EventArgs) Handles AssemblyInfoButton.Click
            ShowChildPage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AssemblyInfo_Title, GetType(AssemblyInfoPropPage), HelpKeywords.VBProjPropAssemblyInfo)
        End Sub

#End Region

#Region "OutputType property ('Application Type' combobox)"

        ''' <summary>
        ''' Gets the output type from the UI fields
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <remarks>OutputType is obtained from the value in the Application Type field</remarks>
        Private Function GetOutputTypeFromUI(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim AppType As ApplicationTypes

            If ApplicationTypeComboBox.SelectedItem IsNot Nothing Then
                AppType = DirectCast(ApplicationTypeComboBox.SelectedItem, ApplicationTypeInfo).ApplicationType
            Else
                Debug.Fail("Why isn't there a selection in the Application Type combobox?")
                AppType = ApplicationTypes.WindowsApp
            End If

            value = OutputTypeFromApplicationType(AppType)
            Return True
        End Function

        ''' <summary>
        ''' Sets the output type into the UI fields
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function SetOutputTypeIntoUI(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If value IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(value) Then
                Dim AppType As ApplicationTypes = ApplicationTypeFromOutputType(CType(value, VSLangProj.prjOutputType))
                ApplicationTypeComboBox.SelectedItem = s_applicationTypes.Find(ApplicationTypeInfo.ApplicationTypePredicate(AppType))
                EnableApplicationIconAccordingToApplicationType(AppType)
                EnableControl(ViewUACSettingsButton, UACSettingsButtonSupported(AppType))
            Else
                ApplicationTypeComboBox.SelectedIndex = -1
                EnableIconComboBox(False)
                EnableControl(ViewUACSettingsButton, False)
            End If

            Return True
        End Function

        ''' <summary>
        ''' Enables/Disables some controls based on the current application type
        ''' </summary>
        ''' <param name="AppType"></param>
        Private Sub EnableApplicationIconAccordingToApplicationType(AppType As ApplicationTypes)
            Select Case AppType
                Case ApplicationTypes.CommandLineApp
                    EnableIconComboBox(True)

                Case ApplicationTypes.WindowsApp
                    EnableIconComboBox(True)

                Case ApplicationTypes.WindowsClassLib
                    EnableIconComboBox(False)

                Case Else
                    Debug.Fail("Unexpected ApplicationType")
                    EnableIconComboBox(False)
            End Select
        End Sub

        '
        ' Application Type and Output Type are related in the following manner (note that it *is* one-to-one for WPF,
        '  unlike the more complicated logic for the non-WPF VB application page):
        '
        '  Application Type      -> Output Type
        '  ---------------------    -----------
        '  Windows Application   -> WinExe
        '  Windows Class Library -> Library
        '  Console Application   -> Exe
        '

        ''' <summary>
        ''' Given an OutputType, returns the Application Type for it, differentiating if necessary based on the value of MyType
        ''' </summary>
        ''' <param name="OutputType">Output type</param>
        Friend Shared Function ApplicationTypeFromOutputType(OutputType As VSLangProj.prjOutputType) As ApplicationTypes
            Select Case OutputType

                Case VSLangProj.prjOutputType.prjOutputTypeExe
                    Return ApplicationTypes.CommandLineApp
                Case VSLangProj.prjOutputType.prjOutputTypeWinExe
                    Return ApplicationTypes.WindowsApp
                Case VSLangProj.prjOutputType.prjOutputTypeLibrary
                    Return ApplicationTypes.WindowsClassLib
                Case Else
                    If Switches.PDApplicationType.Level >= TraceLevel.Warning Then
                        Debug.Fail(String.Format("Unexpected Output Type {0} - Mapping to ApplicationTypes.WindowsApp", OutputType))
                    End If
                    Return ApplicationTypes.WindowsApp
            End Select
        End Function

        ''' <summary>
        ''' Given an Application Type (a VB-only concept), return the Output Type for it (the project system's concept)
        ''' </summary>
        ''' <param name="AppType"></param>
        Friend Shared Function OutputTypeFromApplicationType(AppType As ApplicationTypes) As VSLangProj.prjOutputType
            Select Case AppType

                Case ApplicationTypes.WindowsApp
                    Return VSLangProj.prjOutputType.prjOutputTypeWinExe
                Case ApplicationTypes.WindowsClassLib
                    Return VSLangProj.prjOutputType.prjOutputTypeLibrary
                Case ApplicationTypes.CommandLineApp
                    Return VSLangProj.prjOutputType.prjOutputTypeExe
                Case Else
                    Debug.Fail(String.Format("Unexpected ApplicationType {0}", AppType))
                    Return VSLangProj.prjOutputType.prjOutputTypeExe
            End Select
        End Function

#End Region

#Region "Use Application Framework checkbox"

        ''' <summary>
        ''' Enables the "Enable application framework" checkbox (if Enable=True), but only if it is supported in this project with current settings
        ''' </summary>
        ''' <param name="Enable"></param>
        Private Sub EnableUseApplicationFrameworkCheckBox(Enable As Boolean)
            GetPropertyControlData(PROPID_UseApplicationFramework).EnableControls(Enable)
        End Sub

        Private Enum TriState
            [False]
            [Disabled]
            [True]
        End Enum

        Private Function SetUseApplicationFrameworkIntoUI(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                UseApplicationFrameworkCheckBox.CheckState = CheckState.Indeterminate
                EnableUseApplicationFrameworkCheckBox(False)
            Else
                Select Case CType(value, TriState)
                    Case TriState.Disabled
                        EnableUseApplicationFrameworkCheckBox(False)
                        UseApplicationFrameworkCheckBox.Checked = False
                    Case TriState.True
                        EnableUseApplicationFrameworkCheckBox(True)
                        UseApplicationFrameworkCheckBox.Checked = True
                    Case TriState.False
                        EnableUseApplicationFrameworkCheckBox(True)
                        UseApplicationFrameworkCheckBox.Checked = False
                    Case Else
                        Debug.Fail("Unexpected tristate")
                End Select
            End If

            'Toggle whether the application framework properties are enabled
            EnableControl(WindowsAppGroupBox, UseApplicationFrameworkCheckBox.Enabled AndAlso UseApplicationFrameworkCheckBox.Checked)

            Return True
        End Function

        Private Function GetUseApplicationFrameworkFromUI(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If Not UseApplicationFrameworkCheckBox.Enabled Then
                Debug.Fail("Get shouldn't be called if disabled")
                value = TriState.Disabled
            Else
                value = IIf(UseApplicationFrameworkCheckBox.Checked, TriState.True, TriState.False)
            End If

            Return True
        End Function

        Private Sub SetUseApplicationFrameworkIntoStorage(value As TriState)
            Select Case value
                Case TriState.Disabled
                    Debug.Fail("Shouldn't get here")
                    EnableUseApplicationFrameworkCheckBox(False)
                Case TriState.False
                    'Enable using a start-up object instead of a startup URI.  

                    Dim isStartupObjectMissing, isSubMain As Boolean
                    Dim startupObject As String = GetCurrentStartupObjectFromStorage(isStartupObjectMissing, isSubMain)
                    Debug.Assert(Not isStartupObjectMissing, "Checkbox should have been disabled")

                    ' Must set the project's start-up object to "Sub Main", unless it's already set to
                    '  something non-empty.
                    If startupObject = "" Then
                        SetStartupObjectIntoStorage(STARTUPOBJECT_SubMain)
                    End If

                    'Set the Application.xaml file's build action to None
                    Dim appXamlProjectItem As ProjectItem = FindApplicationXamlProjectItem(createAppXamlIfDoesNotExist:=False)
                    If appXamlProjectItem IsNot Nothing Then
                        DTEUtils.SetBuildAction(appXamlProjectItem, VSLangProj.prjBuildAction.prjBuildActionNone)

                        'Close our cached docdata of the file
                        CleanUpApplicationXamlDocData()
                    End If
                Case TriState.True
                    'Enable using a StartupURI instead of a startup object.  Must 
                    '  create an Application.xaml file and set the project's start-up object 
                    '  to blank (if it's not already).

                    '... First create the Application.xaml if it doesn't exist.  We do this first because
                    '  we don't want to change the startup object until we know this has succeeded.
                    Using CreateAppDotXamlDocumentForApplicationDefinitionFile(True)
                        'Don't need to do anything with it, just make sure it gets created
                    End Using

                    '... Then change the project's start-up object.
                    Dim isStartupObjectMissing, isSubMain As Boolean
                    Dim startupObject As String = GetCurrentStartupObjectFromStorage(isStartupObjectMissing, isSubMain)
                    Debug.Assert(Not isStartupObjectMissing, "Checkbox should have been disabled")

                    If startupObject <> "" Then 'Don't change it if it's already blank
                        SetStartupObjectIntoStorage("")
                    End If
                Case Else
                    Debug.Fail("Unexpected tristate")
            End Select
        End Sub

        Private Function GetUseApplicationFrameworkFromStorage() As TriState
            If Not IsStartUpObjectSupportedInThisProject() Then
                Return TriState.Disabled
            End If

            'The application framework checkbox should only be enabled for WPF Application
            '  projects, not console or class library
            Dim oOutputType As Object = Nothing
            If GetProperty(VsProjPropId.VBPROJPROPID_OutputType, oOutputType) AndAlso oOutputType IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oOutputType) Then
                Dim outputType As VSLangProj.prjOutputType = CType(oOutputType, VSLangProj.prjOutputType)
                If outputType <> VSLangProj.prjOutputType.prjOutputTypeWinExe Then
                    Return TriState.Disabled
                End If
            End If

            Dim isStartupObjectMissing, isSubMain As Boolean
            Dim startupObject As String = GetCurrentStartupObjectFromStorage(isStartupObjectMissing, isSubMain)
            Debug.Assert(Not isStartupObjectMissing, "Should've been caught in IsStartupObjectSupportedInThisProject...")
            If startupObject <> "" Then
                'A start-up object (or Sub Main) is specified for this project.  This takes run-time precedence over
                '  the StartupURI.  So set Use Application Framework to false.
                Return TriState.False
            End If

            'Is there an Application.xaml file?
            If Not ApplicationXamlFileExistsInProject() Then
                'No Application.xaml file currently.  Use startup object, not URI.
                Return TriState.False
            End If

            Return TriState.True
        End Function

#End Region

#Region "Application.xaml handling"

        ''' <summary>
        ''' Returns true iff the project contains an Application.xaml file
        ''' </summary>
        Protected Overridable Function ApplicationXamlFileExistsInProject() As Boolean
            Return FindApplicationXamlProjectItem(False) IsNot Nothing
        End Function

        ''' <summary>
        ''' Finds the Application.xaml file in the application, if one exists.
        ''' </summary>
        ''' <remarks>
        ''' Overridable for unit testing.
        ''' </remarks>
        Private Function FindApplicationXamlProjectItem(createAppXamlIfDoesNotExist As Boolean) As ProjectItem
            Return FindApplicationXamlProjectItem(ProjectHierarchy, createAppXamlIfDoesNotExist)
        End Function

        ''' <summary>
        ''' Finds the Application.xaml file in the application, if one exists.
        ''' </summary>
        ''' <remarks>
        ''' Overridable for unit testing.
        ''' </remarks>
        Friend Shared Function FindApplicationXamlProjectItem(hierarchy As IVsHierarchy, createAppXamlIfDoesNotExist As Boolean) As ProjectItem
            Try
                Dim specialFiles As IVsProjectSpecialFiles = TryCast(hierarchy, IVsProjectSpecialFiles)
                If specialFiles Is Nothing Then
                    Return Nothing
                End If

                Dim flags As UInteger = 0
                Dim bstrFilename As String = Nothing
                Dim itemid As UInteger
                ErrorHandler.ThrowOnFailure(specialFiles.GetFile(__PSFFILEID3.PSFFILEID_AppXaml, flags, itemid, bstrFilename))
                If itemid <> VSITEMID.NIL AndAlso bstrFilename <> "" Then
                    'Get the ProjectItem for it
                    Dim extObject As Object = Nothing
                    ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(itemid, __VSHPROPID.VSHPROPID_ExtObject, extObject))
                    Return CType(extObject, ProjectItem)
                End If

                If createAppXamlIfDoesNotExist Then
                    'There is no current application definition file, and the caller requested us to create it.
                    '  First we need to see if there is an existing Application.xaml file that has its build action
                    '  set to none.  If so, we'll just flip its build action to ApplicationDefinition and try again.
                    Const ApplicationDefinitionExpectedName As String = "Application.xaml"
                    Dim Project As Project = DTEUtils.EnvDTEProject(hierarchy)
                    Dim foundAppDefinition As ProjectItem = DTEUtils.QueryProjectItems(Project.ProjectItems, ApplicationDefinitionExpectedName)
                    If foundAppDefinition IsNot Nothing Then
                        'We only do this if the build action is actually set to None.  We'll assume if it was set to
                        '  anything else that the user intended it that way.
                        If DTEUtils.GetBuildAction(foundAppDefinition) = VSLangProj.prjBuildAction.prjBuildActionNone Then
                            DTEUtils.SetBuildActionAsString(foundAppDefinition, BUILDACTION_APPLICATIONDEFINITION)
                        End If
                    End If

                    'Ask the project system to create the application definition file for us
                    flags = flags Or CUInt(__PSFFLAGS.PSFF_CreateIfNotExist)
                    ErrorHandler.ThrowOnFailure(specialFiles.GetFile(__PSFFILEID3.PSFFILEID_AppXaml, flags, itemid, bstrFilename))
                    If itemid <> VSITEMID.NIL AndAlso bstrFilename <> "" Then
                        'Get the ProjectItem for it
                        Dim extObject As Object = Nothing
                        ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(itemid, __VSHPROPID.VSHPROPID_ExtObject, extObject))
                        Return CType(extObject, ProjectItem)
                    End If

                    'The file should have been created, or it should have failed.  Throw an unexpected
                    '  error, because our contract says we have to succeed or throw if
                    '  createAppXamlIfDoesNotExist is specified.
                    Throw New PropertyPageException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Unexpected)
                End If

                Return Nothing
            Catch ex As Exception
                Throw New PropertyPageException(
                    String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_CantOpenOrCreateAppXaml_1Arg, ex.Message),
                    HelpKeywords.VBProjPropWPFApp_CantOpenOrCreateAppXaml,
                    ex)
            End Try
        End Function

        ''' <summary>
        ''' Lazily creates and returns a DocData representing the application definition file for this
        '''   project (Application.xaml).
        ''' </summary>
        ''' <param name="createAppXamlIfDoesNotExist">If True, will attempt to create the file if it does not exist.  In this case, 
        ''' the function will never return Nothing, but rather will throw an exception if there's a problem.</param>
        Private Function GetApplicationXamlDocData(createAppXamlIfDoesNotExist As Boolean) As DocData
            If _applicationXamlDocData Is Nothing Then
                Dim applicationXamlProjectItem As ProjectItem = FindApplicationXamlProjectItem(createAppXamlIfDoesNotExist)
                If applicationXamlProjectItem IsNot Nothing Then
                    _applicationXamlDocData = New DocData(ServiceProvider, applicationXamlProjectItem.FileNames(1))
                End If
            End If

            If _applicationXamlDocData IsNot Nothing Then
                Return _applicationXamlDocData
            ElseIf createAppXamlIfDoesNotExist Then
                Debug.Fail("This function should not have reached here if createAppDotXamlFileIfNotExist was passed in as True.  It should have thrown an exception by now.")
                Throw New PropertyPageException(
                    String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_CantOpenOrCreateAppXaml_1Arg,
                        My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Unexpected),
                    HelpKeywords.VBProjPropWPFApp_CantOpenOrCreateAppXaml)
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Finds the Application.xaml file, if any, in the project, and returns a
        '''   WFPAppDotXamlDocument to read/write to it.
        ''' If there is no Application.xaml, and createAppDotXamlFileIfNotExist=True, 
        '''   an Application.xaml file is created.  If createAppDotXamlFileIfNotExist is specified,
        '''   this function will either succeed or throw an exception, but will not return Nothing.
        ''' </summary>
        ''' <param name="createAppXamlIfDoesNotExist"></param>
        ''' <returns>The AppDotXamlDocument</returns>
        Protected Overridable Function CreateAppDotXamlDocumentForApplicationDefinitionFile(createAppXamlIfDoesNotExist As Boolean) As AppDotXamlDocument
            Dim docData As DocData = GetApplicationXamlDocData(createAppXamlIfDoesNotExist)
            If docData IsNot Nothing Then
                Dim vsTextLines As IVsTextLines = TryCast(docData.Buffer, IVsTextLines)
                If vsTextLines Is Nothing Then
                    Throw New PropertyPageException(
                        My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_AppXamlOpenInUnsupportedEditor,
                        HelpKeywords.VBProjPropWPFApp_AppXamlOpenInUnsupportedEditor)
                End If
                Dim document As New AppDotXamlDocument(vsTextLines)
                Return document
            End If

            If createAppXamlIfDoesNotExist Then
                Debug.Fail("This function should not have reached here if createAppDotXamlFileIfNotExist was passed in as True.  It should have thrown an exception by now.")
                Throw New PropertyPageException(
                    String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_CantOpenOrCreateAppXaml_1Arg,
                        My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Unexpected),
                    HelpKeywords.VBProjPropWPFApp_CantOpenOrCreateAppXaml)
            Else
                Return Nothing
            End If
        End Function

        Private Function GetStartupUriFromStorage() As String
            Using document As AppDotXamlDocument = CreateAppDotXamlDocumentForApplicationDefinitionFile(False)
                If document Is Nothing Then
                    Return Nothing
                Else
                    Return document.GetStartupUri()
                End If
            End Using
        End Function

        Private Sub SetStartupUriIntoStorage(value As String)
            Using document As AppDotXamlDocument = CreateAppDotXamlDocumentForApplicationDefinitionFile(True)
                Debug.Assert(document IsNot Nothing, "This shouldn't ever be returned as Nothing from GetAppDotXamlDocument(True)")
                document.SetStartupUri(value)
            End Using
        End Sub

#End Region

#Region "StartupObject/StartupUri combobox"

#Region "Nested class hierarchy StartupObjectOrUri, StartupObject, StartupUri"

        ''' <summary>
        ''' Represents an entry in the Startup Object/Startup URI combobox
        '''   (depending on the setting of the Enable Application Framework
        '''   checkbox)
        ''' </summary>
        <Serializable>
        Friend MustInherit Class StartupObjectOrUri
            Private ReadOnly _value As String
            Private ReadOnly _description As String

            Public Sub New(value As String, description As String)
                If value Is Nothing Then
                    value = ""
                End If
                If description Is Nothing Then
                    description = ""
                End If

                _value = value
                _description = description
            End Sub

            ''' <summary>
            ''' The value displayed to the user in the combobox
            ''' </summary>
            Public Overrides Function ToString() As String
                Return Description
            End Function

            Public ReadOnly Property Value As String
                Get
                    Return _value
                End Get
            End Property

            Public ReadOnly Property Description As String
                Get
                    Return _description
                End Get
            End Property

            Public Overrides Function Equals(obj As Object) As Boolean
                Dim startupObjectOrUri = TryCast(obj, StartupObjectOrUri)
                If startupObjectOrUri IsNot Nothing Then
                    If obj.GetType() IsNot [GetType]() Then
                        Return False
                    Else
                        Return Value.Equals(startupObjectOrUri.Value, StringComparison.OrdinalIgnoreCase)
                    End If
                End If

                Return False
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return -1937169414 + StringComparer.OrdinalIgnoreCase.GetHashCode(Value)
            End Function

        End Class

        <Serializable>
        Friend Class StartupObject
            Inherits StartupObjectOrUri

            Public Sub New(value As String, description As String)
                MyBase.New(value, description)
            End Sub

            Protected Overridable ReadOnly Property IsEquivalentToSubMain As Boolean
                Get
                    Return Value = "" OrElse Value.Equals(STARTUPOBJECT_SubMain, StringComparison.OrdinalIgnoreCase)
                End Get
            End Property

            Public Overrides Function Equals(obj As Object) As Boolean
                Dim startupObject = TryCast(obj, StartupObject)
                If startupObject IsNot Nothing Then

                    If [GetType]() IsNot obj.GetType() Then
                        Return False
                    ElseIf IsEquivalentToSubMain AndAlso startupObject.IsEquivalentToSubMain Then
                        Return True
                    Else
                        Return Value.Equals(startupObject.Value, StringComparison.OrdinalIgnoreCase)
                    End If
                End If

                Return False
            End Function

            Public Overrides Function GetHashCode() As Integer
                Dim hashCode = 870297925
                hashCode = hashCode * -1521134295 + MyBase.GetHashCode()
                hashCode = hashCode * -1521134295 + IsEquivalentToSubMain.GetHashCode()
                Return hashCode
            End Function

        End Class

        <Serializable>
        Friend Class StartupObjectNone
            Inherits StartupObject

            Public Sub New()
                MyBase.New("", s_noneText)
            End Sub

            Protected Overrides ReadOnly Property IsEquivalentToSubMain As Boolean
                Get
                    Return False
                End Get
            End Property

        End Class

        <Serializable>
        Friend Class StartupUri
            Inherits StartupObjectOrUri

            Public Sub New(value As String)
                MyBase.New(value, value)
            End Sub

        End Class

#End Region

        ''' <summary>
        ''' Happens when the start-up object combobox box is opened.  Use this to populate it with the 
        '''   correct current choices.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub StartupObjectOrUriComboBox_DropDown(sender As Object, e As EventArgs) Handles StartupObjectOrUriComboBox.DropDown
            PopulateStartupObjectOrUriComboboxAndKeepCurrentEntry()
            SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' Populates the startup object/URI combobox with the available choices, depending on whether
        '''   it should be showing startup URI or startup object.
        ''' </summary>
        Private Sub PopulateStartupObjectOrUriComboboxAndKeepCurrentEntry()
            Dim populateWithStartupUriInsteadOfStartupObject As Boolean = ShouldStartupUriBeDisplayedInsteadOfStartupObject()
#If DEBUG Then
            If populateWithStartupUriInsteadOfStartupObject Then
                Debug.Assert(StartupObjectOrUriComboBox.SelectedItem Is Nothing OrElse TypeOf StartupObjectOrUriComboBox.SelectedItem Is StartupUri,
                    "Current entry in the startup object/URI combobox is out of sync with the current state - it was expected to be a startup URI")
            Else
                Debug.Assert(StartupObjectOrUriComboBox.SelectedItem Is Nothing OrElse TypeOf StartupObjectOrUriComboBox.SelectedItem Is StartupObject,
                    "Current entry in the startup object/URI combobox is out of sync with the current state - it was expected to be a startup Object")
            End If
#End If

            'Remember the current selected item
            Dim currentSelectedItem As StartupObjectOrUri = CType(StartupObjectOrUriComboBox.SelectedItem, StartupObjectOrUri)

            'Populate the dropdowns
            If populateWithStartupUriInsteadOfStartupObject Then
                PopulateStartupUriDropdownValues(StartupObjectOrUriComboBox)
            Else
                PopulateStartupObjectDropdownValues(StartupObjectOrUriComboBox)
            End If

            'Reselect the current selected item
            SetSelectedStartupObjectOrUriIntoCombobox(StartupObjectOrUriComboBox, currentSelectedItem)
        End Sub

        Private Function GetStartupObjectPropertyControlData() As PropertyControlData
            Return GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject)
        End Function

        Private Function IsStartupObjectMissing() As Boolean
            Return GetStartupObjectPropertyControlData().IsMissing
        End Function

        Private Function GetCurrentStartupObjectFromStorage(ByRef isMissing As Boolean, ByRef isSubMain As Boolean) As String
            isMissing = False
            Dim oStartupObject As Object = Nothing
            If GetProperty(VsProjPropId.VBPROJPROPID_StartupObject, oStartupObject) AndAlso oStartupObject IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oStartupObject) Then
                Dim startupObject As String = TryCast(oStartupObject, String)
                If startupObject = "" OrElse startupObject.Equals(STARTUPOBJECT_SubMain, StringComparison.OrdinalIgnoreCase) Then
                    isSubMain = True
                End If

                Return startupObject
            End If

            isMissing = True
            Return Nothing
        End Function

        ''' <summary>
        ''' Returns true if start-up objects other than "(None)" are supported for the current settings
        ''' </summary>
        Private Function IsStartUpObjectSupportedInThisProject() As Boolean
            If IsStartupObjectMissing() Then
                Return False
            End If

            Dim oOutputType As Object = Nothing
            If GetProperty(VsProjPropId.VBPROJPROPID_OutputType, oOutputType) AndAlso oOutputType IsNot Nothing AndAlso Not PropertyControlData.IsSpecialValue(oOutputType) Then
                Dim outputType As VSLangProj.prjOutputType = CType(oOutputType, VSLangProj.prjOutputType)
                If outputType = VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                    'Not supported for class libraries
                    Return False
                End If
            Else
                Return False
            End If

            Return True
        End Function

        ''' <summary>
        ''' Retrieve the current value of the startup object/URI value from the combobox on the page
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function GetStartupObjectOrUriFromUI(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = CType(StartupObjectOrUriComboBox.SelectedItem, StartupObjectOrUri)
            Debug.Assert(value IsNot Nothing, "GetStartupObjectOrUriFromUI(): Shouldn't get null value")
            Return True
        End Function

        Private Shared Sub SetSelectedStartupObjectOrUriIntoCombobox(combobox As ComboBox, startupObjectOrUri As StartupObjectOrUri)
            'Find the value in the combobox
            Dim foundStartupObjectOrUri As StartupObjectOrUri = Nothing
            If startupObjectOrUri IsNot Nothing Then
                For Each entry As StartupObjectOrUri In combobox.Items
                    If entry.Equals(startupObjectOrUri) Then
                        combobox.SelectedItem = entry
                        foundStartupObjectOrUri = entry
                        Exit For
                    End If
                Next
            End If

            If foundStartupObjectOrUri Is Nothing AndAlso startupObjectOrUri IsNot Nothing Then
                'The value wasn't found in the combobox.  Add it now.
                combobox.Items.Add(startupObjectOrUri)
                combobox.SelectedItem = startupObjectOrUri
            End If
        End Sub

        ''' <summary>
        ''' Setter - Place the startup object/URI value into the combobox on the page
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Private Function SetStartupObjectOrUriIntoUI(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                StartupObjectOrUriComboBox.SelectedIndex = -1
                EnableControl(StartupObjectOrUriComboBox, False)
                Return True
            End If

            If GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject).IsReadOnly Then
                EnableControl(control, False)
            End If

            Debug.Assert(TypeOf value Is StartupObjectOrUri)
            Dim valueAsStartupObjectOrUri As StartupObjectOrUri = CType(value, StartupObjectOrUri)
            SetSelectedStartupObjectOrUriIntoCombobox(StartupObjectOrUriComboBox, valueAsStartupObjectOrUri)

            If value Is Nothing Then
                Debug.Fail("Unexpected null value in SetStartupObjectOrUriIntoUI")
            ElseIf TypeOf value Is StartupObject Then
                StartupObjectOrUriLabel.Text = s_startupObjectLabelText
            ElseIf TypeOf value Is StartupUri Then
                StartupObjectOrUriLabel.Text = s_startupUriLabelText
            Else
                Debug.Fail("Unexpected startup/uri type")
            End If
            Return True
        End Function

        ''' <summary>
        ''' True if, according to the current, persisted state, the StartupObject/URI label should be
        '''   "Startup URI".  If it should be "Startup Object", it returns False.
        ''' </summary>
        Private Function ShouldStartupUriBeDisplayedInsteadOfStartupObject() As Boolean
            Dim tristateUseApplicationFramework As TriState = GetUseApplicationFrameworkFromStorage()
            If tristateUseApplicationFramework = TriState.True Then
                Return True 'Show Startup URI
            Else
                'Show Startup Object
                Return False
            End If
        End Function

        ''' <summary>
        ''' Retrieves the value of the Startup Object or Startup Uri from
        '''   its persisted storage (project file or Application.xaml)
        ''' </summary>
        Private Function GetStartupObjectOrUriValueFromStorage() As StartupObjectOrUri
            If ShouldStartupUriBeDisplayedInsteadOfStartupObject() Then
                Return New StartupUri(GetStartupUriFromStorage())
            Else
                If IsStartUpObjectSupportedInThisProject() Then
                    Dim startupObjectMissing, isSubMain As Boolean
                    Dim startupObject As String = GetCurrentStartupObjectFromStorage(startupObjectMissing, isSubMain)
                    Debug.Assert(Not startupObjectMissing, "IsStartUpObjectSupportedInThisProject should have failed")
                    If isSubMain Then
                        Return New StartupObject(startupObject, STARTUPOBJECT_SubMain)
                    Else
                        Debug.Assert(startupObject <> "", "but isSubMain was supposed to be false")
                        Dim fullyQualifiedStartupObject As String = startupObject
                        Dim relativeStartupObject As String = RemoveCurrentRootNamespace(fullyQualifiedStartupObject)
                        Return New StartupObject(fullyQualifiedStartupObject, relativeStartupObject)
                    End If
                Else
                    Return New StartupObjectNone
                End If
            End If
        End Function

        ''' <summary>
        ''' Stores the value of the Startup Object or Startup Uri into
        '''   its persisted storage (project file or Application.xaml)
        ''' </summary>
        Private Sub SetStartupObjectOrUriValueIntoStorage(value As StartupObjectOrUri)
            If TypeOf value Is StartupObject Then
                SetStartupObjectIntoStorage(value.Value)
            ElseIf TypeOf value Is StartupUri Then
                SetStartupUriIntoStorage(value.Value)
            Else
                Debug.Fail("Unexpected startupobject/uri type")
            End If
        End Sub

        Private Sub SetStartupObjectIntoStorage(value As String)
            GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject).SetPropertyValue(value)
        End Sub

        Private Sub PopulateStartupObjectDropdownValues(startupObjectComboBox As ComboBox)
            startupObjectComboBox.DropDownStyle = ComboBoxStyle.DropDownList
            startupObjectComboBox.Items.Clear()

            If Not IsStartUpObjectSupportedInThisProject() Then
                startupObjectComboBox.Items.Add(New StartupObjectNone())
                startupObjectComboBox.SelectedIndex = 0
            Else
                startupObjectComboBox.Items.AddRange(GetAvailableStartupObjects().ToArray())
            End If
        End Sub

        Private Function GetAvailableStartupObjects() As List(Of StartupObject)
            Dim startupObjects As New List(Of StartupObject)

            Dim startupObjectPropertyControlData As PropertyControlData = GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject)
            Dim startupObjectPropertyDescriptor As PropertyDescriptor = startupObjectPropertyControlData.PropDesc

            If Not startupObjectPropertyControlData.IsMissing Then
                Using New WaitCursor
                    Switches.TracePDPerf("*** Populating start-up object list from the project [may be slow for a large project]")
                    Dim rawStartupObjects As ICollection = Nothing

                    'Force us to see any new start-up objects in the project
                    RefreshPropertyStandardValues()

                    'Certain project types may not support standard values
                    If startupObjectPropertyDescriptor.Converter.GetStandardValuesSupported() Then
                        rawStartupObjects = startupObjectPropertyDescriptor.Converter.GetStandardValues()
                    End If

                    If rawStartupObjects IsNot Nothing Then
                        For Each o As Object In rawStartupObjects
                            Dim fullyQualifiedStartupObject As String = TryCast(o, String)
                            Dim relativeStartupObject As String = RemoveCurrentRootNamespace(fullyQualifiedStartupObject)
                            startupObjects.Add(New StartupObject(fullyQualifiedStartupObject, relativeStartupObject))
                        Next
                    End If
                End Using
            End If

            Return startupObjects
        End Function

        Private Sub PopulateStartupUriDropdownValues(startupObjectComboBox As ComboBox)
            startupObjectComboBox.DropDownStyle = ComboBoxStyle.DropDownList
            startupObjectComboBox.Items.Clear()

            If Not IsStartUpObjectSupportedInThisProject() Then
                Debug.Fail("Shouldn't reach here, because we should be showing a Startup Object instead of a Startup URI if StartupObject is not supported")
                startupObjectComboBox.Items.Add(New StartupObjectNone())
                startupObjectComboBox.SelectedIndex = 0
            Else
                startupObjectComboBox.Items.AddRange(GetAvailableStartupUris().ToArray())
            End If
        End Sub

        ''' <summary>
        ''' Returns true if the given file path is relative to the project directory
        ''' </summary>
        ''' <param name="fullPath"></param>
        Private Function IsFileRelativeToProjectPath(fullPath As String) As Boolean
            Dim relativePath As String = GetProjectRelativeFilePath(fullPath)
            Return Not IO.Path.IsPathRooted(relativePath)
        End Function

        ''' <summary>
        ''' Finds all .xaml files in the project which can be used as the start-up URI.
        ''' </summary>
        ''' <param name="projectItems"></param>
        ''' <param name="list"></param>
        Private Sub FindXamlPageFiles(projectItems As ProjectItems, list As List(Of ProjectItem))
            For Each projectItem As ProjectItem In projectItems
                If IO.Path.GetExtension(projectItem.FileNames(1)).Equals(".xaml", StringComparison.OrdinalIgnoreCase) Then
                    'We only want .xaml files with BuildAction="Page"
                    Dim CurrentBuildAction As String = DTEUtils.GetBuildActionAsString(projectItem)
                    If CurrentBuildAction IsNot Nothing AndAlso BUILDACTION_PAGE.Equals(CurrentBuildAction, StringComparison.OrdinalIgnoreCase) Then
                        'Build action is correct.

                        'Is the item inside the project folders (instead of, say, a link to an external file)?
                        If IsFileRelativeToProjectPath(projectItem.FileNames(1)) Then
                            'Okay, we want this one
                            list.Add(projectItem)
                        End If
                    End If
                End If

                If projectItem.ProjectItems IsNot Nothing Then
                    FindXamlPageFiles(projectItem.ProjectItems, list)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Gets all the files (as a list of StartupUri objects) in the project which are appropriate for the 
        '''   StartupUri property.
        ''' </summary>
        ''' <remarks>
        ''' Note: it's currently returning a List of Object only because I'm having trouble getting the VSTS
        '''   code accessors to work properly with List(Of StartupUri).
        ''' </remarks>
        Private Function GetAvailableStartupUris() As List(Of Object)
            Dim startupObjects As New List(Of Object)

            Dim startupObjectPropertyControlData As PropertyControlData = GetPropertyControlData(VsProjPropId.VBPROJPROPID_StartupObject)

            If Not startupObjectPropertyControlData.IsMissing Then
                Using New WaitCursor
                    Switches.TracePDPerf("*** Populating start-up URI list from the project [may be slow for a large project]")
                    Dim xamlFiles As New List(Of ProjectItem)
                    FindXamlPageFiles(DTEProject.ProjectItems, xamlFiles)

                    For Each projectItem As ProjectItem In xamlFiles
                        startupObjects.Add(New StartupUri(GetProjectRelativeFilePath(projectItem.FileNames(1))))
                    Next
                End Using
            End If

            Return startupObjects
        End Function

#End Region

#Region "ShutdownMode"

#Region "Nested class ShutdownMode"

        ''' <summary>
        ''' Nested class that represents a shutdown mode value, and can be placed
        '''   directly into a combobox as an entry.
        ''' </summary>
        Friend Class ShutdownMode
            Private ReadOnly _value As String
            Private ReadOnly _description As String

            Public Sub New(value As String, description As String)
                Requires.NotNull(value, NameOf(value))
                Requires.NotNull(description, NameOf(description))

                _value = value
                _description = description
            End Sub

            Public ReadOnly Property Value As String
                Get
                    Return _value
                End Get
            End Property

            Public ReadOnly Property Description As String
                Get
                    Return _description
                End Get
            End Property

            Public Overrides Function ToString() As String
                Return _description
            End Function

        End Class

#End Region

        Private Shared ReadOnly s_shutdownModes As New List(Of ShutdownMode)
        Private Shared s_defaultShutdownMode As ShutdownMode

        Private Shared Sub InitializeShutdownModeValues()
            'This order affects the order in the combobox
            s_defaultShutdownMode = New ShutdownMode("OnLastWindowClose", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_ShutdownMode_OnLastWindowClose)
            s_shutdownModes.Add(s_defaultShutdownMode)
            s_shutdownModes.Add(New ShutdownMode("OnMainWindowClose", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_ShutdownMode_OnMainWindowClose))
            s_shutdownModes.Add(New ShutdownMode("OnExplicitShutdown", My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_ShutdownMode_OnExplicitShutdown))
        End Sub

        Public Function GetShutdownModeFromStorage() As String
            Using document As AppDotXamlDocument = CreateAppDotXamlDocumentForApplicationDefinitionFile(False)
                If document Is Nothing Then
                    Return Nothing
                Else
                    Return document.GetShutdownMode()
                End If
            End Using
        End Function

        Public Sub SetShutdownModeIntoStorage(value As String)
            Using document As AppDotXamlDocument = CreateAppDotXamlDocumentForApplicationDefinitionFile(True)
                document.SetShutdownMode(value)
            End Using
        End Sub

        ''' <summary>
        ''' Getter for the "ShutdownMode" property.  Retrieves the current value
        '''   of the property from the combobox.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function GetShutdownModeFromUI(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim currentShutdownMode As ShutdownMode = CType(ShutdownModeComboBox.SelectedItem, ShutdownMode)
            If currentShutdownMode Is Nothing Then
                value = ""
            Else
                value = currentShutdownMode.Value
            End If

            Return True
        End Function

        ''' <summary>
        ''' Getter for the "ShutdownMode" property.  Takes the given value for the
        '''   property, and converts it into the display value, then puts it into the
        '''   combobox.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        Protected Function SetShutdownModeIntoUI(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                ShutdownModeComboBox.SelectedIndex = -1
            Else
                Dim shutdownModeStringValue As String = CType(value, String)

                'Display empty string as the default value used by the runtime
                If shutdownModeStringValue = "" Then
                    shutdownModeStringValue = s_defaultShutdownMode.Value
                End If

                'Find the value in the combobox
                Dim foundShutdownMode As ShutdownMode = Nothing
                For Each entry As ShutdownMode In ShutdownModeComboBox.Items
                    If entry.Value.Equals(shutdownModeStringValue, StringComparison.OrdinalIgnoreCase) Then
                        foundShutdownMode = entry
                        ShutdownModeComboBox.SelectedItem = entry
                        Exit For
                    End If
                Next

                If foundShutdownMode Is Nothing Then
                    'The value wasn't found in the combobox.  Add it, but show it as an unsupported value.
                    foundShutdownMode = New ShutdownMode(shutdownModeStringValue, String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_InvalidShutdownMode, shutdownModeStringValue))
                    ShutdownModeComboBox.Items.Add(foundShutdownMode)
                    ShutdownModeComboBox.SelectedItem = foundShutdownMode
                End If

            End If

            Return True
        End Function

#End Region

#Region "User-defined property persistence"

        ''' <summary>
        ''' Override this method to return a property descriptor for user-defined properties in a page.
        ''' </summary>
        ''' <param name="PropertyName">The property to return a property descriptor for.</param>
        ''' <remarks>
        ''' This method must be overridden to handle all user-defined properties defined in a page.  The easiest way to implement
        '''   this is to return a new instance of the UserPropertyDescriptor class, which was created for that purpose.
        ''' </remarks>
        Public Overrides Function GetUserDefinedPropertyDescriptor(PropertyName As String) As PropertyDescriptor
            Select Case PropertyName
                Case PROPNAME_StartupObjectOrUri
                    Return New UserPropertyDescriptor(PropertyName, GetType(StartupObjectOrUri))

                Case PROPNAME_ShutDownMode
                    Return New UserPropertyDescriptor(PropertyName, GetType(String))

                Case PROPNAME_UseApplicationFramework
                    'Note: Need to specify Int32 instead of TriState enum because undo/redo code doesn't
                    '  handle the enum properly.
                    Return New UserPropertyDescriptor(PropertyName, GetType(Integer))

                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Takes a value from the property store, and converts it into the UI-displayable form
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        Public Overrides Function ReadUserDefinedProperty(PropertyName As String, ByRef Value As Object) As Boolean
            '
            'NOTE: We do not want to throw any exceptions from this method for our properties, because if this happens 
            '  during initialization, it will cause the property's controls to get disabled, and simply
            '  doing a refresh will not re-enable them.  Instead, we show an error value inside the control to the user.

            Select Case PropertyName
                Case PROPNAME_StartupObjectOrUri
                    Try
                        If IsStartupObjectMissing() Then
                            Value = PropertyControlData.MissingProperty
                        Else
                            Value = GetStartupObjectOrUriValueFromStorage()
                        End If
                    Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ReadUserDefinedProperty), NameOf(ApplicationPropPageVBWPF))
                        If ShouldStartupUriBeDisplayedInsteadOfStartupObject() Then
                            Value = New StartupUri("")
                        Else
                            Value = New StartupObject("", "")
                        End If
                    End Try

                Case PROPNAME_ShutDownMode
                    Try
                        Value = GetShutdownModeFromStorage()
                    Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ReadUserDefinedProperty), NameOf(ApplicationPropPageVBWPF))
                        Value = ""
                    End Try

                Case PROPNAME_UseApplicationFramework
                    Try
                        Value = GetUseApplicationFrameworkFromStorage()
                    Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ReadUserDefinedProperty), NameOf(ApplicationPropPageVBWPF))
                        Value = TriState.Disabled
                    End Try

                Case Else
                    Return False
            End Select

            Return True
        End Function

        ''' <summary>
        ''' Takes a value from the UI, converts it and writes it into the property store
        ''' </summary>
        ''' <param name="PropertyName"></param>
        ''' <param name="Value"></param>
        Public Overrides Function WriteUserDefinedProperty(PropertyName As String, Value As Object) As Boolean
            Select Case PropertyName
                Case PROPNAME_StartupObjectOrUri
                    SetStartupObjectOrUriValueIntoStorage(CType(Value, StartupObjectOrUri))

                Case PROPNAME_ShutDownMode
                    SetShutdownModeIntoStorage(CType(Value, String))

                Case PROPNAME_UseApplicationFramework
                    SetUseApplicationFrameworkIntoStorage(CType(Value, TriState))

                Case Else
                    Debug.Fail("Unexpected property name")
                    Return False
            End Select

            Return True
        End Function

#End Region

#Region "Edit XAML button"

        ''' <summary>
        ''' The user has clicked the "Edit XAML" button.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub EditXamlButton_Click(sender As Object, e As EventArgs) Handles EditXamlButton.Click
            TryShowXamlEditor(True)
        End Sub

        ''' <summary>
        ''' Attempts to show the editor for the Application.xaml file.  Shows an error message if it
        '''   fails.
        ''' </summary>
        ''' <param name="createAppDotXamlIfItDoesntExist"></param>
        Friend Sub TryShowXamlEditor(createAppDotXamlIfItDoesntExist As Boolean)
            EnterProjectCheckoutSection()
            Try
                Dim appXamlProjectItem As ProjectItem = FindApplicationXamlProjectItem(ProjectHierarchy, createAppDotXamlIfItDoesntExist)
                If appXamlProjectItem Is Nothing Then
                    ShowErrorMessage(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_CantFindAppXaml)
                    Return
                End If

                appXamlProjectItem.Open(LogicalViewID.TextView)
                If appXamlProjectItem.Document IsNot Nothing Then
                    appXamlProjectItem.Document.Activate()
                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(TryShowXamlEditor), NameOf(ApplicationPropPageVBWPF))
                ShowErrorMessage(ex)
            Finally
                LeaveProjectCheckoutSection()
            End Try
        End Sub

#End Region

#Region "View Application Events button"

        Private Sub ViewCodeButton_Click(sender As Object, e As EventArgs) Handles ViewCodeButton.Click
            TryShowApplicationEventsCode()
        End Sub

        ''' <summary>
        ''' Given a project item, finds the first dependent project item with the given extension
        ''' </summary>
        ''' <param name="projectItem"></param>
        ''' <param name="extension"></param>
        Private Shared Function FindDependentFile(projectItem As ProjectItem, extension As String) As ProjectItem
            For Each dependentItem As ProjectItem In projectItem.ProjectItems
                If dependentItem.FileNames(1) IsNot Nothing _
                        AndAlso IO.Path.GetExtension(dependentItem.Name).Equals(extension, StringComparison.OrdinalIgnoreCase) Then
                    Return dependentItem
                End If
            Next

            Return Nothing
        End Function

        Private Shared Function GetExpectedApplicationEventsFileName(appDotXamlFilename As String) As String
            Return appDotXamlFilename & VB_EXTENSION
        End Function

        Private Function CreateApplicationEventsFile(parent As ProjectItem) As ProjectItem
            'First, determine the new name by appending ".vb"
            Dim newFileName As String = GetExpectedApplicationEventsFileName(parent.Name)

            'Find the path to the template
            Dim templateFileName As String = CType(DTE.Solution, EnvDTE80.Solution2).GetProjectItemTemplate(
                "InternalWPFApplicationDefinitionUserCode.zip", "VisualBasic")

            'Add it as a dependent file
            parent.ProjectItems.AddFromTemplate(templateFileName, newFileName)

            'Now find the item that was added (for some reason, AddFromTemplate won't return this
            '  to us).
            Dim newProjectItem As ProjectItem = FindDependentFile(parent, VB_EXTENSION)
            If newProjectItem Is Nothing Then
                Throw New PropertyPageException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Unexpected)
            End If

            Return newProjectItem
        End Function

        ''' <summary>
        ''' Open the XAML editor on the application.xaml file.  If it doesn't exist, create one.
        ''' </summary>
        Private Sub TryShowApplicationEventsCode()
            EnterProjectCheckoutSection()
            Try
                'This will throw if it fails, won't return Nothing
                Dim appXamlProjectItem As ProjectItem = FindApplicationXamlProjectItem(True)

                'Look for a dependent .vb file, this should be the normal case
                Dim dependentVBItem As ProjectItem = FindDependentFile(appXamlProjectItem, VB_EXTENSION)

                If dependentVBItem Is Nothing Then
                    'If none, then also look for a file with the same name as the Application.xaml file (+ .vb) in either the
                    '  root folder or the same folder as the Application.xaml.

                    '... First, check same folder
                    Dim expectedFileName As String = GetExpectedApplicationEventsFileName(appXamlProjectItem.Name)
                    Try
                        'Will throw if not found
                        dependentVBItem = appXamlProjectItem.Collection.Item(expectedFileName)
                    Catch ex As Exception
                    End Try

                    '... Next, check root
                    If dependentVBItem Is Nothing Then
                        Try
                            'Will throw if not found
                            dependentVBItem = appXamlProjectItem.ContainingProject.ProjectItems.Item(expectedFileName)
                        Catch ex As Exception
                        End Try
                    End If
                End If

                If dependentVBItem Is Nothing Then
                    'Still not found - try to create it.
                    Try
                        dependentVBItem = CreateApplicationEventsFile(appXamlProjectItem)
                    Catch ex As Exception
                        Throw New PropertyPageException(
                            String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_CouldntCreateApplicationEventsFile_1Arg, ex.Message),
                            HelpKeywords.VBProjPropWPFApp_CouldntCreateApplicationEventsFile,
                            ex)
                    End Try
                End If

                dependentVBItem.Open(LogicalViewID.TextView)
                If dependentVBItem.Document IsNot Nothing Then
                    dependentVBItem.Document.Activate()
                End If
            Catch ex As Exception
                ShowErrorMessage(ex)
            Finally
                LeaveProjectCheckoutSection()
            End Try
        End Sub

#End Region

#Region "Error control"

        'If this is non-null, then the error control is visible
        Private WithEvents _pageErrorControl As AppDotXamlErrorControl = Nothing

        Private Sub DisplayErrorControl(message As String)
            RemoveErrorControl()

            SuspendLayout()
            overarchingTableLayoutPanel.Visible = False
            _pageErrorControl = New AppDotXamlErrorControl(message) With {
                .Dock = DockStyle.Fill
            }
            Controls.Add(_pageErrorControl)
            _pageErrorControl.BringToFront()
            _pageErrorControl.Visible = True
            ResumeLayout()
            PerformLayout()
        End Sub

        Private Sub RemoveErrorControl()
            If _pageErrorControl IsNot Nothing Then
                Controls.Remove(_pageErrorControl)
                _pageErrorControl.Dispose()
                _pageErrorControl = Nothing
            End If

            overarchingTableLayoutPanel.Visible = True
        End Sub

        Private Sub PageErrorControl_EditXamlClick() Handles _pageErrorControl.EditXamlClicked
            TryShowXamlEditor(False)
        End Sub

        Private Function TryGetAppDotXamlFilename() As String
            Try
                Dim appXaml As ProjectItem = FindApplicationXamlProjectItem(False)
                If appXaml IsNot Nothing Then
                    Return appXaml.FileNames(1)
                End If
            Catch ex As Exception
            End Try

            Return ""
        End Function

        Private Sub DisplayErrorControlIfAppXamlIsInvalid()
            Dim document As AppDotXamlDocument = Nothing
            Try
                Try
                    document = CreateAppDotXamlDocumentForApplicationDefinitionFile(False)
                Catch ex As Exception
                    'Errors here would involve problems creating the file, or perhaps it's loaded already in an incompatible
                    '  editor, etc.
                    DisplayErrorControl(ex.Message)
                    Return
                End Try

                Try
                    If document IsNot Nothing Then
                        document.VerifyAppXamlIsValidAndThrowIfNot()
                    End If
                Catch ex As Exception
                    'Problems here should be parsing errors.
                    Dim message As String =
                        String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WPFApp_ErrorControlMessage_1Arg, TryGetAppDotXamlFilename()) _
                        & vbCrLf & vbCrLf _
                        & ex.Message
                    DisplayErrorControl(message)
                End Try
            Finally
                If document IsNot Nothing Then
                    document.Dispose()
                End If
            End Try
        End Sub

#End Region

#Region "DocData changes"

        Private _docDataHasChanged As Boolean

        Private Sub ApplicationXamlDocData_DataChanged(sender As Object, e As EventArgs) Handles _applicationXamlDocData.DataChanged
            _docDataHasChanged = True
        End Sub

        Private Sub RetryPageLoad()
            If _docDataHasChanged Then
                Try
                    _docDataHasChanged = False
                    RemoveErrorControl()
                    RefreshPropertyValues()
                    DisplayErrorControlIfAppXamlIsInvalid()
                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(RetryPageLoad), NameOf(ApplicationPropPageVBWPF))
                End Try
            End If
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            MyBase.WndProc(m)

            If m.Msg = Interop.Win32Constant.WM_SETFOCUS Then
                If _docDataHasChanged Then
                    BeginInvoke(New MethodInvoker(AddressOf RetryPageLoad))
                End If
            End If
        End Sub

#End Region

#Region "XBAP projects"

        Private Function IsXBAP() As Boolean
            Dim pcd As PropertyControlData = GetPropertyControlData(PROPID_HostInBrowser)
            If pcd.IsSpecialValue Then
                'HostInBrowser property not available.  This shouldn't happen except in
                '  unit tests.
                Return False
            End If

            Return CBool(pcd.InitialValue)
        End Function

        ''' <summary>
        ''' If this is an XBAP project, some properties need to be disabled that currently can't
        '''   be disabled by the flavor mechanism (due to architectural limitations for user-defined
        '''   properties - we should change this in the future).
        ''' </summary>
        Private Sub DisableControlsForXBAPProjects()
            'Note: Once a project is an XBAP, it's always an XBAP (can't change it except
            '  by editing the project file)
            If IsXBAP() Then
                EnableControl(ShutdownModeComboBox, False)
                EnableIconComboBox(False)
                EnableControl(ApplicationTypeComboBox, False)
            End If
        End Sub

#End Region

#Region "Set the drop-down width of comboboxes with user-handled events so they'll fit their contents"

        ''' <summary>
        ''' Set the drop-down width of comboboxes with user-handled events so they'll fit their contents
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ComboBoxes_DropDown(sender As Object, e As EventArgs) Handles IconCombobox.DropDown
            SetComboBoxDropdownWidth(DirectCast(sender, ComboBox))
        End Sub

#End Region

#Region "View UAC Settings button"

        ''' <summary>
        ''' The View UAC Settings button has been clicked...
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        Private Sub ViewUACSettingsButton_Click(sender As Object, e As EventArgs) Handles ViewUACSettingsButton.Click
            ViewUACSettings()
        End Sub

#End Region

#Region "Auto-generate binding redirects checkbox"

        ''' <summary>
        ''' Shows or hides the Auto-generate Binding Redirects checkbox depending on the new target
        ''' framework.
        ''' </summary>
        Protected Overrides Sub TargetFrameworkMonikerChanged()
            ShowAutoGeneratedBindingRedirectsCheckBox(AutoGenerateBindingRedirectsCheckBox)
        End Sub

#End Region

    End Class

End Namespace

