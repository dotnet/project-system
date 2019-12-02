' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
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