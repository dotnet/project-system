' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common
Imports System.IO
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.ProjectSystem.Properties
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.ProjectSystem

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class PackagePropPage
        Inherits PropPageUserControlBase

        Private ReadOnly _fileVersionTextBoxes As TextBox()
        Private ReadOnly _assemblyVersionTextBoxes As TextBox()

        'switch to using a string dictionary to check for the previous property first
        Private ReadOnly _previousProperties As Dictionary(Of String, String) = New Dictionary(Of String, String)
        Private ReadOnly _packageLicenseFilePropName As String = "PackageLicenseFile"
        Private ReadOnly _packageIconFilePropName As String = "PackageIcon"
        Private ReadOnly _packageIconUrlPropName As String = "PackageIconUrl"
        Private _licenseUrlDetected As Boolean
        Private _newLicensePropertyDetectedAtInit As Boolean
        Private _unconfiguredProject As UnconfiguredProject
        Private _configuredProject As ConfiguredProject
        Private _projectSourceItemProvider As IProjectSourceItemProvider
        Private _allItems As IEnumerable(Of IProjectItem)

        'After 65535, the project system doesn't complain, and in theory any value is allowed as
        '  the string version of this, but after this value the numeric version of the file version
        '  no longer matches the string version.
        Private Const MaxFileVersionPartValue As UInteger = 65535

        'After 65535, the project system doesn't complain, but you get a compile error.
        Private Const MaxAssemblyVersionPartValue As UInteger = 65534

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
        End Sub

        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()
            InitializeLicensing()
            InitializeIconFile()
        End Sub

        Private Shared Function GetUnconfiguredProject(hierarchy As IVsHierarchy) As UnconfiguredProject
            Dim context = DirectCast(hierarchy, IVsBrowseObjectContext)
            If context IsNot Nothing Then
                Dim dteProject = DirectCast(GetDTEProject(hierarchy), EnvDTE.Project)
                If dteProject IsNot Nothing Then
                    context = DirectCast(dteProject.Object, IVsBrowseObjectContext)
                End If
            End If
            Return context?.UnconfiguredProject
        End Function

        Private Shared Function GetDTEProject(hierarchy As IVsHierarchy) As EnvDTE.Project
            Dim extObject As Object = Nothing
            If ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID.Root, __VSHPROPID.VSHPROPID_ExtObject, <Out>DirectCast(extObject, Object)</Out>)) Then
                Return DirectCast(extObject, EnvDTE.Project)
            End If
            Return Nothing
        End Function

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            AddChangeHandlers()

            PageRequiresScaling = False

            _fileVersionTextBoxes = New TextBox(3) {
                FileVersionMajorTextBox, FileVersionMinorTextBox, FileVersionBuildTextBox, FileVersionRevisionTextBox}
            _assemblyVersionTextBoxes = New TextBox(3) {
                AssemblyVersionMajorTextBox, AssemblyVersionMinorTextBox, AssemblyVersionBuildTextBox, AssemblyVersionRevisionTextBox}
        End Sub

        ''' <summary>
        ''' This checks the properties for licenses to determine the initial state of the licensing section.
        ''' Currently, If both are set, it will default to just enabling the <see cref="PackageLicenseExpression" />.
        ''' In the future, it might be a good idea to have a warning and only output one value, but the nuget error after packing is helpful.
        ''' <see cref="TryGetNonCommonPropertyValue" /> will get the property if it is set even if it is empty, but because the empty properties 
        ''' are ignored elsewhere, they are ignored here.
        ''' </summary>
        Private Sub InitializeLicensing()
            GetProjectsAndProvider()
            Dim PackageLicenseFileSet = TryCast(TryGetNonCommonPropertyValue(GetPropertyDescriptor(_packageLicenseFilePropName)), String)
            If Not String.IsNullOrEmpty(PackageLicenseFileSet) Then
                _newLicensePropertyDetectedAtInit = True
                LicenseFileNameTextBox.Text = FileTryGetExistingFileItemPath(PackageLicenseFileSet)
                _previousProperties(_packageLicenseFilePropName) = LicenseFileNameTextBox.Text
                SetLicenseRadioButtons(False)
            End If
            Dim PackageLicenseExpressionSet = TryCast(TryGetNonCommonPropertyValue(GetPropertyDescriptor("PackageLicenseExpression")), String)
            If Not String.IsNullOrEmpty(PackageLicenseExpressionSet) Then
                _newLicensePropertyDetectedAtInit = True
                SetLicenseRadioButtons(True)
            End If
            Dim PackageLicenseUrlSet = TryCast(TryGetNonCommonPropertyValue(GetPropertyDescriptor("PackageLicenseUrl")), String)
            If Not String.IsNullOrEmpty(PackageLicenseUrlSet) Then
                SetLicenseUrlWarningActive(True)
                _licenseUrlDetected = True
            End If
        End Sub

        Private Sub InitializeIconFile()
            GetProjectsAndProvider()
            Dim PackageIconUrlSet = TryCast(TryGetNonCommonPropertyValue(GetPropertyDescriptor(_packageIconUrlPropName)), String)
            If Not String.IsNullOrEmpty(PackageIconUrlSet) Then
                SetPackageIconUrlWarninglWarningActive(True)
            End If
            Dim PackageIconFileSet = TryCast(TryGetNonCommonPropertyValue(GetPropertyDescriptor(_packageIconFilePropName)), String)
            If Not String.IsNullOrEmpty(PackageIconFileSet) Then
                PackageIcon.Text = FileTryGetExistingFileItemPath(PackageIconFileSet)
                _previousProperties(_packageIconFilePropName) = PackageIcon.Text
            End If
        End Sub

        ''' <summary>
        ''' Property get for file or assembly version.
        ''' </summary>
        Private Function VersionGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim Version As String = Nothing

            If control Is FileVersionLayoutPanel Then
                ValidateAssemblyFileVersion(Version)
            Else
                ValidateAssemblyVersion(Version)
            End If

            value = Version
            Return True
        End Function

        ''' <summary>
        ''' Property set for either file or assembly version.
        ''' </summary>
        Private Function VersionSet(control As Control, prop As PropertyDescriptor, value As Object) As Boolean
            Dim Version As String
            Dim Values As String()

            If PropertyControlData.IsSpecialValue(value) Then
                Version = ""
            Else
                Version = Trim(CStr(value))
            End If

            If Version <> "" Then
                'Dim VersionAttr As AssemblyVersionAttribute = New AssemblyVersionAttribute(Version)
                Values = Split(Version, ".")
            End If
            'Enforce 4 values 1.2.3.4
            ReDim Preserve Values(3)

            Dim Textboxes As TextBox()
            If control Is FileVersionLayoutPanel Then
                Textboxes = _fileVersionTextBoxes
            Else
                Textboxes = _assemblyVersionTextBoxes
            End If
            For index As Integer = 0 To 3
                Textboxes(index).Text = Values(index)
            Next
            Return True
        End Function

        ''' <summary>
        ''' Validates the version numbers entered into the package version textbox from the user.
        ''' </summary>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        Private Sub ValidatePackageVersion(ByRef Version As String)
            ValidateVersion(PackageVersion, MaxFileVersionPartValue, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_PackageVersion, False, Version)
        End Sub

        ''' <summary>
        ''' Validates the version numbers entered into the assembly version textboxes from the user.
        ''' </summary>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        Private Sub ValidateAssemblyVersion(ByRef Version As String)
            ValidateVersion(_assemblyVersionTextBoxes, MaxAssemblyVersionPartValue, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyVersion, True, Version)
        End Sub

        ''' <summary>
        ''' Validates the version numbers entered into the assembly version textboxes from the user.
        ''' </summary>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        Private Sub ValidateAssemblyFileVersion(ByRef Version As String)
            ValidateVersion(_fileVersionTextBoxes, MaxFileVersionPartValue, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyFileVersion, False, Version)
        End Sub

        Private Sub AssemblyVersionLayoutPanel_TextChanged(sender As Object, e As EventArgs) Handles AssemblyVersionMajorTextBox.TextChanged, AssemblyVersionMinorTextBox.TextChanged, AssemblyVersionBuildTextBox.TextChanged, AssemblyVersionRevisionTextBox.TextChanged
            SetDirty(AssemblyVersionLayoutPanel, False)
        End Sub

        Private Sub FileVersionLayoutPanel_TextChanged(sender As Object, e As EventArgs) Handles FileVersionMajorTextBox.TextChanged, FileVersionMinorTextBox.TextChanged, FileVersionBuildTextBox.TextChanged, FileVersionRevisionTextBox.TextChanged
            SetDirty(FileVersionLayoutPanel, False)
        End Sub

        ''' <summary>
        ''' Validation properties
        ''' </summary>
        Protected Overrides Function ValidateProperty(controlData As PropertyControlData, ByRef message As String, ByRef returnControl As Control) As ValidationResult
            If controlData.FormControl Is AssemblyVersionLayoutPanel Then
                Try
                    Dim Version As String = Nothing
                    ValidateAssemblyVersion(Version)
                Catch ex As ArgumentException
                    message = ex.Message
                    returnControl = _assemblyVersionTextBoxes(0)
                    Return ValidationResult.Failed
                End Try
            ElseIf controlData.FormControl Is FileVersionLayoutPanel Then
                Try
                    Dim Version As String = Nothing
                    ValidateAssemblyFileVersion(Version)
                Catch ex As ArgumentException
                    message = ex.Message
                    returnControl = _fileVersionTextBoxes(0)
                    Return ValidationResult.Failed
                End Try
            ElseIf controlData.FormControl Is PackageVersion Then
                Try
                    Dim Version As String = Nothing
                    ValidatePackageVersion(Version)
                Catch ex As ArgumentException
                    message = ex.Message
                    returnControl = PackageVersion
                    Return ValidationResult.Failed
                End Try
            End If
            Return ValidationResult.Succeeded
        End Function

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then

                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)
                    Dim data As PropertyControlData = New PropertyControlData(100, "GeneratePackageOnBuild", GeneratePackageOnBuild, ControlDataFlags.None)
                    datalist.Add(data)
                    data = New PropertyControlData(101, "PackageId", PackageId, ControlDataFlags.None, New Control() {PackageIdLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(102, "Version", PackageVersion, ControlDataFlags.None, New Control() {PackageVersionLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(103, "Authors", Authors, ControlDataFlags.None, New Control() {AuthorsLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(104, "Description", Description, ControlDataFlags.None, New Control() {DescriptionLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(105, "Copyright", Copyright, ControlDataFlags.None, New Control() {CopyrightLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(106, "PackageLicenseExpression", PackageLicenseExpression, ControlDataFlags.None, New Control() {ExpressionLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(107, "PackageLicenseFile", Nothing, ControlDataFlags.None, Nothing)
                    datalist.Add(data)
                    data = New PropertyControlData(108, "PackageProjectUrl", PackageProjectUrl, ControlDataFlags.None, New Control() {PackageProjectUrlLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(109, "PackageIcon", Nothing, ControlDataFlags.None, Nothing)
                    datalist.Add(data)
                    data = New PropertyControlData(110, "RepositoryUrl", RepositoryUrl, ControlDataFlags.None, New Control() {RepositoryUrlLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(111, "RepositoryType", RepositoryType, ControlDataFlags.None, New Control() {RepositoryTypeLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(112, "PackageTags", PackageTags, ControlDataFlags.None, New Control() {PackageTagsLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(113, "PackageReleaseNotes", PackageReleaseNotes, ControlDataFlags.None, New Control() {PackageReleaseNotesLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(114, "PackageRequireLicenseAcceptance", PackageRequireLicenseAcceptance, ControlDataFlags.None)
                    datalist.Add(data)
                    data = New PropertyControlData(115, "PackageReleaseNotes", PackageReleaseNotes, ControlDataFlags.None, New Control() {PackageReleaseNotesLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(116, "Company", AssemblyCompany, ControlDataFlags.None, New Control() {AssemblyCompanyLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(117, "Product", Product, ControlDataFlags.None, New Control() {ProductLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(118, "NeutralLanguage", NeutralLanguageComboBox, AddressOf NeutralLanguageSet, AddressOf NeutralLanguageGet, ControlDataFlags.None, New Control() {NeutralLanguageLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(119, "AssemblyVersion", AssemblyVersionLayoutPanel, AddressOf VersionSet, AddressOf VersionGet, ControlDataFlags.None, New Control() {AssemblyVersionLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyVersion
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(120, "FileVersion", FileVersionLayoutPanel, AddressOf VersionSet, AddressOf VersionGet, ControlDataFlags.None, New Control() {AssemblyFileVersionLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyFileVersion
                    }
                    datalist.Add(data)
                    m_ControlData = datalist.ToArray()
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <summary>
        ''' Occurs when the neutral language combobox is dropped down.  Use this to
        '''   populate it with entries.
        ''' </summary>
        Private Sub NeutralLanguageComboBox_DropDown(sender As Object, e As EventArgs) Handles NeutralLanguageComboBox.DropDown
            PopulateNeutralLanguageComboBox(NeutralLanguageComboBox)
            SetComboBoxDropdownWidth(NeutralLanguageComboBox)
        End Sub

        ''' <summary>
        '''  Set the radio button selection and update both textboxes
        ''' </summary>
        ''' <param name="setLicenseExpression">Sets the radio button for LicensesExpression, if false sets to LicenseFile</param>
        Private Sub SetLicenseRadioButtons(setLicenseExpression As Boolean)
            If Not _newLicensePropertyDetectedAtInit Then
                LicenseTypeFirstSelected()
                _newLicensePropertyDetectedAtInit = False
            End If
            LicenseFileRadioButton.Checked = Not setLicenseExpression
            LicenseExpressionRadioButton.Checked = setLicenseExpression
            PackageLicenseExpression.Enabled = setLicenseExpression
            LicenseFileNameTextBox.Enabled = Not setLicenseExpression
            LicenseBrowseButton.Enabled = Not setLicenseExpression
            If setLicenseExpression AndAlso Not LicenseFileNameTextBox.Text = "" Then
                RemoveItem(LicenseFileNameTextBox.Text)
                SetCommonPropertyValue(GetPropertyDescriptor(_packageLicenseFilePropName), "")
                LicenseFileNameTextBox.Text = ""
                _previousProperties(_packageLicenseFilePropName) = LicenseFileNameTextBox.Text
            ElseIf Not setLicenseExpression AndAlso Not PackageLicenseExpression.Text = "" Then
                PackageLicenseExpression.Text = ""
                SetDirty(PackageLicenseExpression)
            End If
        End Sub

        Private Sub SetLicenseUrlWarningActive(setActive As Boolean)
            LicenseLineLabel.Visible = Not setActive
            LicenseUrlWarning.Visible = setActive
            LicenseUrlWarning.Enabled = setActive
            If setActive Then
                TableLayoutPanel.SetColumn(LicenseUrlWarning, 1)
                TableLayoutPanel.SetColumn(LicenseLineLabel, 2)
            Else
                TableLayoutPanel.SetColumn(LicenseUrlWarning, 2)
                TableLayoutPanel.SetColumn(LicenseLineLabel, 1)
                SetCommonPropertyValue(GetPropertyDescriptor("PackageLicenseUrl"), "")
            End If
        End Sub

        Private Sub LicenseTypeFirstSelected()
            'When the project has neither of the new license properties AND it has the license URL property and a new license type is selected
            If _licenseUrlDetected Then
                SetLicenseUrlWarningActive(False)
                _licenseUrlDetected = False
            End If
        End Sub

        Private Sub LicenseExpressionRadioButton_CheckedChanged(sender As Object, e As EventArgs) Handles LicenseExpressionRadioButton.CheckedChanged
            If LicenseExpressionRadioButton.Checked Then
                SetLicenseRadioButtons(True)
            End If
        End Sub

        Private Sub LicenseFileRadioButton_CheckChanged(sender As Object, e As EventArgs) Handles LicenseFileRadioButton.CheckedChanged
            If LicenseFileRadioButton.Checked Then
                SetLicenseRadioButtons(False)
            End If
        End Sub

        Private Sub PackageLicenseExpression_Changed(sender As Object, e As EventArgs) Handles PackageLicenseExpression.TextChanged
            If PackageLicenseExpression.Text IsNot "" And Not PackageLicenseExpression.Enabled Then
                'The license expression is not selected, and the text was changed while it was disabled
                'This means there was probably an undo which populated the textbox with text, so give it back control
                'I don't believe that undo will work with the license file text box because it is not user populated
                SetLicenseRadioButtons(True)
            End If
        End Sub

        'These GotFocus methods are for when the property page is first entered and neither have a value
        'It would make sense to selected the corresponding radio button when the text box recieves focus
        Private Sub PackageLicenseExpression_GotFocus(sender As Object, e As EventArgs) Handles PackageLicenseExpression.GotFocus
            SetLicenseRadioButtons(True)
        End Sub

        Private Sub LicenseFileNameTextBox_GotFocus(sender As Object, e As EventArgs) Handles LicenseFileNameTextBox.GotFocus
            SetLicenseRadioButtons(False)
        End Sub

        Private Sub LicenseFileNameTextBox_LostFocus(sender As Object, e As EventArgs) Handles LicenseFileNameTextBox.LostFocus
            If Not String.Equals(LicenseFileNameTextBox.Text, RetrievePreviousProperty(_packageLicenseFilePropName)) Then
                Dim TryConvertToAbsolutePath As String = RelativeToAbsolutePath(LicenseFileNameTextBox.Text)
                If TryConvertToAbsolutePath IsNot Nothing AndAlso String.Equals(TryConvertToAbsolutePath, LicenseFileNameTextBox.Text) Then
                    'If these are equal then the path is absolute
                    AddItemToProject(AbsoluteToRelativePath(LicenseFileNameTextBox.Text), _packageLicenseFilePropName)
                Else
                    If Not String.IsNullOrEmpty(LicenseFileNameTextBox.Text) Then
                        'If it is not definitely an absolute path, assume relative 
                        AddItemToProject(LicenseFileNameTextBox.Text, _packageLicenseFilePropName)
                    Else
                        'Value was changed to empty
                        RemoveItem(RetrievePreviousProperty(_packageLicenseFilePropName))
                        _previousProperties(_packageLicenseFilePropName) = ""
                        SetCommonPropertyValue(GetPropertyDescriptor(_packageLicenseFilePropName), "")
                    End If
                End If
            End If
        End Sub

        Private Sub PackageIconFile_LostFocus(sender As Object, e As EventArgs) Handles PackageIcon.LostFocus
            If Not String.Equals(PackageIcon.Text, RetrievePreviousProperty(_packageIconFilePropName)) Then
                Dim TryConvertToAbsolutePath As String = RelativeToAbsolutePath(PackageIcon.Text)
                If TryConvertToAbsolutePath IsNot Nothing AndAlso String.Equals(TryConvertToAbsolutePath, PackageIcon.Text) Then
                    'If these are equal then the path is absolute
                    AddItemToProject(AbsoluteToRelativePath(PackageIcon.Text), _packageIconFilePropName)
                    SetPackageIconUrlWarninglWarningActive(False)
                Else
                    If Not String.IsNullOrEmpty(PackageIcon.Text) Then
                        AddItemToProject(PackageIcon.Text, _packageIconFilePropName)
                        SetPackageIconUrlWarninglWarningActive(False)
                    Else
                        RemoveItem(RetrievePreviousProperty(_packageIconFilePropName))
                        _previousProperties(_packageIconFilePropName) = ""
                        SetCommonPropertyValue(GetPropertyDescriptor(_packageIconFilePropName), "")
                    End If
                End If
            End If
        End Sub

        'If I open a project that has the PackageIconUrl property, I should get a warning
        'that the property changed to PackageIcon
        Private Sub SetPackageIconUrlWarninglWarningActive(setActive As Boolean)
            PackageIconLineLabel.Visible = Not setActive
            PackageIconUrlWarning.Visible = setActive
            PackageIconUrlWarning.Enabled = setActive

            'Swaps the label and textbox location, as done for license warning
            If setActive Then
                TableLayoutPanel.SetColumn(PackageIconUrlWarning, 1)
                TableLayoutPanel.SetColumn(PackageIconLineLabel, 2)
            Else
                TableLayoutPanel.SetColumn(PackageIconUrlWarning, 2)
                TableLayoutPanel.SetColumn(PackageIconLineLabel, 1)
                SetCommonPropertyValue(GetPropertyDescriptor("PackageIconUrl"), "")
            End If
        End Sub

        Private Sub LicenseBrowseButton_GotFocus(sender As Object, e As EventArgs) Handles LicenseBrowseButton.GotFocus
            SetLicenseRadioButtons(False)
        End Sub

        Private Sub GetProjectsAndProvider()
            _unconfiguredProject = GetUnconfiguredProject(ProjectHierarchy)
            _configuredProject = ThreadHelper.JoinableTaskFactory.Run(Function()
                                                                          Return _unconfiguredProject.GetSuggestedConfiguredProjectAsync
                                                                      End Function)
            _projectSourceItemProvider = _configuredProject.Services.ExportProvider.GetExportedValue(Of IProjectSourceItemProvider)()
            _allItems = ThreadHelper.JoinableTaskFactory.Run(Function()
                                                                 Return _projectSourceItemProvider.GetItemsAsync()
                                                             End Function)
        End Sub

        'This is for checking if the project file changed and updating the textbox with the new value
        Protected Overrides Sub OnExternalPropertyChanged(data As PropertyControlData, source As PropertyChangeSource)
            MyBase.OnExternalPropertyChanged(data, source)
            If String.Equals(data.PropertyName, _packageLicenseFilePropName) Then
                Dim PackageLicenseFileSet = TryCast(TryGetNonCommonPropertyValue(GetPropertyDescriptor(_packageLicenseFilePropName)), String)
                'Because this will be called even when we set the value, we need to check to make sure we are not trying to set it twice
                Dim ExistingItemPath = FileTryGetExistingFileItemPath(PackageLicenseFileSet)
                If Not String.Equals(LicenseFileNameTextBox.Text, ExistingItemPath) Then
                    'If trying to resolve the existing item path fails, we should not modify anything
                    LicenseFileNameTextBox.Text = ExistingItemPath
                    _previousProperties(_packageLicenseFilePropName) = LicenseFileNameTextBox.Text
                End If
            ElseIf String.Equals(data.PropertyName, _packageIconFilePropName) Then
                Dim PackageIconFileSet = TryCast(TryGetNonCommonPropertyValue(GetPropertyDescriptor(_packageIconFilePropName)), String)
                'Because this will be called even when we set the value, we need to check to make sure we are not trying to set it twice
                Dim ExistingItemPath = FileTryGetExistingFileItemPath(PackageIconFileSet)
                If Not String.Equals(PackageIcon.Text, ExistingItemPath) Then
                    'If trying to resolve the existing item path fails, we should not modify anything
                    PackageIcon.Text = ExistingItemPath
                    _previousProperties(_packageIconFilePropName) = PackageIcon.Text
                End If
            End If

        End Sub

        Private Sub AddOrChangeItem(oldInclude As String, newInclude As String)
            Dim projectLock = _configuredProject.Services.ExportProvider.GetExportedValue(Of IProjectLockService)()
            ThreadHelper.JoinableTaskFactory.Run(
                Async Function()
                    Await projectLock.WriteLockAsync(
                        Async Function(access)
                            Await access.CheckoutAsync(_unconfiguredProject.FullPath)
                            Dim projectXML = Await access.GetProjectXmlAsync(_unconfiguredProject.FullPath)
                            If Not String.IsNullOrEmpty(oldInclude) Then
                                Dim foundItem = projectXML.ItemGroups.SelectMany(Function(x) x.Items).FirstOrDefault(Function(x) x.Include = oldInclude Or x.Include = newInclude)
                                If foundItem IsNot Nothing Then
                                    foundItem.Include = newInclude
                                Else
                                    'We couldn't find one to change so we should add it as a new item
                                    projectXML.AddItem("None", newInclude, {New KeyValuePair(Of String, String)("Pack", "True"), New KeyValuePair(Of String, String)("PackagePath", "")})
                                End If
                            Else
                                'There was no old include, so we need to add it as a new item
                                projectXML.AddItem("None", newInclude, {New KeyValuePair(Of String, String)("Pack", "True"), New KeyValuePair(Of String, String)("PackagePath", "")})
                            End If
                            Await access.ReleaseAsync()
                        End Function)
                End Function)
        End Sub

        Private Sub RemoveItem(include As String)
            Dim projectLock = _configuredProject.Services.ExportProvider.GetExportedValue(Of IProjectLockService)()
            ThreadHelper.JoinableTaskFactory.Run(
                Async Function()
                    Await projectLock.WriteLockAsync(
                        Async Function(access)
                            Await access.CheckoutAsync(_unconfiguredProject.FullPath)
                            Await _projectSourceItemProvider.RemoveAsync("None", include)
                            Await access.ReleaseAsync()
                        End Function)
                End Function)
        End Sub

        Private Function AbsoluteToRelativePath(fileName As String) As String
            Dim correctDirectory = Path.GetDirectoryName(_unconfiguredProject.FullPath)
            Return GetRelativePath(correctDirectory + "\", Path.GetFullPath(fileName))
        End Function

        Private Shared Function RelativeToAbsolutePath(relativePath As String) As String
            If relativePath Is Nothing Then
                Return Nothing
            End If
            Try
                Return Path.GetFullPath(relativePath)
            Catch
                'Hitting an exception here probably means the path is invalid
                Return Nothing
            End Try
        End Function

        Private Function RetrievePreviousProperty(propertyName As String) As String
            If _previousProperties.ContainsKey(propertyName) Then
                Return _previousProperties(propertyName)
            Else
                Return String.Empty
            End If
        End Function

        Private Sub AddItemToProject(relativeFilePath As String, propertyName As String)
            If _unconfiguredProject Is Nothing OrElse _configuredProject Is Nothing Then
                GetProjectsAndProvider()
            End If
            If relativeFilePath.IndexOfAny(Path.GetInvalidPathChars()) = -1 Then
                'The TextBox needs to have the relative path, so the property isn't linked to the TextBox. It must be set manually.
                AddOrChangeItem(RetrievePreviousProperty(propertyName), relativeFilePath)
                If String.Equals(_packageLicenseFilePropName, propertyName) Then
                    LicenseFileNameTextBox.Text = relativeFilePath
                ElseIf String.Equals(_packageIconFilePropName, propertyName) Then
                    PackageIcon.Text = relativeFilePath
                End If

                _previousProperties(propertyName) = relativeFilePath

                'If the user has changed the directory on their PackageLicenseFile, we should keep it there
                Dim PackageLicenseFileSet = TryCast(TryGetNonCommonPropertyValue(GetPropertyDescriptor(propertyName)), String)
                If Not String.IsNullOrEmpty(PackageLicenseFileSet) Then
                    Dim currentPackageLicenseFileDirectory = Path.GetDirectoryName(PackageLicenseFileSet)
                    Dim potentialFullString = Path.Combine(currentPackageLicenseFileDirectory, Path.GetFileName(relativeFilePath))
                    SetCommonPropertyValue(GetPropertyDescriptor(propertyName), potentialFullString)
                Else
                    SetCommonPropertyValue(GetPropertyDescriptor(propertyName), Path.GetFileName(relativeFilePath))
                End If
            End If
        End Sub

        'I want to be able to, given a  file name, try to find an item that matches the file name to populate the textbox
        Private Function FileTryGetExistingFileItemPath(packageFile As String) As String
            GetProjectsAndProvider()
            For Each item As IProjectItem In _allItems
                'PackageFile can have a package path as a prefix, so we need to just look at file name
                If Path.GetFileName(packageFile) = Path.GetFileName(item.EvaluatedIncludeAsRelativePath) Then
                    Return item.EvaluatedIncludeAsRelativePath
                End If
            Next
            Return Nothing
        End Function

        Private Sub LicenseBrowseButton_Click(sender As Object, e As EventArgs) Handles LicenseBrowseButton.Click
            Dim fileName = ""
            Dim initialDirectory = Path.GetFullPath(DTEProject.FullName)
            Dim fileNames As ArrayList = GetFilesViaBrowse(ServiceProvider, Handle, initialDirectory, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AddExistingFilesTitle,
                    CombineDialogFilters(
                        My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Filter_License + " (*.txt, *.md)|*.txt;*.md",
                        GetAllFilesDialogFilter()
                        ),
                        0, False, fileName)
            If fileNames IsNot Nothing AndAlso fileNames.Count = 1 Then
                fileName = DirectCast(fileNames(0), String)
                If File.Exists(fileName) Then
                    AddItemToProject(AbsoluteToRelativePath(fileName), _packageLicenseFilePropName)
                End If
            End If
        End Sub

        Private Sub IconFileBrowseButton_Click(sender As Object, e As EventArgs) Handles IconFileBrowseButton.Click
            Dim fileName = ""
            Dim initialDirectory = Path.GetFullPath(DTEProject.FullName)
            Dim fileNames As ArrayList = GetFilesViaBrowse(ServiceProvider, Handle, initialDirectory, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AddExistingFilesTitle,
                    CombineDialogFilters(
                        My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Filter_Icon + " (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                        GetAllFilesDialogFilter()
                        ),
                        0, False, fileName)
            If fileNames IsNot Nothing AndAlso fileNames.Count = 1 Then
                fileName = DirectCast(fileNames(0), String)
                If File.Exists(fileName) Then
                    AddItemToProject(AbsoluteToRelativePath(fileName), _packageIconFilePropName)
                    SetPackageIconUrlWarninglWarningActive(False)
                End If
            End If
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            ' TODO: New help keyword
            Return HelpKeywords.VBProjPropAssemblyInfo
        End Function

    End Class

End Namespace
