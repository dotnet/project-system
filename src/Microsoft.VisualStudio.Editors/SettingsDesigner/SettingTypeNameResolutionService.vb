' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' Map between display names (the name that appear in the UI), the type name persisted in the .settings file
    ''' and the corresponding .NET FX type name.
    ''' 
    ''' There are three sets of names:
    ''' 1) Display names. This is the name that appears in the settings designer UI. This includes the language specific 
    '''    type names (i.e. String instead of System.String) and the localized name for the "virtual" types (i.e. connection string)
    ''' 2) Names as they are persisted in the .settings file. This is usually the same as the .NET FX type name for the setting
    '''    except in the case of connection strings and web service URLs, which use the culture invariant representation for these
    '''    types
    ''' 3) The .NET FX type name. This is what we present to the single file generator and the settings global object.
    ''' </summary>
    Friend Class SettingTypeNameResolutionService

        Private Enum Language
            UNKNOWN = -1
            CSharp = 0
            VB = 1
        End Enum
#Region "Private fields"

        ' Map from language specific names to the corresponding .NET FX type name
        Private ReadOnly _languageSpecificToFxTypeName As Dictionary(Of String, String)

        ' Map from .NET FX type names to language specific type names
        Private ReadOnly _fxTypeNameToLanguageSpecific As Dictionary(Of String, String)

        ' Is the current language case-sensitive?
        Private ReadOnly _caseSensitive As Boolean

#End Region

        Public Sub New(languageGuid As String, Optional caseSensitive As Boolean = True)
            Dim language As Language
            Select Case languageGuid
                Case EnvDTE.CodeModelLanguageConstants.vsCMLanguageCSharp
                    language = Language.CSharp
                Case EnvDTE.CodeModelLanguageConstants.vsCMLanguageVB
                    language = Language.VB
                Case Else
                    language = Language.UNKNOWN
            End Select

            _caseSensitive = caseSensitive

            Dim comparer As IEqualityComparer(Of String)
            If caseSensitive Then
                comparer = StringComparer.Ordinal
            Else
                comparer = StringComparer.OrdinalIgnoreCase
            End If

            _languageSpecificToFxTypeName = New Dictionary(Of String, String)(16, comparer)
            _fxTypeNameToLanguageSpecific = New Dictionary(Of String, String)(16, comparer)
            If language <> Language.UNKNOWN Then
                ' add language specific type names for C# and VB respectively
                AddEntry(GetType(Boolean).FullName, New String() {"bool", "Boolean"}(language))
                AddEntry(GetType(Byte).FullName, New String() {"byte", "Byte"}(language))
                AddEntry(GetType(Char).FullName, New String() {"char", "Char"}(language))
                AddEntry(GetType(Decimal).FullName, New String() {"decimal", "Decimal"}(language))
                AddEntry(GetType(Double).FullName, New String() {"double", "Double"}(language))
                AddEntry(GetType(Short).FullName, New String() {"short", "Short"}(language))
                AddEntry(GetType(Integer).FullName, New String() {"int", "Integer"}(language))
                AddEntry(GetType(Long).FullName, New String() {"long", "Long"}(language))
                AddEntry(GetType(SByte).FullName, New String() {"sbyte", "SByte"}(language))
                AddEntry(GetType(Single).FullName, New String() {"float", "Single"}(language))
                AddEntry(GetType(UShort).FullName, New String() {"ushort", "UShort"}(language))
                AddEntry(GetType(UInteger).FullName, New String() {"uint", "UInteger"}(language))
                AddEntry(GetType(ULong).FullName, New String() {"ulong", "ULong"}(language))
                AddEntry(GetType(String).FullName, New String() {"string", "String"}(language))
                AddEntry(GetType(Date).FullName, New String() {Nothing, "Date"}(language))
            End If
        End Sub

        ''' <summary>
        ''' Is the current language case sensitive?
        ''' </summary>
        Public ReadOnly Property IsCaseSensitive As Boolean
            Get
                Return _caseSensitive
            End Get
        End Property
        ''' <summary>
        ''' Given the text persisted in the .settings file, return the string that we'll 
        ''' show in the UI
        ''' </summary>
        ''' <param name="typeName"></param>
        Public Function PersistedSettingTypeNameToTypeDisplayName(typeName As String) As String
            Dim displayName As String = Nothing
            If String.Equals(typeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                Return DisplayTypeNameConnectionString
            ElseIf String.Equals(typeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                Return DisplayTypeNameWebReference
            ElseIf _fxTypeNameToLanguageSpecific.TryGetValue(typeName, displayName) Then
                Return displayName
            End If
            Return typeName
        End Function

        ''' <summary>
        ''' Given the string we persisted in the .settings file, return the .NET FX type name
        ''' that we'll use when building the CodeDom tree
        ''' </summary>
        ''' <param name="typeName"></param>
        Public Shared Function PersistedSettingTypeNameToFxTypeName(typeName As String) As String
            If String.Equals(typeName, SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString, StringComparison.Ordinal) Then
                Return GetType(String).FullName
            ElseIf String.Equals(typeName, SettingsSerializer.CultureInvariantVirtualTypeNameWebReference, StringComparison.Ordinal) Then
                Return GetType(String).FullName
            Else
                Return typeName
            End If
        End Function

        ''' <summary>
        ''' Given the text showing in the UI, return the string that we'll actually persist in the
        ''' .settings file
        ''' </summary>
        ''' <param name="typeName"></param>
        Public Function TypeDisplayNameToPersistedSettingTypeName(typeName As String) As String
            Dim persistedTypeName As String = Nothing
            If String.Equals(typeName, DisplayTypeNameConnectionString, StringComparison.Ordinal) Then
                Return SettingsSerializer.CultureInvariantVirtualTypeNameConnectionString
            ElseIf String.Equals(typeName, DisplayTypeNameWebReference, StringComparison.Ordinal) Then
                Return SettingsSerializer.CultureInvariantVirtualTypeNameWebReference
            ElseIf _languageSpecificToFxTypeName.TryGetValue(typeName, persistedTypeName) Then
                Return persistedTypeName
            Else
                Return typeName
            End If
        End Function

#Region "Localized name of virtual types"
        Private Shared ReadOnly Property DisplayTypeNameConnectionString As String
            Get
                Static VirtualTypeNameConnectionString As String = "(" & My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_ConnectionStringType & ")"
                Return VirtualTypeNameConnectionString
            End Get
        End Property

        Private Shared ReadOnly Property DisplayTypeNameWebReference As String
            Get
                Static VirtualTypeNameWebReference As String = "(" & My.Resources.Microsoft_VisualStudio_Editors_Designer.SD_ComboBoxItem_WebReferenceType & ")"
                Return VirtualTypeNameWebReference
            End Get
        End Property
#End Region

#Region "Private implementation details"

        Private Sub AddEntry(FxName As String, languageSpecificName As String)
            If languageSpecificName <> "" Then
                _languageSpecificToFxTypeName(languageSpecificName) = FxName
                _fxTypeNameToLanguageSpecific(FxName) = languageSpecificName
            End If
        End Sub
#End Region

    End Class
End Namespace
