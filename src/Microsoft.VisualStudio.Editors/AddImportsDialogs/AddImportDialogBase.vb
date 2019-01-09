' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

Imports VsErrorHandler = Microsoft.VisualStudio.ErrorHandler
Imports VsShell = Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.AddImports

    <TypeDescriptionProvider(GetType(AbstractControlTypeDescriptionProvider(Of AddImportDialogBase, Form)))>
    Friend MustInherit Class AddImportDialogBase
        Inherits Form

        Private ReadOnly _serviceProvider As IServiceProvider

        Public Sub New(serviceProvider As IServiceProvider)
            If serviceProvider Is Nothing Then
                Throw New ArgumentNullException(NameOf(serviceProvider))
            End If
            _serviceProvider = serviceProvider
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            ' Set the Font according to the system settings.
            Font = DialogFont
            MyBase.OnLoad(e)
        End Sub

        Protected ReadOnly Property DialogFont() As Font
            Get
                Dim hostLocale As VsShell.IUIHostLocale2 = CType(_serviceProvider.GetService(GetType(VsShell.SUIHostLocale)), VsShell.IUIHostLocale2)
                If hostLocale IsNot Nothing Then
                    Dim fonts(1) As VsShell.UIDLGLOGFONT
                    If VsErrorHandler.Succeeded(hostLocale.GetDialogFont(fonts)) Then
                        Return Font.FromLogFont(fonts(0))
                    End If
                End If
                Debug.Fail("Couldn't get a IUIService... cheating instead :)")
                Return DefaultFont
            End Get
        End Property
    End Class
End Namespace