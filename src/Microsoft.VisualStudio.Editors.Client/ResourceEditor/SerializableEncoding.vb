' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Option Explicit On
Option Strict On
Option Compare Binary
Imports System.Runtime.Serialization
Imports System.Text

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is a serializable wrapper around the System.Text.Encoding class.  We need this because we expose an "Encoding"
    '''   property on Resource for text files.  The Undo engine requires all the properties to be serializable, and 
    '''   System.Text.Encoding is not.
    ''' </summary>
    <Serializable>
    Friend NotInheritable Class SerializableEncoding
        Implements ISerializable

        'The Encoding instance that we're wrapping
        Private _encoding As Encoding

        'Key for serialization
        Private Const KEY_NAME As String = "Name"

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="Encoding">The encoding to wrap.  Nothing is acceptable (indicates a default value - won't be written out to the resx if Nothing).</param>
        Public Sub New(Encoding As Encoding)
            _encoding = Encoding
        End Sub

        ''' <summary>
        ''' Serialization constructor.
        ''' </summary>
        ''' <param name="info"></param>
        ''' <param name="context"></param>
        Private Sub New(info As SerializationInfo, context As StreamingContext)
            Dim EncodingName As String = info.GetString(KEY_NAME)
            If EncodingName <> "" Then
                _encoding = Encoding.GetEncoding(EncodingName)
            End If
        End Sub

        ''' <summary>
        ''' Returns/sets the encoding wrapped by this class.  Nothing is an okay value (indicates a default encoding).
        ''' </summary>
        Public Property Encoding As Encoding
            Get
                Return _encoding
            End Get
            Set
                _encoding = Encoding
            End Set
        End Property

        ''' <summary>
        ''' Gets the display name (localized) of the encoding.
        ''' </summary>
        Public Function DisplayName() As String
            If _encoding IsNot Nothing Then
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_EncodingDisplayName, _encoding.EncodingName, CStr(_encoding.CodePage))
            Else
                'Default
                Return My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_DefaultEncoding
            End If
        End Function

        ''' <summary>
        ''' Used during serialization.
        ''' </summary>
        ''' <param name="info"></param>
        ''' <param name="context"></param>
        Private Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
            If _encoding IsNot Nothing Then
                info.AddValue(KEY_NAME, _encoding.WebName)
            Else
                info.AddValue(KEY_NAME, "")
            End If
        End Sub
    End Class

End Namespace
