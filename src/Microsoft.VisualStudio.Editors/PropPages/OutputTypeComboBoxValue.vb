' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports VSLangProj110

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class OutputTypeComboBoxValue

        Private ReadOnly _value As UInteger
        Private ReadOnly _displayName As String

        Public Sub New(value As UInteger)
            _value = value

            Select Case _value
                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_WinExe)
                    _displayName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WindowsApp

                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_Exe)
                    _displayName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_CommandLineApp

                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_Library)
                    _displayName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WindowsClassLib

                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_WinMDObj)
                    _displayName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_WinMDObj

                Case CUInt(prjOutputTypeEx.prjOutputTypeEx_AppContainerExe)
                    _displayName = My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_AppContainerExe

                Case Else
                    _displayName = My.Resources.Microsoft_VisualStudio_Editors_Designer.GetString(My.Resources.Microsoft_VisualStudio_Editors_Designer.PPG_UnknownOutputType, _value)

            End Select
        End Sub

        Public ReadOnly Property Value As UInteger
            Get
                Return _value
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return _displayName
        End Function

    End Class

End Namespace
