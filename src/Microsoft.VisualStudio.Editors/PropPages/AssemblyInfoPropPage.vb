' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel
Imports System.Windows.Forms

Imports VSLangProj80

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class AssemblyInfoPropPage
        Inherits PropPageUserControlBase

        Private ReadOnly _fileVersionTextBoxes As TextBox()
        Private ReadOnly _assemblyVersionTextBoxes As TextBox()

        'After 65535, the project system doesn't complain, and in theory any value is allowed as
        '  the string version of this, but after this value the numeric version of the file version
        '  no longer matches the string version.
        Private Const MaxFileVersionPartValue As UInteger = 65535
        Friend WithEvents NeutralLanguageComboBox As ComboBox

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

        Protected Overrides ReadOnly Property ControlData As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then

                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)
                    Dim data As PropertyControlData = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyTitle, "Title", Title, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {TitleLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyDescription, "Description", Description, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {DescriptionLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyCompany, "Company", Company, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {CompanyLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyProduct, "Product", Product, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {ProductLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyCopyright, "Copyright", Copyright, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {CopyrightLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyTrademark, "Trademark", Trademark, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {TrademarkLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyVersion, "AssemblyVersion", AssemblyVersionLayoutPanel, AddressOf VersionSet, AddressOf VersionGet, ControlDataFlags.UserHandledEvents Or ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {AssemblyVersionLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyVersion
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyFileVersion, "AssemblyFileVersion", FileVersionLayoutPanel, AddressOf VersionSet, AddressOf VersionGet, ControlDataFlags.UserHandledEvents Or ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {FileVersionLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyFileVersion
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_ComVisible, "ComVisible", ComVisibleCheckBox, ControlDataFlags.PersistedInAssemblyInfoFile)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyGuid, "AssemblyGuid", GuidTextBox, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {GuidLabel}) With {
                        .DisplayPropertyName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Property_AssemblyGuid
                    }
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_NeutralResourcesLanguage, "NeutralResourcesLanguage", NeutralLanguageComboBox, AddressOf NeutralLanguageSet, AddressOf NeutralLanguageGet, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {NeutralLanguageLabel})
                    datalist.Add(data)

                    m_ControlData = datalist.ToArray()
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <summary>
        ''' Property get for file or assembly version.
        ''' </summary>
        Private Function VersionGet(control As Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim Version As String = Nothing

            If control Is FileVersionLayoutPanel Then
                ValidateAssemblyFileVersion(Version)
            Else
                Debug.Assert(control Is AssemblyVersionLayoutPanel)
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
                Debug.Assert(control Is AssemblyVersionLayoutPanel)
                Textboxes = _assemblyVersionTextBoxes
            End If
            For index As Integer = 0 To 3
                Textboxes(index).Text = Values(index)
            Next
            Return True
        End Function

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

        ''' <summary>
        ''' Occurs when the neutral language combobox is dropped down.  Use this to
        '''   populate it with entries.
        ''' </summary>
        Private Sub NeutralLanguageComboBox_DropDown(sender As Object, e As EventArgs) Handles NeutralLanguageComboBox.DropDown
            PopulateNeutralLanguageComboBox(NeutralLanguageComboBox)
            Common.SetComboBoxDropdownWidth(NeutralLanguageComboBox)
        End Sub

        Private _neutralLanguageWasDroppedDown As Boolean

        ''' <summary>
        ''' For checking if the NeutralLanguageComboBox was DroppedDown when the escape key was pressed.
        ''' With a combination of AutoCompleteMode.SuggestAppend and AutoCompleteSource.ListItems the 
        ''' NeutralLanguageComboBox and the Assembly Info window will both handle a Escape key press and both close.
        ''' We only want the NeutralLanguageComboBox to close in that case.
        ''' </summary>
        Private Sub NeutralLanguageComboBox_PreviewKeyDown(sender As Object, e As PreviewKeyDownEventArgs) Handles NeutralLanguageComboBox.PreviewKeyDown
            If (e.KeyData And Keys.KeyCode) = Keys.Escape And NeutralLanguageComboBox.DroppedDown Then
                _neutralLanguageWasDroppedDown = True
            End If
        End Sub

        Protected Overrides Function ProcessDialogKey(keyData As Keys) As Boolean
            If _neutralLanguageWasDroppedDown Then
                _neutralLanguageWasDroppedDown = False
                Return True
            End If
            Return MyBase.ProcessDialogKey(keyData)
        End Function

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropAssemblyInfo
        End Function

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
            If controlData.FormControl Is GuidTextBox Then
                Try
                    Dim guid As New Guid(Trim(GuidTextBox.Text))
                Catch e As FormatException
                    message = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_Application_BadGuid
                    Return ValidationResult.Failed
                End Try
            ElseIf controlData.FormControl Is AssemblyVersionLayoutPanel Then
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
            End If
            Return ValidationResult.Succeeded
        End Function
    End Class

End Namespace
