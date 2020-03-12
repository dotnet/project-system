' Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

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

