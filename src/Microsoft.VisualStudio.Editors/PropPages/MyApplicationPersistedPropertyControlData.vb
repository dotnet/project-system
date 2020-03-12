' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.MyApplication

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Friend Class MyApplicationPersistedPropertyControlData
        Inherits PropertyControlData

        Private ReadOnly _myApplicationGetter As GetDelegate

        Public Sub New(id As Integer, name As String, control As Control, flags As ControlDataFlags, myApplicationGetter As GetDelegate)
            MyBase.New(id, name, control, flags Or ControlDataFlags.PersistedInVBMyAppFile)

            _myApplicationGetter = myApplicationGetter
        End Sub

        Public Sub New(id As Integer, name As String, control As Control, flags As ControlDataFlags, assocControls() As Control, myApplicationGetter As GetDelegate)
            MyBase.New(id, name, control, flags Or ControlDataFlags.PersistedInVBMyAppFile, assocControls)

            _myApplicationGetter = myApplicationGetter
        End Sub

        Public Sub New(id As Integer, name As String, control As Control, setter As SetDelegate, getter As GetDelegate, flags As ControlDataFlags, myApplicationGetter As GetDelegate)
            MyBase.New(id, name, control, setter, getter, flags Or ControlDataFlags.PersistedInVBMyAppFile)

            _myApplicationGetter = myApplicationGetter
        End Sub

        Public Overrides Function FilesToCheckOut() As String()
            Try
                Dim value As Object = Nothing
                _myApplicationGetter(FormControl, PropDesc, value)

                Dim MyAppProperties As MyApplicationPropertiesBase = DirectCast(value, MyApplicationPropertiesBase)
                Debug.Assert(MyAppProperties IsNot Nothing)
                If MyAppProperties IsNot Nothing Then
                    Return MyAppProperties.FilesToCheckOut(True)
                End If
            Catch ex As Exception When Common.ReportWithoutCrash(ex, "Unable to retrieve MyApplicationProperties to figure out set of files to check out", NameOf(PropertyControlData))
            End Try
        End Function

    End Class
End Namespace
