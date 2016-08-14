' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Globalization
Imports VSLangProj80
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common

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
        ''' Validates the version numbers entered into the given textboxes from the user.
        ''' </summary>
        ''' <param name="VersionTextboxes">The textboxes containing the version parts.</param>
        ''' <param name="MaxVersionPartValue">The maximum value allowed for each individual version part.</param>
        ''' <param name="PropertyName">The (localized) name of the property that is being validated.  Used for error messages.</param>
        ''' <param name="WildcardsAllowed">Whether or not wildcards are allowed.</param>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        Private Sub ValidateVersion(VersionTextboxes As System.Windows.Forms.TextBox(), MaxVersionPartValue As UInteger, PropertyName As String, WildcardsAllowed As Boolean, ByRef version As String)
            InternalParseVersion(VersionTextboxes(0).Text,
                VersionTextboxes(1).Text,
                VersionTextboxes(2).Text,
                VersionTextboxes(3).Text,
                PropertyName,
                MaxVersionPartValue,
                WildcardsAllowed, version)
        End Sub


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
        ''' Returns true iff the given string value is a valid numeric part of a version.  I.e., 
        '''   all digits must be numeric and the range must not be exceeded.
        ''' </summary>
        ''' <param name="Value">The value (as a string) to validate.</param>
        ''' <param name="MaxValue">The maximum value allowable for the value.</param>
        ''' <returns>True if Value is valid.</returns>
        Private Function ValidateIsNumericVersionPart(Value As String, MaxValue As UInteger) As Boolean
            Dim numericValue As UInteger

            'Must be valid unsigned integer.
            If Not UInteger.TryParse(Value, numericValue) Then
                Return False
            End If

            If numericValue > MaxValue Then
                Return False
            End If

            Return True
        End Function


        ''' <summary>
        ''' Parses a version from separated string values into a combined string value for the project system.
        ''' </summary>
        ''' <param name="Major">Major version to parse (as string).</param>
        ''' <param name="Minor">Minor version to parse (as string).</param>
        ''' <param name="Build">Build version to parse (as string).</param>
        ''' <param name="Revision">Revision version to parse (as string).</param>
        ''' <param name="PropertyName">The (localized) name of the property that is being validated.  Used for error messages.</param>
        ''' <param name="MaxVersionPartValue">Maximum value of each part of the version.</param>
        ''' <param name="WildcardsAllowed">Whether or not wildcards are allowed.</param>
        ''' <param name="FormattedVersion">[out] The resulting combined version string.</param>
        Private Sub InternalParseVersion(Major As String, Minor As String, Build As String, Revision As String, PropertyName As String, MaxVersionPartValue As UInteger, WildcardsAllowed As Boolean, ByRef FormattedVersion As String)
            Major = Trim(Major)
            Minor = Trim(Minor)
            Build = Trim(Build)
            Revision = Trim(Revision)

            Dim Fields As String() = New String() {Major, Minor, Build, Revision}
            Dim CombinedVersion As String = String.Join(".", Fields)
            Dim IsValid As Boolean = True

            'Remove extra trailing '.'s
            Do While (CombinedVersion.Length > 0) AndAlso (CombinedVersion.Chars(CombinedVersion.Length - 1) = "."c)
                CombinedVersion = CombinedVersion.Substring(0, CombinedVersion.Length - 1)
            Loop

            Fields = CombinedVersion.Split("."c)

            If Fields.Length > 4 Then
                IsValid = False 'Too many fields (the user puts periods into a cell)
            ElseIf Fields.Length = 1 AndAlso Fields(0) = "" Then
                'All fields blank - this is legal

                '... but unfortunately for Whidbey the DTE project properties don't allow empty because the 
                '  attribute doesn't allow empty, and the project properties code doesn't handle removing the
                '  attribute if it's empty.  So we have to disallow this for now (work-around is to edit the
                '  AssemblyInfo.{vb,cs,js} file manually if you really need this (usually it won't be an issue).
                IsValid = False
            Else
                'The following are the only allowed patterns:
                '  X
                '  X.X
                '  X.X.*
                '  X.X.X
                '  X.X.X.*
                '  X.X.X.X
                '
                'The fields which allow wildcards are passed in, so we only need to validate the following:


                Dim AsteriskFound As Boolean = False
                For Field As Integer = 0 To Fields.Length - 1
                    If AsteriskFound Then
                        'If we previously found an asterisk, additional fields are not allowed
                        IsValid = False
                    End If

                    If Fields(Field) = "*" Then
                        AsteriskFound = True

                        'Verify an asterisk was allowed in that field                        
                        Select Case Field
                            Case 0, 1
                                'Wildcards never allowed in this field
                                Throw New ArgumentException(SR.GetString(SR.PPG_AssemblyInfo_BadWildcard))
                            Case 2, 3
                                If Not WildcardsAllowed Then
                                    Throw New ArgumentException(SR.GetString(SR.PPG_AssemblyInfo_BadWildcard))
                                End If
                            Case Else
                                Debug.Fail("Unexpected case")
                                IsValid = False
                        End Select
                    Else
                        'If not an asterisk, it had better be numeric in the accepted range
                        If Not ValidateIsNumericVersionPart(Fields(Field), MaxVersionPartValue) Then
                            Throw New ArgumentException(SR.GetString(SR.PPG_AssemblyInfo_VersionOutOfRange_2Args, PropertyName, CStr(MaxVersionPartValue)))
                        End If
                    End If
                Next
            End If

            If IsValid Then
                FormattedVersion = CombinedVersion
            Else
                Throw New ArgumentException(SR.GetString(SR.PPG_AssemblyInfo_InvalidVersion))
            End If

        End Sub


#Region "Neutral Language Combobox"

        ''' <summary>
        ''' Populate the neutral language combobox with cultures
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub PopulateNeutralLanguageComboBox()
            'The list of cultures can't change on us, no reason to
            '  re-populate every time it's dropped down.
            If NeutralLanguageComboBox.Items.Count = 0 Then
                Using New WaitCursor
                    'First, the "None" entry
                    NeutralLanguageComboBox.Items.Add(_neutralLanguageNoneText)

                    'Followed by all possible cultures
                    Dim AllCultures As CultureInfo() = CultureInfo.GetCultures(CultureTypes.NeutralCultures Or CultureTypes.SpecificCultures Or CultureTypes.InstalledWin32Cultures)
                    For Each Culture As CultureInfo In AllCultures
                        NeutralLanguageComboBox.Items.Add(Culture.DisplayName)
                    Next
                End Using
            End If
        End Sub

        ''' <summary>
        ''' Occurs when the neutral language combobox is dropped down.  Use this to
        '''   populate it with entries.
        ''' </summary>
        Private Sub NeutralLanguageComboBox_DropDown(sender As Object, e As System.EventArgs) Handles NeutralLanguageComboBox.DropDown
            PopulateNeutralLanguageComboBox()
            Common.SetComboBoxDropdownWidth(NeutralLanguageComboBox)
        End Sub

        ''' <summary>
        ''' Converts a value for neutral language into the display string used in the
        '''   combobox.
        ''' </summary>
        Private Function NeutralLanguageSet(control As System.Windows.Forms.Control, prop As PropertyDescriptor, value As Object) As Boolean
            'Value is the abbreviation of a culture, e.g. "de-ch"
            If PropertyControlData.IsSpecialValue(value) Then
                NeutralLanguageComboBox.SelectedIndex = -1
            Else
                Dim SelectedText As String = ""
                Dim LanguageAbbrev As String = CStr(value)
                Dim Culture As CultureInfo = Nothing
                If LanguageAbbrev = "" Then
                    SelectedText = _neutralLanguageNoneText
                Else
                    Try
                        Culture = CultureInfo.GetCultureInfo(LanguageAbbrev)
                        SelectedText = Culture.DisplayName
                    Catch ex As ArgumentException
                        SelectedText = LanguageAbbrev
                    End Try
                End If

                NeutralLanguageComboBox.Text = SelectedText
            End If
            Return True
        End Function


        ''' <summary>
        ''' Convert the value displayed in the neutral language combobox into the string format to actually
        '''   be stored in the project.
        ''' </summary>
        Private Function NeutralLanguageGet(control As System.Windows.Forms.Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If NeutralLanguageComboBox.SelectedIndex < 0 Then
                'Nothing selected, return the typed-in text - we will try to accept it as is
                '  (i.e., they might have entered just a culture abbrevation, such as "de-ch", and
                '  we will accept it if it's valid)
                value = NeutralLanguageComboBox.Text
            Else
                Dim DisplayName As String = DirectCast(NeutralLanguageComboBox.SelectedItem, String)
                If DisplayName = "" OrElse DisplayName.Equals(_neutralLanguageNoneText, StringComparison.CurrentCultureIgnoreCase) Then
                    '"None"
                    value = ""
                Else
                    value = Nothing
                    For Each Culture As CultureInfo In CultureInfo.GetCultures(CultureTypes.NeutralCultures Or CultureTypes.SpecificCultures Or CultureTypes.InstalledWin32Cultures)
                        If Culture.DisplayName.Equals(DisplayName, StringComparison.CurrentCultureIgnoreCase) Then
                            value = Culture.Name
                            Exit For
                        End If
                    Next
                    If value Is Nothing Then
                        'Not recognized, return the typed-in text
                        Debug.Fail("How is the selected text not recognized as a culture when we put it into the combobox ourselves?")
                        value = NeutralLanguageComboBox.Text 'defensive
                    End If
                End If
            End If

            Return True
        End Function


#End Region

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
