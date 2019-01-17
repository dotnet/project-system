' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common
Imports System.IO
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.ProjectSystem.Properties
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class PackagePropPage
        Inherits PropPageUserControlBase

        Private ReadOnly _fileVersionTextBoxes As TextBox()
        Private ReadOnly _assemblyVersionTextBoxes As TextBox()

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

        'Unused for now, leaving in case needed for different implementation. Please ignore.
        'Private Function GetUnconfiguredProject(project As IVsProject) As ProjectSystem.UnconfiguredProject
        '    Dim context = CType(project, IVsBrowseObjectContext)
        '    If context Is Nothing Then
        '        Dim hierarchy = CType(project, IVsHierarchy)
        '        If hierarchy IsNot Nothing Then
        '            Dim extObject As Object = Nothing
        '            If ErrorHandler.Succeeded(hierarchy.GetProperty(CType(VSConstants.VSITEMID.Root, UInteger), CType(__VSHPROPID.VSHPROPID_ExtObject, Integer), <Out>CType(extObject, Object)</Out>)) Then
        '                Dim dteProject = CType(extObject, EnvDTE.Projects)
        '                If (dteProject IsNot Nothing) Then
        '                    'context = CType(dteProject.Object, IVsBrowseObjectContext)
        '                End If
        '            End If
        '        End If
        '    End If

        '    Return context?.UnconfiguredProject
        'End Function

        'Unused for now, leaving in case needed for different implementation. Please ignore.
        'Private Function GetUnconfiguredProject(project As EnvDTE.Project) As ProjectSystem.UnconfiguredProject
        '    Dim context = CType(project, IVsBrowseObjectContext)
        '    If context IsNot Nothing And project IsNot Nothing Then
        '        context = CType(project.Object, IVsBrowseObjectContext)
        '    End If

        '    Return context?.UnconfiguredProject
        'End Function

        Private Function GetUnconfiguredProject(hierarchy As IVsHierarchy) As ProjectSystem.UnconfiguredProject
            Dim context = CType(hierarchy, IVsBrowseObjectContext)
            If context IsNot Nothing Then
                Dim dteProject = CType(GetDTEProject(hierarchy), EnvDTE.Project)
                If (dteProject IsNot Nothing) Then
                    context = CType(dteProject.Object, IVsBrowseObjectContext)
                End If
            End If
            Return context?.UnconfiguredProject
        End Function

        Private Shared Function GetDTEProject(hierarchy As IVsHierarchy) As EnvDTE.Project
            Dim extObject As Object = Nothing
            If ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID.Root, __VSHPROPID.VSHPROPID_ExtObject, <Out>CType(extObject, Object)</Out>)) Then
                Return CType(extObject, EnvDTE.Project)
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
        ''' Property get for file or assembly version.
        ''' </summary>
        Private Function VersionGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim Version As String = Nothing

            If (control Is FileVersionLayoutPanel) Then
                ValidateAssemblyFileVersion(Version)
            Else
                'Debug.Assert(control Is AssemblyVersionLayoutPanel)
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
            If (control Is FileVersionLayoutPanel) Then
                Textboxes = _fileVersionTextBoxes
            Else
                'Debug.Assert(control Is AssemblyVersionLayoutPanel)
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

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
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
                    data = New PropertyControlData(106, "PackageLicenseExpression", PackageLicenseExpression, ControlDataFlags.None, New Control() {PackageLicenseLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(107, "PackageLicenseFile", LicenseFileNameTextBox, ControlDataFlags.None, New Control() {PackageLicenseLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(108, "PackageProjectUrl", PackageProjectUrl, ControlDataFlags.None, New Control() {PackageProjectUrlLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(109, "PackageIconUrl", PackageIconUrl, ControlDataFlags.None, New Control() {PackageIconUrlLabel})
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

        Public Function AccessTheConfiguredProject(unconfiguredProject As ProjectSystem.UnconfiguredProject) As ProjectSystem.ConfiguredProject
            Return ThreadHelper.JoinableTaskFactory.Run(Function()
                                                            Return unconfiguredProject.GetSuggestedConfiguredProjectAsync
                                                        End Function)
        End Function

        Private Sub LicenseBrowseButton_Click(sender As Object, e As EventArgs) Handles LicenseBrowseButton.Click
            Dim sInitialDirectory = ""
            Dim sFileName = ""

            Dim fileNames As ArrayList = GetFilesViaBrowse(ServiceProvider, Handle, sInitialDirectory, My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AddExistingFilesTitle,
                    CombineDialogFilters(
                        CreateDialogFilter(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Filter_Text, "txt"),
                        GetAllFilesDialogFilter()
                        ),
                        0, False, sFileName)

            If fileNames IsNot Nothing AndAlso fileNames.Count = 1 Then
                sFileName = DirectCast(fileNames(0), String)
                If File.Exists(sFileName) Then
                    LicenseFileNameTextBox.Text = Path.GetFileName(sFileName)
                    SetDirty(LicenseFileNameTextBox, True)
                    Dim unconfiguredProject = GetUnconfiguredProject(ProjectHierarchy)
                    Dim configuredProject As ProjectSystem.ConfiguredProject = AccessTheConfiguredProject(unconfiguredProject)
                    Dim projectSourceItemProvider = configuredProject.Services.ExportProvider.GetExportedValue(Of ProjectSystem.IProjectSourceItemProvider)()

                    ThreadHelper.JoinableTaskFactory.Run(Function()
                                                             Return projectSourceItemProvider.AddAsync("None", LicenseFileNameTextBox.Text, {(New KeyValuePair(Of String, String)("Pack", "True")), New KeyValuePair(Of String, String)("PackagePath", "")})
                                                         End Function)
                    Dim correctDirectory = Directory.GetParent(unconfiguredProject.FullPath).ToString
                    Dim fileWriteLocation = correctDirectory + "\" + LicenseFileNameTextBox.Text
                    File.Copy(sFileName, fileWriteLocation)
                End If
            End If

        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            ' TODO: New help keyword
            Return HelpKeywords.VBProjPropAssemblyInfo
        End Function
    End Class

End Namespace
