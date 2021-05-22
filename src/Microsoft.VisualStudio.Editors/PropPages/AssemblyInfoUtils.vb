' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Imports System.ComponentModel
Imports System.Globalization

Imports Microsoft.VisualStudio.Editors.Common

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Module AssemblyInfoUtils
        ''' <summary>
        ''' Validates the version numbers entered into the given textboxes from the user.
        ''' </summary>
        ''' <param name="VersionTextboxes">The textboxes containing the version parts.</param>
        ''' <param name="MaxVersionPartValue">The maximum value allowed for each individual version part.</param>
        ''' <param name="PropertyName">The (localized) name of the property that is being validated.  Used for error messages.</param>
        ''' <param name="WildcardsAllowed">Whether or not wildcards are allowed.</param>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        Public Sub ValidateVersion(VersionTextboxes As System.Windows.Forms.TextBox(), MaxVersionPartValue As UInteger, PropertyName As String, WildcardsAllowed As Boolean, ByRef version As String)
            Dim Major As String = Trim(VersionTextboxes(0).Text)
            Dim Minor As String = Trim(VersionTextboxes(1).Text)
            Dim Build As String = Trim(VersionTextboxes(2).Text)
            Dim Revision As String = Trim(VersionTextboxes(3).Text)

            Dim Fields As String() = New String() {Major, Minor, Build, Revision}
            Dim CombinedVersion As String = String.Join(".", Fields)
            InternalParseVersion(CombinedVersion, PropertyName, MaxVersionPartValue, WildcardsAllowed, version)
        End Sub

        ''' <summary>
        ''' Validates the semantic version number entered into the given textbox from the user.
        ''' </summary>
        ''' <param name="VersionTextBox">The textbox containing the version.</param>
        ''' <param name="MaxVersionPartValue">The maximum value allowed for each individual version part.</param>
        ''' <param name="PropertyName">The (localized) name of the property that is being validated.  Used for error messages.</param>
        ''' <param name="WildcardsAllowed">Whether or not wildcards are allowed.</param>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        Public Sub ValidateVersion(VersionTextBox As System.Windows.Forms.TextBox, MaxVersionPartValue As UInteger, PropertyName As String, WildcardsAllowed As Boolean, ByRef version As String)
            ' Validate the semantic version prefix (i.e. "1.0.0" prefix of "1.0.0-beta1")
            Dim CombinedVersion = Split(Split(VersionTextBox.Text, "+")(0), "-")(0).TrimStart()
            InternalParseVersion(CombinedVersion, PropertyName, MaxVersionPartValue, WildcardsAllowed, version)
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
        ''' <param name="CombinedVersion">Combined version to parse (as string).</param>
        ''' <param name="PropertyName">The (localized) name of the property that is being validated.  Used for error messages.</param>
        ''' <param name="MaxVersionPartValue">Maximum value of each part of the version.</param>
        ''' <param name="WildcardsAllowed">Whether or not wildcards are allowed.</param>
        ''' <param name="FormattedVersion">[out] The resulting combined version string.</param>
        Private Sub InternalParseVersion(CombinedVersion As String, PropertyName As String, MaxVersionPartValue As UInteger, WildcardsAllowed As Boolean, ByRef FormattedVersion As String)
            Dim IsValid As Boolean = True

            'Remove extra trailing '.'s
            Do While (CombinedVersion.Length > 0) AndAlso (CombinedVersion.Chars(CombinedVersion.Length - 1) = "."c)
                CombinedVersion = CombinedVersion.Substring(0, CombinedVersion.Length - 1)
            Loop

            Dim Fields As String() = CombinedVersion.Split("."c)

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
                                Throw New ArgumentException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AssemblyInfo_BadWildcard)
                            Case 2, 3
                                If Not WildcardsAllowed Then
                                    Throw New ArgumentException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AssemblyInfo_BadWildcard)
                                End If
                            Case Else
                                Debug.Fail("Unexpected case")
                                IsValid = False
                        End Select
                    Else
                        'If not an asterisk, it had better be numeric in the accepted range
                        If Not ValidateIsNumericVersionPart(Fields(Field), MaxVersionPartValue) Then
                            Throw New ArgumentException(My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AssemblyInfo_VersionOutOfRange_2Args, PropertyName, CStr(MaxVersionPartValue)))
                        End If
                    End If
                Next
            End If

            If IsValid Then
                FormattedVersion = CombinedVersion
            Else
                Throw New ArgumentException(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AssemblyInfo_InvalidVersion)
            End If
        End Sub

#Region "Neutral Language Combobox"

        Private ReadOnly s_neutralLanguageNoneText As String = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_NeutralLanguage_None 'Text for "None" in the neutral language combobox (stored in case thread language changes)

        ''' <summary>
        ''' Populate the neutral language combobox with cultures
        ''' </summary>
        Public Sub PopulateNeutralLanguageComboBox(NeutralLanguageComboBox As System.Windows.Forms.ComboBox)
            'The list of cultures can't change on us, no reason to
            '  re-populate every time it's dropped down.
            If NeutralLanguageComboBox.Items.Count = 0 Then
                Using New WaitCursor
                    'First, the "None" entry
                    NeutralLanguageComboBox.Items.Add(s_neutralLanguageNoneText)

                    'Followed by all possible cultures
                    Dim AllCultures As IEnumerable(Of CultureInfo) = CultureInfo.GetCultures(CultureTypes.NeutralCultures Or CultureTypes.SpecificCultures Or CultureTypes.InstalledWin32Cultures).
                                                                   OrderBy(Function(language) language.DisplayName)
                    For Each Culture As CultureInfo In AllCultures

                        ' Exclude Invariant as it's the same as "None" above
                        If Culture.Name.Length <> 0 Then
                            NeutralLanguageComboBox.Items.Add(Culture.DisplayName)
                        End If

                    Next
                End Using
            End If
        End Sub

        ''' <summary>
        ''' Converts a value for neutral language into the display string used in the
        '''   combobox.
        ''' </summary>
        Public Function NeutralLanguageSet(control As System.Windows.Forms.Control, prop As PropertyDescriptor, value As Object) As Boolean
            Dim NeutralLanguageComboBox = DirectCast(control, System.Windows.Forms.ComboBox)

            'Value is the abbreviation of a culture, e.g. "de-ch"
            If PropertyControlData.IsSpecialValue(value) Then
                NeutralLanguageComboBox.SelectedIndex = -1
            Else

                Dim LanguageAbbrev As String = CStr(value)

                Dim SelectedText As String
                If LanguageAbbrev = "" Then
                    SelectedText = s_neutralLanguageNoneText
                Else
                    Try
                        Dim Culture As CultureInfo = CultureInfo.GetCultureInfo(LanguageAbbrev)

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
        Public Function NeutralLanguageGet(control As System.Windows.Forms.Control, prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim NeutralLanguageComboBox = DirectCast(control, System.Windows.Forms.ComboBox)
            If NeutralLanguageComboBox.SelectedIndex < 0 Then
                'Nothing selected, return the typed-in text - we will try to accept it as is
                '  (i.e., they might have entered just a culture abbreviation, such as "de-ch", and
                '  we will accept it if it's valid)
                value = NeutralLanguageComboBox.Text
            Else
                Dim DisplayName As String = DirectCast(NeutralLanguageComboBox.SelectedItem, String)
                If DisplayName = "" OrElse DisplayName.Equals(s_neutralLanguageNoneText, StringComparison.CurrentCultureIgnoreCase) Then
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
    End Module
End Namespace
