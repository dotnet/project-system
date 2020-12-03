' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Strict On
Option Explicit On
Option Compare Binary
Imports System.IO

Imports Microsoft.VisualBasic.FileIO

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This class contains routines for reading and writing CSV format (for communicating
    '''   via the clipboard with Excel and other applications)
    ''' </summary>
    Friend NotInheritable Class CsvEncoder

        Public Enum EncodingType
            Csv
            TabDelimited
        End Enum

        ''' <summary>
        ''' Check whether the string containing only one field.
        ''' </summary>
        ''' <param name="Text">The CSV text to encode</param>
        ''' <param name="EncodingType"></param>
        Public Shared Function IsSimpleString(Text As String, EncodingType As EncodingType, ByRef SimpleString As String) As Boolean
            If Text Is Nothing Then
                Text = String.Empty
            End If

            SimpleString = Text

            Using Reader As New TextFieldParser(New StringReader(Text))
                Reader.TextFieldType = FieldType.Delimited
                Reader.TrimWhiteSpace = False
                Reader.HasFieldsEnclosedInQuotes = True

                Select Case EncodingType
                    Case EncodingType.Csv
                        Reader.Delimiters = New String() {","}
                    Case EncodingType.TabDelimited
                        Reader.Delimiters = New String() {vbTab}
                    Case Else
                        Debug.Fail("Unrecognized encodingtype")
                        Return True
                End Select

                Try
                    If Not Reader.EndOfData Then
                        Dim Fields() As String = Reader.ReadFields()
                        If Fields.Length = 1 AndAlso Reader.EndOfData Then
                            SimpleString = Fields(0)
                            Return True
                        End If
                    End If
                Catch ex As MalformedLineException
                    Return True
                End Try
            End Using

            Return False
        End Function

        ''' <summary>
        ''' Decodes the given CSV string into an array of Resources
        ''' </summary>
        ''' <param name="Text">The CSV text to decode</param>
        ''' <param name="View">The ResourceEditorView.  This is used for displaying messageboxes and creating resources.</param>
        ''' <param name="EncodingType"></param>
        Public Shared Function DecodeResources(Text As String, View As ResourceEditorView, EncodingType As EncodingType) As Resource()
            Dim ResourceList As New List(Of Resource)

            If Text Is Nothing Then
                Text = ""
            End If

            'Create our field reader
            Using Reader As New TextFieldParser(New StringReader(Text))
                Reader.TextFieldType = FieldType.Delimited
                Reader.TrimWhiteSpace = False

                Select Case EncodingType
                    Case EncodingType.Csv
                        Reader.Delimiters = New String() {","}
                    Case EncodingType.TabDelimited
                        Reader.Delimiters = New String() {vbTab}
                    Case Else
                        Debug.Fail("Unrecognized encodingtype")
                        Return DecodeResources(Text, View, EncodingType.Csv) 'defensive
                End Select

                'For decoding, we'll allow quotes whether they're there or not
                Reader.HasFieldsEnclosedInQuotes = True

                'And start reading...
                While Not Reader.EndOfData
                    Dim Fields() As String
                    Try
                        '... a line at a time
                        Fields = Reader.ReadFields()
                    Catch ex As MalformedLineException
                        If MsgBox("The following line from the clipboard data appears to be malformed.  Do you want to skip this line and continue pasting?" & vbCrLf & vbCrLf & Reader.ErrorLine, Global.Microsoft.VisualBasic.MsgBoxStyle.YesNo) = Global.Microsoft.VisualBasic.MsgBoxResult.Yes Then
                            'Continue to the next line
                            Continue While
                        Else
                            Return Nothing
                        End If
                    End Try

                    If Fields.Length = 0 Then
                        Continue While
                    End If

                    '1st column: Name
                    Dim Name As String = Fields(0)
                    Dim Value As String = ""
                    Dim Comment As String = ""

                    If Fields.Length = 1 Then
                        If EncodingType = CsvEncoder.EncodingType.TabDelimited Then
                            ' we put the unformatted string in the value field, which will be more useful
                            Value = Name.Trim()
                            Name = ResourceTypeEditors.String.GetSuggestedNamePrefix()
                        End If
                    Else
                        If Fields.Length >= 2 Then
                            '2nd column: Value
                            Value = Fields(1)
                        End If

                        If Fields.Length >= 3 Then
                            '3rd column: Comment
                            Comment = Fields(2)
                        End If
                    End If

                    If Name.Length = 0 Then
                        If Value.Length = 0 AndAlso Comment.Length = 0 Then
                            ' we should skip those empty lines...
                            Continue While
                        End If

                        'If no name, we assign a default Name...
                        Name = ResourceTypeEditors.String.GetSuggestedNamePrefix()
                    Else
                        Name = Name.Trim()
                    End If

                    'Ignore any other fields

                    Dim Resource As New Resource(View.ResourceFile, Name, Comment, Value, View)
                    ResourceList.Add(Resource)
                End While

                Return ResourceList.ToArray()
            End Using
        End Function

        ''' <summary>
        ''' Given a set of Resources, encode all of the string-convertible ones and return a CSV text string containing them
        ''' </summary>
        ''' <param name="Resources">The Resource array to encode</param>
        ''' <remarks>Returns Nothing if there are no resources to encode</remarks>
        Public Shared Function EncodeResources(Resources() As Resource, EncodingType As EncodingType) As String
            If Resources Is Nothing OrElse Resources.Length = 0 Then
                Return Nothing
            End If

            'Alphabetize the resources
            Dim SortedResources As New SortedList(Of String, Resource)
            For Each resource As Resource In Resources
                SortedResources.Add(resource.Name, resource)
            Next

            Dim Results As New System.Text.StringBuilder()
            Dim Delimiter As Char

            Select Case EncodingType
                Case EncodingType.Csv
                    Delimiter = ","c
                Case EncodingType.TabDelimited
                    Delimiter = CChar(vbTab)
                Case Else
                    Debug.Fail("Unrecognized encoding type")
                    Delimiter = ","c
            End Select

            For Each Pair As KeyValuePair(Of String, Resource) In SortedResources
                Dim Resource As Resource = Pair.Value

                If Resource.IsConvertibleFromToString() Then
                    Dim Name As String = Resource.Name
                    Dim Comment As String = Resource.Comment
                    Dim value = Resource.TryGetValue()

                    If value IsNot Nothing Then
                        Dim ValueAsString As String = Resource.GetTypeConverter().ConvertToString(value)

                        Results.Append(EscapeField(Name, EncodingType))
                        Results.Append(Delimiter)
                        Results.Append(EscapeField(ValueAsString, EncodingType))
                        Results.Append(Delimiter)
                        Results.Append(EscapeField(Comment, EncodingType))
                        Results.AppendLine()
                    End If
                End If
            Next

            Return Results.ToString()
        End Function

        ''' <summary>
        ''' Given a string, escape it for use as a field in a CSV line
        ''' </summary>
        ''' <param name="Field">The string field to encode.</param>
        Private Shared Function EscapeField(Field As String, EncodingType As EncodingType) As String
            If Field Is Nothing Then
                Field = ""
            End If

            Select Case EncodingType
                Case EncodingType.Csv
                    'Escape quotes
                    Field = Field.Replace("""", """""")

                    'Place the entire string in quotes
                    Field = """" & Field & """"

                Case EncodingType.TabDelimited
                    'If the field contains tabs, enclose the entire field in quotes
                    ' Tab, Return, NewLine...
                    If Field.IndexOfAny(New Char() {Chr(9), Chr(10), Chr(13)}) > 0 Then
                        Field = """" & Field & """"
                    End If

                Case Else
                    Debug.Fail("Unrecognized encoding type")
                    Return EscapeField(Field, EncodingType.Csv) 'defensive
            End Select

            Return Field
        End Function

    End Class

End Namespace
