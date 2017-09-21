' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Represents a csharp language version and can be placed into a control
    ''' </summary>
    Friend Class CSharpLanguageVersion

        Private Shared ReadOnly _resources As ComponentModel.ComponentResourceManager = New ComponentModel.ComponentResourceManager(GetType(AdvBuildSettingsPropPage))

        Private Const s_languageVersion_Default As String = "default"
        Private Const s_languageVersion_Latest As String = "latest"
        Private Const s_languageVersion_ISO1 As String = "ISO-1"
        Private Const s_languageVersion_ISO2 As String = "ISO-2"
        Private Const s_languageVersion_3 As String = "3"
        Private Const s_languageVersion_DisplayNameFor3 As String = "C# 3"
        Private Const s_languageVersion_4 As String = "4"
        Private Const s_languageVersion_DisplayNameFor4 As String = "C# 4"
        Private Const s_languageVersion_5 As String = "5"
        Private Const s_languageVersion_DisplayNameFor5 As String = "C# 5"
        Private Const s_languageVersion_6 As String = "6"
        Private Const s_languageVersion_DisplayNameFor6 As String = "C# 6"
        Private Const s_languageVersion_7 As String = "7"
        Private Const s_languageVersion_DisplayNameFor7 As String = "C# 7.0"
        Private Const s_languageVersion_7_1 As String = "7.1"
        Private Const s_languageVersion_DisplayNameFor7_1 As String = "C# 7.1"
        Private Const s_languageVersion_7_2 As String = "7.2"
        Private Const s_languageVersion_DisplayNameFor7_2 As String = "C# 7.2"

        ''' <summary>
        ''' Stores the property value corresponding to the language version
        ''' </summary>
        Private _value As String

        ''' <summary>
        ''' Stores the display name of the language version
        ''' </summary>
        Private _displayName As String

        ''' <summary>
        ''' Constructor that uses the provided value and display name
        ''' </summary>
        Private Sub New(value As String, displayName As String)

            _value = value
            _displayName = displayName

        End Sub

        ''' <summary>
        ''' Gets the value of the language version
        ''' </summary>
        Public ReadOnly Property Value() As String
            Get
                Return _value
            End Get
        End Property

        ''' <summary>
        ''' Use the display name for the string display
        ''' </summary>
        Public Overrides Function ToString() As String
            Return _displayName
        End Function

        ''' <summary>
        ''' Return the 'default' language version object
        ''' </summary>
        Public Shared ReadOnly Property [Default]() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_Default, _resources.GetString("CSharpLanguageVerison.Default"))
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the 'latest' language version object
        ''' </summary>
        Public Shared ReadOnly Property Latest() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_Latest, _resources.GetString("CSharpLanguageVerison.Latest"))
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the ISO-1 language version object
        ''' </summary>
        Public Shared ReadOnly Property ISO1() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_ISO1, s_languageVersion_ISO1)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the ISO-2 language version object
        ''' </summary>
        Public Shared ReadOnly Property ISO2() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_ISO2, s_languageVersion_ISO2)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 3.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version3() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_3, s_languageVersion_DisplayNameFor3)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 4.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version4() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_4, s_languageVersion_DisplayNameFor4)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 5.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version5() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_5, s_languageVersion_DisplayNameFor5)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 6.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version6() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_6, s_languageVersion_DisplayNameFor6)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 7.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version7() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_7, s_languageVersion_DisplayNameFor7)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 7.1 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version7_1() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_7_1, s_languageVersion_DisplayNameFor7_1)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 7.2 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version7_2() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(s_languageVersion_7_2, s_languageVersion_DisplayNameFor7_2)
                Return value
            End Get
        End Property
    End Class

End Namespace
