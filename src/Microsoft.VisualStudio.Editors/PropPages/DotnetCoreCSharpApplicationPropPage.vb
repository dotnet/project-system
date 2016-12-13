' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' C# dotnet core application property page - see comments in proppage.vb: "Application property pages (VB, C#, J#)"
    ''' </summary>
    ''' <remarks></remarks>
    Partial Friend Class DotnetCoreCSharpApplicationPropPage
        Inherits CSharpApplicationPropPage

        Public Sub New()
            MyBase.New()

            InitializeComponent()

            ' Hide the Assembly Information button on the Application property page for dotnet core projects.
            Me.AssemblyInfoButton.Visible = False

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
        End Sub
    End Class
End Namespace
