' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Friend Class AdvBuildSettingsPropPage

        Private Class ComboItem

            ''' <summary>
            ''' Stores the property value
            ''' </summary>
            Private ReadOnly _value As String

            ''' <summary>
            ''' Stores the display name
            ''' </summary>
            Private ReadOnly _displayName As String

            ''' <summary>
            ''' Constructor that uses the provided value and display name
            ''' </summary>
            Friend Sub New(value As String, displayName As String)

                _value = value
                _displayName = displayName

            End Sub

            ''' <summary>
            ''' Gets the value
            ''' </summary>
            Public ReadOnly Property Value As String
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

        End Class

    End Class

End Namespace
