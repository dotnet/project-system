' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' an exception is thrown when the customer cancel an operation. 
    '''  We need specialize it, because we don't need pop an error message when this happens
    ''' </summary>
    Friend NotInheritable Class UserCanceledException
        Inherits ApplicationException

        Public Sub New()
            MyBase.New(My.Resources.Microsoft_VisualStudio_Editors_Designer.RSE_Err_UserCancel)
        End Sub

    End Class

End Namespace

