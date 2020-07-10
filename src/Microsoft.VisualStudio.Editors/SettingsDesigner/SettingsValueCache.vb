' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' Caching implementation for value serializations. 
    ''' </summary>
    Friend Class SettingsValueCache

        ' We cache the values in a hashtable of hashtables:
        ' Type -> Serialized value -> Deserialized value
        Private ReadOnly _cachedSettingValues As New Dictionary(Of Type, Dictionary(Of String, Object))

        Private ReadOnly _culture As Globalization.CultureInfo

        Public Sub New(culture As Globalization.CultureInfo)
            _culture = culture
        End Sub

        ''' <summary>
        ''' Given a type for the setting and a serialized representation, get the deserialized value.
        ''' If the value is not found in the cache, it will be added.
        ''' </summary>
        ''' <param name="settingType"></param>
        ''' <param name="serializedValue"></param>
        Public Function GetValue(settingType As Type, serializedValue As String) As Object
            Dim valueDictionary As Dictionary(Of String, Object) = Nothing
            If Not _cachedSettingValues.TryGetValue(settingType, valueDictionary) Then
                ' Make sure we have a 1st level entry for this type
                valueDictionary = New Dictionary(Of String, Object)
                _cachedSettingValues(settingType) = valueDictionary
            End If

            Dim value As Object = Nothing
            If Not valueDictionary.TryGetValue(serializedValue, value) Then
                ' Make sure we have an entry for the serialized value for this type
                value = SettingsValueSerializer.Deserialize(settingType, serializedValue, _culture)
                valueDictionary(serializedValue) = value
            End If
            Return value
        End Function

    End Class

End Namespace
