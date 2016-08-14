' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Partial Class CSharpApplicationPropPage

        Private _components As System.ComponentModel.IContainer

        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If Not (_components Is Nothing) Then
                    _components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(ApplicationPropPage))
            CType(Me.AppIconImage, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'ApplicationPropPage
            '
            Me.Font = CType(resources.GetObject("$this.Font"), System.Drawing.Font)
            Me.ImeMode = CType(resources.GetObject("$this.ImeMode"), System.Windows.Forms.ImeMode)
            Me.Name = "ApplicationPropPage"
            Me.Size = CType(resources.GetObject("$this.Size"), System.Drawing.Size)
            CType(Me.AppIconImage, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

    End Class

End Namespace
