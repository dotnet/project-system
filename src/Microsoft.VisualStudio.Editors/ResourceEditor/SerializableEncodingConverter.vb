' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.ComponentModel
Imports System.Globalization
Imports System.Text

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' A type converter for SerializableEncoding.  Associating this class with the Encoding property
    '''   on Resource allows the Encoding property to have a dropdown list that we control and fill with
    '''   suggested encoding values.
    ''' </summary>
    Friend NotInheritable Class SerializableEncodingConverter
        Inherits TypeConverter

        'Our cached set of standard SerializableEncoding values
        Private _standardValuesCache As StandardValuesCollection

        ''' <summary>
        ''' Gets a value indicating whether this converter can convert an object in the given source 
        '''   type to a SerializableEncoding object using the specified context.
        ''' </summary>
        Public Overrides Function CanConvertFrom(Context As ITypeDescriptorContext, SourceType As Type) As Boolean
            If SourceType.Equals(GetType(String)) Then
                Return True
            End If

            Return MyBase.CanConvertFrom(Context, SourceType)
        End Function

        ''' <summary>
        ''' Converts the specified value object to a SerializableEncoding object.
        ''' </summary>
        Public Overrides Function ConvertFrom(Context As ITypeDescriptorContext, Culture As CultureInfo, Value As Object) As Object
            Dim EncodingName = TryCast(Value, String)
            If EncodingName IsNot Nothing Then
                'Try empty (indicates an Encoding of Nothing [default] - won't be written to the resx)
                If EncodingName = "" Then
                    Return New SerializableEncoding(Nothing)
                End If

                'Try as a codepage (in case they try typing in a codepage manually)
                If IsNumeric(Value) Then
                    Return New SerializableEncoding(Encoding.GetEncoding(CInt(Value)))
                End If

                'Otherwise, try as a web name
                Return New SerializableEncoding(Encoding.GetEncoding(EncodingName))
            End If

            Return MyBase.ConvertFrom(Context, Culture, Value)
        End Function

        ''' <summary>
        ''' Converts the given value object to the specified destination type.
        ''' </summary>
        Public Overrides Function ConvertTo(Context As ITypeDescriptorContext, Culture As CultureInfo, Value As Object, DestinationType As Type) As Object
            Requires.NotNull(DestinationType, NameOf(DestinationType))

            If DestinationType.Equals(GetType(String)) AndAlso TypeOf Value Is SerializableEncoding Then
                Dim SerializableEncoding As SerializableEncoding = DirectCast(Value, SerializableEncoding)

                'Here we return the localized encoding name.  That's what actually shows up
                '  in the properties window.
                Return SerializableEncoding.DisplayName()
            End If

            Return MyBase.ConvertTo(Context, Culture, Value, DestinationType)
        End Function

        ''' <summary>
        ''' Gets a value indicating whether this object supports a standard set of values that 
        '''   can be picked from a list using the specified context.
        ''' </summary>
        Public Overrides Function GetStandardValuesSupported(Context As ITypeDescriptorContext) As Boolean
            Return True
        End Function

        ''' <summary>
        ''' Indicates whether the standard values that we return are the only allowable values.
        ''' </summary>
        ''' <param name="Context"></param>
        ''' <remarks>
        ''' We return false so that the user is allows to type in a value manually (in particular,
        '''    a codepage value).
        ''' </remarks>
        Public Overrides Function GetStandardValuesExclusive(Context As ITypeDescriptorContext) As Boolean
            Return False
        End Function

        ''' <summary>
        ''' Gets a collection of standard values collection for a System.Globalization.CultureInfo
        '''   object using the specified context.
        ''' </summary>
        Public Overrides Function GetStandardValues(Context As ITypeDescriptorContext) As StandardValuesCollection
            If _standardValuesCache Is Nothing Then
                'We want to sort like the the Save As... dialog does.  In particular, we want this sorting:
                '
                '  Default
                '  Current code page
                '  Unicode encodings (alphabetized)
                '  All others (alphabetized)
                '
                'This corresponds to approximate likeliness of use

                Dim SortedUnicodeEncodings As New SortedList(Of String, String)() 'Key=display name (localized), value = web name
                Dim SortedEncodings As New SortedList(Of String, String)() 'Key=display name (localized), value = web name
                Dim CurrentCodePageEncoding As Encoding = Encoding.Default

                'Find all Unicode and other encodings, and alphabetize them
                For Each Info As EncodingInfo In Encoding.GetEncodings()
                    'Add the short name (web name) of the encoding to our list.  This
                    '  name is not localized, which is what we need, because ConvertFrom
                    '  will be used with this name to get the actual FriendlyEncoding
                    '  class.  The text displayed in the properties windows' dropdown
                    '  will come from calling ConvertToString

                    Dim Key As String = Info.DisplayName

                    Dim Encoding As Encoding = Info.GetEncoding()
                    If IsValidEncoding(Encoding) Then
                        If IsUnicodeEncoding(Encoding) Then
                            If Not SortedUnicodeEncodings.ContainsKey(Key) Then
                                SortedUnicodeEncodings.Add(Info.DisplayName, Info.Name)
                            End If
                        ElseIf Encoding.Equals(CurrentCodePageEncoding) Then
                            'We'll this separately, so skip it for now
                        Else
                            If Not SortedEncodings.ContainsKey(Key) Then
                                SortedEncodings.Add(Info.DisplayName, Info.Name)
                            End If
                        End If
                    Else
                        'If it's not valid (i.e., installed on this system), we don't want it in the list.
                    End If
                Next

                'Build up the full list
                Dim AllEncodings As New List(Of String) From {
                    "", 'default
                    CurrentCodePageEncoding.WebName
                }
                AllEncodings.AddRange(SortedUnicodeEncodings.Values)
                AllEncodings.AddRange(SortedEncodings.Values)

                _standardValuesCache = New StandardValuesCollection(AllEncodings)
            End If

            Return _standardValuesCache
        End Function

        ''' <summary>
        ''' Returns true if the encoding is a Unicode encoding variant
        ''' </summary>
        ''' <param name="Encoding"></param>
        Private Shared Function IsUnicodeEncoding(Encoding As Encoding) As Boolean
            Return Encoding.Equals(Encoding.BigEndianUnicode) _
                OrElse Encoding.Equals(Encoding.Unicode) _
                OrElse Encoding.Equals(Encoding.UTF7) _
                OrElse Encoding.Equals(Encoding.UTF8)
        End Function

        ''' <summary>
        ''' Returns True iff the given Encoding is valid (which means essentially that
        '''   it's currently installed in Windows).  The goal is to get the same list
        '''   of encodings that show up in the code page code editors or save as... list
        '''   in Visual Studio.
        ''' </summary>
        ''' <param name="Encoding">The encoding to check for validity.</param>
        ''' <returns>True if the encoding is valid.</returns>
        Private Shared Function IsValidEncoding(Encoding As Encoding) As Boolean
            If Interop.NativeMethods.IsValidCodePage(CUInt(Encoding.CodePage)) Then
                Return True
            End If

            'A few exceptions that we consider valid
            If IsUnicodeEncoding(Encoding) OrElse Encoding.Equals(Encoding.ASCII) Then
                Return True
            End If

            Return False
        End Function

    End Class

End Namespace
