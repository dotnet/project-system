' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.OptionPages
    <Guid("2E6DB64B-DA09-4B9F-A334-37A86FECDA6A")>
    Friend NotInheritable Class GeneralOptionPage
        Inherits OptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider) As OptionPageControl
            Return New GeneralOptionPageControl(serviceProvider)
        End Function
    End Class
End Namespace
