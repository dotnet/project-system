' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports VSLangProj80
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class AssemblyInfoPropPage
        Inherits PropPageUserControlBase

        Private _fileVersionTextBoxes As System.Windows.Forms.TextBox()
        Private _assemblyVersionTextBoxes As System.Windows.Forms.TextBox()

        'After 65535, the project system doesn't complain, and in theory any value is allowed as
        '  the string version of this, but after this value the numeric version of the file version
        '  no longer matches the string version.
        Private Const s_maxFileVersionPartValue As UInteger = 65535
        Friend WithEvents NeutralLanguageComboBox As System.Windows.Forms.ComboBox

        'After 65535, the project system doesn't complain, but you get a compile error.
        Private Const s_maxAssemblyVersionPartValue As UInteger = 65534

        Private _neutralLanguageNoneText As String 'Text for "None" in the neutral language combobox (stored in case thread language changes)

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

            MyBase.PageRequiresScaling = False

            _fileVersionTextBoxes = New System.Windows.Forms.TextBox(3) {
                Me.FileVersionMajorTextBox, Me.FileVersionMinorTextBox, Me.FileVersionBuildTextBox, Me.FileVersionRevisionTextBox}
            _assemblyVersionTextBoxes = New System.Windows.Forms.TextBox(3) {
                Me.AssemblyVersionMajorTextBox, Me.AssemblyVersionMinorTextBox, Me.AssemblyVersionBuildTextBox, Me.AssemblyVersionRevisionTextBox}
            _neutralLanguageNoneText = SR.GetString(SR.PPG_NeutralLanguage_None)
        End Sub

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then

                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)
                    Dim data As PropertyControlData = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyTitle, "Title", Me.Title, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.TitleLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyDescription, "Description", Me.Description, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.DescriptionLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyCompany, "Company", Me.Company, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.CompanyLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyProduct, "Product", Me.Product, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.ProductLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyCopyright, "Copyright", Me.Copyright, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.CopyrightLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyTrademark, "Trademark", Me.Trademark, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.TrademarkLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyVersion, "AssemblyVersion", Me.AssemblyVersionLayoutPanel, AddressOf VersionSet, AddressOf VersionGet, ControlDataFlags.UserHandledEvents Or ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.AssemblyVersionLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyVersion)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyFileVersion, "AssemblyFileVersion", Me.FileVersionLayoutPanel, AddressOf VersionSet, AddressOf VersionGet, ControlDataFlags.UserHandledEvents Or ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.FileVersionLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyFileVersion)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_ComVisible, "ComVisible", Me.ComVisibleCheckBox, ControlDataFlags.PersistedInAssemblyInfoFile)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyGuid, "AssemblyGuid", Me.GuidTextBox, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.GuidLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyGuid)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_NeutralResourcesLanguage, "NeutralResourcesLanguage", Me.NeutralLanguageComboBox, AddressOf NeutralLanguageSet, AddressOf NeutralLanguageGet, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.NeutralLanguageLabel})
                    datalist.Add(data)

                    m_ControlData = datalist.ToArray()
                End If
                Return m_ControlData
            End Get
        End Property


        ''' <summary>
        ''' Property get for file or assembly version.
        ''' </summary>
        Private Function VersionGet(control As System.Windows.Forms.Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim Version As String = Nothing

            If (control Is Me.FileVersionLayoutPanel) Then
                ValidateAssemblyFileVersion(Version)
            Else
                Debug.Assert(control Is Me.AssemblyVersionLayoutPanel)
                ValidateAssemblyVersion(Version)
            End If

            value = Version
            Return True
        End Function


        ''' <summary>
        ''' Property set for either file or assembly version.
        ''' </summary>
        Private Function VersionSet(control As System.Windows.Forms.Control, prop As PropertyDescriptor, value As Object) As Boolean
            Dim Major As String = Nothing, Minor As String = Nothing, Build As String = Nothing, Revision As String = Nothing
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

            Dim Textboxes As System.Windows.Forms.TextBox()
            If (control Is Me.FileVersionLayoutPanel) Then
                Textboxes = Me._fileVersionTextBoxes
            Else
                Debug.Assert(control Is Me.AssemblyVersionLayoutPanel)
                Textboxes = Me._assemblyVersionTextBoxes
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
            ValidateVersion(Me._assemblyVersionTextBoxes, s_maxAssemblyVersionPartValue, SR.GetString(SR.PPG_Property_AssemblyVersion), True, Version)
        End Sub


        ''' <summary>
        ''' Validates the version numbers entered into the assembly version textboxes from the user.
        ''' </summary>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        Private Sub ValidateAssemblyFileVersion(ByRef Version As String)
            ValidateVersion(Me._fileVersionTextBoxes, s_maxFileVersionPartValue, SR.GetString(SR.PPG_Property_AssemblyFileVersion), False, Version)
        End Sub

        ''' <summary>
        ''' Occurs when the neutral language combobox is dropped down.  Use this to
        '''   populate it with entries.
        ''' </summary>
        Private Sub NeutralLanguageComboBox_DropDown(sender As Object, e As System.EventArgs) Handles NeutralLanguageComboBox.DropDown
            PopulateNeutralLanguageComboBox(NeutralLanguageComboBox)
            Common.SetComboBoxDropdownWidth(NeutralLanguageComboBox)
        End Sub


        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropAssemblyInfo
        End Function

        Private Sub AssemblyVersionLayoutPanel_TextChanged(sender As Object, e As System.EventArgs) Handles AssemblyVersionMajorTextBox.TextChanged, AssemblyVersionMinorTextBox.TextChanged, AssemblyVersionBuildTextBox.TextChanged, AssemblyVersionRevisionTextBox.TextChanged
            SetDirty(Me.AssemblyVersionLayoutPanel, False)
        End Sub

        Private Sub FileVersionLayoutPanel_TextChanged(sender As Object, e As System.EventArgs) Handles FileVersionMajorTextBox.TextChanged, FileVersionMinorTextBox.TextChanged, FileVersionBuildTextBox.TextChanged, FileVersionRevisionTextBox.TextChanged
            SetDirty(Me.FileVersionLayoutPanel, False)
        End Sub

        ''' <summary>
        ''' Validation properties
        ''' </summary>
        Protected Overrides Function ValidateProperty(controlData As PropertyControlData, ByRef message As String, ByRef returnControl As System.Windows.Forms.Control) As ValidationResult
            If controlData.FormControl Is GuidTextBox Then
                Try
                    Dim guid As New Guid(Trim(GuidTextBox.Text))
                Catch e As FormatException
                    message = SR.GetString(SR.PPG_Application_BadGuid)
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
