' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Collections.Specialized
Imports System.Drawing.Design

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Since the stringcollection isn't associated with the stringcollectioneditor class, we
    ''' invent our own little editor that uses the stringarrayeditor instead.
    ''' </summary>
    Friend Class StringArrayEditorForStringCollections
        Inherits UITypeEditor

        Private ReadOnly _parent As UITypeEditor

        ''' <summary>
        ''' Create a new StringArrayEditorForStringCollections
        ''' </summary>
        Public Sub New()
            _parent = DirectCast(ComponentModel.TypeDescriptor.GetEditor(GetType(String()), GetType(UITypeEditor)), UITypeEditor)
        End Sub

        ''' <summary>
        ''' Edit value by converting it from a string collection, passing that to the string array editor and
        ''' then 
        ''' </summary>
        ''' <param name="context"></param>
        ''' <param name="provider"></param>
        ''' <param name="value"></param>
        Public Overrides Function EditValue(context As ComponentModel.ITypeDescriptorContext, provider As IServiceProvider, value As Object) As Object
            Dim result As Object = _parent.EditValue(context, provider, ConvertToUITypeEditorSource(value))
            Return ConvertToOriginal(result)
        End Function

#Region "Forwarding UITypeEditor methods to our parent UITypeEditor"
        Public Overrides Function GetEditStyle(context As ComponentModel.ITypeDescriptorContext) As UITypeEditorEditStyle
            Return _parent.GetEditStyle(context)
        End Function

        Public Overrides Sub PaintValue(e As PaintValueEventArgs)
            _parent.PaintValue(e)
        End Sub

        Public Overrides ReadOnly Property IsDropDownResizable As Boolean
            Get
                Return _parent.IsDropDownResizable()
            End Get
        End Property

        Public Overrides Function GetPaintValueSupported(context As ComponentModel.ITypeDescriptorContext) As Boolean
            Return _parent.GetPaintValueSupported(context)
        End Function
#End Region
        ''' <summary>
        ''' Convert from StringCollection to string()
        ''' </summary>
        ''' <param name="value"></param>
        Private Shared Function ConvertToUITypeEditorSource(value As Object) As Object
            If value Is Nothing Then
                Return Nothing
            End If

            If value.GetType().Equals(GetType(StringCollection)) Then
                Dim strCol As StringCollection = DirectCast(value, StringCollection)
                Dim result(strCol.Count - 1) As String
                strCol.CopyTo(result, 0)
                Return result
            End If
            Return value
        End Function

        ''' <summary>
        ''' Convert back from String() to StringCollection
        ''' </summary>
        ''' <param name="value"></param>
        Private Shared Function ConvertToOriginal(value As Object) As Object
            If value Is Nothing Then
                Return Nothing
            End If

            If value.GetType().Equals(GetType(String())) Then
                Dim strings() As String = DirectCast(value, String())
                Dim result As New StringCollection
                result.AddRange(strings)
                Return result
            End If
            Return value
        End Function
    End Class
End Namespace
