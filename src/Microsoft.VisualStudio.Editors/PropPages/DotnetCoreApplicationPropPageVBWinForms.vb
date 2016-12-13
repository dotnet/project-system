' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' The application property page for VB WinForms apps
    ''' - see comments in proppage.vb: "Application property pages (VB, C#, J#)"
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class DotnetCoreApplicationPropPageVBWinForms
        Inherits ApplicationPropPageVBWinForms
        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            ' Hide the Assembly Information dialog on the Application property page for dotnet core project.
            Me.AssemblyInfoButton.Visible = False

            MyBase.PageRequiresScaling = False
        End Sub

    End Class

End Namespace

