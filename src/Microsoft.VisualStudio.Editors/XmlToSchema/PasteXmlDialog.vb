' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

Imports System.ComponentModel.Design
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.XmlToSchema

    <HelpKeyword("vb.XmlToSchemaWizard")>
    Friend NotInheritable Class PasteXmlDialog
        Private _xml As XElement
        Public ReadOnly Property Xml As XElement
            <DebuggerStepThrough>
            Get
                Return _xml
            End Get
        End Property

        Protected Overrides Sub OnClosing(e As System.ComponentModel.CancelEventArgs)
            If Me.DialogResult = DialogResult.OK Then
                Try
                    _xml = XElement.Parse(_xmlTextBox.Text)
                Catch ex As Exception
                    If FilterException(ex) Then
                        ShowWarning(String.Format(My.Resources.Microsoft_VisualStudio_Editors_Designer.XmlToSchema_InvalidXMLFormat, ex.Message))
                        e.Cancel = True
                    Else
                        Throw
                    End If
                End Try
            End If
            MyBase.OnClosing(e)
        End Sub
    End Class
End Namespace
