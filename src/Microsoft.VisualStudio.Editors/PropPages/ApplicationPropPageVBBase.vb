' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.MyApplication
Imports Microsoft.VisualStudio.Shell.Interop

Imports VSLangProj80

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Contains functionality common to the VB-only application prop pages, for
    '''   both WinForms and WPF projects
    '''   See comments in proppage.vb: "Application property pages (VB and C#)"
    ''' </summary>
    Friend Class ApplicationPropPageVBBase
        Inherits ApplicationPropPageInternalBase

#Region "Common Controls"
        'We should be able to use visual inheritance, but there have been
        '  issues using it, so instead the pages have separate UI's, and 
        '  controls which have the same function between the two pages can
        '  be identified and shared here
        Protected Friend Class CommonPageControls
            Public IconCombobox As ComboBox
            Public IconLabel As Label
            Public IconPicturebox As PictureBox

            Public Sub New(IconCombobox As ComboBox, IconLabel As Label, IconPicturebox As PictureBox)
                Debug.Assert(IconCombobox IsNot Nothing)
                Me.IconCombobox = IconCombobox
                Debug.Assert(IconLabel IsNot Nothing)
                Me.IconLabel = IconLabel
                Debug.Assert(IconPicturebox IsNot Nothing)
                Me.IconPicturebox = IconPicturebox
            End Sub
        End Class
        Protected Friend CommonControls As CommonPageControls
#End Region

        Protected IconBrowseText As String

        'Property names
        Protected Const Const_ApplicationIcon As String = "ApplicationIcon"
        Protected Const Const_RootNamespace As String = "RootNamespace"
        Protected Const Const_StartupObject As String = "StartupObject"

        ' Remove this once it's in the VSCore interop assembly
        Protected Const PSFFILEID_AppManifest As Integer = -1003

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            IconBrowseText = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_BrowseText
        End Sub

#Region "Icon combobox"

        ''' <summary>
        ''' Populates the given application icon combobox with appropriate entries
        ''' </summary>
        ''' <param name="FindIconsInProject">If False, only the standard items are added (this is faster
        '''   and so may be appropriate for page initialization).</param>
        Protected Overloads Sub PopulateIconList(FindIconsInProject As Boolean)
            PopulateIconList(FindIconsInProject, CommonControls.IconCombobox, CType(GetControlValueNative(Const_ApplicationIcon), String))
        End Sub

        ''' <summary>
        ''' Populates the given application icon combobox with appropriate entries
        ''' </summary>
        ''' <param name="FindIconsInProject">If False, only the standard items are added (this is faster
        '''   and so may be appropriate for page initialization).</param>
        ''' <param name="ApplicationIconCombobox">The combobox that displays the list of icons</param>
        ''' <param name="CurrentIconValue">The current icon as a relative path.</param>
        ''' <remarks>
        ''' CurrentIconValue must be passed in because it's pulled from the control's current value, which is initially
        '''   set up by PropertyControlData), since clearing the list will clear the text value, too,
        '''   for a dropdown list.
        ''' </remarks>
        Protected Overrides Sub PopulateIconList(FindIconsInProject As Boolean, ApplicationIconCombobox As ComboBox, CurrentIconValue As String)
            MyBase.PopulateIconList(FindIconsInProject, ApplicationIconCombobox, CurrentIconValue)

            'Now add the <browse> entry
            If ProjectProperties.OutputType <> VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                ApplicationIconCombobox.Items.Add(IconBrowseText)
            End If
        End Sub

        ''' <summary>
        ''' Enables the Icon combobox (if Enable=True), but only if the associated property is supported
        ''' </summary>
        Protected Overridable Sub EnableIconComboBox(Enable As Boolean)
            EnableControl(CommonControls.IconCombobox, Enable)
            UpdateIconImage(False)
        End Sub

        ''' <summary>
        ''' Adds an icon entry to the application icon combobox in its correct place
        ''' </summary>
        ''' <param name="ApplicationIconCombobox"></param>
        Protected Overrides Sub AddIconEntryToCombobox(ApplicationIconCombobox As ComboBox, IconRelativePath As String)
            'In VB, the last entry in the combobox is the <browse> entry, so we add it in the
            '  next-to-last position
            Debug.Assert(ApplicationIconCombobox.Items.Count > 0 AndAlso IconEntryIsBrowse(CStr(ApplicationIconCombobox.Items(ApplicationIconCombobox.Items.Count - 1))))
            ApplicationIconCombobox.Items.Insert(ApplicationIconCombobox.Items.Count - 1, IconRelativePath)
        End Sub

        ''' <summary>
        ''' Update the image displayed for the currently-selected application icon
        ''' </summary>
        Protected Overloads Sub UpdateIconImage(AddToProject As Boolean)
            UpdateIconImage(CommonControls.IconCombobox, CommonControls.IconPicturebox, AddToProject)
        End Sub

        ''' <summary>
        ''' Sets the icon path for the textbox
        ''' </summary>
        Protected Function ApplicationIconSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            If PropertyControlData.IsSpecialValue(value) Then
                CommonControls.IconCombobox.SelectedIndex = -1
            Else
                Dim IconText As String = CStr(value) 'Relative path to the icon
                Debug.Assert(Not IconEntryIsSpecial(IconText))
                If IconText = "" Then
                    IconText = m_DefaultIconText
                End If

                Dim index As Integer = CommonControls.IconCombobox.Items.IndexOf(IconText)
                If index = -1 Then
                    index = CommonControls.IconCombobox.Items.Add(IconText)
                End If
                CommonControls.IconCombobox.SelectedIndex = index
                UpdateIconImage(AddToProject:=False)
            End If

            Return True
        End Function

        ''' <summary>
        ''' Handles the DropDown event for the icon combobox.  Must be called by the 
        '''   inherited page.
        ''' </summary>
        Protected Sub HandleIconComboboxDropDown(sender As Object)
            If GetPropertyControlData(Const_ApplicationIcon).IsDirty() Then
                UpdateIconImage(True)
                SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
            End If

            'When the icon combobox is dropped down, update it with all current entries from the project
            PopulateIconList(True)
            SetComboBoxDropdownWidth(CType(sender, ComboBox))
        End Sub

        ''' <summary>
        ''' If we close the drop down list, and the text is currently Browse... then we should revert
        ''' to the last set value...
        ''' </summary>
        Protected Sub HandleIconComboboxDropDownClosed(sender As Object)
            Dim IconCombobox As ComboBox = CType(sender, ComboBox)

            If IconEntryIsBrowse(TryCast(IconCombobox.SelectedItem, String)) Then
                IconCombobox.Text = LastIconImage
            End If
        End Sub

        ''' <summary>
        ''' Handles dirtying the icon property when the dropdown's value is changed
        ''' </summary>
        Protected Sub HandleIconComboboxSelectionChangeCommitted(sender As Object)
            Dim IconCombobox As ComboBox = CType(sender, ComboBox)

            If m_fInsideInit Then
                Return
            End If
            Dim ItemText As String = TryCast(IconCombobox.SelectedItem, String)
            If IconEntryIsBrowse(ItemText) Then
                BrowseForAppIcon(IconCombobox, CommonControls.IconPicturebox)
            Else
                UpdateIconImage(True)
                SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
            End If
        End Sub

        ''' <summary>
        ''' Returns true if the text is the special "Browse" text for the icon combobox
        ''' </summary>
        Protected Overrides Function IconEntryIsBrowse(EntryText As String) As Boolean
            Return EntryText IsNot Nothing AndAlso EntryText.Equals(IconBrowseText, StringComparison.OrdinalIgnoreCase)
        End Function

#End Region

#Region "Root namespace changes"

        ''' <summary>
        ''' Retrieves the current root namespace property value
        ''' </summary>
        Protected ReadOnly Property CurrentRootNamespace As String
            Get
                Return DirectCast(GetControlValue(Const_RootNamespace), String)
            End Get
        End Property

        ''' <summary>
        ''' Called after a property is changed through UI on this property page
        ''' </summary>
        Public Overrides Sub OnPropertyChanged(PropertyName As String, PropDesc As PropertyDescriptor, OldValue As Object, NewValue As Object)
            MyBase.OnPropertyChanged(PropertyName, PropDesc, OldValue, NewValue)

            If PropertyName = "RootNamespace" Then
                'The root namespace has changed.  We have changes to make to app.config files.
                OnRootNamespaceChanged(DTEProject, ServiceProvider, DirectCast(OldValue, String), DirectCast(NewValue, String))
            End If
        End Sub

        ''' <summary>
        ''' This gets called whenever the root namespace is changed via the property pages.  We have
        '''   fix-ups that have to be done in this case.
        ''' </summary>
        Protected Overridable Sub OnRootNamespaceChanged(Project As EnvDTE.Project, ServiceProvider As IServiceProvider, OldRootNamespace As String, NewRootNamespace As String)
            Try
                If Project Is Nothing Then
                    Debug.Fail("Project is nothing in OnRootNamespaceChanged")
                    Exit Sub
                End If
                If ServiceProvider Is Nothing Then
                    Debug.Fail("ServiceProvider is nothing in OnRootNamespaceChanged")
                    Exit Sub
                End If

                If OldRootNamespace Is Nothing Then
                    OldRootNamespace = ""
                End If
                If NewRootNamespace Is Nothing Then
                    NewRootNamespace = ""
                End If

                If OldRootNamespace.Equals(NewRootNamespace, StringComparison.Ordinal) Then
                    'Nothing changed - don't do anything (fixing up can be expensive)
                    Exit Sub
                End If

                Dim objectService As Shell.Design.GlobalObjectService = New Shell.Design.GlobalObjectService(ServiceProvider, Project, GetType(Serialization.CodeDomSerializer))
                If objectService IsNot Nothing Then
                    Dim objectCollection As Shell.Design.GlobalObjectCollection = objectService.GetGlobalObjects(GetType(Configuration.ApplicationSettingsBase))
                    If objectCollection IsNot Nothing Then
                        For Each gob As Shell.Design.GlobalObject In objectCollection
                            Dim sgob As SettingsGlobalObjects.SettingsFileGlobalObject = TryCast(gob, SettingsGlobalObjects.SettingsFileGlobalObject)
                            If sgob IsNot Nothing Then
                                sgob.OnRootNamespaceChanged(OldRootNamespace)
                            End If
                        Next
                    End If

                End If
            Catch ex As Exception When ReportWithoutCrash(ex, NameOf(OnRootNamespaceChanged), NameOf(ApplicationPropPageBase))
            End Try
        End Sub

#End Region

#Region "Root namespace string handling utilities"

        ''' <summary>
        ''' Given a class name, adds the current root namespace, if any.  If the class given is empty,
        '''   returns empty.
        ''' </summary>
        Protected Function AddCurrentRootNamespace(ClassName As String) As String
            Debug.Assert(Not GetPropertyControlData(Const_RootNamespace).IsMissing, "Control should not have been enabled to allow this")
            Dim RootNamespace As String = Trim(DirectCast(GetPropertyControlData(Const_RootNamespace).InitialValue, String))
            Return AddNamespace(RootNamespace, ClassName)
        End Function

        ''' <summary>
        ''' Removes the current root namespace from a class name.
        ''' </summary>
        Protected Function RemoveCurrentRootNamespace(value As String) As String
            Debug.Assert(Not GetPropertyControlData(Const_RootNamespace).IsMissing, "Control should not have been enabled to allow this")
            Dim RootNamespace As String = Trim(DirectCast(GetPropertyControlData(Const_RootNamespace).InitialValue, String))
            Return RemoveRootNamespace(value, RootNamespace)
        End Function

#End Region

#Region "Application types"

        ''' <summary>
        ''' Depending on SKU and project type, populate the icon list...
        ''' </summary>
        Protected Sub PopulateApplicationTypes(ApplicationTypeComboBox As ComboBox, applicationTypesSupportedForThisPage As List(Of ApplicationTypeInfo))
            Dim isExpressSKU As Boolean = VSProductSKU.IsExpress

            Dim objSupportedMyAppTypes As Object = Nothing
            VSErrorHandler.ThrowOnFailure(ProjectHierarchy.GetProperty(VSITEMID.ROOT, __VSHPROPID2.VSHPROPID_SupportedMyApplicationTypes, objSupportedMyAppTypes))
            Dim supportedMyAppTypes As String = TryCast(objSupportedMyAppTypes, String)
            Debug.Assert(supportedMyAppTypes IsNot Nothing, "Failed to get supported MyApplicationTypes")
            ApplicationTypeComboBox.Items.Clear()
            ApplicationTypeComboBox.Items.AddRange(applicationTypesSupportedForThisPage.FindAll(ApplicationTypeInfo.GetSemicolonSeparatedNamesPredicate(supportedMyAppTypes, isExpressSKU)).ToArray())
        End Sub

#Region "Nested class ApplicationTypeInfo"

        ''' <summary>
        ''' Encapsulate all info for an MyApplicationType
        ''' </summary>
        Protected Class ApplicationTypeInfo

            Private ReadOnly _applicationType As ApplicationTypes
            Private ReadOnly _displayName As String
            Private ReadOnly _name As String 'Non-localized name
            Private ReadOnly _supportedInExpress As Boolean

            ' Basic references need to be added to the project when the user changed the type of the application.
            '  We should maintain it to be the same as the list in the project templates.
            Private Shared ReadOnly s_references_WindowsApp As String() = New String() {"System.Deployment", "System.Drawing", "System.Windows.Forms"}
            Private Shared ReadOnly s_references_WindowsClassLib As String() = New String() {"System.Data", "System.Xml"}
            Private Shared ReadOnly s_references_CommandLineApp As String() = New String() {"System.Data", "System.Deployment", "System.Xml"}
            Private Shared ReadOnly s_references_WindowsService As String() = New String() {"System.Data", "System.Deployment", "System.ServiceProcess", "System.Xml"}
            Private Shared ReadOnly s_references_WebControl As String() = New String() {"System.Data", "System.Drawing", "System.Management", "System.Web", "System.Xml"}

            ''' <summary>
            ''' Create a new instance
            ''' </summary>
            ''' <param name="ApplicationType"></param>
            ''' <param name="DisplayName"></param>
            ''' <param name="SupportedInExpress"></param>
            Public Sub New(ApplicationType As ApplicationTypes, DisplayName As String, SupportedInExpress As Boolean)
                _applicationType = ApplicationType
                _displayName = DisplayName
                _name = [Enum].GetName(GetType(ApplicationTypes), ApplicationType)
                _supportedInExpress = SupportedInExpress
            End Sub

#Region "Trivial property get:ers"
            Public ReadOnly Property ApplicationType As ApplicationTypes
                Get
                    Return _applicationType
                End Get
            End Property

            Public ReadOnly Property DisplayName As String
                Get
                    Return _displayName
                End Get
            End Property

            Public ReadOnly Property Name As String
                Get
                    Return _name
                End Get
            End Property

            Public ReadOnly Property SupportedInExpress As Boolean
                Get
                    Return _supportedInExpress
                End Get
            End Property

#End Region

            ''' <summary>
            ''' Get the references required for each project type...
            ''' </summary>
            Public ReadOnly Property References As String()
                Get
                    Select Case ApplicationType
                        Case ApplicationTypes.WindowsApp
                            Return s_references_WindowsApp
                        Case ApplicationTypes.WindowsClassLib
                            Return s_references_WindowsClassLib
                        Case ApplicationTypes.CommandLineApp
                            Return s_references_CommandLineApp
                        Case ApplicationTypes.WindowsService
                            Return s_references_WindowsService
                        Case ApplicationTypes.WebControl
                            Return s_references_WebControl
                        Case Else
                            Debug.Fail("Unknown application type")
                            Return Array.Empty(Of String)
                    End Select
                End Get
            End Property

            ''' <summary>
            ''' Override ToString to get the "right" look in the ApplicationType combobox
            ''' </summary>
            ''' <remarks>The current (localized) display name for the application type</remarks>
            Public Overrides Function ToString() As String
                Return DisplayName
            End Function

#Region "Match predicates to get properties - used in IList(of ApplicationTypeInfo).Find() and FindAll"
#Region "Predicate getters"

            ''' <summary>
            ''' Match against a given semicolon separated list of (non-localized) names and optionally if the application type must be supported
            ''' by Express SKUs
            ''' </summary>
            Public Shared Function GetSemicolonSeparatedNamesPredicate(SemicolonSeparatedNames As String, MustBeSupportedByExpressSKUs As Boolean) As Predicate(Of ApplicationTypeInfo)
                Dim pred As New SemicolonSeparatedNamesPredicate(SemicolonSeparatedNames, MustBeSupportedByExpressSKUs)
                Return AddressOf pred.Compare
            End Function

            ''' <summary>
            ''' Matches application types
            ''' </summary>
            Public Shared Function ApplicationTypePredicate(AppType As ApplicationTypes) As Predicate(Of ApplicationTypeInfo)
                Dim pred As New AppTypePredicate(AppType)
                Return AddressOf pred.Compare
            End Function
#End Region

#Region "Simple predicate implementations"

            ''' <summary>
            ''' Filter out only supported application types based on a semi-colon separated list of (non-localized) names and an optional 
            ''' flag to only allow apptypes supported by Express SKUs
            ''' </summary>
            Private Class SemicolonSeparatedNamesPredicate

                ' Non-localized name to match
                Private ReadOnly _names As New Dictionary(Of String, Boolean)
                Private ReadOnly _mustBeSupportedInExpressSKUs As Boolean

                ''' <summary>
                ''' Create a new filter predicate
                ''' </summary>
                ''' <param name="SemicolonSeparatedNames"></param>
                ''' <param name="MustBeSupportedInExpressSKUs">If true, only application types supported by express SKUs will be returned</param>
                Friend Sub New(SemicolonSeparatedNames As String, MustBeSupportedInExpressSKUs As Boolean)
                    _mustBeSupportedInExpressSKUs = MustBeSupportedInExpressSKUs
                    For Each AppType As String In SemicolonSeparatedNames.Split(";"c)
                        _names(AppType) = True
                    Next
                End Sub

                ''' <summary>
                ''' Does the given item satisfy the requireents?
                ''' </summary>
                Friend Function Compare(Item As ApplicationTypeInfo) As Boolean
                    If Not _mustBeSupportedInExpressSKUs OrElse Item.SupportedInExpress Then
                        Return _names.ContainsKey(Item.Name)
                    Else
                        Return False
                    End If
                End Function
            End Class

            ''' <summary>
            ''' Does a given ApplicationTypeInfo instance have the same application type as I was constructed with?
            ''' </summary>
            Private Class AppTypePredicate
                Private ReadOnly _appTypeToFind As ApplicationTypes

                Friend Sub New(appType As ApplicationTypes)
                    _appTypeToFind = appType
                End Sub

                Friend Function Compare(Item As ApplicationTypeInfo) As Boolean
                    Return _appTypeToFind = Item.ApplicationType
                End Function
            End Class
#End Region
#End Region

        End Class

#End Region

#End Region

#Region "View UAC Settings"
        Protected Const ApplicationManifest_Default As String = "DefaultManifest"
        Protected Const ApplicationManifest_NoManifest As String = "NoManifest"

        ''' <summary>
        ''' Enables or disables the UAC Settings button, depending on whether we're in a class
        '''   library or not.
        ''' </summary>
        Protected Function UACSettingsButtonSupported(outputType As VSLangProj.prjOutputType) As Boolean
            If Not ApplicationManifestSupported() Then
                ' The flavor has requested the "View UAC Settings" button to be disabled
                Return False
            End If

            If outputType = VSLangProj.prjOutputType.prjOutputTypeLibrary Then
                'Doesn't make sense in class libraries
                Return False
            End If

            Return True
        End Function

        Protected Function UACSettingsButtonSupported(appType As ApplicationTypes) As Boolean
            Return UACSettingsButtonSupported(CType(MyApplicationProperties.OutputTypeFromApplicationType(appType), VSLangProj.prjOutputType))
        End Function

        Private Shared Function AddApplicationManifestToProjectFromTemplate(SpecialProjectItems As IVsProjectSpecialFiles, ByRef ItemId As UInteger, ByRef MkDocument As String) As Boolean
            ' This will trigger the application manifest item template and set the ApplicationManifest property
            ' Note: IVSProjectSpecialFiles is responsible for the SCC implications
            ' of adding the app.manifest file here. 
            Dim hr As Integer = SpecialProjectItems.GetFile(PSFFILEID_AppManifest, CUInt(__PSFFLAGS.PSFF_FullPath Or __PSFFLAGS.PSFF_CreateIfNotExist), ItemId, MkDocument)
            System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr)
            If ItemId = VSITEMID.NIL OrElse MkDocument Is Nothing Then
                Debug.Assert(MkDocument IsNot Nothing, "Why is filename returned as nothing?")
                Return False
            End If
            Return True
        End Function

        ''' <summary>
        ''' View the manifest for Vista UAC settings, creating a new file from template if needed
        ''' </summary> 
        Protected Sub ViewUACSettings()

            Using New WaitCursor
                Try
                    ' get the current project property value
                    Dim ApplicationManifest As String = CType(DTEProject.Properties.Item("ApplicationManifest").Value, String)

                    ' Due to the way that MSBuild can either use the user-specified NoWin32Manifest property from a project
                    '   file OR construct it when external manifest generation is on (ClickOnce or reg-free COM),
                    '   we can't differentiate between when the user has actually specified NoWin32Manifest manually or the
                    '   build system has cooked up the value. Therefore, we simply stomp on that property value
                    '   by pretending the default manifest has been specified and continue along the same code-path which will
                    '   add a new manifest to the project file if we find that the property value is the special "no manifest"
                    '   value.
                    '
                    If String.Equals(ApplicationManifest, ApplicationManifest_NoManifest, StringComparison.Ordinal) Then
                        ApplicationManifest = ApplicationManifest_Default
                    End If

                    Debug.Assert(ProjectHierarchy IsNot Nothing, "Hierarchy is nothing...")
                    Dim SpecialProjectItems As IVsProjectSpecialFiles = TryCast(ProjectHierarchy, IVsProjectSpecialFiles)
                    If SpecialProjectItems Is Nothing Then
                        Debug.Fail("Failed to get IVsProjectSpecialFiles from project")
                        Throw New InvalidOperationException()
                    End If

                    Dim MkDocument As String = Nothing
                    Dim ItemId As UInteger

                    ' if the string value is the special "default manifest" string value, then the user's project does not have
                    '   a manifest in it, and we should try to add one.
                    ' otherwise, the project already has one and we should use that document.
                    '
                    If String.Equals(ApplicationManifest, ApplicationManifest_Default, StringComparison.Ordinal) Then
                        ' this will call ProjectSpecialFiles to find an app.manifest, creating one if it does not already
                        '   exist. note that "find" may actually find one without setting our ApplicationManifest property.
                        '
                        If Not AddApplicationManifestToProjectFromTemplate(SpecialProjectItems, ItemId, MkDocument) Then
                            Return
                        End If

                        ' get the current project property value again because sometimes, SpecialProjectItems will find
                        '   app.manifest in the project (usually in upgraded VS2005 projects where this property didn't
                        '   exist), and if it finds a file with the right name, it doesn't actually set the property
                        '   value to point to the file.
                        '
                        ApplicationManifest = CType(DTEProject.Properties.Item("ApplicationManifest").Value, String)
                    Else
                        MkDocument = ApplicationManifest
                    End If

                    If String.Equals(ApplicationManifest, ApplicationManifest_Default, StringComparison.Ordinal) Then

                        ' If the project was upgraded from Whidbey and app.manifest exists in the project already then
                        ' ApplicationManifest would not be set by default and thus remain "DefaultManifest".
                        ' However since the file exists in the project the item template AppManifestInternal would not be triggered.
                        ' Thus we would miss setting both ApplicationManifest property and setting the Build Action to "None".
                        ' The fix here will set the Application Manifest property.
                        ' It is ported from wizard\vsdesigner\designer\microsoft\vsdesigner\ProjectWizard\AppManifestTemplateWizard.cs
                        Dim appManifestPath As String = Nothing

                        If (Not String.IsNullOrEmpty(MkDocument)) AndAlso IO.Path.IsPathRooted(MkDocument) Then

                            Dim fullPathProperty As EnvDTE.Property = DTEProject.Properties.Item("FullPath")
                            If fullPathProperty IsNot Nothing AndAlso fullPathProperty.Value IsNot Nothing Then

                                Dim projectFullPath As String = CType(fullPathProperty.Value, String)
                                If Not String.IsNullOrEmpty(projectFullPath) Then

                                    If MkDocument.StartsWith(projectFullPath, True, Globalization.CultureInfo.InvariantCulture) Then
                                        ' we really only expect app.manifest to be added somewhere under the root of the project, so the project's full-path
                                        ' should always be in the first part of the app.manifest file-path. However, if it's not, we don't want to suddenly
                                        ' strip some random part of the app.manifest full-path out, so we first check to see that the two paths overlap
                                        ' before assiging the property value.
                                        '
                                        appManifestPath = MkDocument.Substring(projectFullPath.Length)
                                    Else
                                        ' if app.manifest is a linked file in a location where ProjectSpecialFiles finds it, the path
                                        '   could be fully-qualified but not actually be under the current project. [Note that this is
                                        '   different than the code ported from the AppManifestTemplateWizard because this code-path
                                        '   has to handle cases where a linked file may already exist that the template-wizard does
                                        '   not expect].
                                        '
                                        appManifestPath = MkDocument
                                    End If

                                    ' set the ApplicationManifest property to point to the file we now have a path for.
                                    '
                                    DTEProject.Properties.Item("ApplicationManifest").Value = appManifestPath
                                End If
                            End If
                            'Else
                            '    appManifestRelativePath = MkDocument
                        End If

                        ' The following code clears the Whidbey Build Action.  However we don't do this currently
                        ' because we allow our project to fall back to Whidbey Build Action by default anyway.
                        'If appManifestRelativePath IsNot Nothing Then
                        '    Dim Item As EnvDTE.ProjectItem = Nothing
                        '    Dim Items As EnvDTE.ProjectItems = DTEProject.ProjectItems
                        '    For Each Name As String In appManifestRelativePath.Split("\"c)
                        '        If Items Is Nothing Then
                        '            Exit For
                        '        End If
                        '        Item = Items.Item(Name)
                        '        Items = Item.ProjectItems
                        '    Next
                        '    If Item IsNot Nothing Then
                        '        Item.Properties.Item("BuildAction").Value = 0   ' Clear any build action from upgraded Whidbey project.
                        '    End If
                        'End If
                    End If

                    Dim LogicalViewGuid As Guid
                    Dim WindowFrame As IVsWindowFrame = Nothing

                    Dim VsUIHierarchy As IVsUIHierarchy = Nothing
                    Dim pDocInProj As Integer

                    Dim VsUIShellOpenDocument As IVsUIShellOpenDocument = CType(GetServiceFromPropertyPageSite(GetType(IVsUIShellOpenDocument)), IVsUIShellOpenDocument)
                    Dim OleServiceProvider As OLE.Interop.IServiceProvider = CType(GetServiceFromPropertyPageSite(GetType(OLE.Interop.IServiceProvider)), OLE.Interop.IServiceProvider)
                    Debug.Assert(VsUIShellOpenDocument IsNot Nothing, "Unable to get IVsUIShellOpenDocument")

                    If Not IO.Path.IsPathRooted(MkDocument) Then
                        Dim fullPathProperty As EnvDTE.Property = DTEProject.Properties.Item("FullPath")
                        Dim projectFullPath As String = CType(fullPathProperty.Value, String)
                        If Not String.IsNullOrEmpty(projectFullPath) Then
                            MkDocument = IO.Path.Combine(projectFullPath, MkDocument)
                        End If
                    End If

                    'the file may not exist on disk if it was deleted out from under the project system
                    'in that case, opening the file would fail. If this happens we recreate the file from the template.
                    Dim docFileInfo As New IO.FileInfo(MkDocument)

                    If Not docFileInfo.Exists Then
                        AddApplicationManifestToProjectFromTemplate(SpecialProjectItems, ItemId, MkDocument)
                    End If

                    VSErrorHandler.ThrowOnFailure(VsUIShellOpenDocument.IsDocumentInAProject(MkDocument, VsUIHierarchy, ItemId, OleServiceProvider, pDocInProj))
                    Debug.Assert(VsUIShellOpenDocument IsNot Nothing, "Unable to get IVsUIShellOpenDocument")

                    VSErrorHandler.ThrowOnFailure(VsUIShellOpenDocument.OpenDocumentViaProject(MkDocument, (LogicalViewGuid), OleServiceProvider, VsUIHierarchy, ItemId, WindowFrame))

                    'If the file was opened in an intrinsic editor (as opposed to an external editor), then WindowFrame will 
                    '  have a non-Nothing value.
                    If WindowFrame IsNot Nothing Then
                        'Okay, it was an intrinsic editor.  We are responsible for making sure the editor is visible.
                        VSErrorHandler.ThrowOnFailure(WindowFrame.Show())
                    End If

                Catch ex As Exception When ReportWithoutCrash(ex, NameOf(ViewUACSettings), NameOf(ApplicationPropPageVBBase))
                    If Not ProjectReloadedDuringCheckout Then
                        ShowErrorMessage(ex)
                    End If
                End Try
            End Using
        End Sub
#End Region

    End Class

End Namespace
